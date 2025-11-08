using Unity.Collections;
using Unity.Mathematics;

namespace HalfEdgeMesh2.Generators
{
    public static class Cylinder
    {
        public static MeshData Generate(float radius, float height, int2 segments, bool capped, Allocator allocator)
        {
            var clampedSegments = math.max(segments, new int2(3, 1));
            var radialSegments = clampedSegments.x;
            var heightSegments = clampedSegments.y;

            // Estimate capacity
            var vertexCount = (heightSegments + 1) * radialSegments + (capped ? 2 : 0);
            var faceCount = heightSegments * radialSegments + (capped ? radialSegments * 2 : 0);
            var edgeCount = faceCount * 2; // Rough estimate

            var builder = new MeshBuilder(Allocator.TempJob, edgeCount);

            var vertexRings = CreateVertexRings(ref builder, radius, height, radialSegments, heightSegments);
            CreateSideFaces(ref builder, vertexRings, radialSegments, heightSegments);

            if (capped)
            {
                var halfHeight = height * 0.5f;
                var bottomCenter = builder.AddVertex(new float3(0, 0, -halfHeight));
                var topCenter = builder.AddVertex(new float3(0, 0, halfHeight));

                CreateBottomCap(ref builder, vertexRings, radialSegments, bottomCenter);
                CreateTopCap(ref builder, vertexRings, radialSegments, heightSegments, topCenter);
            }

            var result = builder.Build(allocator);
            builder.Dispose();
            vertexRings.Dispose();

            return result;
        }

        struct VertexRings : System.IDisposable
        {
            public NativeArray<int> vertices;
            public int radialSegments;

            public VertexRings(int heightSegments, int radialSegments, Allocator allocator)
            {
                this.radialSegments = radialSegments;
                vertices = new NativeArray<int>((heightSegments + 1) * radialSegments, allocator);
            }

            public int GetVertex(int h, int i) => vertices[h * radialSegments + i];
            public void SetVertex(int h, int i, int vertexIndex) => vertices[h * radialSegments + i] = vertexIndex;
            public void Dispose() => vertices.Dispose();
        }

        static VertexRings CreateVertexRings(ref MeshBuilder builder, float radius, float height, int radialSegments, int heightSegments)
        {
            var rings = new VertexRings(heightSegments, radialSegments, Allocator.TempJob);
            var halfHeight = height * 0.5f;
            var angleStep = math.PI * 2f / radialSegments;
            var heightStep = height / heightSegments;

            // Generate vertices in rings (Z-axis as vertical)
            for (var h = 0; h <= heightSegments; h++)
            {
                var z = -halfHeight + h * heightStep;
                for (var i = 0; i < radialSegments; i++)
                {
                    var angle = i * angleStep;
                    var x = math.cos(angle) * radius;
                    var y = math.sin(angle) * radius;

                    var vertexIndex = builder.AddVertex(new float3(x, y, z));
                    rings.SetVertex(h, i, vertexIndex);
                }
            }

            return rings;
        }

        static void CreateSideFaces(ref MeshBuilder builder, VertexRings rings, int radialSegments, int heightSegments)
        {
            for (var h = 0; h < heightSegments; h++)
            {
                for (var i = 0; i < radialSegments; i++)
                {
                    var i0 = rings.GetVertex(h, i);
                    var i1 = rings.GetVertex(h + 1, i);
                    var i2 = rings.GetVertex(h + 1, (i + 1) % radialSegments);
                    var i3 = rings.GetVertex(h, (i + 1) % radialSegments);

                    builder.AddFace(i0, i3, i2, i1);
                }
            }
        }

        static void CreateBottomCap(ref MeshBuilder builder, VertexRings rings, int radialSegments, int centerIndex)
        {
            for (var i = 0; i < radialSegments; i++)
            {
                var i0 = rings.GetVertex(0, i);
                var i1 = rings.GetVertex(0, (i + 1) % radialSegments);
                builder.AddFace(centerIndex, i1, i0);
            }
        }

        static void CreateTopCap(ref MeshBuilder builder, VertexRings rings, int radialSegments, int heightSegments, int centerIndex)
        {
            for (var i = 0; i < radialSegments; i++)
            {
                var i0 = rings.GetVertex(heightSegments, i);
                var i1 = rings.GetVertex(heightSegments, (i + 1) % radialSegments);
                builder.AddFace(centerIndex, i0, i1);
            }
        }
    }
}
