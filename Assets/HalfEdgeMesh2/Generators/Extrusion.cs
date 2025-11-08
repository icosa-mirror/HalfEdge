using Unity.Collections;
using Unity.Mathematics;

namespace HalfEdgeMesh2.Generators
{
    // Generates a mesh by extruding a 2D profile along the Y axis
    // Useful for creating columns, beams, and other extruded shapes
    public static class Extrusion
    {
        // Generate an extruded mesh from a 2D profile
        // profile: Array of float3 points defining the base shape (Y component is ignored)
        // height: Distance to extrude along the Y axis
        // capped: Whether to cap the top and bottom
        public static MeshData Generate(float3[] profile, float height, bool capped, Allocator allocator)
        {
            var profileCount = profile.Length;
            if (profileCount < 3)
                throw new System.ArgumentException("Profile must have at least 3 points");

            var vertexCount = profileCount * 2;
            var sideFaceCount = profileCount;
            var capFaceCount = capped ? 2 : 0;
            var faceCount = sideFaceCount + capFaceCount;

            var builder = new MeshBuilder(Allocator.TempJob, faceCount * 4);

            // Add bottom vertices
            for (var i = 0; i < profileCount; i++)
            {
                var p = profile[i];
                builder.AddVertex(new float3(p.x, 0, p.z));
            }

            // Add top vertices
            for (var i = 0; i < profileCount; i++)
            {
                var p = profile[i];
                builder.AddVertex(new float3(p.x, height, p.z));
            }

            // Add side faces (quads connecting bottom and top)
            for (var i = 0; i < profileCount; i++)
            {
                var nextI = (i + 1) % profileCount;
                var v0 = i;                      // Bottom current
                var v1 = nextI;                  // Bottom next
                var v2 = profileCount + nextI;   // Top next
                var v3 = profileCount + i;       // Top current

                builder.AddFace(v0, v1, v2, v3);
            }

            // Add caps if requested
            if (capped)
            {
                // Bottom cap (reverse winding for outward normal)
                var bottomIndices = new int[profileCount];
                for (var i = 0; i < profileCount; i++)
                    bottomIndices[i] = profileCount - 1 - i;
                builder.AddFace(new System.ReadOnlySpan<int>(bottomIndices));

                // Top cap (normal winding)
                var topIndices = new int[profileCount];
                for (var i = 0; i < profileCount; i++)
                    topIndices[i] = profileCount + i;
                builder.AddFace(new System.ReadOnlySpan<int>(topIndices));
            }

            var result = builder.Build(allocator);
            builder.Dispose();
            return result;
        }

        // Burst-compatible version using NativeArray
        public static MeshData Generate(NativeArray<float3> profile, float height, bool capped, Allocator allocator)
        {
            var profileCount = profile.Length;
            if (profileCount < 3)
                throw new System.ArgumentException("Profile must have at least 3 points");

            var vertexCount = profileCount * 2;
            var sideFaceCount = profileCount;
            var capFaceCount = capped ? 2 : 0;
            var faceCount = sideFaceCount + capFaceCount;

            var builder = new MeshBuilder(Allocator.TempJob, faceCount * 4);

            // Add bottom vertices
            for (var i = 0; i < profileCount; i++)
            {
                var p = profile[i];
                builder.AddVertex(new float3(p.x, 0, p.z));
            }

            // Add top vertices
            for (var i = 0; i < profileCount; i++)
            {
                var p = profile[i];
                builder.AddVertex(new float3(p.x, height, p.z));
            }

            // Add side faces (quads connecting bottom and top)
            for (var i = 0; i < profileCount; i++)
            {
                var nextI = (i + 1) % profileCount;
                var v0 = i;                      // Bottom current
                var v1 = nextI;                  // Bottom next
                var v2 = profileCount + nextI;   // Top next
                var v3 = profileCount + i;       // Top current

                builder.AddFace(v0, v1, v2, v3);
            }

            // Add caps if requested
            if (capped)
            {
                // Bottom cap (reverse winding for outward normal)
                using (var bottomIndices = new NativeArray<int>(profileCount, Allocator.Temp))
                {
                    for (var i = 0; i < profileCount; i++)
                        bottomIndices[i] = profileCount - 1 - i;

                    unsafe
                    {
                        builder.AddFace(new System.ReadOnlySpan<int>(bottomIndices.GetUnsafeReadOnlyPtr(), profileCount));
                    }
                }

                // Top cap (normal winding)
                using (var topIndices = new NativeArray<int>(profileCount, Allocator.Temp))
                {
                    for (var i = 0; i < profileCount; i++)
                        topIndices[i] = profileCount + i;

                    unsafe
                    {
                        builder.AddFace(new System.ReadOnlySpan<int>(topIndices.GetUnsafeReadOnlyPtr(), profileCount));
                    }
                }
            }

            var result = builder.Build(allocator);
            builder.Dispose();
            return result;
        }
    }
}
