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

            var v0 = builder.AddVertex(new float3( s,  s,  s));
            var v1 = builder.AddVertex(new float3( s, -s, -s));
            var v2 = builder.AddVertex(new float3(-s,  s, -s));
            var v3 = builder.AddVertex(new float3(-s, -s,  s));

            // Tetrahedron faces
            builder.AddFace(v0, v1, v2);
            builder.AddFace(v0, v3, v1);
            builder.AddFace(v0, v2, v3);
            builder.AddFace(v1, v3, v2);

            var result = builder.Build(allocator);
            builder.Dispose();

            return result;
        }
    }
}
