using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using HalfEdgeMesh2.Generators;
using HalfEdgeMesh2.Modifiers;

namespace HalfEdgeMesh2.Tests
{
    [TestFixture]
    public class ExpandVerticesTests
    {
        [Test]
        public void Apply_ZeroDistance_DoesNothing()
        {
            var mesh = Box.Generate(new float3(1, 1, 1), new int3(1, 1, 1), Allocator.Temp);
            try
            {
                var originalPositions = new float3[mesh.vertexCount];
                for (var i = 0; i < mesh.vertexCount; i++)
                    originalPositions[i] = mesh.vertices[i].position;

                ExpandVertices.Apply(mesh, 0f);

                for (var i = 0; i < mesh.vertexCount; i++)
                {
                    Assert.AreEqual(originalPositions[i], mesh.vertices[i].position,
                        $"Vertex {i} should not move with zero distance");
                }
            }
            finally
            {
                mesh.Dispose();
            }
        }

        [Test]
        public void Apply_PositiveDistance_MovesVerticesOutward()
        {
            var mesh = Box.Generate(new float3(2, 2, 2), new int3(1, 1, 1), Allocator.Temp);
            try
            {
                var originalBounds = ComputeBounds(ref mesh);
                var distance = 0.5f;

                ExpandVertices.Apply(mesh, distance);

                var newBounds = ComputeBounds(ref mesh);

                // Bounds should be larger after expansion
                Assert.Greater(newBounds.x, originalBounds.x, "X bounds should increase");
                Assert.Greater(newBounds.y, originalBounds.y, "Y bounds should increase");
                Assert.Greater(newBounds.z, originalBounds.z, "Z bounds should increase");
            }
            finally
            {
                mesh.Dispose();
            }
        }

        [Test]
        public void Apply_NegativeDistance_MovesVerticesInward()
        {
            var mesh = Box.Generate(new float3(2, 2, 2), new int3(1, 1, 1), Allocator.Temp);
            try
            {
                var originalBounds = ComputeBounds(ref mesh);
                var distance = -0.2f;

                ExpandVertices.Apply(mesh, distance);

                var newBounds = ComputeBounds(ref mesh);

                // Bounds should be smaller after inward expansion
                Assert.Less(newBounds.x, originalBounds.x, "X bounds should decrease");
                Assert.Less(newBounds.y, originalBounds.y, "Y bounds should decrease");
                Assert.Less(newBounds.z, originalBounds.z, "Z bounds should decrease");
            }
            finally
            {
                mesh.Dispose();
            }
        }

        [Test]
        public void Apply_Sphere_MaintainsSphericalShape()
        {
            var originalRadius = 1f;
            var mesh = Sphere.Generate(originalRadius, new int2(16, 12), Allocator.Temp);
            try
            {
                var distance = 0.5f;

                ExpandVertices.Apply(mesh, distance);

                // All vertices should still be at approximately the same distance from origin
                var expectedRadius = originalRadius + distance;
                var tolerance = 0.1f; // Allow some variance due to UV sphere topology

                for (var i = 0; i < mesh.vertexCount; i++)
                {
                    var vertex = mesh.vertices[i].position;
                    var distanceFromOrigin = math.length(vertex);
                    Assert.AreEqual(expectedRadius, distanceFromOrigin, tolerance,
                        $"Vertex {i} should be at radius {expectedRadius}");
                }
            }
            finally
            {
                mesh.Dispose();
            }
        }

        [Test]
        public void Apply_PreservesTopology()
        {
            var mesh = Tetrahedron.Generate(1f, Allocator.Temp);
            try
            {
                var originalVertexCount = mesh.vertexCount;
                var originalFaceCount = mesh.faceCount;
                var originalHalfEdgeCount = mesh.halfEdgeCount;

                ExpandVertices.Apply(mesh, 0.3f);

                Assert.AreEqual(originalVertexCount, mesh.vertexCount, "Vertex count should not change");
                Assert.AreEqual(originalFaceCount, mesh.faceCount, "Face count should not change");
                Assert.AreEqual(originalHalfEdgeCount, mesh.halfEdgeCount, "Half-edge count should not change");
                Assert.IsTrue(MeshOperations.ValidateMesh(ref mesh), "Mesh should still be valid");
            }
            finally
            {
                mesh.Dispose();
            }
        }

        [Test]
        public void Apply_LargeDistance_WorksCorrectly()
        {
            var mesh = Octahedron.Generate(1f, Allocator.Temp);
            try
            {
                var distance = 5f;

                ExpandVertices.Apply(mesh, distance);

                // Verify all vertices moved
                for (var i = 0; i < mesh.vertexCount; i++)
                {
                    var vertex = mesh.vertices[i].position;
                    var distanceFromOrigin = math.length(vertex);
                    Assert.Greater(distanceFromOrigin, 1f, $"Vertex {i} should have moved outward");
                }

                Assert.IsTrue(MeshOperations.ValidateMesh(ref mesh), "Mesh should still be valid");
            }
            finally
            {
                mesh.Dispose();
            }
        }

        [Test]
        public void Apply_ComplexMesh_WorksCorrectly()
        {
            var mesh = Torus.Generate(2f, 0.5f, new int2(16, 12), Allocator.Temp);
            try
            {
                var originalPositions = new float3[mesh.vertexCount];
                for (var i = 0; i < mesh.vertexCount; i++)
                    originalPositions[i] = mesh.vertices[i].position;

                var distance = 0.2f;
                ExpandVertices.Apply(mesh, distance);

                // Verify all vertices moved
                var movedCount = 0;
                for (var i = 0; i < mesh.vertexCount; i++)
                {
                    var newPos = mesh.vertices[i].position;
                    var oldPos = originalPositions[i];
                    if (math.length(newPos - oldPos) > 0.01f)
                        movedCount++;
                }

                Assert.Greater(movedCount, mesh.vertexCount * 0.9f,
                    "At least 90% of vertices should have moved");
                Assert.IsTrue(MeshOperations.ValidateMesh(ref mesh), "Mesh should still be valid");
            }
            finally
            {
                mesh.Dispose();
            }
        }

        float3 ComputeBounds(ref MeshData mesh)
        {
            if (mesh.vertexCount == 0)
                return float3.zero;

            var min = mesh.vertices[0].position;
            var max = mesh.vertices[0].position;

            for (var i = 1; i < mesh.vertexCount; i++)
            {
                var pos = mesh.vertices[i].position;
                min = math.min(min, pos);
                max = math.max(max, pos);
            }

            return max - min;
        }
    }
}
