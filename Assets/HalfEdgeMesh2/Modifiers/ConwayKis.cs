using Unity.Collections;
using Unity.Mathematics;

namespace HalfEdgeMesh2.Modifiers
{
    // Conway Kis operator: adds a vertex at the center of each face
    // Subdivides each n-gon face into n triangles radiating from the center
    public static class ConwayKis
    {
        public static MeshData Apply(MeshData input, float height, Allocator allocator)
        {
            // Estimate sizes: vertices = old + faces, faces = sum of face valences
            var estimatedVertices = input.vertexCount + input.faceCount;
            var estimatedFaces = 0;

            // Count total valence
            for (var faceIdx = 0; faceIdx < input.faceCount; faceIdx++)
            {
                var face = input.faces[faceIdx];
                var he = face.halfEdge;
                var start = he;
                var valence = 0;

                do
                {
                    valence++;
                    he = input.halfEdges[he].next;
                } while (he != start && valence < 100);

                estimatedFaces += valence;
            }

            var estimatedHalfEdges = estimatedFaces * 3; // All triangles
            var result = new MeshData(estimatedVertices, estimatedHalfEdges, estimatedFaces, allocator);

            // Copy original vertices
            for (var i = 0; i < input.vertexCount; i++)
            {
                var vertex = input.vertices[i];
                result.AddVertex(vertex);
            }

            // Process each face
            for (var faceIdx = 0; faceIdx < input.faceCount; faceIdx++)
            {
                var face = input.faces[faceIdx];
                var startHe = face.halfEdge;
                var he = startHe;

                // Calculate face centroid and normal
                var centroid = float3.zero;
                var normal = float3.zero;
                var valence = 0;
                var faceVertices = new NativeList<int>(Allocator.Temp);

                do
                {
                    var halfEdge = input.halfEdges[he];
                    faceVertices.Add(halfEdge.vertex);
                    var vertex = input.vertices[halfEdge.vertex];
                    centroid += vertex.position;
                    valence++;
                    he = halfEdge.next;
                } while (he != startHe && valence < 100);

                centroid /= valence;

                // Calculate face normal (Newell's method)
                for (var i = 0; i < faceVertices.Length; i++)
                {
                    var v1 = input.vertices[faceVertices[i]].position;
                    var v2 = input.vertices[faceVertices[(i + 1) % faceVertices.Length]].position;
                    normal.x += (v1.y - v2.y) * (v1.z + v2.z);
                    normal.y += (v1.z - v2.z) * (v1.x + v2.x);
                    normal.z += (v1.x - v2.x) * (v1.y + v2.y);
                }
                normal = math.normalize(normal);

                // Create center vertex offset by height along normal
                var centerPos = centroid + normal * height;
                var centerVertex = new Vertex(centerPos, new float2(0.5f, 0.5f));
                var centerVertexIdx = result.AddVertex(centerVertex);

                // Create triangular faces from center to each edge
                for (var i = 0; i < faceVertices.Length; i++)
                {
                    var v0 = centerVertexIdx;
                    var v1 = faceVertices[i];
                    var v2 = faceVertices[(i + 1) % faceVertices.Length];

                    // Create triangle face
                    var faceStartHe = result.halfEdgeCount;
                    var newFace = new Face(faceStartHe);
                    var newFaceIdx = result.AddFace(newFace);

                    // Create three half-edges for triangle
                    var he0 = new HalfEdge(faceStartHe + 1, -1, v0, newFaceIdx);
                    var he1 = new HalfEdge(faceStartHe + 2, -1, v1, newFaceIdx);
                    var he2 = new HalfEdge(faceStartHe + 0, -1, v2, newFaceIdx);

                    result.AddHalfEdge(he0);
                    result.AddHalfEdge(he1);
                    result.AddHalfEdge(he2);

                    // Update vertex half-edge references
                    var cv = result.vertices[v0];
                    if (cv.halfEdge == -1)
                    {
                        cv.halfEdge = faceStartHe;
                        result.vertices[v0] = cv;
                    }

                    var v1v = result.vertices[v1];
                    if (v1v.halfEdge == -1)
                    {
                        v1v.halfEdge = faceStartHe + 1;
                        result.vertices[v1] = v1v;
                    }

                    var v2v = result.vertices[v2];
                    if (v2v.halfEdge == -1)
                    {
                        v2v.halfEdge = faceStartHe + 2;
                        result.vertices[v2] = v2v;
                    }
                }

                faceVertices.Dispose();
            }

            return result;
        }
    }
}
