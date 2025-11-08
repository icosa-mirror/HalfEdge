using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using HalfEdgeMesh2.Generators;

namespace HalfEdgeMesh2.Tests
{
    [TestFixture]
    public class PlatonicSolidsTests
    {
        [Test]
        public void Generate_Tetrahedron_CreatesValidMesh()
        {
            var size = 1f;
            var mesh = Tetrahedron.Generate(size, Allocator.Temp);
            try
            {
                Assert.AreEqual(4, mesh.vertexCount, "Tetrahedron should have 4 vertices");
                Assert.AreEqual(4, mesh.faceCount, "Tetrahedron should have 4 faces");
                Assert.IsTrue(MeshOperations.ValidateMesh(ref mesh), "Generated mesh should be valid");

                // All faces should be triangles
                for (var i = 0; i < mesh.faceCount; i++)
                {
                    var face = mesh.faces[i];
                    var edgeCount = CountFaceEdges(ref mesh, face);
                    Assert.AreEqual(3, edgeCount, $"Face {i} should be a triangle");
                }
            }
            finally
            {
                mesh.Dispose();
            }
        }

        [Test]
        public void Generate_Octahedron_CreatesValidMesh()
        {
            var size = 2f;
            var mesh = Octahedron.Generate(size, Allocator.Temp);
            try
            {
                Assert.AreEqual(6, mesh.vertexCount, "Octahedron should have 6 vertices");
                Assert.AreEqual(8, mesh.faceCount, "Octahedron should have 8 faces");
                Assert.IsTrue(MeshOperations.ValidateMesh(ref mesh), "Generated mesh should be valid");

                // All faces should be triangles
                for (var i = 0; i < mesh.faceCount; i++)
                {
                    var face = mesh.faces[i];
                    var edgeCount = CountFaceEdges(ref mesh, face);
                    Assert.AreEqual(3, edgeCount, $"Face {i} should be a triangle");
                }
            }
            finally
            {
                mesh.Dispose();
            }
        }

        [Test]
        public void Generate_Dodecahedron_CreatesValidMesh()
        {
            var size = 2f;
            var mesh = Dodecahedron.Generate(size, Allocator.Temp);
            try
            {
                Assert.AreEqual(20, mesh.vertexCount, "Dodecahedron should have 20 vertices");
                Assert.AreEqual(12, mesh.faceCount, "Dodecahedron should have 12 faces");
                Assert.IsTrue(MeshOperations.ValidateMesh(ref mesh), "Generated mesh should be valid");

                // All faces should be pentagons
                for (var i = 0; i < mesh.faceCount; i++)
                {
                    var face = mesh.faces[i];
                    var edgeCount = CountFaceEdges(ref mesh, face);
                    Assert.AreEqual(5, edgeCount, $"Face {i} should be a pentagon");
                }
            }
            finally
            {
                mesh.Dispose();
            }
        }

        [Test]
        public void Generate_Tetrahedron_VerifiesEulerCharacteristic()
        {
            var mesh = Tetrahedron.Generate(1f, Allocator.Temp);
            try
            {
                // Euler characteristic: V - E + F = 2 for closed polyhedra
                var V = mesh.vertexCount;
                var E = MeshOperations.CountEdges(ref mesh);
                var F = mesh.faceCount;

                Assert.AreEqual(2, V - E + F, "Should satisfy Euler characteristic");
            }
            finally
            {
                mesh.Dispose();
            }
        }

        [Test]
        public void Generate_Octahedron_VerifiesEulerCharacteristic()
        {
            var mesh = Octahedron.Generate(1f, Allocator.Temp);
            try
            {
                var V = mesh.vertexCount;
                var E = MeshOperations.CountEdges(ref mesh);
                var F = mesh.faceCount;

                Assert.AreEqual(2, V - E + F, "Should satisfy Euler characteristic");
            }
            finally
            {
                mesh.Dispose();
            }
        }

        [Test]
        public void Generate_Dodecahedron_VerifiesEulerCharacteristic()
        {
            var mesh = Dodecahedron.Generate(1f, Allocator.Temp);
            try
            {
                var V = mesh.vertexCount;
                var E = MeshOperations.CountEdges(ref mesh);
                var F = mesh.faceCount;

                Assert.AreEqual(2, V - E + F, "Should satisfy Euler characteristic");
            }
            finally
            {
                mesh.Dispose();
            }
        }

