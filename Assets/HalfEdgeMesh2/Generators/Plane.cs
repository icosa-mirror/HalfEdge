using Unity.Collections;
using Unity.Mathematics;

namespace HalfEdgeMesh2.Generators
{
    public static class Plane
    {
        public static MeshData Generate(float2 size, int2 segments, Allocator allocator)
        {
            var clampedSegments = math.max(segments, 1);

            // Estimate capacity
            var vertexCount = (clampedSegments.x + 1) * (clampedSegments.y + 1);
            var faceCount = clampedSegments.x * clampedSegments.y;
            var edgeCount = faceCount * 2;

            var builder = new MeshBuilder(Allocator.TempJob, edgeCount);
            var vertexGrid = CreateVertexGrid(ref builder, size, clampedSegments);

            CreateFaces(ref builder, vertexGrid, clampedSegments);

            var result = builder.Build(allocator);
            builder.Dispose();
            vertexGrid.Dispose();

            return result;
        }

        struct VertexGrid : System.IDisposable
        {
            public NativeArray<int> vertices;
            public int width;

            public VertexGrid(int width, int height, Allocator allocator)
            {
                this.width = width;
                vertices = new NativeArray<int>(width * height, allocator);
            }

            public int GetVertex(int x, int y) => vertices[y * width + x];
            public void SetVertex(int x, int y, int vertexIndex) => vertices[y * width + x] = vertexIndex;
            public void Dispose() => vertices.Dispose();
        }

        static VertexGrid CreateVertexGrid(ref MeshBuilder builder, float2 size, int2 segments)
        {
            var grid = new VertexGrid(segments.x + 1, segments.y + 1, Allocator.TempJob);
            var halfSize = size * 0.5f;
            var widthStep = size.x / segments.x;
            var heightStep = size.y / segments.y;

            for (var y = 0; y <= segments.y; y++)
            {
                for (var x = 0; x <= segments.x; x++)
                {
                    var position = new float3(
                        x * widthStep - halfSize.x,
                        y * heightStep - halfSize.y,
                        0
                    );

                    var uv = new float2(
                        x / (float)segments.x,
                        y / (float)segments.y
                    );

                    var vertexIndex = builder.AddVertex(position, uv);
                    grid.SetVertex(x, y, vertexIndex);
                }
            }

            return grid;
        }

        static void CreateFaces(ref MeshBuilder builder, VertexGrid grid, int2 segments)
        {
            for (var y = 0; y < segments.y; y++)
            {
                for (var x = 0; x < segments.x; x++)
                {
                    var i0 = grid.GetVertex(x, y);
                    var i1 = grid.GetVertex(x + 1, y);
                    var i2 = grid.GetVertex(x + 1, y + 1);
                    var i3 = grid.GetVertex(x, y + 1);

                    builder.AddFace(i3, i0, i1, i2);
                }
            }
        }
    }
}
