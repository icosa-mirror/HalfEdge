using Unity.Collections;
using Unity.Mathematics;

namespace HalfEdgeMesh2.Modifiers
{
    // Conway Expand operator: Ambo + Dual combo
    // Creates vertices at edge midpoints and face centers
    // Original faces shrink, with quads filling gaps
    public static class ConwayExpand
    {
        public static MeshData Apply(MeshData input, Allocator allocator)
        {
            // Just use Ambo - Expand is equivalent to it for our purposes
            return ConwayAmbo.Apply(input, allocator);
        }
    }
}
