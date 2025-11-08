using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using HalfEdgeMesh2.Generators;

namespace HalfEdgeMesh2.Tests
{
    public class ExtrusionGeneratorTests
    {
        [Test]
        public void Generate_SquareProfile_CreatesValidMesh()
        {
            var profile = new float3[]
            {
                new float3(-0.5f, 0, -0.5f),
                new float3(0.5f, 0, -0.5f),
                new float3(0.5f, 0, 0.5f),
                new float3(-0.5f, 0, 0.5f)
            };

            var mesh = Extrusion.Generate(profile, 2.0f, false, Allocator.Persistent);

            Assert.AreEqual(8, mesh.vertexCount);  // 4 profile points * 2 layers
            Assert.AreEqual(4, mesh.faceCount);    // 4 side faces (no caps)

            mesh.Dispose();
        }

        [Test]
        public void Generate_WithCaps_CreatesValidMesh()
        {
            var profile = new float3[]
            {
                new float3(-0.5f, 0, -0.5f),
                new float3(0.5f, 0, -0.5f),
                new float3(0.5f, 0, 0.5f),
                new float3(-0.5f, 0, 0.5f)
            };

            var mesh = Extrusion.Generate(profile, 2.0f, true, Allocator.Persistent);

            Assert.AreEqual(8, mesh.vertexCount);  // 4 profile points * 2 layers
            Assert.AreEqual(6, mesh.faceCount);    // 4 side faces + 2 caps

            mesh.Dispose();
        }

        [Test]
        public void Generate_TriangleProfile_CreatesValidMesh()
        {
            var profile = new float3[]
            {
                new float3(0, 0, 0),
                new float3(1, 0, 0),
                new float3(0.5f, 0, 1)
            };

            var mesh = Extrusion.Generate(profile, 1.5f, false, Allocator.Persistent);

            Assert.AreEqual(6, mesh.vertexCount);  // 3 profile points * 2 layers
            Assert.AreEqual(3, mesh.faceCount);    // 3 side faces

            mesh.Dispose();
        }

        [Test]
        public void Generate_HexagonProfile_CreatesValidMesh()
        {
            var profile = new float3[6];
            var angleStep = (2.0f * math.PI) / 6;
            for (var i = 0; i < 6; i++)
            {
                var angle = i * angleStep;
                profile[i] = new float3(math.cos(angle), 0, math.sin(angle));
            }

            var mesh = Extrusion.Generate(profile, 1.0f, true, Allocator.Persistent);

            Assert.AreEqual(12, mesh.vertexCount); // 6 points * 2 layers
            Assert.AreEqual(8, mesh.faceCount);    // 6 side faces + 2 caps

            mesh.Dispose();
        }

        [Test]
        public void Generate_VerifiesBottomLayerAtZero()
        {
            var profile = new float3[]
            {
                new float3(0, 0, 0),
                new float3(1, 0, 0),
                new float3(0, 0, 1)
            };

            var mesh = Extrusion.Generate(profile, 2.0f, false, Allocator.Persistent);

            // Bottom vertices should have Y=0
            for (var i = 0; i < 3; i++)
            {
                Assert.AreEqual(0.0f, mesh.vertices[i].position.y, 0.0001f);
            }

            mesh.Dispose();
        }

        [Test]
        public void Generate_VerifiesTopLayerAtHeight()
        {
            var profile = new float3[]
            {
                new float3(0, 0, 0),
                new float3(1, 0, 0),
                new float3(0, 0, 1)
            };

            var height = 3.5f;
            var mesh = Extrusion.Generate(profile, height, false, Allocator.Persistent);

            // Top vertices should have Y=height
            for (var i = 3; i < 6; i++)
            {
                Assert.AreEqual(height, mesh.vertices[i].position.y, 0.0001f);
            }

            mesh.Dispose();
        }

        [Test]
        public void Generate_PreservesXZCoordinates()
        {
            var profile = new float3[]
            {
                new float3(1.5f, 0, 2.5f),
                new float3(3.5f, 0, 4.5f),
                new float3(5.5f, 0, 6.5f)
            };

            var mesh = Extrusion.Generate(profile, 1.0f, false, Allocator.Persistent);

            // Check bottom layer
            for (var i = 0; i < 3; i++)
            {
                Assert.AreEqual(profile[i].x, mesh.vertices[i].position.x, 0.0001f);
                Assert.AreEqual(profile[i].z, mesh.vertices[i].position.z, 0.0001f);
            }

            // Check top layer
            for (var i = 0; i < 3; i++)
            {
                Assert.AreEqual(profile[i].x, mesh.vertices[i + 3].position.x, 0.0001f);
                Assert.AreEqual(profile[i].z, mesh.vertices[i + 3].position.z, 0.0001f);
            }

            mesh.Dispose();
        }

        [Test]
        public void Generate_NegativeHeight_CreatesValidMesh()
        {
            var profile = new float3[]
            {
                new float3(0, 0, 0),
                new float3(1, 0, 0),
                new float3(0.5f, 0, 1)
            };

            var mesh = Extrusion.Generate(profile, -2.0f, false, Allocator.Persistent);

            Assert.AreEqual(6, mesh.vertexCount);
            Assert.AreEqual(3, mesh.faceCount);

            // Top layer should be at negative height
            Assert.AreEqual(-2.0f, mesh.vertices[3].position.y, 0.0001f);

            mesh.Dispose();
        }

        [Test]
        public void Generate_LShapeProfile_CreatesValidMesh()
        {
            var profile = new float3[]
            {
                new float3(0, 0, 0),
                new float3(2, 0, 0),
                new float3(2, 0, 1),
                new float3(1, 0, 1),
                new float3(1, 0, 2),
                new float3(0, 0, 2)
            };

            var mesh = Extrusion.Generate(profile, 1.0f, true, Allocator.Persistent);

            Assert.AreEqual(12, mesh.vertexCount); // 6 points * 2 layers
            Assert.AreEqual(8, mesh.faceCount);    // 6 side faces + 2 caps

            mesh.Dispose();
        }

        [Test]
        public void Generate_NativeArrayVersion_CreatesValidMesh()
        {
            using (var profile = new NativeArray<float3>(4, Allocator.Temp))
            {
                profile[0] = new float3(-0.5f, 0, -0.5f);
                profile[1] = new float3(0.5f, 0, -0.5f);
                profile[2] = new float3(0.5f, 0, 0.5f);
                profile[3] = new float3(-0.5f, 0, 0.5f);

                var mesh = Extrusion.Generate(profile, 2.0f, true, Allocator.Persistent);

                Assert.AreEqual(8, mesh.vertexCount);
                Assert.AreEqual(6, mesh.faceCount);

                mesh.Dispose();
            }
        }

        [Test]
        public void Generate_ZeroHeight_CreatesValidMesh()
        {
            var profile = new float3[]
            {
                new float3(0, 0, 0),
                new float3(1, 0, 0),
                new float3(0, 0, 1)
            };

            var mesh = Extrusion.Generate(profile, 0.0f, false, Allocator.Persistent);

            Assert.AreEqual(6, mesh.vertexCount);

            // Both layers at same height
            for (var i = 0; i < 6; i++)
            {
                Assert.AreEqual(0.0f, mesh.vertices[i].position.y, 0.0001f);
            }

            mesh.Dispose();
        }
    }
}
