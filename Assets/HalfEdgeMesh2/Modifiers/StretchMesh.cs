using Unity.Mathematics;

namespace HalfEdgeMesh2.Modifiers
{
    public static class StretchMesh
    {
        public static void Apply(MeshData meshData, float3 scale, float3 center = default)
        {
            for (var i = 0; i < meshData.vertexCount; i++)
            {
                var vertex = meshData.vertices[i];
                var relativePos = vertex.position - center;
                vertex.position = center + relativePos * scale;
                meshData.vertices[i] = vertex;
            }
        }

        public static void Apply(MeshData meshData, float uniformScale, float3 center = default)
        {
            var scale = new float3(uniformScale, uniformScale, uniformScale);
            Apply(meshData, scale, center);
        }
    }
}