        [Test]
        public void Generate_PlatonicSolids_AreCenteredAtOrigin()
        {
            var meshes = new[]
            {
                Tetrahedron.Generate(1f, Allocator.Temp),
                Octahedron.Generate(1f, Allocator.Temp),
                Dodecahedron.Generate(1f, Allocator.Temp)
            };

            var names = new[] { "Tetrahedron", "Octahedron", "Dodecahedron" };

            for (var i = 0; i < meshes.Length; i++)
            {
                try
                {
                    MeshOperations.ComputeBounds(ref meshes[i], out var center, out _);
                    Assert.AreEqual(0f, math.length(center), 0.001f, $"{names[i]} should be centered at origin");
                }
                finally
                {
                    meshes[i].Dispose();
                }
            }
        }

        [Test]
        public void Generate_Icosphere_CreatesValidMesh()
        {
            var radius = 1f;
            var subdivisions = 0;

            var mesh = Icosphere.Generate(radius, subdivisions, Allocator.Temp);
            try
            {
                // Subdivision 0 = icosahedron: 12 vertices, 20 faces
                Assert.AreEqual(12, mesh.vertexCount, "Icosphere (subdiv 0) should have 12 vertices");
                Assert.AreEqual(20, mesh.faceCount, "Icosphere (subdiv 0) should have 20 faces");
                Assert.IsTrue(MeshOperations.ValidateMesh(ref mesh), "Generated mesh should be valid");
            }
            finally
            {
                mesh.Dispose();
            }
        }

        [Test]
        public void Generate_Icosphere_SubdivisionIncreasesComplexity()
        {
            var radius = 1f;

            var mesh0 = Icosphere.Generate(radius, 0, Allocator.Temp);
            var mesh1 = Icosphere.Generate(radius, 1, Allocator.Temp);
            var mesh2 = Icosphere.Generate(radius, 2, Allocator.Temp);

            try
            {
                // Each subdivision quadruples the face count
                Assert.Greater(mesh1.faceCount, mesh0.faceCount, "Subdivision 1 should have more faces than 0");
                Assert.Greater(mesh2.faceCount, mesh1.faceCount, "Subdivision 2 should have more faces than 1");

                // Validate all meshes
                Assert.IsTrue(MeshOperations.ValidateMesh(ref mesh0), "Subdivision 0 mesh should be valid");
                Assert.IsTrue(MeshOperations.ValidateMesh(ref mesh1), "Subdivision 1 mesh should be valid");
                Assert.IsTrue(MeshOperations.ValidateMesh(ref mesh2), "Subdivision 2 mesh should be valid");
            }
            finally
            {
                mesh0.Dispose();
                mesh1.Dispose();
                mesh2.Dispose();
            }
        }

        [Test]
        public void Generate_Icosphere_VerticesAreOnSphere()
        {
            var radius = 2f;
            var subdivisions = 2;

            var mesh = Icosphere.Generate(radius, subdivisions, Allocator.Temp);
            try
            {
                var tolerance = 0.001f;

                for (var i = 0; i < mesh.vertexCount; i++)
                {
                    var vertex = mesh.vertices[i].position;
                    var distance = math.length(vertex);
                    Assert.AreEqual(radius, distance, tolerance, $"Vertex {i} should be at radius {radius}");
                }
            }
            finally
            {
                mesh.Dispose();
            }
        }

        [Test]
        public void Generate_Icosphere_NegativeSubdivisionsClamped()
        {
            var radius = 1f;
            var subdivisions = -1;

            var mesh = Icosphere.Generate(radius, subdivisions, Allocator.Temp);
            try
            {
                // Should clamp to 0 subdivisions (icosahedron)
                Assert.AreEqual(12, mesh.vertexCount, "Should have 12 vertices (icosahedron)");
                Assert.AreEqual(20, mesh.faceCount, "Should have 20 faces (icosahedron)");
                Assert.IsTrue(MeshOperations.ValidateMesh(ref mesh), "Generated mesh should be valid");
            }
            finally
            {
                mesh.Dispose();
            }
        }

        [Test]
        public void Generate_Icosphere_AllFacesAreTriangles()
        {
            var radius = 1f;
            var subdivisions = 1;

            var mesh = Icosphere.Generate(radius, subdivisions, Allocator.Temp);
            try
            {
                for (var i = 0; i < mesh.faceCount; i++)
                {
                    var face = mesh.faces[i];
                    var edgeCount = CountFaceEdges(ref mesh, face);
                    Assert.AreEqual(3, edgeCount, $"Face {i} should be a triangle");
                }
            }
            finally
            {
                mesh.Dispose();
            }
        }

        int CountFaceEdges(ref MeshData mesh, Face face)
        {
            var count = 0;
            var currentHe = face.halfEdge;
            var startHe = currentHe;

            do
            {
                count++;
                var he = mesh.halfEdges[currentHe];
                currentHe = he.next;
            } while (currentHe != startHe);

            return count;
        }
    }
}
