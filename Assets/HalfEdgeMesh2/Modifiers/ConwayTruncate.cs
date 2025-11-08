using Unity.Collections;
using Unity.Mathematics;

namespace HalfEdgeMesh2.Modifiers
{
    // Conway Truncate operator: cuts off vertices
    // Each vertex becomes a small face
    // Each original face becomes larger with truncated corners
    // Each original edge becomes a rectangular face
    public static class ConwayTruncate
    {
        public static MeshData Apply(MeshData input, float ratio, Allocator allocator)
        {
            // Clamp ratio to reasonable range
            ratio = math.clamp(ratio, 0.1f, 0.45f);

            var estimatedVertices = input.halfEdgeCount; // One per half-edge
            var estimatedFaces = input.faceCount + input.vertexCount + (input.halfEdgeCount / 2);
            var estimatedHalfEdges = estimatedVertices * 4;
            var result = new MeshData(estimatedVertices, estimatedHalfEdges, estimatedFaces, allocator);

            // Create vertex for each half-edge, positioned along the edge
            var heToVertex = new NativeArray<int>(input.halfEdgeCount, Allocator.Temp);

            for (var heIdx = 0; heIdx < input.halfEdgeCount; heIdx++)
            {
                var halfEdge = input.halfEdges[heIdx];
                var nextHe = input.halfEdges[halfEdge.next];

                var v0Pos = input.vertices[halfEdge.vertex].position;
                var v1Pos = input.vertices[nextHe.vertex].position;

                // Position vertex along edge, closer to the starting vertex
                var pos = math.lerp(v0Pos, v1Pos, ratio);

                var newVertex = new Vertex(pos, new float2(0.5f, 0.5f));
                heToVertex[heIdx] = result.AddVertex(newVertex);
            }

            // 1. Create truncated faces from original faces
            for (var faceIdx = 0; faceIdx < input.faceCount; faceIdx++)
            {
                var face = input.faces[faceIdx];
                var startHe = face.halfEdge;
                var he = startHe;
                var faceVerts = new NativeList<int>(Allocator.Temp);

                do
                {
                    // Each original edge contributes two vertices to the truncated face
                    faceVerts.Add(heToVertex[he]);

                    // Find previous half-edge in this face
                    var prevHe = he;
                    var searchHe = input.halfEdges[prevHe].next;
                    while (searchHe != he)
                    {
                        prevHe = searchHe;
                        searchHe = input.halfEdges[searchHe].next;
                    }

                    faceVerts.Add(heToVertex[prevHe]);

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

                    // Rectangle connecting the four vertices created along this edge
                    var v0 = heToVertex[heIdx];
                    var v1 = heToVertex[halfEdge.next];
                    var twin = input.halfEdges[twinIdx];
                    var v2 = heToVertex[twinIdx];
                    var v3 = heToVertex[twin.next];

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

            // 3. Create small faces at each original vertex
            var vertexEdges = new NativeList<int>(Allocator.Temp);
            var vertexPositions = new NativeArray<float3>(input.halfEdgeCount, Allocator.Temp);

            for (var i = 0; i < input.halfEdgeCount; i++)
            {
                var he = input.halfEdges[i];
                var nextHe = input.halfEdges[he.next];
                var v0Pos = input.vertices[he.vertex].position;
                var v1Pos = input.vertices[nextHe.vertex].position;
                vertexPositions[i] = math.lerp(v0Pos, v1Pos, ratio);
            }

            for (var vertIdx = 0; vertIdx < input.vertexCount; vertIdx++)
            {
                vertexEdges.Clear();

                // Collect all edges from this vertex
                for (var heIdx = 0; heIdx < input.halfEdgeCount; heIdx++)
                {
                    if (input.halfEdges[heIdx].vertex == vertIdx)
                        vertexEdges.Add(heIdx);
                }

                if (vertexEdges.Length < 3)
                    continue;

                // Sort by angle
                var vertexPos = input.vertices[vertIdx].position;
                var avgNormal = float3.zero;

                for (var i = 0; i < vertexEdges.Length; i++)
                {
                    var he = vertexEdges[i];
                    if (input.halfEdges[he].face != -1)
                    {
                        var face = input.faces[input.halfEdges[he].face];
                        var he0 = face.halfEdge;
                        var he1 = input.halfEdges[he0].next;
                        var he2 = input.halfEdges[he1].next;

                        var v0 = input.vertices[input.halfEdges[he0].vertex].position;
                        var v1 = input.vertices[input.halfEdges[he1].vertex].position;
                        var v2 = input.vertices[input.halfEdges[he2].vertex].position;

                        avgNormal += math.normalize(math.cross(v1 - v0, v2 - v0));
                    }
                }
                avgNormal = math.normalize(avgNormal);

                var sortedEdges = new NativeList<int>(vertexEdges.Length, Allocator.Temp);
                for (var i = 0; i < vertexEdges.Length; i++)
                    sortedEdges.Add(vertexEdges[i]);

                var refDir = math.normalize(vertexPositions[sortedEdges[0]] - vertexPos);
                var tangent = math.normalize(math.cross(avgNormal, refDir));

                for (var i = 0; i < sortedEdges.Length - 1; i++)
                {
                    for (var j = i + 1; j < sortedEdges.Length; j++)
                    {
                        var dir_i = math.normalize(vertexPositions[sortedEdges[i]] - vertexPos);
                        var dir_j = math.normalize(vertexPositions[sortedEdges[j]] - vertexPos);

                        var angle_i = math.atan2(math.dot(tangent, dir_i), math.dot(refDir, dir_i));
                        var angle_j = math.atan2(math.dot(tangent, dir_j), math.dot(refDir, dir_j));

                        if (angle_i > angle_j)
                        {
                            var temp = sortedEdges[i];
                            sortedEdges[i] = sortedEdges[j];
                            sortedEdges[j] = temp;
                        }
                    }
                }

                // Create vertex face (reversed)
                var faceStartHe = result.halfEdgeCount;
                var newFace = new Face(faceStartHe);
                var newFaceIdx = result.AddFace(newFace);

                for (var i = 0; i < sortedEdges.Length; i++)
                {
                    var reversedIdx = sortedEdges.Length - 1 - i;
                    var edgeVertexIdx = heToVertex[sortedEdges[reversedIdx]];
                    var newHe = new HalfEdge(
                        faceStartHe + ((i + 1) % sortedEdges.Length),
                        -1,
                        edgeVertexIdx,
                        newFaceIdx
                    );
                    result.AddHalfEdge(newHe);
                }

                sortedEdges.Dispose();
            }

            heToVertex.Dispose();
            vertexEdges.Dispose();
            vertexPositions.Dispose();

            return result;
        }
    }
}
