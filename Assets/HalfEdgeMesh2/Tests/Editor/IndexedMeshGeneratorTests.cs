using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using HalfEdgeMesh2.Generators;

namespace HalfEdgeMesh2.Tests
{
    public class IndexedMeshGeneratorTests
    {
        [Test]
        public void Generate_Triangle_CreatesValidMesh()
        {
            var vertices = new float3[]
            {
                new float3(0, 0, 0),
                new float3(1, 0, 0),
                new float3(0.5f, 1, 0)
            };

            var faces = new int[][]
            {
                new int[] { 0, 1, 2 }
            };

            var mesh = IndexedMesh.Generate(vertices, faces, Allocator.Persistent);

            Assert.AreEqual(3, mesh.vertexCount);
            Assert.AreEqual(1, mesh.faceCount);
            Assert.AreEqual(3, mesh.halfEdgeCount);

            mesh.Dispose();
        }

        [Test]
        public void Generate_Quad_CreatesValidMesh()
        {
            var vertices = new float3[]
            {
                new float3(0, 0, 0),
                new float3(1, 0, 0),
                new float3(1, 1, 0),
                new float3(0, 1, 0)
            };

            var faces = new int[][]
            {
                new int[] { 0, 1, 2, 3 }
            };

            var mesh = IndexedMesh.Generate(vertices, faces, Allocator.Persistent);

            Assert.AreEqual(4, mesh.vertexCount);
            Assert.AreEqual(1, mesh.faceCount);
            Assert.AreEqual(4, mesh.halfEdgeCount);

            mesh.Dispose();
        }

        [Test]
        public void Generate_Cube_CreatesValidMesh()
        {
            var vertices = new float3[]
            {
                new float3(-1, -1, -1), // 0
                new float3(1, -1, -1),  // 1
                new float3(1, -1, 1),   // 2
                new float3(-1, -1, 1),  // 3
                new float3(-1, 1, -1),  // 4
                new float3(1, 1, -1),   // 5
                new float3(1, 1, 1),    // 6
                new float3(-1, 1, 1)    // 7
            };

            var faces = new int[][]
            {
                new int[] { 0, 1, 2, 3 }, // Bottom
                new int[] { 4, 7, 6, 5 }, // Top
                new int[] { 0, 4, 5, 1 }, // Front
                new int[] { 2, 6, 7, 3 }, // Back
                new int[] { 0, 3, 7, 4 }, // Left
                new int[] { 1, 5, 6, 2 }  // Right
            };

            var mesh = IndexedMesh.Generate(vertices, faces, Allocator.Persistent);

            Assert.AreEqual(8, mesh.vertexCount);
            Assert.AreEqual(6, mesh.faceCount);
            Assert.AreEqual(24, mesh.halfEdgeCount); // 6 faces * 4 edges

            mesh.Dispose();
        }

        [Test]
        public void Generate_Pentagon_CreatesValidMesh()
        {
            var vertices = new float3[5];
            var angleStep = (2.0f * math.PI) / 5;
            for (var i = 0; i < 5; i++)
            {
                var angle = i * angleStep;
                vertices[i] = new float3(math.cos(angle), math.sin(angle), 0);
            }

            var faces = new int[][]
            {
                new int[] { 0, 1, 2, 3, 4 }
            };

            var mesh = IndexedMesh.Generate(vertices, faces, Allocator.Persistent);

            Assert.AreEqual(5, mesh.vertexCount);
            Assert.AreEqual(1, mesh.faceCount);
            Assert.AreEqual(5, mesh.halfEdgeCount);

            mesh.Dispose();
        }

