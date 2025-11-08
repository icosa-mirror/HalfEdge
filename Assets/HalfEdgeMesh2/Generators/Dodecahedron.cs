using Unity.Collections;
using Unity.Mathematics;

namespace HalfEdgeMesh2.Generators
{
    public static class Dodecahedron
    {
        public static MeshData Generate(float size, Allocator allocator)
        {
            var builder = new MeshBuilder(Allocator.TempJob, 64);

            // Golden ratio for dodecahedron construction
            var phi = (1f + math.sqrt(5f)) / 2f; // Golden ratio ≈ 1.618
            var invPhi = 1f / phi; // ≈ 0.618
            var scale = size * 0.5f;

            // 20 vertices of a regular dodecahedron
            var vertices = new NativeArray<int>(20, Allocator.Temp);

            // Helper to add vertex with spherical UV
            int AddDodecaVertex(ref MeshBuilder b, float3 pos)
            {
                var normalized = math.normalize(pos);
                var u = 0.5f + math.atan2(normalized.z, normalized.x) / (2f * math.PI);
                var v = 0.5f - math.asin(normalized.y) / math.PI;
                return b.AddVertex(pos, new float2(u, v));
            }

            // Cube vertices (8 vertices)
            vertices[0] = AddDodecaVertex(ref builder, new float3( 1,  1,  1) * scale);
            vertices[1] = AddDodecaVertex(ref builder, new float3( 1,  1, -1) * scale);
            vertices[2] = AddDodecaVertex(ref builder, new float3( 1, -1,  1) * scale);
            vertices[3] = AddDodecaVertex(ref builder, new float3( 1, -1, -1) * scale);
            vertices[4] = AddDodecaVertex(ref builder, new float3(-1,  1,  1) * scale);
            vertices[5] = AddDodecaVertex(ref builder, new float3(-1,  1, -1) * scale);
            vertices[6] = AddDodecaVertex(ref builder, new float3(-1, -1,  1) * scale);
            vertices[7] = AddDodecaVertex(ref builder, new float3(-1, -1, -1) * scale);

            // Golden ratio rectangles in YZ plane (4 vertices)
            vertices[8] = AddDodecaVertex(ref builder, new float3( 0,  phi,  invPhi) * scale);
            vertices[9] = AddDodecaVertex(ref builder, new float3( 0,  phi, -invPhi) * scale);
            vertices[10] = AddDodecaVertex(ref builder, new float3( 0, -phi,  invPhi) * scale);
            vertices[11] = AddDodecaVertex(ref builder, new float3( 0, -phi, -invPhi) * scale);

            // Golden ratio rectangles in XZ plane (4 vertices)
            vertices[12] = AddDodecaVertex(ref builder, new float3( invPhi,  0,  phi) * scale);
            vertices[13] = AddDodecaVertex(ref builder, new float3(-invPhi,  0,  phi) * scale);
            vertices[14] = AddDodecaVertex(ref builder, new float3( invPhi,  0, -phi) * scale);
            vertices[15] = AddDodecaVertex(ref builder, new float3(-invPhi,  0, -phi) * scale);

            // Golden ratio rectangles in XY plane (4 vertices)
            vertices[16] = AddDodecaVertex(ref builder, new float3( phi,  invPhi,  0) * scale);
            vertices[17] = AddDodecaVertex(ref builder, new float3( phi, -invPhi,  0) * scale);
            vertices[18] = AddDodecaVertex(ref builder, new float3(-phi,  invPhi,  0) * scale);
            vertices[19] = AddDodecaVertex(ref builder, new float3(-phi, -invPhi,  0) * scale);

            // 12 pentagonal faces
            unsafe
            {
                var face0 = stackalloc int[] { vertices[0], vertices[8], vertices[4], vertices[13], vertices[12] };
                var face1 = stackalloc int[] { vertices[0], vertices[12], vertices[2], vertices[17], vertices[16] };
                var face2 = stackalloc int[] { vertices[0], vertices[16], vertices[1], vertices[9], vertices[8] };
                var face3 = stackalloc int[] { vertices[1], vertices[14], vertices[15], vertices[5], vertices[9] };
                var face4 = stackalloc int[] { vertices[1], vertices[16], vertices[17], vertices[3], vertices[14] };
                var face5 = stackalloc int[] { vertices[2], vertices[12], vertices[13], vertices[6], vertices[10] };
                var face6 = stackalloc int[] { vertices[2], vertices[10], vertices[11], vertices[3], vertices[17] };
                var face7 = stackalloc int[] { vertices[3], vertices[11], vertices[7], vertices[15], vertices[14] };
                var face8 = stackalloc int[] { vertices[4], vertices[8], vertices[9], vertices[5], vertices[18] };
                var face9 = stackalloc int[] { vertices[4], vertices[18], vertices[19], vertices[6], vertices[13] };
                var face10 = stackalloc int[] { vertices[5], vertices[15], vertices[7], vertices[19], vertices[18] };
                var face11 = stackalloc int[] { vertices[6], vertices[19], vertices[7], vertices[11], vertices[10] };

                builder.AddFace(new System.ReadOnlySpan<int>(face0, 5));
                builder.AddFace(new System.ReadOnlySpan<int>(face1, 5));
                builder.AddFace(new System.ReadOnlySpan<int>(face2, 5));
                builder.AddFace(new System.ReadOnlySpan<int>(face3, 5));
                builder.AddFace(new System.ReadOnlySpan<int>(face4, 5));
                builder.AddFace(new System.ReadOnlySpan<int>(face5, 5));
                builder.AddFace(new System.ReadOnlySpan<int>(face6, 5));
                builder.AddFace(new System.ReadOnlySpan<int>(face7, 5));
                builder.AddFace(new System.ReadOnlySpan<int>(face8, 5));
                builder.AddFace(new System.ReadOnlySpan<int>(face9, 5));
                builder.AddFace(new System.ReadOnlySpan<int>(face10, 5));
                builder.AddFace(new System.ReadOnlySpan<int>(face11, 5));
            }

            var result = builder.Build(allocator);
            builder.Dispose();
            vertices.Dispose();

            return result;
        }
    }
}
