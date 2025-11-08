using Unity.Collections;
using Unity.Mathematics;

namespace HalfEdgeMesh2.Modifiers
{
    // Conway Ortho (Medial) operator: creates all-quad mesh
    // Places vertices at edge midpoints and face centers
    // Creates quadrilateral faces from the connectivity
    public static class ConwayOrtho
    {
        public static MeshData Apply(MeshData input, Allocator allocator)
        {
            var estimatedVertices = (input.halfEdgeCount / 2) + input.faceCount;
            var estimatedFaces = input.halfEdgeCount;
            var estimatedHalfEdges = estimatedFaces * 4;
            var result = new MeshData(estimatedVertices, estimatedHalfEdges, estimatedFaces, allocator);

            // Create vertices at edge midpoints
            var edgeToVertex = new NativeArray<int>(input.halfEdgeCount, Allocator.Temp);
            for (var i = 0; i < edgeToVertex.Length; i++)
                edgeToVertex[i] = -1;

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

            // Create vertices at face centers
            var faceToVertex = new NativeArray<int>(input.faceCount, Allocator.Temp);

            for (var faceIdx = 0; faceIdx < input.faceCount; faceIdx++)
            {
                var face = input.faces[faceIdx];
                var startHe = face.halfEdge;
                var he = startHe;

                var centroid = float3.zero;
                var valence = 0;

                do
                {
                    var halfEdge = input.halfEdges[he];
                    centroid += input.vertices[halfEdge.vertex].position;
                    valence++;
                    he = halfEdge.next;
                } while (he != startHe && valence < 100);

                centroid /= valence;
                var centerVertex = new Vertex(centroid, new float2(0.5f, 0.5f));
                faceToVertex[faceIdx] = result.AddVertex(centerVertex);
            }

            // Create quad faces: for each half-edge, create a quad from:
            // edge midpoint -> face center -> next edge midpoint -> adjacent face center
            for (var heIdx = 0; heIdx < input.halfEdgeCount; heIdx++)
            {
                var halfEdge = input.halfEdges[heIdx];
                if (halfEdge.face == -1)
                    continue;

                var twinIdx = halfEdge.twin;
                if (twinIdx == -1)
                    continue; // Skip boundary edges

                var twin = input.halfEdges[twinIdx];
                if (twin.face == -1)
                    continue;

                // Only create each face once
                if (heIdx > twinIdx)
                    continue;

                // Quad: edge midpoint -> face1 center -> next edge midpoint -> face2 center
                var v0 = edgeToVertex[heIdx];
                var v1 = faceToVertex[halfEdge.face];
                var v2 = edgeToVertex[halfEdge.next];
                var v3 = faceToVertex[twin.face];

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

            edgeToVertex.Dispose();
            faceToVertex.Dispose();
            return result;
        }
    }
}
