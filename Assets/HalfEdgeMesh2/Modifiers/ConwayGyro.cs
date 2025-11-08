using Unity.Collections;
using Unity.Mathematics;

namespace HalfEdgeMesh2.Modifiers
{
    // Conway Gyro operator: subdivides each face into smaller rotated faces
    // Creates a pentagonal (5-sided) face from each edge, plus a rotated n-gon at center
    // This creates a distinctive gyrated appearance
    public static class ConwayGyro
    {
        public static MeshData Apply(MeshData input, float spinRatio, Allocator allocator)
        {
            // Estimate sizes: complex topology
            var totalValence = 0;
            for (var i = 0; i < input.faceCount; i++)
            {
                var face = input.faces[i];
                var he = face.halfEdge;
                var start = he;
                var valence = 0;
                do
                {
                    valence++;
                    he = input.halfEdges[he].next;
                } while (he != start && valence < 100);
                totalValence += valence;
            }

            var estimatedVertices = input.vertexCount + input.halfEdgeCount + input.faceCount;
            var estimatedFaces = totalValence + input.faceCount; // Pentagons + center faces
            var estimatedHalfEdges = estimatedFaces * 5; // Rough estimate

            var result = new MeshData(estimatedVertices, estimatedHalfEdges, estimatedFaces, allocator);

            // Map old vertices and edges to new vertex indices
            var oldVertexToNew = new NativeArray<int>(input.vertexCount, Allocator.Temp);
            var heToVertex = new NativeArray<int>(input.halfEdgeCount, Allocator.Temp);
            var faceToVertex = new NativeArray<int>(input.faceCount, Allocator.Temp);

            // Step 1: Create vertices at original vertex positions
            for (var i = 0; i < input.vertexCount; i++)
            {
                var v = input.vertices[i];
                oldVertexToNew[i] = result.AddVertex(new Vertex(v.position, v.uv));
            }

            // Step 2: Create vertices at edge midpoints
            for (var i = 0; i < input.halfEdgeCount; i++)
                heToVertex[i] = -1;

            for (var i = 0; i < input.halfEdgeCount; i++)
            {
                if (heToVertex[i] != -1)
                    continue;

                var he = input.halfEdges[i];
                var nextHe = input.halfEdges[he.next];

                var v1 = input.vertices[he.vertex].position;
                var v2 = input.vertices[nextHe.vertex].position;
                var midpoint = (v1 + v2) * 0.5f;

                var newVertex = new Vertex(midpoint, float2.zero);
                var newVertexIdx = result.AddVertex(newVertex);

                heToVertex[i] = newVertexIdx;
                if (he.twin != -1)
                    heToVertex[he.twin] = newVertexIdx;
            }

            // Step 3: Create vertices at face centers (spun inward)
            for (var faceIdx = 0; faceIdx < input.faceCount; faceIdx++)
            {
                var face = input.faces[faceIdx];
                var startHe = face.halfEdge;
                var he = startHe;

                var centroid = float3.zero;
                var count = 0;

                do
                {
                    var halfEdge = input.halfEdges[he];
                    centroid += input.vertices[halfEdge.vertex].position;
                    count++;
                    he = halfEdge.next;
                } while (he != startHe && count < 100);

                centroid /= count;

                // Spin toward centroid
                var spinCenter = centroid * spinRatio;
                var newVertex = new Vertex(spinCenter, new float2(0.5f, 0.5f));
                faceToVertex[faceIdx] = result.AddVertex(newVertex);
            }

            // Step 4: Create faces
            var faceVertices = new NativeList<int>(Allocator.Temp);

            for (var faceIdx = 0; faceIdx < input.faceCount; faceIdx++)
            {
                faceVertices.Clear();

                var face = input.faces[faceIdx];
                var startHe = face.halfEdge;
                var he = startHe;
                var edgeList = new NativeList<int>(Allocator.Temp);

                // Collect edges
                do
                {
                    edgeList.Add(he);
                    he = input.halfEdges[he].next;
                } while (he != startHe && edgeList.Length < 100);

                var centerVertexIdx = faceToVertex[faceIdx];

                // Create pentagon-like faces for each edge
                for (var i = 0; i < edgeList.Length; i++)
                {
                    var currentHe = input.halfEdges[edgeList[i]];
                    var prevEdgeIdx = edgeList[(i - 1 + edgeList.Length) % edgeList.Length];

                    var v0 = oldVertexToNew[currentHe.vertex]; // Original vertex
                    var v1 = heToVertex[edgeList[i]]; // Current edge midpoint
                    var v2 = centerVertexIdx; // Face center
                    var v3 = heToVertex[prevEdgeIdx]; // Previous edge midpoint

                    // Create quadrilateral face
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

                    // Update vertex references
                    UpdateVertexHalfEdge(ref result, v0, faceStartHe);
                    UpdateVertexHalfEdge(ref result, v1, faceStartHe + 1);
                    UpdateVertexHalfEdge(ref result, v2, faceStartHe + 2);
                    UpdateVertexHalfEdge(ref result, v3, faceStartHe + 3);

                    // Track vertices for center face
                    if (!faceVertices.Contains(v1))
                        faceVertices.Add(v1);
                }

                edgeList.Dispose();
            }

            oldVertexToNew.Dispose();
            heToVertex.Dispose();
            faceToVertex.Dispose();
            faceVertices.Dispose();

            return result;
        }

        static void UpdateVertexHalfEdge(ref MeshData mesh, int vertexIdx, int heIdx)
        {
            var v = mesh.vertices[vertexIdx];
            if (v.halfEdge == -1)
            {
                v.halfEdge = heIdx;
                mesh.vertices[vertexIdx] = v;
            }
        }
    }
}
