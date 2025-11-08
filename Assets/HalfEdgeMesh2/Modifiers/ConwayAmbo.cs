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
            var estimatedHalfEdges = estimatedFaces * 4; // Rough estimate

            var result = new MeshData(estimatedVertices, estimatedHalfEdges, estimatedFaces, allocator);

            // Map half-edges to new vertex indices
            var heToVertex = new NativeArray<int>(input.halfEdgeCount, Allocator.Temp);
            for (var i = 0; i < input.halfEdgeCount; i++)
                heToVertex[i] = -1;

            // Step 1: Create vertices at edge midpoints
            for (var i = 0; i < input.halfEdgeCount; i++)
            {
                if (heToVertex[i] != -1)
                    continue;

                var he = input.halfEdges[i];
                var nextHe = input.halfEdges[he.next];

                var v1Pos = input.vertices[he.vertex].position;
                var v2Pos = input.vertices[nextHe.vertex].position;
                var midpoint = (v1Pos + v2Pos) * 0.5f;

                var newVertex = new Vertex(midpoint, float2.zero);
                var newVertexIdx = result.AddVertex(newVertex);

                // Map both this half-edge and its twin
                heToVertex[i] = newVertexIdx;
                if (he.twin != -1)
                    heToVertex[he.twin] = newVertexIdx;
            }

            // Step 2: Create faces from original faces
            // Each original face becomes a face connecting edge midpoints
            for (var faceIdx = 0; faceIdx < input.faceCount; faceIdx++)
            {
                var face = input.faces[faceIdx];
                var startHe = face.halfEdge;
                var he = startHe;
                var faceEdges = new NativeList<int>(Allocator.Temp);

                // Collect half-edges around face
                do
                {
                    faceEdges.Add(he);
                    he = input.halfEdges[he].next;
                } while (he != startHe && faceEdges.Length < 100);

                if (faceEdges.Length < 3)
                {
                    faceEdges.Dispose();
                    continue;
                }

                // Create new face from edge midpoints
                var faceStartHe = result.halfEdgeCount;
                var newFace = new Face(faceStartHe);
                var newFaceIdx = result.AddFace(newFace);

                for (var i = 0; i < faceEdges.Length; i++)
                {
                    var edgeVertexIdx = heToVertex[faceEdges[i]];
                    var nextIdx = (i + 1) % faceEdges.Length;

                    var newHe = new HalfEdge(
                        next: faceStartHe + nextIdx,
                        twin: -1,
                        vertex: edgeVertexIdx,
                        face: newFaceIdx
                    );
                    result.AddHalfEdge(newHe);

                    // Update vertex half-edge reference
                    var v = result.vertices[edgeVertexIdx];
                    if (v.halfEdge == -1)
                    {
                        v.halfEdge = faceStartHe + i;
                        result.vertices[edgeVertexIdx] = v;
                    }
                }

                faceEdges.Dispose();
            }

            // Step 3: Create faces from original vertices
            // Each original vertex becomes a face connecting edge midpoints of incident edges
            var vertexEdges = new NativeList<int>(Allocator.Temp);

            for (var vertIdx = 0; vertIdx < input.vertexCount; vertIdx++)
            {
                vertexEdges.Clear();

                var vertex = input.vertices[vertIdx];
                if (vertex.halfEdge == -1)
                    continue;

                var startHe = vertex.halfEdge;
                var he = startHe;
                var iterations = 0;

                // Collect all half-edges around this vertex
                do
                {
                    // The edge going OUT from this vertex
                    var outEdge = he;
                    var inEdge = input.halfEdges[outEdge].next;

                    // We want the vertex at the midpoint of the INcoming edge
                    vertexEdges.Add(inEdge);

                    // Move to next half-edge around vertex (via twin and next)
                    var current = input.halfEdges[he];
                    if (current.twin != -1)
                    {
                        he = input.halfEdges[current.twin].next;
                    }
                    else
                    {
                        break; // Boundary edge
                    }

                    iterations++;
                } while (he != startHe && iterations < 100);

                if (vertexEdges.Length < 3)
                    continue;

                // Create face from edge midpoints
                var faceStartHe = result.halfEdgeCount;
                var newFace = new Face(faceStartHe);
                var newFaceIdx = result.AddFace(newFace);

                for (var i = 0; i < vertexEdges.Length; i++)
                {
                    var edgeVertexIdx = heToVertex[vertexEdges[i]];
                    var nextIdx = (i + 1) % vertexEdges.Length;

                    var newHe = new HalfEdge(
                        next: faceStartHe + nextIdx,
                        twin: -1,
                        vertex: edgeVertexIdx,
                        face: newFaceIdx
                    );
                    result.AddHalfEdge(newHe);
                }

                // Update vertex half-edge references
                for (var i = 0; i < vertexEdges.Length; i++)
                {
                    var edgeVertexIdx = heToVertex[vertexEdges[i]];
                    var v = result.vertices[edgeVertexIdx];
                    if (v.halfEdge == -1)
                    {
                        v.halfEdge = faceStartHe + i;
                        result.vertices[edgeVertexIdx] = v;
                    }
                }
            }

            heToVertex.Dispose();
            vertexEdges.Dispose();

            return result;
        }
    }
}
