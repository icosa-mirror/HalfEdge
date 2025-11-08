using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using HalfEdgeMesh2.Generators;

namespace HalfEdgeMesh2.Tests
{
    [TestFixture]
    public class PlaneGeneratorTests
    {
        [Test]
        public void Generate_SimplePlane_CreatesValidMesh()
        {
            var size = new float2(2, 2);
            var segments = new int2(1, 1);

            var mesh = Plane.Generate(size, segments, Allocator.Temp);
            try
            {
                Assert.AreEqual(4, mesh.vertexCount, "Simple plane should have 4 vertices");
                Assert.AreEqual(1, mesh.faceCount, "Simple plane should have 1 face");
                Assert.Greater(mesh.halfEdgeCount, 0, "Should have half-edges");
                Assert.IsTrue(MeshOperations.ValidateMesh(ref mesh), "Generated mesh should be valid");
            }
            finally
            {
                mesh.Dispose();
            }
        }

        [Test]
        public void Generate_SubdividedPlane_CreatesCorrectTopology()
        {
            var size = new float2(4, 4);
            var segments = new int2(4, 4);

            var mesh = Plane.Generate(size, segments, Allocator.Temp);
            try
            {
                // (4+1) * (4+1) = 25 vertices
                Assert.AreEqual(25, mesh.vertexCount, "Should have 25 vertices");

                // 4 * 4 = 16 faces
                Assert.AreEqual(16, mesh.faceCount, "Should have 16 faces with 4x4 segments");

                Assert.IsTrue(MeshOperations.ValidateMesh(ref mesh), "Generated mesh should be valid");
            }
            finally
            {
                mesh.Dispose();
            }
        }

        [Test]
        public void Generate_NonSquareSegments_CreatesCorrectCount()
        {
            var size = new float2(3, 2);
            var segments = new int2(6, 3);

            var mesh = Plane.Generate(size, segments, Allocator.Temp);
            try
            {
                // (6+1) * (3+1) = 28 vertices
                Assert.AreEqual(28, mesh.vertexCount, "Should have 28 vertices");

                // 6 * 3 = 18 faces
                Assert.AreEqual(18, mesh.faceCount, "Should have 18 faces");

                Assert.IsTrue(MeshOperations.ValidateMesh(ref mesh), "Generated mesh should be valid");
            }
            finally
            {
                mesh.Dispose();
            }
        }

        [Test]
        public void Generate_MinimumSegments_ClampsToOne()
        {
            var size = new float2(2, 2);
            var segments = new int2(0, -1);

            var mesh = Plane.Generate(size, segments, Allocator.Temp);
            try
            {
                // Should clamp to minimum of 1 segment per axis
                Assert.AreEqual(4, mesh.vertexCount, "Should have 4 vertices with minimum segments");
                Assert.AreEqual(1, mesh.faceCount, "Should have 1 face with minimum segments");
                Assert.IsTrue(MeshOperations.ValidateMesh(ref mesh), "Generated mesh should be valid");
            }
            finally
            {
                mesh.Dispose();
            }
        }

        [Test]
        public void Generate_PlaneVertices_AreInXYPlane()
        {
            var size = new float2(4, 3);
            var segments = new int2(4, 3);

            var mesh = Plane.Generate(size, segments, Allocator.Temp);
            try
            {
                var tolerance = 0.001f;

                // All vertices should have Z = 0
                for (var i = 0; i < mesh.vertexCount; i++)
                {
                    var vertex = mesh.vertices[i].position;
                    Assert.AreEqual(0f, vertex.z, tolerance, $"Vertex {i} should be in XY plane (z=0)");
                }
            }
            finally
            {
                mesh.Dispose();
            }
        }

        [Test]
        public void Generate_PlaneVertices_AreWithinBounds()
        {
            var size = new float2(6, 4);
            var segments = new int2(3, 2);

            var mesh = Plane.Generate(size, segments, Allocator.Temp);
            try
            {
                var halfSize = size * 0.5f;
                var tolerance = 0.001f;

                for (var i = 0; i < mesh.vertexCount; i++)
                {
                    var vertex = mesh.vertices[i].position;
                    Assert.LessOrEqual(math.abs(vertex.x), halfSize.x + tolerance, "X coordinate should be within bounds");
                    Assert.LessOrEqual(math.abs(vertex.y), halfSize.y + tolerance, "Y coordinate should be within bounds");
                }
            }
            finally
            {
                mesh.Dispose();
            }
        }

