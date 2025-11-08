using Unity.Collections;
using Unity.Mathematics;

namespace HalfEdgeMesh2.Modifiers
{
    // Conway Bevel operator: truncate then ambo (ta)
    // Composite operator combining truncation and rectification
    public static class ConwayBevel
    {
        public static MeshData Apply(MeshData input, float ratio, Allocator allocator)
        {
            // b = ta (truncate then ambo)
            var truncated = ConwayTruncate.Apply(input, ratio, Allocator.Persistent);
            var result = ConwayAmbo.Apply(truncated, allocator);
            truncated.Dispose();
            return result;
        }
    }
}
