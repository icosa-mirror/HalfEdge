using Unity.Collections;
using Unity.Mathematics;

namespace HalfEdgeMesh2.Modifiers
{
    public static class ExpandVertices
    {
        public static void Apply(MeshData meshData, float distance)
        {
            if (distance == 0f) return;

            var normals = new NativeArray<float3>(meshData.vertexCount, Allocator.Temp);

            // Compute vertex normals
            MeshOperations.ComputeVertexNormals(ref meshData, ref normals);

            // Move vertices along their normals
            for (var i = 0; i < meshData.vertexCount; i++)
            {
                var vertex = meshData.vertices[i];
                vertex.position += normals[i] * distance;
                meshData.vertices[i] = vertex;
            }

            normals.Dispose();
        }
    }
}
