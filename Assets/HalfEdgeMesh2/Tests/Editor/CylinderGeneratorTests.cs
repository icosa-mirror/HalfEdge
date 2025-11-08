using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using HalfEdgeMesh2.Generators;

namespace HalfEdgeMesh2.Tests
{
    [TestFixture]
    public class CylinderGeneratorTests
    {
        [Test]
        public void Generate_SimpleCylinder_CreatesValidMesh()
        {
            var radius = 1f;
            var height = 2f;
            var segments = new int2(8, 1);

            var mesh = Cylinder.Generate(radius, height, segments, true, Allocator.Temp);
            try
            {
                // 8 vertices per ring * 2 rings + 2 cap centers = 18 vertices
                Assert.AreEqual(18, mesh.vertexCount, "Simple cylinder should have 18 vertices");

                // 8 side faces + 8 bottom cap + 8 top cap = 24 faces
                Assert.AreEqual(24, mesh.faceCount, "Simple cylinder should have 24 faces");

                Assert.Greater(mesh.halfEdgeCount, 0, "Should have half-edges");
                Assert.IsTrue(MeshOperations.ValidateMesh(ref mesh), "Generated mesh should be valid");
            }
            finally
            {
                mesh.Dispose();
            }
        }

        [Test]
        public void Generate_UncappedCylinder_HasNoCapFaces()
        {
            var radius = 1f;
            var height = 2f;
            var segments = new int2(6, 1);

            var mesh = Cylinder.Generate(radius, height, segments, false, Allocator.Temp);
            try
            {
                // 6 vertices per ring * 2 rings = 12 vertices (no cap centers)
                Assert.AreEqual(12, mesh.vertexCount, "Uncapped cylinder should have 12 vertices");

                // Only 6 side faces
                Assert.AreEqual(6, mesh.faceCount, "Uncapped cylinder should have 6 side faces");

                Assert.IsTrue(MeshOperations.ValidateMesh(ref mesh), "Generated mesh should be valid");
            }
            finally
            {
                mesh.Dispose();
            }
        }

        [Test]
        public void Generate_SubdividedCylinder_CreatesCorrectTopology()
        {
            var radius = 1f;
            var height = 3f;
            var segments = new int2(12, 4);

            var mesh = Cylinder.Generate(radius, height, segments, true, Allocator.Temp);
            try
            {
                // 12 vertices per ring * 5 rings + 2 cap centers = 62 vertices
                Assert.AreEqual(62, mesh.vertexCount, "Subdivided cylinder should have 62 vertices");

                // 12 radial * 4 height = 48 side faces + 12 bottom + 12 top = 72 faces
                Assert.AreEqual(72, mesh.faceCount, "Should have 72 faces with subdivisions");

                Assert.IsTrue(MeshOperations.ValidateMesh(ref mesh), "Generated mesh should be valid");
            }
            finally
            {
                mesh.Dispose();
            }
        }

        [Test]
        public void Generate_MinimumSegments_ClampsToValidValues()
        {
            var radius = 1f;
            var height = 2f;
            var segments = new int2(2, 0); // Invalid segments

            var mesh = Cylinder.Generate(radius, height, segments, true, Allocator.Temp);
            try
            {
                // Should clamp to minimum of 3 radial segments and 1 height segment
                // 3 vertices per ring * 2 rings + 2 cap centers = 8 vertices
                Assert.AreEqual(8, mesh.vertexCount, "Should have 8 vertices with minimum segments");

                // 3 side faces + 3 bottom + 3 top = 9 faces
                Assert.AreEqual(9, mesh.faceCount, "Should create cylinder with minimum segments");

                Assert.IsTrue(MeshOperations.ValidateMesh(ref mesh), "Generated mesh should be valid");
            }
            finally
            {
                mesh.Dispose();
            }
        }

