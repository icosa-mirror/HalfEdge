using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using HalfEdgeMesh2.Generators;
using HalfEdgeMesh2.Unity;
using PlaneGenerator = HalfEdgeMesh2.Generators.Plane;

namespace HalfEdgeMesh2.Samples
{
    // Creates a gallery showcasing all generators with UV textures
    // Useful for verifying UV mapping works correctly across all generators
    [ExecuteInEditMode]
    public class UVGallerySample : MonoBehaviour
    {
        public enum TextureType
        {
            Checkerboard,
            UVGradient,
            ColoredGrid
        }

        [Header("Texture Settings")]
        [SerializeField] TextureType textureType = TextureType.Checkerboard;
        [SerializeField] int gridSize = 8;
        [SerializeField] int textureResolution = 512;

        [Header("Layout")]
        [SerializeField] float spacing = 3f;
        [SerializeField] int itemsPerRow = 5;
        [SerializeField] bool autoRotate = false;

        [Header("Display Options")]
        [SerializeField] NormalGenerationMode normalMode = NormalGenerationMode.Smooth;

        Material sharedMaterial;
        Texture2D currentTexture;
        GameObject[] generatorObjects;

        void Start()
        {
            if (Application.isPlaying)
                CreateGallery();
        }

        void Update()
        {
            if (autoRotate && Application.isPlaying && generatorObjects != null)
            {
                var rotation = Quaternion.Euler(0, Time.deltaTime * 30f, 0);
                foreach (var obj in generatorObjects)
                {
                    if (obj != null)
                        obj.transform.Rotate(0, Time.deltaTime * 30f, 0);
                }
            }
        }

        void OnValidate()
        {
            gridSize = Mathf.Max(2, gridSize);
            textureResolution = Mathf.Clamp(textureResolution, 64, 2048);
            spacing = Mathf.Max(1f, spacing);
            itemsPerRow = Mathf.Max(1, itemsPerRow);
        }

        [ContextMenu("Create Gallery")]
        public void CreateGallery()
        {
            ClearGallery();
            CreateMaterial();
            CreateGeneratorObjects();
        }

        [ContextMenu("Clear Gallery")]
        public void ClearGallery()
        {
            if (generatorObjects != null)
            {
                foreach (var obj in generatorObjects)
                {
                    if (obj != null)
                        DestroyImmediate(obj);
                }
                generatorObjects = null;
            }

            if (sharedMaterial != null)
            {
                DestroyImmediate(sharedMaterial);
                sharedMaterial = null;
            }

            if (currentTexture != null)
            {
                DestroyImmediate(currentTexture);
                currentTexture = null;
            }
        }

        void CreateMaterial()
        {
            // Create texture based on type
            switch (textureType)
            {
                case TextureType.Checkerboard:
                    currentTexture = UVTextureGenerator.CreateCheckerboard(gridSize, textureResolution);
                    break;
                case TextureType.UVGradient:
                    currentTexture = UVTextureGenerator.CreateUVGradient(textureResolution);
                    break;
                case TextureType.ColoredGrid:
                    currentTexture = UVTextureGenerator.CreateColoredGrid(gridSize, textureResolution);
                    break;
            }

            currentTexture.name = $"UV Test Texture ({textureType})";

            // Create material
            sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            sharedMaterial.mainTexture = currentTexture;
            sharedMaterial.name = "UV Test Material";
        }

        void CreateGeneratorObjects()
        {
            var generators = new System.Action<Transform, int>[]
            {
                (t, i) => CreateBox(t, i),
                (t, i) => CreateSphere(t, i),
                (t, i) => CreateCylinder(t, i),
                (t, i) => CreatePlane(t, i),
                (t, i) => CreateCone(t, i),
                (t, i) => CreateTorus(t, i),
                (t, i) => CreateTetrahedron(t, i),
                (t, i) => CreateOctahedron(t, i),
                (t, i) => CreateDodecahedron(t, i),
                (t, i) => CreateIcosphere(t, i),
            };

            generatorObjects = new GameObject[generators.Length];

            for (var i = 0; i < generators.Length; i++)
            {
                var row = i / itemsPerRow;
                var col = i % itemsPerRow;

                var obj = new GameObject($"Generator_{i}");
                obj.transform.SetParent(transform);
                obj.transform.localPosition = new Vector3(col * spacing, 0, -row * spacing);

                generators[i](obj.transform, i);
                generatorObjects[i] = obj;
            }
        }

