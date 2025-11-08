using Unity.Collections;
using Unity.Mathematics;

namespace HalfEdgeMesh2.Generators
{
    public static class Octahedron
    {
        public static MeshData Generate(float size, Allocator allocator)
        {
            var builder = new MeshBuilder(Allocator.TempJob, 32);

            // Regular octahedron vertices (6 vertices at unit distance along axes)
            var s = size * 0.5f;

            var p0 = new float3( s,  0,  0); // +X
            var p1 = new float3(-s,  0,  0); // -X
            var p2 = new float3( 0,  s,  0); // +Y
            var p3 = new float3( 0, -s,  0); // -Y
            var p4 = new float3( 0,  0,  s); // +Z
            var p5 = new float3( 0,  0, -s); // -Z

            var v0 = builder.AddVertex(p0, CalculateSphericalUV(p0));
            var v1 = builder.AddVertex(p1, CalculateSphericalUV(p1));
            var v2 = builder.AddVertex(p2, CalculateSphericalUV(p2));
            var v3 = builder.AddVertex(p3, CalculateSphericalUV(p3));
            var v4 = builder.AddVertex(p4, CalculateSphericalUV(p4));
            var v5 = builder.AddVertex(p5, CalculateSphericalUV(p5));

            // Octahedron faces (8 triangular faces)
            // Top half (around +Y vertex)
            builder.AddFace(v2, v4, v0);
            builder.AddFace(v2, v1, v4);
            builder.AddFace(v2, v5, v1);
            builder.AddFace(v2, v0, v5);

            // Bottom half (around -Y vertex)
            builder.AddFace(v3, v0, v4);
            builder.AddFace(v3, v4, v1);
            builder.AddFace(v3, v1, v5);
            builder.AddFace(v3, v5, v0);

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
