using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using HalfEdgeMesh2.Generators;

namespace HalfEdgeMesh2.Tests
{
    [TestFixture]
    public class ConeGeneratorTests
    {
        [Test]
        public void Generate_SimpleCone_CreatesValidMesh()
        {
            var radius = 1f;
            var height = 2f;
            var segments = 6;

            var mesh = Cone.Generate(radius, height, segments, Allocator.Temp);
            try
            {
                // 1 apex + 1 base center + 6 rim = 8 vertices
                Assert.AreEqual(8, mesh.vertexCount, "Simple cone should have 8 vertices");

                // 6 side triangles + 6 base triangles = 12 faces
                Assert.AreEqual(12, mesh.faceCount, "Simple cone should have 12 faces");

                Assert.Greater(mesh.halfEdgeCount, 0, "Should have half-edges");
                Assert.IsTrue(MeshOperations.ValidateMesh(ref mesh), "Generated mesh should be valid");
            }
            finally
            {
                mesh.Dispose();
            }
        }

        [Test]
        public void Generate_HighSegmentCount_CreatesCorrectTopology()
        {
            var radius = 1f;
            var height = 2f;
            var segments = 16;

            var mesh = Cone.Generate(radius, height, segments, Allocator.Temp);
            try
            {
                // 1 apex + 1 base center + 16 rim = 18 vertices
                Assert.AreEqual(18, mesh.vertexCount, "Should have 18 vertices");

                // 16 side + 16 base = 32 faces
                Assert.AreEqual(32, mesh.faceCount, "Should have 32 faces");

                Assert.IsTrue(MeshOperations.ValidateMesh(ref mesh), "Generated mesh should be valid");
            }
            finally
            {
                mesh.Dispose();
            }
        }

        [Test]
        public void Generate_MinimumSegments_ClampsToThree()
        {
            var radius = 1f;
            var height = 2f;
            var segments = 2; // Invalid, should clamp to 3

            var mesh = Cone.Generate(radius, height, segments, Allocator.Temp);
            try
            {
                // 1 apex + 1 base center + 3 rim = 5 vertices
                Assert.AreEqual(5, mesh.vertexCount, "Should have 5 vertices with minimum segments");

                // 3 side + 3 base = 6 faces
                Assert.AreEqual(6, mesh.faceCount, "Should have 6 faces with minimum segments");

                Assert.IsTrue(MeshOperations.ValidateMesh(ref mesh), "Generated mesh should be valid");
            }
            finally
            {
                mesh.Dispose();
            }
        }

        [Test]
        public void Generate_ApexVertex_IsAtTop()
        {
            var radius = 1f;
            var height = 3f;
            var segments = 8;

            var mesh = Cone.Generate(radius, height, segments, Allocator.Temp);
            try
            {
                var halfHeight = height * 0.5f;
                var tolerance = 0.001f;

                // Apex is the first vertex
                var apex = mesh.vertices[0].position;
                Assert.AreEqual(0f, apex.x, tolerance, "Apex X should be 0");
                Assert.AreEqual(0f, apex.y, tolerance, "Apex Y should be 0");
                Assert.AreEqual(halfHeight, apex.z, tolerance, "Apex Z should be at top");
            }
            finally
            {
                mesh.Dispose();
            }
        }

        [Test]
        public void Generate_BaseCenterVertex_IsAtBottom()
        {
            var radius = 1f;
            var height = 3f;
            var segments = 8;

            var mesh = Cone.Generate(radius, height, segments, Allocator.Temp);
            try
            {
                var halfHeight = height * 0.5f;
                var tolerance = 0.001f;

                // Base center is the second vertex
                var baseCenter = mesh.vertices[1].position;
                Assert.AreEqual(0f, baseCenter.x, tolerance, "Base center X should be 0");
                Assert.AreEqual(0f, baseCenter.y, tolerance, "Base center Y should be 0");
                Assert.AreEqual(-halfHeight, baseCenter.z, tolerance, "Base center Z should be at bottom");
            }
            finally
            {
                mesh.Dispose();
            }
        }

