using Unity.Collections;
using Unity.Mathematics;

namespace HalfEdgeMesh2.Generators
{
    public static class Torus
    {
        public static MeshData Generate(float majorRadius, float minorRadius, int2 segments, Allocator allocator)
        {
            var clampedSegments = math.max(segments, 3);

            var vertexCount = clampedSegments.x * clampedSegments.y;
            var faceCount = clampedSegments.x * clampedSegments.y;
            var edgeCount = faceCount * 2;

            var builder = new MeshBuilder(Allocator.TempJob, edgeCount);
            var vertexGrid = CreateVertexGrid(ref builder, majorRadius, minorRadius, clampedSegments);

            CreateFaces(ref builder, vertexGrid, clampedSegments);

            var result = builder.Build(allocator);
            builder.Dispose();
            vertexGrid.Dispose();

            return result;
        }

        struct VertexGrid : System.IDisposable
        {
            public NativeArray<int> vertices;
            public int minorSegments;

            public VertexGrid(int majorSegments, int minorSegments, Allocator allocator)
            {
                this.minorSegments = minorSegments;
                vertices = new NativeArray<int>(majorSegments * minorSegments, allocator);
            }

            public int GetVertex(int i, int j) => vertices[i * minorSegments + j];
            public void SetVertex(int i, int j, int vertexIndex) => vertices[i * minorSegments + j] = vertexIndex;
            public void Dispose() => vertices.Dispose();
        }

        static VertexGrid CreateVertexGrid(ref MeshBuilder builder, float majorRadius, float minorRadius, int2 segments)
        {
            var grid = new VertexGrid(segments.x, segments.y, Allocator.TempJob);

            // Generate vertices (Z-axis as vertical)
            for (var i = 0; i < segments.x; i++)
            {
                var majorAngle = i * math.PI * 2f / segments.x;
                var majorCos = math.cos(majorAngle);
                var majorSin = math.sin(majorAngle);

                for (var j = 0; j < segments.y; j++)
                {
                    var minorAngle = j * math.PI * 2f / segments.y;
                    var minorCos = math.cos(minorAngle);
                    var minorSin = math.sin(minorAngle);

                    var x = (majorRadius + minorRadius * minorCos) * majorCos;
                    var y = (majorRadius + minorRadius * minorCos) * majorSin;
                    var z = minorRadius * minorSin;

                    var vertexIndex = builder.AddVertex(new float3(x, y, z));
                    grid.SetVertex(i, j, vertexIndex);
                }
            }

            return grid;
        }

        static void CreateFaces(ref MeshBuilder builder, VertexGrid grid, int2 segments)
        {
            for (var i = 0; i < segments.x; i++)
            {
                var nextI = (i + 1) % segments.x;

                for (var j = 0; j < segments.y; j++)
                {
                    var nextJ = (j + 1) % segments.y;

                    var i0 = grid.GetVertex(i, j);
                    var i1 = grid.GetVertex(nextI, j);
                    var i2 = grid.GetVertex(nextI, nextJ);
                    var i3 = grid.GetVertex(i, nextJ);

                    builder.AddFace(i3, i0, i1, i2);
                }
            }
        }
    }
}
