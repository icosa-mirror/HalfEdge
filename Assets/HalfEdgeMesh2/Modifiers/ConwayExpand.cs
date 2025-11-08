using Unity.Collections;
using Unity.Mathematics;

namespace HalfEdgeMesh2.Modifiers
{
    // Conway Expand operator: moves faces apart and fills gaps
    // Creates vertices at edge midpoints
    // Each original face becomes a face of edge midpoints
    // Each original edge becomes a quadrilateral
    // Each original vertex becomes a polygon
    public static class ConwayExpand
    {
        public static MeshData Apply(MeshData input, Allocator allocator)
        {
            var estimatedVertices = input.halfEdgeCount; // One per half-edge
            var estimatedFaces = input.faceCount + (input.halfEdgeCount / 2) + input.vertexCount;
            var estimatedHalfEdges = estimatedFaces * 4; // Rough estimate
            var result = new MeshData(estimatedVertices, estimatedHalfEdges, estimatedFaces, allocator);

            // Create a vertex for each half-edge at its midpoint
            var heToVertex = new NativeArray<int>(input.halfEdgeCount, Allocator.Temp);

            for (var heIdx = 0; heIdx < input.halfEdgeCount; heIdx++)
            {
                var halfEdge = input.halfEdges[heIdx];
                var nextHe = input.halfEdges[halfEdge.next];

                var v0Pos = input.vertices[halfEdge.vertex].position;
                var v1Pos = input.vertices[nextHe.vertex].position;
                var midPos = (v0Pos + v1Pos) * 0.5f;

                var midVertex = new Vertex(midPos, new float2(0.5f, 0.5f));
                heToVertex[heIdx] = result.AddVertex(midVertex);
            }

            // 1. Recreate each original face using edge midpoint vertices
            for (var faceIdx = 0; faceIdx < input.faceCount; faceIdx++)
            {
                var face = input.faces[faceIdx];
                var startHe = face.halfEdge;
                var he = startHe;
                var faceVerts = new NativeList<int>(Allocator.Temp);

                do
                {
                    faceVerts.Add(heToVertex[he]);
                    he = input.halfEdges[he].next;
                } while (he != startHe);

                // Create face from these vertices
                var faceStartHe = result.halfEdgeCount;
                var newFace = new Face(faceStartHe);
                var newFaceIdx = result.AddFace(newFace);

                for (var i = 0; i < faceVerts.Length; i++)
                {
                    var vIdx = faceVerts[i];
                    var nextVIdx = faceVerts[(i + 1) % faceVerts.Length];

                    var newHe = new HalfEdge(
                        faceStartHe + ((i + 1) % faceVerts.Length),
                        -1,
                        vIdx,
                        newFaceIdx
                    );
                    result.AddHalfEdge(newHe);
                }

                faceVerts.Dispose();
            }

            // 2. Create quadrilateral faces for each original edge
            for (var heIdx = 0; heIdx < input.halfEdgeCount; heIdx++)
            {
                var halfEdge = input.halfEdges[heIdx];
                if (halfEdge.twin == -1 || heIdx < halfEdge.twin)
                {
                    // Only process each edge once
                    var twinIdx = halfEdge.twin;

                    if (twinIdx == -1)
                        continue; // Skip boundary edges for now

                    // Quad vertices: heToVertex[heIdx], heToVertex[next], heToVertex[twin.next], heToVertex[twin]
                    var v0 = heToVertex[heIdx];
                    var v1 = heToVertex[halfEdge.next];
                    var twin = input.halfEdges[twinIdx];
                    var v2 = heToVertex[twin.next];
                    var v3 = heToVertex[twinIdx];

                    var faceStartHe = result.halfEdgeCount;
                    var newFace = new Face(faceStartHe);
                    var newFaceIdx = result.AddFace(newFace);

                    var he0 = new HalfEdge(faceStartHe + 1, -1, v0, newFaceIdx);
                    var he1 = new HalfEdge(faceStartHe + 2, -1, v1, newFaceIdx);
                    var he2 = new HalfEdge(faceStartHe + 3, -1, v2, newFaceIdx);
                    var he3 = new HalfEdge(faceStartHe + 0, -1, v3, newFaceIdx);

                    result.AddHalfEdge(he0);
                    result.AddHalfEdge(he1);
                    result.AddHalfEdge(he2);
                    result.AddHalfEdge(he3);
                }
            }

            // 3. Create polygon faces for each original vertex
            for (var vertIdx = 0; vertIdx < input.vertexCount; vertIdx++)
            {
                var vertexFaceVerts = new NativeList<int>(Allocator.Temp);

                // Find a half-edge originating from this vertex
                var startHe = -1;
                for (var heIdx = 0; heIdx < input.halfEdgeCount; heIdx++)
                {
                    if (input.halfEdges[heIdx].vertex == vertIdx)
                    {
                        if (startHe == -1 || input.halfEdges[heIdx].twin != -1)
                        {
                            startHe = heIdx;
                            if (input.halfEdges[heIdx].twin != -1)
                                break;
                        }
                    }
                }

                if (startHe == -1)
                    continue;

                // Traverse around vertex collecting the midpoint vertices
                var he = startHe;
                var iterations = 0;
                do
                {
                    vertexFaceVerts.Add(heToVertex[he]);

                    var halfEdge = input.halfEdges[he];
                    if (halfEdge.twin != -1)
                        he = input.halfEdges[halfEdge.twin].next;
                    else
                        break;

                    iterations++;
                } while (he != startHe && iterations < 100);

                if (vertexFaceVerts.Length >= 3)
                {
                    // Create face
                    var faceStartHe = result.halfEdgeCount;
                    var newFace = new Face(faceStartHe);
                    var newFaceIdx = result.AddFace(newFace);

                    // Reverse order for outward normals
                    for (var i = 0; i < vertexFaceVerts.Length; i++)
                    {
                        var reversedIdx = vertexFaceVerts.Length - 1 - i;
                        var vIdx = vertexFaceVerts[reversedIdx];

                        var newHe = new HalfEdge(
                            faceStartHe + ((i + 1) % vertexFaceVerts.Length),
                            -1,
                            vIdx,
                            newFaceIdx
                        );
                        result.AddHalfEdge(newHe);
                    }
                }

                vertexFaceVerts.Dispose();
            }

            heToVertex.Dispose();
            return result;
        }
    }
}
