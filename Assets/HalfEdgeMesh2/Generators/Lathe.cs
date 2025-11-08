using Unity.Collections;
using Unity.Mathematics;

namespace HalfEdgeMesh2.Generators
{
    // Generates a mesh by revolving a 2D profile around the Y axis
    // Useful for creating vases, bowls, bottles, and other rotationally symmetric objects
    public static class Lathe
    {
        // Generate a lathed mesh from a 2D profile
        // profile: Array of float2 points (x = radius, y = height)
        // segments: Number of radial subdivisions around the axis
        public static MeshData Generate(float2[] profile, int segments, Allocator allocator)
        {
            var profileCount = profile.Length;
            if (profileCount < 2)
                throw new System.ArgumentException("Profile must have at least 2 points");

            if (segments < 3)
                segments = 3;

            var vertexCount = profileCount * segments;
            var faceCount = (profileCount - 1) * segments;
            var builder = new MeshBuilder(Allocator.TempJob, faceCount * 4);

            // Create vertex rings
            var angleStep = (2.0f * math.PI) / segments;
            for (var s = 0; s < segments; s++)
            {
                var angle = s * angleStep;
                var cos = math.cos(angle);
                var sin = math.sin(angle);
                var u = s / (float)segments;

                for (var p = 0; p < profileCount; p++)
                {
                    var profilePoint = profile[p];
                    var radius = profilePoint.x;
                    var height = profilePoint.y;
                    var v = p / (float)(profileCount - 1);

                    var position = new float3(
                        radius * cos,
                        height,
                        radius * sin
                    );

                    var uv = new float2(u, v);
                    builder.AddVertex(position, uv);
                }
            }

            // Create quad faces connecting rings
            for (var s = 0; s < segments; s++)
            {
                var nextS = (s + 1) % segments;

                for (var p = 0; p < profileCount - 1; p++)
                {
                    var v0 = s * profileCount + p;
                    var v1 = nextS * profileCount + p;
                    var v2 = nextS * profileCount + (p + 1);
                    var v3 = s * profileCount + (p + 1);

                    builder.AddFace(v0, v1, v2, v3);
                }
            }

            var result = builder.Build(allocator);
            builder.Dispose();
            return result;
        }

        // Burst-compatible version using NativeArray
        public static MeshData Generate(NativeArray<float2> profile, int segments, Allocator allocator)
        {
            var profileCount = profile.Length;
            if (profileCount < 2)
                throw new System.ArgumentException("Profile must have at least 2 points");

            if (segments < 3)
                segments = 3;

            var vertexCount = profileCount * segments;
            var faceCount = (profileCount - 1) * segments;
            var builder = new MeshBuilder(Allocator.TempJob, faceCount * 4);

            // Create vertex rings
            var angleStep = (2.0f * math.PI) / segments;
            for (var s = 0; s < segments; s++)
            {
                var angle = s * angleStep;
                var cos = math.cos(angle);
                var sin = math.sin(angle);
                var u = s / (float)segments;

                for (var p = 0; p < profileCount; p++)
                {
                    var profilePoint = profile[p];
                    var radius = profilePoint.x;
                    var height = profilePoint.y;
                    var v = p / (float)(profileCount - 1);

                    var position = new float3(
                        radius * cos,
                        height,
                        radius * sin
                    );

                    var uv = new float2(u, v);
                    builder.AddVertex(position, uv);
                }
            }

            // Create quad faces connecting rings
            for (var s = 0; s < segments; s++)
            {
                var nextS = (s + 1) % segments;

                for (var p = 0; p < profileCount - 1; p++)
                {
                    var v0 = s * profileCount + p;
                    var v1 = nextS * profileCount + p;
                    var v2 = nextS * profileCount + (p + 1);
                    var v3 = s * profileCount + (p + 1);

                    builder.AddFace(v0, v1, v2, v3);
                }
            }

            var result = builder.Build(allocator);
            builder.Dispose();
            return result;
        }
    }
}
