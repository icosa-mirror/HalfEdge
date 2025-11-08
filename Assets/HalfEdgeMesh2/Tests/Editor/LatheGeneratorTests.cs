using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using HalfEdgeMesh2.Generators;

namespace HalfEdgeMesh2.Tests
{
    public class LatheGeneratorTests
    {
        [Test]
        public void Generate_SimpleCylinder_CreatesValidMesh()
        {
            var profile = new float2[]
            {
                new float2(0.5f, 0),
                new float2(0.5f, 1)
            };

            var mesh = Lathe.Generate(profile, 8, Allocator.Persistent);

            Assert.AreEqual(16, mesh.vertexCount); // 2 profile points * 8 segments
            Assert.AreEqual(8, mesh.faceCount);    // 1 ring * 8 segments
            Assert.AreEqual(32, mesh.halfEdgeCount); // 8 quads * 4 edges

            mesh.Dispose();
        }

        [Test]
        public void Generate_SimpleCone_CreatesValidMesh()
        {
            var profile = new float2[]
            {
                new float2(0, 0),     // Point at bottom
                new float2(1, 1)      // Wide at top
            };

            var mesh = Lathe.Generate(profile, 12, Allocator.Persistent);

            Assert.AreEqual(24, mesh.vertexCount); // 2 points * 12 segments
            Assert.AreEqual(12, mesh.faceCount);

            mesh.Dispose();
        }

        [Test]
        public void Generate_Bowl_CreatesValidMesh()
        {
            var profile = new float2[]
            {
                new float2(0, 0),
                new float2(0.7f, 0.2f),
                new float2(1, 0.5f),
                new float2(0.9f, 0.8f),
                new float2(0.5f, 1)
            };

            var mesh = Lathe.Generate(profile, 16, Allocator.Persistent);

            Assert.AreEqual(80, mesh.vertexCount);  // 5 points * 16 segments
            Assert.AreEqual(64, mesh.faceCount);    // 4 rings * 16 segments

            mesh.Dispose();
        }

        [Test]
        public void Generate_MinimumSegments_UsesThree()
        {
            var profile = new float2[]
            {
                new float2(0.5f, 0),
                new float2(0.5f, 1)
            };

            var mesh = Lathe.Generate(profile, 2, Allocator.Persistent); // Request 2, should get 3

            Assert.AreEqual(6, mesh.vertexCount); // 2 points * 3 segments (minimum)
            Assert.AreEqual(3, mesh.faceCount);

            mesh.Dispose();
        }

        [Test]
        public void Generate_HighSegmentCount_CreatesValidMesh()
        {
            var profile = new float2[]
            {
                new float2(0.5f, 0),
                new float2(0.5f, 1)
            };

            var mesh = Lathe.Generate(profile, 32, Allocator.Persistent);

            Assert.AreEqual(64, mesh.vertexCount); // 2 points * 32 segments
            Assert.AreEqual(32, mesh.faceCount);

            mesh.Dispose();
        }

        [Test]
        public void Generate_VerifiesCircularSymmetry()
        {
            var profile = new float2[]
            {
                new float2(1, 0),
                new float2(1, 1)
            };

            var mesh = Lathe.Generate(profile, 8, Allocator.Persistent);

            // Check that vertices form circles
            // First ring (bottom)
            for (var i = 0; i < 8; i++)
            {
                var v = mesh.vertices[i * 2];
                var radius = math.length(new float2(v.position.x, v.position.z));
                Assert.AreEqual(1.0f, radius, 0.001f);
                Assert.AreEqual(0.0f, v.position.y, 0.001f);
            }

            // Second ring (top)
            for (var i = 0; i < 8; i++)
            {
                var v = mesh.vertices[i * 2 + 1];
                var radius = math.length(new float2(v.position.x, v.position.z));
                Assert.AreEqual(1.0f, radius, 0.001f);
                Assert.AreEqual(1.0f, v.position.y, 0.001f);
            }

            mesh.Dispose();
        }

        [Test]
        public void Generate_VaryingRadius_CreatesProperShape()
        {
            var profile = new float2[]
            {
                new float2(0.5f, 0),
                new float2(1.0f, 0.5f),
                new float2(0.5f, 1.0f)
            };

            var mesh = Lathe.Generate(profile, 6, Allocator.Persistent);

            // Verify middle ring has larger radius
            for (var i = 0; i < 6; i++)
            {
                var vBottom = mesh.vertices[i * 3];
                var vMiddle = mesh.vertices[i * 3 + 1];
                var vTop = mesh.vertices[i * 3 + 2];

                var radiusBottom = math.length(new float2(vBottom.position.x, vBottom.position.z));
                var radiusMiddle = math.length(new float2(vMiddle.position.x, vMiddle.position.z));
                var radiusTop = math.length(new float2(vTop.position.x, vTop.position.z));

                Assert.AreEqual(0.5f, radiusBottom, 0.001f);
                Assert.AreEqual(1.0f, radiusMiddle, 0.001f);
                Assert.AreEqual(0.5f, radiusTop, 0.001f);
            }

            mesh.Dispose();
        }

        [Test]
        public void Generate_NativeArrayVersion_CreatesValidMesh()
        {
            using (var profile = new NativeArray<float2>(2, Allocator.Temp))
            {
                profile[0] = new float2(0.5f, 0);
                profile[1] = new float2(0.5f, 1);

                var mesh = Lathe.Generate(profile, 8, Allocator.Persistent);

                Assert.AreEqual(16, mesh.vertexCount);
                Assert.AreEqual(8, mesh.faceCount);

                mesh.Dispose();
            }
        }

        [Test]
        public void Generate_ProfileWithZeroRadius_CreatesValidMesh()
        {
            var profile = new float2[]
            {
                new float2(0, 0),     // Point (r=0)
                new float2(1, 0.5f),
                new float2(0, 1)      // Point (r=0)
            };

            var mesh = Lathe.Generate(profile, 8, Allocator.Persistent);

            Assert.AreEqual(24, mesh.vertexCount); // 3 points * 8 segments
            Assert.AreEqual(16, mesh.faceCount);   // 2 rings * 8 segments

            // Verify points at r=0 are all at origin (in XZ plane)
            for (var i = 0; i < 8; i++)
            {
                var vBottom = mesh.vertices[i * 3];
                var vTop = mesh.vertices[i * 3 + 2];

                Assert.AreEqual(0, vBottom.position.x, 0.001f);
                Assert.AreEqual(0, vBottom.position.z, 0.001f);
                Assert.AreEqual(0, vTop.position.x, 0.001f);
                Assert.AreEqual(0, vTop.position.z, 0.001f);
            }

            mesh.Dispose();
        }
    }
}
