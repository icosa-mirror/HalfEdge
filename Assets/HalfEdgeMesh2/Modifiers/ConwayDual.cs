using Unity.Collections;
using Unity.Mathematics;

namespace HalfEdgeMesh2.Modifiers
{
    // Conway Dual operator: swaps faces and vertices
    // Each face becomes a vertex at its centroid
    // Each vertex becomes a face connecting the dual vertices of its adjacent faces
    public static class ConwayDual
    {
        public static MeshData Apply(MeshData input, Allocator allocator)
        {
            // Count unique edges (half-edges / 2) to estimate new face count
            var edgeCount = 0;
            for (var i = 0; i < input.halfEdgeCount; i++)
            {
                var he = input.halfEdges[i];
                if (he.twin == -1 || i < he.twin)
                    edgeCount++;
            }

            // Dual will have: vertices = old faces, faces = old vertices
            var newVertexCount = input.faceCount;
            var newFaceCount = input.vertexCount;
            var estimatedHalfEdges = input.halfEdgeCount; // Roughly same

            var result = new MeshData(newVertexCount, estimatedHalfEdges, newFaceCount, allocator);

            // Step 1: Create new vertices at face centroids
            var faceCentroids = new NativeArray<float3>(input.faceCount, Allocator.Temp);
            var faceToVertex = new NativeArray<int>(input.faceCount, Allocator.Temp);

            for (var faceIdx = 0; faceIdx < input.faceCount; faceIdx++)
            {
                var face = input.faces[faceIdx];
                var startHe = face.halfEdge;
                var he = startHe;
                var centroid = float3.zero;
                var count = 0;

                // Calculate centroid
                do
                {
                    var halfEdge = input.halfEdges[he];
                    var vertex = input.vertices[halfEdge.vertex];
                    centroid += vertex.position;
                    count++;
                    he = halfEdge.next;
                } while (he != startHe && count < 100);

                centroid /= count;
                faceCentroids[faceIdx] = centroid;

                // Create dual vertex
                var newVertex = new Vertex(centroid, float2.zero);
                faceToVertex[faceIdx] = result.AddVertex(newVertex);
            }

            // Step 2: Create new faces (one per original vertex)
            // Collect all faces adjacent to each vertex by scanning all half-edges
            var vertexFaces = new NativeList<int>(Allocator.Temp);

            for (var vertIdx = 0; vertIdx < input.vertexCount; vertIdx++)
            {
                vertexFaces.Clear();

                // Scan ALL half-edges to find ones originating from this vertex
                for (var heIdx = 0; heIdx < input.halfEdgeCount; heIdx++)
                {
                    var halfEdge = input.halfEdges[heIdx];
                    if (halfEdge.vertex == vertIdx && halfEdge.face != -1)
                    {
                        // Check if we already have this face
                        var alreadyHave = false;
                        for (var i = 0; i < vertexFaces.Length; i++)
                        {
                            if (vertexFaces[i] == halfEdge.face)
                            {
                                alreadyHave = true;
                                break;
                            }
                        }
                        if (!alreadyHave)
                            vertexFaces.Add(halfEdge.face);
                    }
                }

                if (vertexFaces.Length < 3)
                    continue;

                // Now we need to order the faces correctly by following the circular pattern
                // Use the original vertex position and face centroids to sort by angle
                var vertexPos = input.vertices[vertIdx].position;

                // Calculate average normal at vertex
                var avgNormal = float3.zero;
                for (var i = 0; i < vertexFaces.Length; i++)
                {
                    var faceIdx = vertexFaces[i];
                    var face = input.faces[faceIdx];

                    // Calculate face normal using first 3 vertices
                    var he0 = face.halfEdge;
                    var he1 = input.halfEdges[he0].next;
                    var he2 = input.halfEdges[he1].next;

                    var v0 = input.vertices[input.halfEdges[he0].vertex].position;
                    var v1 = input.vertices[input.halfEdges[he1].vertex].position;
                    var v2 = input.vertices[input.halfEdges[he2].vertex].position;

                    var faceNormal = math.normalize(math.cross(v1 - v0, v2 - v0));
                    avgNormal += faceNormal;
                }
                avgNormal = math.normalize(avgNormal);

                // Sort faces by angle around the vertex
                var sortedFaces = new NativeList<int>(vertexFaces.Length, Allocator.Temp);
                for (var i = 0; i < vertexFaces.Length; i++)
                    sortedFaces.Add(vertexFaces[i]);

                // Create reference vector for angle calculation
                var refDir = math.normalize(faceCentroids[sortedFaces[0]] - vertexPos);
                var tangent = math.normalize(math.cross(avgNormal, refDir));

                // Bubble sort by angle (simple but works for small counts)
                for (var i = 0; i < sortedFaces.Length - 1; i++)
                {
                    for (var j = i + 1; j < sortedFaces.Length; j++)
                    {
                        var dir_i = math.normalize(faceCentroids[sortedFaces[i]] - vertexPos);
                        var dir_j = math.normalize(faceCentroids[sortedFaces[j]] - vertexPos);

                        var angle_i = math.atan2(math.dot(tangent, dir_i), math.dot(refDir, dir_i));
                        var angle_j = math.atan2(math.dot(tangent, dir_j), math.dot(refDir, dir_j));

                        if (angle_i > angle_j)
                        {
                            var temp = sortedFaces[i];
                            sortedFaces[i] = sortedFaces[j];
                            sortedFaces[j] = temp;
                        }
                    }
                }

                // Create face from dual vertices (reverse order for correct winding)
                var faceStartHe = result.halfEdgeCount;
                var newFace = new Face(faceStartHe);
                var newFaceIdx = result.AddFace(newFace);

                // Reverse the face order for correct outward-facing normals
                for (var i = 0; i < sortedFaces.Length; i++)
                {
                    var reversedIdx = sortedFaces.Length - 1 - i;
                    var dualVertexIdx = faceToVertex[sortedFaces[reversedIdx]];
                    var nextIdx = (i + 1) % sortedFaces.Length;

                    var newHe = new HalfEdge(
                        next: faceStartHe + nextIdx,
                        twin: -1,
                        vertex: dualVertexIdx,
                        face: newFaceIdx
                    );
                    result.AddHalfEdge(newHe);
                }

                // Update vertex half-edge references
                for (var i = 0; i < sortedFaces.Length; i++)
                {
                    var reversedIdx = sortedFaces.Length - 1 - i;
                    var dualVertexIdx = faceToVertex[sortedFaces[reversedIdx]];
                    var heIdx = faceStartHe + i;
                    var v = result.vertices[dualVertexIdx];
                    if (v.halfEdge == -1)
                    {
                        v.halfEdge = heIdx;
                        result.vertices[dualVertexIdx] = v;
                    }
                }

                sortedFaces.Dispose();
            }

            faceCentroids.Dispose();
            faceToVertex.Dispose();
            vertexFaces.Dispose();

            return result;
        }
    }
}