        [Test]
        public void Generate_PlaneBounds_MatchSpecifications()
        {
            var size = new float2(8, 6);
            var segments = new int2(4, 3);

            var mesh = Plane.Generate(size, segments, Allocator.Temp);
            try
            {
                MeshOperations.ComputeBounds(ref mesh, out var boundsCenter, out var boundsSize);

                Assert.AreEqual(0f, math.length(boundsCenter), 0.001f, "Plane should be centered at origin");

                var expectedSize = new float3(size.x, size.y, 0);
                Assert.AreEqual(0f, math.length(expectedSize - boundsSize), 0.001f, "Bounds should match plane dimensions");
            }
            finally
            {
                mesh.Dispose();
            }
        }

        [Test]
        public void Generate_AllFaces_AreQuads()
        {
            var size = new float2(4, 4);
            var segments = new int2(3, 3);

            var mesh = Plane.Generate(size, segments, Allocator.Temp);
            try
            {
                // All faces should be quads (4 vertices)
                for (var faceIndex = 0; faceIndex < mesh.faceCount; faceIndex++)
                {
                    var face = mesh.faces[faceIndex];
                    var vertexCount = 0;
                    var currentHe = face.halfEdge;
                    var startHe = currentHe;

                    do
                    {
                        vertexCount++;
                        var he = mesh.halfEdges[currentHe];
                        currentHe = he.next;
                    } while (currentHe != startHe);

                    Assert.AreEqual(4, vertexCount, $"Face {faceIndex} should be a quad");
                }
            }
            finally
            {
                mesh.Dispose();
            }
        }

        [Test]
        public void Generate_PlaneNormals_PointUpward()
        {
            var size = new float2(2, 2);
            var segments = new int2(2, 2);

            var mesh = Plane.Generate(size, segments, Allocator.Temp);
            try
            {
                var normals = new NativeArray<float3>(mesh.faceCount, Allocator.Temp);
                MeshOperations.ComputeFaceNormals(ref mesh, ref normals);

                var expectedNormal = new float3(0, 0, 1);
                var tolerance = 0.001f;

                for (var i = 0; i < normals.Length; i++)
                {
                    var normal = normals[i];
                    Assert.AreEqual(0f, math.length(expectedNormal - normal), tolerance,
                        $"Face {i} normal should point in +Z direction");
                }

                normals.Dispose();
            }
            finally
            {
                mesh.Dispose();
            }
        }

        [Test]
        public void Generate_DifferentSizes_ScaleCorrectly()
        {
            var size1 = new float2(2, 2);
            var size2 = new float2(6, 4);
            var segments = new int2(2, 2);

            var mesh1 = Plane.Generate(size1, segments, Allocator.Temp);
            var mesh2 = Plane.Generate(size2, segments, Allocator.Temp);

            try
            {
                Assert.AreEqual(mesh1.vertexCount, mesh2.vertexCount, "Same segments should produce same vertex count");
                Assert.AreEqual(mesh1.faceCount, mesh2.faceCount, "Same segments should produce same face count");

                MeshOperations.ComputeBounds(ref mesh1, out var bounds1Center, out var bounds1Size);
                MeshOperations.ComputeBounds(ref mesh2, out var bounds2Center, out var bounds2Size);

                Assert.AreEqual(0f, math.length(size1.x - bounds1Size.x), 0.001f, "Bounds X should match size");
                Assert.AreEqual(0f, math.length(size1.y - bounds1Size.y), 0.001f, "Bounds Y should match size");

                Assert.AreEqual(0f, math.length(size2.x - bounds2Size.x), 0.001f, "Bounds X should match size");
                Assert.AreEqual(0f, math.length(size2.y - bounds2Size.y), 0.001f, "Bounds Y should match size");
            }
            finally
            {
                mesh1.Dispose();
                mesh2.Dispose();
            }
        }
    }
}
