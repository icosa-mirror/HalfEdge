using Unity.Collections;
using Unity.Mathematics;

namespace HalfEdgeMesh2.Modifiers
{
    // Conway Zip operator: dual of Kis
    // Creates pyramids at each vertex by raising vertices and connecting to surrounding edges
    public static class ConwayZip
    {
        public static MeshData Apply(MeshData input, float height, Allocator allocator)
        {
            // Estimate sizes
            var estimatedVertices = input.vertexCount + (input.halfEdgeCount / 2); // Original + edge midpoints
            var estimatedFaces = 0;

            // Count faces needed: each edge gets a triangular face
            for (var heIdx = 0; heIdx < input.halfEdgeCount; heIdx++)
            {
                var halfEdge = input.halfEdges[heIdx];
                if (halfEdge.face != -1)
                    estimatedFaces++;
            }

            var estimatedHalfEdges = estimatedFaces * 3;
            var result = new MeshData(estimatedVertices, estimatedHalfEdges, estimatedFaces, allocator);

            // Map: original edge -> new vertex at edge midpoint
            var edgeToVertex = new NativeArray<int>(input.halfEdgeCount, Allocator.Temp);
            for (var i = 0; i < edgeToVertex.Length; i++)
                edgeToVertex[i] = -1;

            // Create raised vertex for each original vertex
            var vertexRaised = new NativeArray<int>(input.vertexCount, Allocator.Temp);
            for (var vertIdx = 0; vertIdx < input.vertexCount; vertIdx++)
            {
                var vertex = input.vertices[vertIdx];

                // Calculate vertex normal by averaging adjacent face normals
                var normal = float3.zero;
                var faceCount = 0;

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

                if (startHe != -1)
                {
                    var he = startHe;
                    var iterations = 0;
                    do
                    {
                        var halfEdge = input.halfEdges[he];
                        if (halfEdge.face != -1)
                        {
                            // Calculate face normal
                            var face = input.faces[halfEdge.face];
                            var faceHe = face.halfEdge;
                            var faceStart = faceHe;
                            var faceVertices = new NativeList<float3>(Allocator.Temp);

                            do
                            {
                                var fhe = input.halfEdges[faceHe];
                                faceVertices.Add(input.vertices[fhe.vertex].position);
                                faceHe = fhe.next;
                            } while (faceHe != faceStart);

                            var faceNormal = float3.zero;
                            for (var i = 0; i < faceVertices.Length; i++)
                            {
                                var v1 = faceVertices[i];
                                var v2 = faceVertices[(i + 1) % faceVertices.Length];
                                faceNormal.x += (v1.y - v2.y) * (v1.z + v2.z);
                                faceNormal.y += (v1.z - v2.z) * (v1.x + v2.x);
                                faceNormal.z += (v1.x - v2.x) * (v1.y + v2.y);
                            }

                            normal += math.normalize(faceNormal);
                            faceCount++;
                            faceVertices.Dispose();
                        }

                        if (halfEdge.twin != -1)
                            he = input.halfEdges[halfEdge.twin].next;
                        else
                            break;

                        iterations++;
                    } while (he != startHe && iterations < 100);
                }

                if (faceCount > 0)
                    normal = math.normalize(normal);
                else
                    normal = new float3(0, 1, 0);

                // Create raised vertex
                var raisedPos = vertex.position + normal * height;
                var raisedVertex = new Vertex(raisedPos, vertex.uv);
                vertexRaised[vertIdx] = result.AddVertex(raisedVertex);
            }

            // Create edge midpoint vertices
            for (var heIdx = 0; heIdx < input.halfEdgeCount; heIdx++)
            {
                if (edgeToVertex[heIdx] != -1)
                    continue;

                var halfEdge = input.halfEdges[heIdx];
                var twinIdx = halfEdge.twin;

                if (twinIdx != -1 && edgeToVertex[twinIdx] != -1)
                {
                    edgeToVertex[heIdx] = edgeToVertex[twinIdx];
                    continue;
                }

                // Calculate edge midpoint
                var v0Pos = input.vertices[halfEdge.vertex].position;
                var nextHe = input.halfEdges[halfEdge.next];
                var v1Pos = input.vertices[nextHe.vertex].position;
                var midPos = (v0Pos + v1Pos) * 0.5f;

                var midVertex = new Vertex(midPos, new float2(0.5f, 0.5f));
                var midVertexIdx = result.AddVertex(midVertex);

                edgeToVertex[heIdx] = midVertexIdx;
                if (twinIdx != -1)
                    edgeToVertex[twinIdx] = midVertexIdx;
            }

            // Create triangular faces
            for (var heIdx = 0; heIdx < input.halfEdgeCount; heIdx++)
            {
                var halfEdge = input.halfEdges[heIdx];
                if (halfEdge.face == -1)
                    continue;

                // Triangle: raised vertex -> edge midpoint -> next edge midpoint
                var v0 = vertexRaised[halfEdge.vertex];
                var v1 = edgeToVertex[heIdx];
                var v2 = edgeToVertex[halfEdge.next];

                var faceStartHe = result.halfEdgeCount;
                var newFace = new Face(faceStartHe);
                var newFaceIdx = result.AddFace(newFace);

                var he0 = new HalfEdge(faceStartHe + 1, -1, v0, newFaceIdx);
                var he1 = new HalfEdge(faceStartHe + 2, -1, v1, newFaceIdx);
                var he2 = new HalfEdge(faceStartHe + 0, -1, v2, newFaceIdx);

                result.AddHalfEdge(he0);
                result.AddHalfEdge(he1);
                result.AddHalfEdge(he2);
            }

            edgeToVertex.Dispose();
            vertexRaised.Dispose();

            return result;
        }
    }
}
