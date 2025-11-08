using Unity.Mathematics;

namespace HalfEdgeMesh2.Modifiers
{
    public static class SkewMesh
    {
        public static void Apply(MeshData meshData, float angle, float3 direction)
        {
            if (angle == 0f) return;

            var skewDirection = math.normalize(direction);

            // Calculate mesh bounds
            MeshOperations.ComputeBounds(ref meshData, out var boundsCenter, out var boundsSize);

            // Determine the primary axis for skewing (largest dimension)
            var primaryAxis = 0; // X = 0, Y = 1, Z = 2
            if (boundsSize.y > boundsSize.x && boundsSize.y > boundsSize.z) primaryAxis = 1;
            else if (boundsSize.z > boundsSize.x && boundsSize.z > boundsSize.y) primaryAxis = 2;

            // Transform each vertex
            for (var i = 0; i < meshData.vertexCount; i++)
            {
                var vertex = meshData.vertices[i];
                var relativePos = vertex.position - boundsCenter;

                // Apply skew based on position along primary axis
                float skewFactor = 0;
                switch (primaryAxis)
                {
                    case 0: // X-axis
                        skewFactor = relativePos.x / (boundsSize.x * 0.5f);
                        break;
                    case 1: // Y-axis
                        skewFactor = relativePos.y / (boundsSize.y * 0.5f);
                        break;
                    case 2: // Z-axis
                        skewFactor = relativePos.z / (boundsSize.z * 0.5f);
                        break;
                }

                // Apply skew transformation
                var skewOffset = skewDirection * (skewFactor * math.tan(angle) * math.length(boundsSize) * 0.1f);
                vertex.position += skewOffset;
                meshData.vertices[i] = vertex;
            }
        }
    }
}
