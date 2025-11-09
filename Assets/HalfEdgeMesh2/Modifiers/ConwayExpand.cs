using Unity.Collections;
using Unity.Mathematics;

namespace HalfEdgeMesh2.Modifiers
{
    // Conway Expand operator: ambo applied twice (aa)
    // Creates vertices at edge midpoints and face centers
    // Original faces shrink, with quads filling gaps
    public static class ConwayExpand
    {
        public static MeshData Apply(MeshData input, Allocator allocator)
        {
            // e = aa (ambo then ambo)
            var firstAmbo = ConwayAmbo.Apply(input, Allocator.Persistent);
            var result = ConwayAmbo.Apply(firstAmbo, allocator);
            firstAmbo.Dispose();
            return result;
        }
    }
}
