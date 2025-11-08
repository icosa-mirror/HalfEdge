using Unity.Collections;
using Unity.Mathematics;

namespace HalfEdgeMesh2.Modifiers
{
    // Conway Truncate operator: cuts off vertices
    // Creates one vertex per half-edge near each original vertex
    // Each vertex becomes a small face
    // Each original face becomes larger (truncated corners)
    // No edge faces (those come from bevel = truncate+ambo)
    public static class ConwayTruncate
    {
        public static MeshData Apply(MeshData input, float ratio, Allocator allocator)
        {
            // Clamp ratio to avoid degenerate cases
            ratio = math.clamp(ratio, 0.1f, 0.45f);

            var estimatedVertices = input.halfEdgeCount;
            var estimatedFaces = input.faceCount + input.vertexCount;
            var estimatedHalfEdges = estimatedVertices * 4;
            var result = new MeshData(estimatedVertices, estimatedHalfEdges, estimatedFaces, allocator);

            // Create ONE vertex per half-edge, positioned near the vertex
            var heToVertex = new NativeArray<int>(input.halfEdgeCount, Allocator.Temp);

            for (var heIdx = 0; heIdx < input.halfEdgeCount; heIdx++)
            {
                var halfEdge = input.halfEdges[heIdx];
                var nextHe = input.halfEdges[halfEdge.next];

                var v0Pos = input.vertices[halfEdge.vertex].position;
                var v1Pos = input.vertices[nextHe.vertex].position;

                // Create vertex along edge, moved from v0 toward v1 by ratio
                var pos = math.lerp(v0Pos, v1Pos, ratio);

                heToVertex[heIdx] = result.AddVertex(new Vertex(pos, new float2(0.5f, 0.5f)));
            }

            // 1. Recreate original faces with truncated corners
            for (var faceIdx = 0; faceIdx < input.faceCount; faceIdx++)
            {
                var face = input.faces[faceIdx];
                var startHe = face.halfEdge;
                var he = startHe;
                var faceVerts = new NativeList<int>(Allocator.Temp);

                do
                {
                    // Add the one vertex created for this half-edge
                    faceVerts.Add(heToVertex[he]);
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

            // 2. Create small faces at each original vertex (where vertex was cut off)
            for (var vertIdx = 0; vertIdx < input.vertexCount; vertIdx++)
            {
                var vertexEdges = new NativeList<int>(Allocator.Temp);

                // Find all half-edges originating from this vertex
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
                    vertexEdges.Add(he);

                    var halfEdge = input.halfEdges[he];
                    if (halfEdge.twin != -1)
                        he = input.halfEdges[halfEdge.twin].next;
                    else
                        break;

                    iterations++;
                } while (he != startHe && iterations < 100);

                if (vertexEdges.Length >= 3)
                {
                    var faceStartHe = result.halfEdgeCount;
                    var newFace = new Face(faceStartHe);
                    var newFaceIdx = result.AddFace(newFace);

                    // Reverse for outward normals
                    for (var i = 0; i < vertexEdges.Length; i++)
                    {
                        var reversedIdx = vertexEdges.Length - 1 - i;
                        var vIdx = heToVertex[vertexEdges[reversedIdx]];
                        var newHe = new HalfEdge(
                            faceStartHe + ((i + 1) % vertexEdges.Length),
                            -1,
                            vIdx,
                            newFaceIdx
                        );
                        result.AddHalfEdge(newHe);
                    }
                }

                vertexEdges.Dispose();
            }

            heToVertex.Dispose();
            return result;
        }
    }
}
