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

            var v0 = builder.AddVertex(new float3( s,  0,  0)); // +X
            var v1 = builder.AddVertex(new float3(-s,  0,  0)); // -X
            var v2 = builder.AddVertex(new float3( 0,  s,  0)); // +Y
            var v3 = builder.AddVertex(new float3( 0, -s,  0)); // -Y
            var v4 = builder.AddVertex(new float3( 0,  0,  s)); // +Z
            var v5 = builder.AddVertex(new float3( 0,  0, -s)); // -Z

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
    }
}
