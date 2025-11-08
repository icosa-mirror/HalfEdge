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
            // Collect all edges incident to each vertex by scanning all half-edges
            var vertexEdges = new NativeList<int>(Allocator.Temp);
            var edgeMidpoints = new NativeArray<float3>(input.halfEdgeCount, Allocator.Temp);

            // Pre-calculate edge midpoint positions for sorting
            for (var i = 0; i < input.halfEdgeCount; i++)
            {
                var he = input.halfEdges[i];
                var nextHe = input.halfEdges[he.next];
                var v1Pos = input.vertices[he.vertex].position;
                var v2Pos = input.vertices[nextHe.vertex].position;
                edgeMidpoints[i] = (v1Pos + v2Pos) * 0.5f;
            }

            for (var vertIdx = 0; vertIdx < input.vertexCount; vertIdx++)
            {
                vertexEdges.Clear();

                // Scan ALL half-edges to find ones originating from this vertex
                for (var heIdx = 0; heIdx < input.halfEdgeCount; heIdx++)
                {
                    if (input.halfEdges[heIdx].vertex == vertIdx)
                    {
                        // Check if we already have this edge (avoid duplicates from twins)
                        var alreadyHave = false;
                        for (var i = 0; i < vertexEdges.Length; i++)
                        {
                            if (vertexEdges[i] == heIdx)
                            {
                                alreadyHave = true;
                                break;
                            }
                        }
                        if (!alreadyHave)
                            vertexEdges.Add(heIdx);
                    }
                }

                if (vertexEdges.Length < 3)
                    continue;

                // Sort edges by angle around vertex
                var vertexPos = input.vertices[vertIdx].position;

                // Calculate average normal at vertex
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

                        var faceNormal = math.normalize(math.cross(v1 - v0, v2 - v0));
                        avgNormal += faceNormal;
                    }
                }
                avgNormal = math.normalize(avgNormal);

                // Sort edges by angle
                var sortedEdges = new NativeList<int>(vertexEdges.Length, Allocator.Temp);
                for (var i = 0; i < vertexEdges.Length; i++)
                    sortedEdges.Add(vertexEdges[i]);

                var refDir = math.normalize(edgeMidpoints[sortedEdges[0]] - vertexPos);
                var tangent = math.normalize(math.cross(avgNormal, refDir));

                for (var i = 0; i < sortedEdges.Length - 1; i++)
                {
                    for (var j = i + 1; j < sortedEdges.Length; j++)
                    {
                        var dir_i = math.normalize(edgeMidpoints[sortedEdges[i]] - vertexPos);
                        var dir_j = math.normalize(edgeMidpoints[sortedEdges[j]] - vertexPos);

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

                // Create face from edge midpoints (reverse order for correct winding)
                var faceStartHe = result.halfEdgeCount;
                var newFace = new Face(faceStartHe);
                var newFaceIdx = result.AddFace(newFace);

                // Reverse the edge order for correct outward-facing normals
                for (var i = 0; i < sortedEdges.Length; i++)
                {
                    var reversedIdx = sortedEdges.Length - 1 - i;
                    var edgeVertexIdx = heToVertex[sortedEdges[reversedIdx]];
                    var nextIdx = (i + 1) % sortedEdges.Length;

                    var newHe = new HalfEdge(
                        next: faceStartHe + nextIdx,
                        twin: -1,
                        vertex: edgeVertexIdx,
                        face: newFaceIdx
                    );
                    result.AddHalfEdge(newHe);
                }

                // Update vertex half-edge references
                for (var i = 0; i < sortedEdges.Length; i++)
                {
                    var reversedIdx = sortedEdges.Length - 1 - i;
                    var edgeVertexIdx = heToVertex[sortedEdges[reversedIdx]];
                    var v = result.vertices[edgeVertexIdx];
                    if (v.halfEdge == -1)
                    {
                        v.halfEdge = faceStartHe + i;
                        result.vertices[edgeVertexIdx] = v;
                    }
                }

                sortedEdges.Dispose();
            }

            edgeMidpoints.Dispose();

            heToVertex.Dispose();
            vertexEdges.Dispose();

            return result;
        }
    }
}
