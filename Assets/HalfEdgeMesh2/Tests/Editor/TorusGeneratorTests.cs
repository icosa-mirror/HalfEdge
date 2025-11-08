using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using HalfEdgeMesh2.Generators;

namespace HalfEdgeMesh2.Tests
{
    [TestFixture]
    public class TorusGeneratorTests
    {
        [Test]
        public void Generate_SimpleTorus_CreatesValidMesh()
        {
            var majorRadius = 2f;
            var minorRadius = 0.5f;
            var segments = new int2(8, 6);

            var mesh = Torus.Generate(majorRadius, minorRadius, segments, Allocator.Temp);
            try
            {
                // 8 * 6 = 48 vertices
                Assert.AreEqual(48, mesh.vertexCount, "Simple torus should have 48 vertices");

                // 8 * 6 = 48 quad faces
                Assert.AreEqual(48, mesh.faceCount, "Simple torus should have 48 faces");

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
            var majorRadius = 3f;
            var minorRadius = 1f;
            var segments = new int2(16, 12);

            var mesh = Torus.Generate(majorRadius, minorRadius, segments, Allocator.Temp);
            try
            {
                // 16 * 12 = 192 vertices
                Assert.AreEqual(192, mesh.vertexCount, "Should have 192 vertices");

                // 16 * 12 = 192 faces
                Assert.AreEqual(192, mesh.faceCount, "Should have 192 faces");

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
            var majorRadius = 2f;
            var minorRadius = 0.5f;
            var segments = new int2(2, 1); // Invalid segments

            var mesh = Torus.Generate(majorRadius, minorRadius, segments, Allocator.Temp);
            try
            {
                // Should clamp to 3x3 minimum
                // 3 * 3 = 9 vertices
                Assert.AreEqual(9, mesh.vertexCount, "Should have 9 vertices with minimum segments");

                // 3 * 3 = 9 faces
                Assert.AreEqual(9, mesh.faceCount, "Should have 9 faces with minimum segments");

                Assert.IsTrue(MeshOperations.ValidateMesh(ref mesh), "Generated mesh should be valid");
            }
            finally
            {
                mesh.Dispose();
            }
        }

        [Test]
        public void Generate_TorusVertices_AreCorrectDistance()
        {
            var majorRadius = 3f;
            var minorRadius = 1f;
            var segments = new int2(12, 8);

            var mesh = Torus.Generate(majorRadius, minorRadius, segments, Allocator.Temp);
            try
            {
                var tolerance = 0.001f;

                // Each vertex should be at distance between (majorRadius - minorRadius) and (majorRadius + minorRadius)
                // from the Z axis
                for (var i = 0; i < mesh.vertexCount; i++)
                {
                    var vertex = mesh.vertices[i].position;
                    var radialDistance = math.sqrt(vertex.x * vertex.x + vertex.y * vertex.y);

                    Assert.GreaterOrEqual(radialDistance, majorRadius - minorRadius - tolerance,
                        $"Vertex {i} should be at least {majorRadius - minorRadius} from Z axis");
                    Assert.LessOrEqual(radialDistance, majorRadius + minorRadius + tolerance,
                        $"Vertex {i} should be at most {majorRadius + minorRadius} from Z axis");
                }
            }
            finally
            {
                mesh.Dispose();
            }
        }

        [Test]
        public void Generate_TorusBounds_MatchSpecifications()
        {
            var majorRadius = 2f;
            var minorRadius = 0.5f;
            var segments = new int2(12, 8);

            var mesh = Torus.Generate(majorRadius, minorRadius, segments, Allocator.Temp);
            try
            {
                MeshOperations.ComputeBounds(ref mesh, out var boundsCenter, out var boundsSize);

                Assert.AreEqual(0f, math.length(boundsCenter), 0.001f, "Torus should be centered at origin");

                // Bounds should be (majorRadius + minorRadius) * 2 in XY, minorRadius * 2 in Z
                var expectedXY = (majorRadius + minorRadius) * 2f;
                var expectedZ = minorRadius * 2f;

                Assert.AreEqual(expectedXY, boundsSize.x, 0.001f, "Bounds X should match torus outer diameter");
                Assert.AreEqual(expectedXY, boundsSize.y, 0.001f, "Bounds Y should match torus outer diameter");
                Assert.AreEqual(expectedZ, boundsSize.z, 0.001f, "Bounds Z should match tube diameter");
            }
            finally
            {
                mesh.Dispose();
            }
        }

        [Test]
        public void Generate_AllFaces_AreQuads()
        {
            var majorRadius = 2f;
            var minorRadius = 0.5f;
            var segments = new int2(8, 6);

            var mesh = Torus.Generate(majorRadius, minorRadius, segments, Allocator.Temp);
            try
            {
                // All faces should be quads
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
        public void Generate_DifferentMajorRadii_ScaleCorrectly()
        {
            var majorRadius1 = 2f;
            var majorRadius2 = 4f;
            var minorRadius = 0.5f;
            var segments = new int2(8, 6);

            var mesh1 = Torus.Generate(majorRadius1, minorRadius, segments, Allocator.Temp);
            var mesh2 = Torus.Generate(majorRadius2, minorRadius, segments, Allocator.Temp);

            try
            {
                Assert.AreEqual(mesh1.vertexCount, mesh2.vertexCount, "Same segments should produce same vertex count");
                Assert.AreEqual(mesh1.faceCount, mesh2.faceCount, "Same segments should produce same face count");

                MeshOperations.ComputeBounds(ref mesh1, out var bounds1Center, out var bounds1Size);
                MeshOperations.ComputeBounds(ref mesh2, out var bounds2Center, out var bounds2Size);

                // XY bounds should scale with major radius change
                var expectedXYRatio = (majorRadius2 + minorRadius) / (majorRadius1 + minorRadius);
                var actualXYRatio = bounds2Size.x / bounds1Size.x;

                Assert.AreEqual(expectedXYRatio, actualXYRatio, 0.001f, "XY bounds should scale with major radius");

                // Z bounds should stay the same (depends on minor radius only)
                Assert.AreEqual(bounds1Size.z, bounds2Size.z, 0.001f, "Z bounds should be same (same minor radius)");
            }
            finally
            {
                mesh1.Dispose();
                mesh2.Dispose();
            }
        }

        [Test]
        public void Generate_DifferentMinorRadii_ScaleCorrectly()
        {
            var majorRadius = 2f;
            var minorRadius1 = 0.5f;
            var minorRadius2 = 1f;
            var segments = new int2(8, 6);

            var mesh1 = Torus.Generate(majorRadius, minorRadius1, segments, Allocator.Temp);
            var mesh2 = Torus.Generate(majorRadius, minorRadius2, segments, Allocator.Temp);

            try
            {
                MeshOperations.ComputeBounds(ref mesh1, out var bounds1Center, out var bounds1Size);
                MeshOperations.ComputeBounds(ref mesh2, out var bounds2Center, out var bounds2Size);

                var expectedZRatio = minorRadius2 / minorRadius1;
                var actualZRatio = bounds2Size.z / bounds1Size.z;

                Assert.AreEqual(expectedZRatio, actualZRatio, 0.001f, "Z bounds should scale with minor radius");
            }
            finally
            {
                mesh1.Dispose();
                mesh2.Dispose();
            }
        }

        [Test]
        public void Generate_NonSquareSegments_CreatesCorrectCount()
        {
            var majorRadius = 2f;
            var minorRadius = 0.5f;
            var segments = new int2(16, 8);

            var mesh = Torus.Generate(majorRadius, minorRadius, segments, Allocator.Temp);
            try
            {
                // 16 * 8 = 128 vertices and faces
                Assert.AreEqual(128, mesh.vertexCount, "Should have 128 vertices");
                Assert.AreEqual(128, mesh.faceCount, "Should have 128 faces");

                Assert.IsTrue(MeshOperations.ValidateMesh(ref mesh), "Generated mesh should be valid");
            }
            finally
            {
                mesh.Dispose();
            }
        }
    }
}
