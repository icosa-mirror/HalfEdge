using Unity.Collections;
using Unity.Mathematics;

namespace HalfEdgeMesh2.Modifiers
{
    // Conway Zip operator: truncate then dual (td)
    // Not an original Conway operator but commonly used
    public static class ConwayZip
    {
        public static MeshData Apply(MeshData input, float ratio, Allocator allocator)
        {
            // zip = td (truncate then dual)
            var truncated = ConwayTruncate.Apply(input, ratio, Allocator.Persistent);
            var result = ConwayDual.Apply(truncated, allocator);
            truncated.Dispose();
            return result;
        }
    }
}