        [Test]
        public void Generate_MultipleFaces_CreatesValidMesh()
        {
            var vertices = new float3[]
            {
                new float3(0, 0, 0),
                new float3(1, 0, 0),
                new float3(2, 0, 0),
                new float3(0, 1, 0),
                new float3(1, 1, 0),
                new float3(2, 1, 0)
            };

            var faces = new int[][]
            {
                new int[] { 0, 1, 4, 3 },
                new int[] { 1, 2, 5, 4 }
            };

            var mesh = IndexedMesh.Generate(vertices, faces, Allocator.Persistent);

            Assert.AreEqual(6, mesh.vertexCount);
            Assert.AreEqual(2, mesh.faceCount);
            Assert.AreEqual(8, mesh.halfEdgeCount); // 2 quads * 4 edges

            mesh.Dispose();
        }

        [Test]
        public void Generate_MixedFaceSizes_CreatesValidMesh()
        {
            var vertices = new float3[]
            {
                new float3(0, 0, 0),
                new float3(1, 0, 0),
                new float3(0.5f, 1, 0),
                new float3(2, 0, 0),
                new float3(2, 1, 0)
            };

            var faces = new int[][]
            {
                new int[] { 0, 1, 2 },      // Triangle
                new int[] { 1, 3, 4, 2 }    // Quad
            };

            var mesh = IndexedMesh.Generate(vertices, faces, Allocator.Persistent);

            Assert.AreEqual(5, mesh.vertexCount);
            Assert.AreEqual(2, mesh.faceCount);
            Assert.AreEqual(7, mesh.halfEdgeCount); // 3 + 4

            mesh.Dispose();
        }

        [Test]
        public void Generate_SkipsDegenerateFaces()
        {
            var vertices = new float3[]
            {
                new float3(0, 0, 0),
                new float3(1, 0, 0),
                new float3(0, 1, 0)
            };

            var faces = new int[][]
            {
                new int[] { 0, 1, 2 },  // Valid triangle
                new int[] { 0, 1 },     // Invalid (only 2 vertices)
                new int[] { }           // Invalid (empty)
            };

            var mesh = IndexedMesh.Generate(vertices, faces, Allocator.Persistent);

            Assert.AreEqual(3, mesh.vertexCount);
            Assert.AreEqual(1, mesh.faceCount); // Only the valid face

            mesh.Dispose();
        }

        [Test]
        public void Generate_NativeArrayVersion_CreatesValidMesh()
        {
            using (var vertices = new NativeArray<float3>(3, Allocator.Temp))
            using (var faceIndices = new NativeArray<int>(3, Allocator.Temp))
            using (var faceSizes = new NativeArray<int>(1, Allocator.Temp))
            {
                vertices[0] = new float3(0, 0, 0);
                vertices[1] = new float3(1, 0, 0);
                vertices[2] = new float3(0.5f, 1, 0);

                faceIndices[0] = 0;
                faceIndices[1] = 1;
                faceIndices[2] = 2;

                faceSizes[0] = 3;

                var mesh = IndexedMesh.Generate(vertices, faceIndices, faceSizes, Allocator.Persistent);

                Assert.AreEqual(3, mesh.vertexCount);
                Assert.AreEqual(1, mesh.faceCount);

                mesh.Dispose();
            }
        }

        [Test]
        public void Generate_PreservesVertexPositions()
        {
            var vertices = new float3[]
            {
                new float3(1.5f, 2.5f, 3.5f),
                new float3(4.5f, 5.5f, 6.5f),
                new float3(7.5f, 8.5f, 9.5f)
            };

            var faces = new int[][]
            {
                new int[] { 0, 1, 2 }
            };

            var mesh = IndexedMesh.Generate(vertices, faces, Allocator.Persistent);

            for (var i = 0; i < vertices.Length; i++)
            {
                Assert.AreEqual(vertices[i].x, mesh.vertices[i].position.x, 0.0001f);
                Assert.AreEqual(vertices[i].y, mesh.vertices[i].position.y, 0.0001f);
                Assert.AreEqual(vertices[i].z, mesh.vertices[i].position.z, 0.0001f);
            }

            mesh.Dispose();
        }
    }
}