        [Test]
        public void Generate_CylinderVertices_AreOnCircumference()
        {
            var radius = 2f;
            var height = 4f;
            var segments = new int2(16, 2);

            var mesh = Cylinder.Generate(radius, height, segments, true, Allocator.Temp);
            try
            {
                var tolerance = 0.001f;
                var halfHeight = height * 0.5f;

                // Check vertices (excluding cap centers)
                var ringVertexCount = (segments.y + 1) * segments.x;
                for (var i = 0; i < ringVertexCount; i++)
                {
                    var vertex = mesh.vertices[i].position;

                    // Check radial distance (XY plane)
                    var radialDistance = math.sqrt(vertex.x * vertex.x + vertex.y * vertex.y);
                    Assert.AreEqual(radius, radialDistance, tolerance, $"Vertex {i} should be at radius {radius}");

                    // Check height bounds
                    Assert.LessOrEqual(math.abs(vertex.z), halfHeight + tolerance, "Z coordinate should be within height bounds");
                }

                // Check cap centers
                var bottomCenter = mesh.vertices[ringVertexCount].position;
                var topCenter = mesh.vertices[ringVertexCount + 1].position;

                Assert.AreEqual(0f, bottomCenter.x, tolerance, "Bottom center X should be 0");
                Assert.AreEqual(0f, bottomCenter.y, tolerance, "Bottom center Y should be 0");
                Assert.AreEqual(-halfHeight, bottomCenter.z, tolerance, "Bottom center Z should be -halfHeight");

                Assert.AreEqual(0f, topCenter.x, tolerance, "Top center X should be 0");
                Assert.AreEqual(0f, topCenter.y, tolerance, "Top center Y should be 0");
                Assert.AreEqual(halfHeight, topCenter.z, tolerance, "Top center Z should be halfHeight");
            }
            finally
            {
                mesh.Dispose();
            }
        }

        [Test]
        public void Generate_CylinderBounds_MatchSpecifications()
        {
            var radius = 1.5f;
            var height = 3f;
            var segments = new int2(8, 2);

            var mesh = Cylinder.Generate(radius, height, segments, true, Allocator.Temp);
            try
            {
                MeshOperations.ComputeBounds(ref mesh, out var boundsCenter, out var boundsSize);

                Assert.AreEqual(0f, math.length(boundsCenter), 0.001f, "Cylinder should be centered at origin");

                // Bounds size should be diameter x diameter x height
                var expectedSize = new float3(radius * 2f, radius * 2f, height);
                Assert.AreEqual(0f, math.length(expectedSize - boundsSize), 0.001f, "Bounds should match cylinder dimensions");
            }
            finally
            {
                mesh.Dispose();
            }
        }

        [Test]
        public void Generate_SideFaces_AreQuads()
        {
            var radius = 1f;
            var height = 2f;
            var segments = new int2(6, 2);

            var mesh = Cylinder.Generate(radius, height, segments, false, Allocator.Temp);
            try
            {
                // All side faces should be quads (4 vertices)
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

                    Assert.AreEqual(4, vertexCount, $"Side face {faceIndex} should be a quad");
                }
            }
            finally
            {
                mesh.Dispose();
            }
        }

        [Test]
        public void Generate_CapFaces_AreTriangles()
        {
            var radius = 1f;
            var height = 2f;
            var segments = new int2(8, 1);

            var mesh = Cylinder.Generate(radius, height, segments, true, Allocator.Temp);
            try
            {
                // Side faces: 8, then cap faces start
                // Cap faces should be triangles (3 vertices)
                var sideFaceCount = segments.x * segments.y;
                var totalFaceCount = mesh.faceCount;

                for (var faceIndex = sideFaceCount; faceIndex < totalFaceCount; faceIndex++)
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

                    Assert.AreEqual(3, vertexCount, $"Cap face {faceIndex} should be a triangle");
                }
            }
            finally
            {
                mesh.Dispose();
            }
        }

        [Test]
        public void Generate_DifferentRadii_ScalesCorrectly()
        {
            var radius1 = 1f;
            var radius2 = 3f;
            var height = 2f;
            var segments = new int2(8, 1);

            var mesh1 = Cylinder.Generate(radius1, height, segments, false, Allocator.Temp);
            var mesh2 = Cylinder.Generate(radius2, height, segments, false, Allocator.Temp);

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
    }
}
