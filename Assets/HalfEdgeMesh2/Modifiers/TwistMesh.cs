using Unity.Mathematics;

namespace HalfEdgeMesh2.Modifiers
{
    public static class TwistMesh
    {
        public static void Apply(MeshData meshData, float3 axis, float3 center, float angle, float falloffDistance = 0f)
        {
            var normalizedAxis = math.normalize(axis);

            for (var i = 0; i < meshData.vertexCount; i++)
            {
                var vertex = meshData.vertices[i];
                var relativePos = vertex.position - center;
                var axisProjection = math.dot(relativePos, normalizedAxis) * normalizedAxis;
                var perpendicular = relativePos - axisProjection;

                // Use signed distance along the axis to determine twist amount
                var signedDistance = math.dot(relativePos, normalizedAxis);
                var twistAmount = angle;

                if (falloffDistance > 0f)
                {
                    var normalizedDistance = math.abs(signedDistance) / falloffDistance;
                    var falloff = 1f - math.saturate(normalizedDistance);
                    twistAmount *= falloff;
                }

                // Apply twist proportional to distance along the axis
                twistAmount *= signedDistance;

                var rotation = quaternion.AxisAngle(normalizedAxis, twistAmount);
                var rotatedPerpendicular = math.rotate(rotation, perpendicular);

                vertex.position = center + axisProjection + rotatedPerpendicular;
                meshData.vertices[i] = vertex;
            }
        }
    }
}
