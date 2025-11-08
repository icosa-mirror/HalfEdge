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
            // For each original vertex, find all adjacent faces and create a new face
            var vertexFaces = new NativeList<int>(Allocator.Temp);

            for (var vertIdx = 0; vertIdx < input.vertexCount; vertIdx++)
            {
                vertexFaces.Clear();

                var vertex = input.vertices[vertIdx];
                if (vertex.halfEdge == -1)
                    continue;

                var startHe = vertex.halfEdge;
                var he = startHe;
                var iterations = 0;

                // Collect all faces around this vertex
                do
                {
                    var halfEdge = input.halfEdges[he];
                    if (halfEdge.face != -1 && !vertexFaces.Contains(halfEdge.face))
                        vertexFaces.Add(halfEdge.face);

                    // Move to next half-edge around vertex (via twin and next)
                    if (halfEdge.twin != -1)
                    {
                        var twinHe = input.halfEdges[halfEdge.twin];
                        he = twinHe.next;
                    }
                    else
                    {
                        break; // Boundary edge
                    }

                    iterations++;
                    if (iterations >= 100)
                    {
                        // Safety check - prevent infinite loops
                        break;
                    }
                } while (he != startHe);

                if (vertexFaces.Length < 3)
                    continue;

                // Create face from dual vertices (reverse order for correct winding)
                var faceStartHe = result.halfEdgeCount;
                var newFace = new Face(faceStartHe);
                var newFaceIdx = result.AddFace(newFace);

                // Reverse the face order for correct outward-facing normals
                for (var i = 0; i < vertexFaces.Length; i++)
                {
                    // Walk backwards through the collected faces
                    var reversedIdx = vertexFaces.Length - 1 - i;
                    var dualVertexIdx = faceToVertex[vertexFaces[reversedIdx]];
                    var nextIdx = (i + 1) % vertexFaces.Length;

                    var newHe = new HalfEdge(
                        next: faceStartHe + nextIdx,
                        twin: -1,
                        vertex: dualVertexIdx,
                        face: newFaceIdx
                    );
                    result.AddHalfEdge(newHe);
                }

                // Update vertex half-edge references
                for (var i = 0; i < vertexFaces.Length; i++)
                {
                    var reversedIdx = vertexFaces.Length - 1 - i;
                    var dualVertexIdx = faceToVertex[vertexFaces[reversedIdx]];
                    var heIdx = faceStartHe + i;
                    var v = result.vertices[dualVertexIdx];
                    if (v.halfEdge == -1)
                    {
                        v.halfEdge = heIdx;
                        result.vertices[dualVertexIdx] = v;
                    }
                }
            }

            faceCentroids.Dispose();
            faceToVertex.Dispose();
            vertexFaces.Dispose();

            return result;
        }
    }
}
