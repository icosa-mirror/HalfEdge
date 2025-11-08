using Unity.Collections;
using Unity.Mathematics;

namespace HalfEdgeMesh2.Modifiers
{
    // Conway Truncate operator: cuts off vertices
    // Creates small faces at each original vertex
    // Original faces become larger with truncated corners
    // Creates rectangular faces along edges
    public static class ConwayTruncate
    {
        public static MeshData Apply(MeshData input, float ratio, Allocator allocator)
        {
            // Clamp ratio to avoid degenerate cases
            ratio = math.clamp(ratio, 0.1f, 0.45f);

            var estimatedVertices = input.halfEdgeCount * 2;
            var estimatedFaces = input.faceCount + input.vertexCount + (input.halfEdgeCount / 2);
            var estimatedHalfEdges = estimatedVertices * 4;
            var result = new MeshData(estimatedVertices, estimatedHalfEdges, estimatedFaces, allocator);

            // Create two vertices per half-edge (near start and near end)
            var heToVertexStart = new NativeArray<int>(input.halfEdgeCount, Allocator.Temp);
            var heToVertexEnd = new NativeArray<int>(input.halfEdgeCount, Allocator.Temp);

            for (var heIdx = 0; heIdx < input.halfEdgeCount; heIdx++)
            {
                var halfEdge = input.halfEdges[heIdx];
                var nextHe = input.halfEdges[halfEdge.next];

                var v0Pos = input.vertices[halfEdge.vertex].position;
                var v1Pos = input.vertices[nextHe.vertex].position;

                // Create vertex near start (moved toward midpoint by ratio)
                var nearStart = math.lerp(v0Pos, v1Pos, ratio);
                var nearEnd = math.lerp(v0Pos, v1Pos, 1.0f - ratio);

                heToVertexStart[heIdx] = result.AddVertex(new Vertex(nearStart, new float2(0.5f, 0.5f)));
                heToVertexEnd[heIdx] = result.AddVertex(new Vertex(nearEnd, new float2(0.5f, 0.5f)));
            }

            // 1. Recreate original faces with expanded vertex count (truncated corners)
            for (var faceIdx = 0; faceIdx < input.faceCount; faceIdx++)
            {
                var face = input.faces[faceIdx];
                var startHe = face.halfEdge;
                var he = startHe;
                var faceVerts = new NativeList<int>(Allocator.Temp);

                do
                {
                    // Add both vertices created for this half-edge
                    faceVerts.Add(heToVertexStart[he]);
                    faceVerts.Add(heToVertexEnd[he]);
                    he = input.halfEdges[he].next;
                } while (he != startHe);

                if (faceVerts.Length >= 3)
                {
                    var faceStartHe = result.halfEdgeCount;
                    var newFace = new Face(faceStartHe);
                    var newFaceIdx = result.AddFace(newFace);

                    for (var i = 0; i < faceVerts.Length; i++)
                    {
                        var vIdx = faceVerts[i];
                        var newHe = new HalfEdge(
                            faceStartHe + ((i + 1) % faceVerts.Length),
                            -1,
                            vIdx,
                            newFaceIdx
                        );
                        result.AddHalfEdge(newHe);
                    }
                }

                faceVerts.Dispose();
            }

            // 2. Create rectangular faces for each original edge
            for (var heIdx = 0; heIdx < input.halfEdgeCount; heIdx++)
            {
                var halfEdge = input.halfEdges[heIdx];
                if (halfEdge.twin == -1 || heIdx < halfEdge.twin)
                {
                    var twinIdx = halfEdge.twin;
                    if (twinIdx == -1)
                        continue;

                    // Rectangle connecting the four truncation vertices along this edge
                    var v0 = heToVertexEnd[heIdx];
                    var twin = input.halfEdges[twinIdx];
                    var v1 = heToVertexStart[twin.next];
                    var v2 = heToVertexEnd[twin.next];
                    var v3 = heToVertexStart[halfEdge.next];

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

            // 3. Create small faces at each original vertex (where vertex was cut off)
            for (var vertIdx = 0; vertIdx < input.vertexCount; vertIdx++)
            {
                var vertexFaceVerts = new NativeList<int>(Allocator.Temp);

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

                var he = startHe;
                var iterations = 0;
                do
                {
                    // Use the "start" vertex of this outgoing half-edge
                    vertexFaceVerts.Add(heToVertexStart[he]);

                    var halfEdge = input.halfEdges[he];
                    if (halfEdge.twin != -1)
                        he = input.halfEdges[halfEdge.twin].next;
                    else
                        break;

                    iterations++;
                } while (he != startHe && iterations < 100);

                if (vertexFaceVerts.Length >= 3)
                {
                    var faceStartHe = result.halfEdgeCount;
                    var newFace = new Face(faceStartHe);
                    var newFaceIdx = result.AddFace(newFace);

                    // Reverse for outward normals
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

            heToVertexStart.Dispose();
            heToVertexEnd.Dispose();
            return result;
        }
    }
}
