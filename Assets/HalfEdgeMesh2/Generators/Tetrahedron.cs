using Unity.Collections;
using Unity.Mathematics;

namespace HalfEdgeMesh2.Generators
{
    public static class Tetrahedron
    {
        public static MeshData Generate(float size, Allocator allocator)
        {
            var builder = new MeshBuilder(Allocator.TempJob, 16);

            // Regular tetrahedron vertices
            var s = size / math.sqrt(2f);

            var p0 = new float3( s,  s,  s);
            var p1 = new float3( s, -s, -s);
            var p2 = new float3(-s,  s, -s);
            var p3 = new float3(-s, -s,  s);

            var v0 = builder.AddVertex(p0, CalculateSphericalUV(p0));
            var v1 = builder.AddVertex(p1, CalculateSphericalUV(p1));
            var v2 = builder.AddVertex(p2, CalculateSphericalUV(p2));
            var v3 = builder.AddVertex(p3, CalculateSphericalUV(p3));

            // Tetrahedron faces
            builder.AddFace(v0, v1, v2);
            builder.AddFace(v0, v3, v1);
            builder.AddFace(v0, v2, v3);
            builder.AddFace(v1, v3, v2);

            var result = builder.Build(allocator);
            builder.Dispose();

            return result;
        }

        static float2 CalculateSphericalUV(float3 position)
        {
            var normalized = math.normalize(position);
            var u = 0.5f + math.atan2(normalized.z, normalized.x) / (2f * math.PI);
            var v = 0.5f - math.asin(normalized.y) / math.PI;
            return new float2(u, v);
        }
    }
}
