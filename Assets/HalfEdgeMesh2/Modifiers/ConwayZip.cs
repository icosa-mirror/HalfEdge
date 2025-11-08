using Unity.Collections;
using Unity.Mathematics;

namespace HalfEdgeMesh2.Modifiers
{
    // Conway Zip operator: creates kite-shaped faces
    // For now, simplified to use Kis as a placeholder
    public static class ConwayZip
    {
        public static MeshData Apply(MeshData input, float height, Allocator allocator)
        {
            // Use Kis as a simple working operator for now
            return ConwayKis.Apply(input, height, allocator);
        }
    }
}
