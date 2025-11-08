using Unity.Collections;
using Unity.Mathematics;

namespace HalfEdgeMesh2.Generators
{
    public static class Cone
    {
        public static MeshData Generate(float radius, float height, int segments, Allocator allocator)
        {
            var clampedSegments = math.max(segments, 3);

            // 1 apex + 1 base center + rim vertices
            var vertexCount = 2 + clampedSegments;
            var faceCount = clampedSegments * 2; // side triangles + base triangles
            var edgeCount = faceCount * 2;

            var builder = new MeshBuilder(Allocator.TempJob, edgeCount);

            var halfHeight = height * 0.5f;

            // Add apex vertex
            var apexUV = new float2(0.5f, 1.0f);
            var apexIndex = builder.AddVertex(new float3(0, 0, halfHeight), apexUV);

            // Add base center vertex
            var baseCenterUV = new float2(0.5f, 0.5f);
            var baseCenterIndex = builder.AddVertex(new float3(0, 0, -halfHeight), baseCenterUV);

            // Add base rim vertices
            var rimIndices = new NativeArray<int>(clampedSegments, Allocator.Temp);
            var angleStep = math.PI * 2f / clampedSegments;

            for (var i = 0; i < clampedSegments; i++)
            {
                var angle = i * angleStep;
                var x = math.cos(angle) * radius;
                var y = math.sin(angle) * radius;
                var u = i / (float)clampedSegments;
                var uv = new float2(u, 0);
                rimIndices[i] = builder.AddVertex(new float3(x, y, -halfHeight), uv);
            }

            // Create side faces (triangles from apex to rim)
            for (var i = 0; i < clampedSegments; i++)
            {
                var next = (i + 1) % clampedSegments;
                builder.AddFace(apexIndex, rimIndices[i], rimIndices[next]);
            }

            // Create base face (triangles from center to rim)
            for (var i = 0; i < clampedSegments; i++)
            {
                var next = (i + 1) % clampedSegments;
                builder.AddFace(baseCenterIndex, rimIndices[i], rimIndices[next]);
            }

            var result = builder.Build(allocator);
            builder.Dispose();
            rimIndices.Dispose();

            return result;
        }
    }
}