        [Test]
        public void Generate_RimVertices_AreOnCircumference()
        {
            var radius = 2f;
            var height = 4f;
            var segments = 12;

            var mesh = Cone.Generate(radius, height, segments, Allocator.Temp);
            try
            {
                var halfHeight = height * 0.5f;
                var tolerance = 0.001f;

                // Rim vertices start at index 2
                for (var i = 2; i < mesh.vertexCount; i++)
                {
                    var vertex = mesh.vertices[i].position;

                    // Check radial distance
                    var radialDistance = math.sqrt(vertex.x * vertex.x + vertex.y * vertex.y);
                    Assert.AreEqual(radius, radialDistance, tolerance, $"Vertex {i} should be at radius {radius}");

                    // Check Z position (should be at base)
                    Assert.AreEqual(-halfHeight, vertex.z, tolerance, $"Vertex {i} should be at base height");
                }
            }
            finally
            {
                mesh.Dispose();
            }
        }

        [Test]
        public void Generate_ConeBounds_MatchSpecifications()
        {
            var radius = 1.5f;
            var height = 3f;
            var segments = 8;

            var mesh = Cone.Generate(radius, height, segments, Allocator.Temp);
            try
            {
                MeshOperations.ComputeBounds(ref mesh, out var boundsCenter, out var boundsSize);

                Assert.AreEqual(0f, math.length(boundsCenter), 0.001f, "Cone should be centered at origin");

                // Bounds should be diameter x diameter x height
                var expectedSize = new float3(radius * 2f, radius * 2f, height);
                Assert.AreEqual(0f, math.length(expectedSize - boundsSize), 0.001f, "Bounds should match cone dimensions");
            }
            finally
            {
                mesh.Dispose();
            }
        }

        [Test]
        public void Generate_AllFaces_AreTriangles()
        {
            var radius = 1f;
            var height = 2f;
            var segments = 8;

            var mesh = Cone.Generate(radius, height, segments, Allocator.Temp);
            try
            {
                // All faces should be triangles
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

                    Assert.AreEqual(3, vertexCount, $"Face {faceIndex} should be a triangle");
                }
            }
            finally
            {
                mesh.Dispose();
            }
        }

        [Test]
        public void Generate_DifferentRadii_ScaleCorrectly()
        {
            var radius1 = 1f;
            var radius2 = 3f;
            var height = 2f;
            var segments = 8;

            var mesh1 = Cone.Generate(radius1, height, segments, Allocator.Temp);
            var mesh2 = Cone.Generate(radius2, height, segments, Allocator.Temp);

            try
            {
                Assert.AreEqual(mesh1.vertexCount, mesh2.vertexCount, "Same segments should produce same vertex count");
                Assert.AreEqual(mesh1.faceCount, mesh2.faceCount, "Same segments should produce same face count");

                MeshOperations.ComputeBounds(ref mesh1, out var bounds1Center, out var bounds1Size);
                MeshOperations.ComputeBounds(ref mesh2, out var bounds2Center, out var bounds2Size);

                var expectedRatio = radius2 / radius1;
                var actualRatio = bounds2Size.x / bounds1Size.x;

                Assert.AreEqual(expectedRatio, actualRatio, 0.001f, "Bounds should scale with radius");
            }
            finally
            {
                mesh1.Dispose();
                mesh2.Dispose();
            }
        }

        [Test]
        public void Generate_DifferentHeights_ScaleCorrectly()
        {
            var radius = 1f;
            var height1 = 2f;
            var height2 = 6f;
            var segments = 8;

            var mesh1 = Cone.Generate(radius, height1, segments, Allocator.Temp);
            var mesh2 = Cone.Generate(radius, height2, segments, Allocator.Temp);

            try
            {
                MeshOperations.ComputeBounds(ref mesh1, out var bounds1Center, out var bounds1Size);
                MeshOperations.ComputeBounds(ref mesh2, out var bounds2Center, out var bounds2Size);

                var expectedRatio = height2 / height1;
                var actualRatio = bounds2Size.z / bounds1Size.z;

                Assert.AreEqual(expectedRatio, actualRatio, 0.001f, "Bounds should scale with height");
            }
            finally
            {
                mesh1.Dispose();
                mesh2.Dispose();
            }
        }
    }
}