        void CreateBox(Transform parent, int index)
        {
            var obj = CreateMeshObject("Box", parent);
            var meshData = Box.Generate(new float3(1.5f, 1.5f, 1.5f), new int3(2, 2, 2), Allocator.Persistent);
            ApplyMeshData(obj, meshData, "Box");
        }

        void CreateSphere(Transform parent, int index)
        {
            var obj = CreateMeshObject("Sphere", parent);
            var meshData = Sphere.Generate(1f, new int2(24, 16), Allocator.Persistent);
            ApplyMeshData(obj, meshData, "Sphere");
        }

        void CreateCylinder(Transform parent, int index)
        {
            var obj = CreateMeshObject("Cylinder", parent);
            var meshData = Cylinder.Generate(0.6f, 1.5f, new int2(24, 1), true, Allocator.Persistent);
            ApplyMeshData(obj, meshData, "Cylinder");
        }

        void CreatePlane(Transform parent, int index)
        {
            var obj = CreateMeshObject("Plane", parent);
            var meshData = PlaneGenerator.Generate(new float2(2f, 2f), new int2(4, 4), Allocator.Persistent);
            ApplyMeshData(obj, meshData, "Plane");
        }

        void CreateCone(Transform parent, int index)
        {
            var obj = CreateMeshObject("Cone", parent);
            var meshData = Cone.Generate(0.7f, 1.5f, 24, Allocator.Persistent);
            ApplyMeshData(obj, meshData, "Cone");
        }

        void CreateTorus(Transform parent, int index)
        {
            var obj = CreateMeshObject("Torus", parent);
            var meshData = Torus.Generate(0.8f, 0.3f, new int2(32, 16), Allocator.Persistent);
            ApplyMeshData(obj, meshData, "Torus");
        }

        void CreateTetrahedron(Transform parent, int index)
        {
            var obj = CreateMeshObject("Tetrahedron", parent);
            var meshData = Tetrahedron.Generate(1.5f, Allocator.Persistent);
            ApplyMeshData(obj, meshData, "Tetrahedron");
        }

        void CreateOctahedron(Transform parent, int index)
        {
            var obj = CreateMeshObject("Octahedron", parent);
            var meshData = Octahedron.Generate(1.5f, Allocator.Persistent);
            ApplyMeshData(obj, meshData, "Octahedron");
        }

        void CreateDodecahedron(Transform parent, int index)
        {
            var obj = CreateMeshObject("Dodecahedron", parent);
            var meshData = Dodecahedron.Generate(1.2f, Allocator.Persistent);
            ApplyMeshData(obj, meshData, "Dodecahedron");
        }

        void CreateIcosphere(Transform parent, int index)
        {
            var obj = CreateMeshObject("Icosphere", parent);
            var meshData = Icosphere.Generate(1f, 2, Allocator.Persistent);
            ApplyMeshData(obj, meshData, "Icosphere");
        }

        GameObject CreateMeshObject(string name, Transform parent)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent);
            obj.transform.localPosition = Vector3.zero;
            obj.AddComponent<MeshFilter>();
            var renderer = obj.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = sharedMaterial;
            return obj;
        }

        void ApplyMeshData(GameObject obj, MeshData meshData, string meshName)
        {
            try
            {
                var mesh = new Mesh { name = meshName };
                meshData.UpdateUnityMesh(mesh, normalMode);

                var meshFilter = obj.GetComponent<MeshFilter>();
                meshFilter.sharedMesh = mesh;
            }
            finally
            {
                meshData.Dispose();
            }
        }

        void OnDestroy()
        {
            ClearGallery();
        }
    }
}
