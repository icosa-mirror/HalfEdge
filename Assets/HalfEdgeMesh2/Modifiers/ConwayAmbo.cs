using Unity.Collections;
using Unity.Mathematics;

namespace HalfEdgeMesh2.Modifiers
{
    // Conway Ambo operator: places a vertex at the midpoint of each edge
    // Creates new faces by connecting edge-midpoint vertices
    // Results in a mesh where each original face and vertex becomes a face
    public static class ConwayAmbo
    {
        public static MeshData Apply(MeshData input, Allocator allocator)
        {
            // Count unique edges
            var edgeCount = 0;
            for (var i = 0; i < input.halfEdgeCount; i++)
            {
                var he = input.halfEdges[i];
                if (he.twin == -1 || i < he.twin)
                    edgeCount++;
            }

            // Ambo creates: vertices = edges, faces = old faces + old vertices
            var estimatedVertices = edgeCount;
            var estimatedFaces = input.faceCount + input.vertexCount;
            var estimatedHalfEdges = estimatedFaces * 4;

            var result = new MeshData(estimatedVertices, estimatedHalfEdges, estimatedFaces, allocator);

            // Map half-edges to new vertex indices
            var heToVertex = new NativeArray<int>(input.halfEdgeCount, Allocator.Temp);
            for (var i = 0; i < input.halfEdgeCount; i++)
                heToVertex[i] = -1;

            // Create vertices at edge midpoints (twins share same vertex)
            for (var i = 0; i < input.halfEdgeCount; i++)
            {
                if (heToVertex[i] != -1)
                    continue;

                var he = input.halfEdges[i];
                var nextHe = input.halfEdges[he.next];

                var v1Pos = input.vertices[he.vertex].position;
                var v2Pos = input.vertices[nextHe.vertex].position;
                var midpoint = (v1Pos + v2Pos) * 0.5f;

                var newVertexIdx = result.AddVertex(new Vertex(midpoint, float2.zero));

                // Map both this half-edge and its twin to the same vertex
                heToVertex[i] = newVertexIdx;
                if (he.twin != -1)
                    heToVertex[he.twin] = newVertexIdx;
            }

            // Faces to faces: each original face becomes a new face
            for (var faceIdx = 0; faceIdx < input.faceCount; faceIdx++)
            {
                var face = input.faces[faceIdx];
                var startHe = face.halfEdge;
                var he = startHe;
                var vertices = new NativeList<int>(Allocator.Temp);

                do
                {
                    vertices.Add(heToVertex[he]);
                    he = input.halfEdges[he].next;
                } while (he != startHe && vertices.Length < 100);

                if (vertices.Length >= 3)
                {
                    var faceStartHe = result.halfEdgeCount;
                    var newFaceIdx = result.AddFace(new Face(faceStartHe));

                    for (var i = 0; i < vertices.Length; i++)
                    {
                        var newHe = new HalfEdge(
                            next: faceStartHe + ((i + 1) % vertices.Length),
                            twin: -1,
                            vertex: vertices[i],
                            face: newFaceIdx
                        );
                        result.AddHalfEdge(newHe);

                        var v = result.vertices[vertices[i]];
                        if (v.halfEdge == -1)
                        {
                            v.halfEdge = faceStartHe + i;
                            result.vertices[vertices[i]] = v;
                        }
                    }
                }

                vertices.Dispose();
            }

            // Vertices to faces: each original vertex becomes a new face
            for (var vertIdx = 0; vertIdx < input.vertexCount; vertIdx++)
            {
                // Find a half-edge starting from this vertex
                var startHe = -1;
                for (var i = 0; i < input.halfEdgeCount; i++)
                {
                    if (input.halfEdges[i].vertex == vertIdx)
                    {
                        startHe = i;
                        break;
                    }
                }

                if (startHe == -1)
                    continue;

                var vertices = new NativeList<int>(Allocator.Temp);
                var he = startHe;

                // Walk around the vertex via twin->next
                do
                {
                    vertices.Add(heToVertex[he]);

                    var twin = input.halfEdges[he].twin;
                    if (twin == -1)
                        break; // Boundary edge

                    he = input.halfEdges[twin].next;

                    if (vertices.Length >= 100)
                        break; // Safety limit
                } while (he != startHe);

                if (vertices.Length >= 3)
                {
                    var faceStartHe = result.halfEdgeCount;
                    var newFaceIdx = result.AddFace(new Face(faceStartHe));

                    // Reverse order for correct winding
                    for (var i = 0; i < vertices.Length; i++)
                    {
                        var reversedIdx = vertices.Length - 1 - i;
                        var newHe = new HalfEdge(
                            next: faceStartHe + ((i + 1) % vertices.Length),
                            twin: -1,
                            vertex: vertices[reversedIdx],
                            face: newFaceIdx
                        );
                        result.AddHalfEdge(newHe);

                        var v = result.vertices[vertices[reversedIdx]];
                        if (v.halfEdge == -1)
                        {
                            v.halfEdge = faceStartHe + i;
                            result.vertices[vertices[reversedIdx]] = v;
                        }
                    }
                }

                vertices.Dispose();
            }

            heToVertex.Dispose();

            return result;
        }
    }
}
