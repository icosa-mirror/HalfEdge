using Unity.Collections;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;
using HalfEdgeMesh2.Generators;
using HalfEdgeMesh2.Modifiers;
using HalfEdgeMesh2.Unity;
using PlaneGenerator = HalfEdgeMesh2.Generators.Plane;

namespace HalfEdgeMesh2.Samples
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class GeneratorSample : MonoBehaviour
    {
        public enum GeneratorType
        {
            Box,
            Sphere,
            Cylinder,
            Plane,
            Cone,
            Torus,
            Tetrahedron,
            Octahedron,
            Dodecahedron,
            Icosphere
        }

        [SerializeField] GeneratorType generatorType = GeneratorType.Box;
        [SerializeField] NormalGenerationMode normalMode = NormalGenerationMode.Smooth;
        [SerializeField] bool animateSize = false;

        [Header("Box")]
        [SerializeField] float3 boxSize = new float3(2, 2, 2);
        [SerializeField] int3 boxSegments = new int3(2, 2, 2);

        [Header("Sphere")]
        [SerializeField] float sphereRadius = 1.0f;
        [SerializeField] int2 sphereSegments = new int2(16, 12);

        [Header("Cylinder")]
        [SerializeField] float cylinderRadius = 0.5f;
        [SerializeField] float cylinderHeight = 2.0f;
        [SerializeField] int2 cylinderSegments = new int2(16, 1);
        [SerializeField] bool cylinderCapped = true;

        [Header("Plane")]
        [SerializeField] float2 planeSize = new float2(2, 2);
        [SerializeField] int2 planeSegments = new int2(4, 4);

        [Header("Cone")]
        [SerializeField] float coneRadius = 0.5f;
        [SerializeField] float coneHeight = 2.0f;
        [SerializeField] int coneSegments = 16;

        [Header("Torus")]
        [SerializeField] float torusMajorRadius = 1.0f;
        [SerializeField] float torusMinorRadius = 0.3f;
        [SerializeField] int2 torusSegments = new int2(24, 12);

        [Header("Platonic Solids")]
        [SerializeField] float platonicSize = 1.0f;

        [Header("Icosphere")]
        [SerializeField] float icosphereRadius = 1.0f;
        [SerializeField] int icosphereSubdivisions = 2;

        [Header("Modifiers")]
        [SerializeField] bool applySmooth = false;
        [SerializeField] float smoothingFactor = 0.5f;
        [SerializeField] int smoothingIterations = 1;

        [SerializeField] bool applyExpand = false;
        [SerializeField] float expandDistance = 0.1f;

        [SerializeField] bool applyStretch = false;
        [SerializeField] float3 stretchScale = new float3(1, 1, 1);

        [SerializeField] bool applyTwist = false;
        [SerializeField] float twistAngle = 0.5f;
        [SerializeField] float3 twistAxis = new float3(0, 1, 0);

        [SerializeField] bool applySkew = false;
        [SerializeField] float skewAngle = 0.3f;
        [SerializeField] float3 skewDirection = new float3(1, 0, 0);

        [Header("Conway Operators")]
        [SerializeField] bool applyConwayKis = false;
        [SerializeField] float kisHeight = 0.3f;

        [SerializeField] bool applyConwayDual = false;

        [SerializeField] bool applyConwayAmbo = false;

        [SerializeField] bool applyConwayGyro = false;
        [SerializeField] float gyroSpinRatio = 0.3f;

        [SerializeField] bool applyConwayZip = false;
        [SerializeField] float zipHeight = 0.3f;

        [SerializeField] bool applyConwayExpand = false;

        [SerializeField] bool applyConwayBevel = false;
        [SerializeField] float bevelRatio = 0.2f;

        [SerializeField] bool applyConwayOrtho = false;

        MeshFilter meshFilter;

        // Profiler markers
        static readonly ProfilerMarker s_GenerateMeshMarker = new ProfilerMarker("GeneratorSample.GenerateMesh");
        static readonly ProfilerMarker s_GeneratorMarker = new ProfilerMarker("GeneratorSample.Generator");
        static readonly ProfilerMarker s_ModifierMarker = new ProfilerMarker("GeneratorSample.Modifier");
        static readonly ProfilerMarker s_UpdateUnityMeshMarker = new ProfilerMarker("GeneratorSample.UpdateUnityMesh");
        Mesh generatedMesh;
        bool needsMeshUpdate;
        bool isAnimating;

        void Start()
        {
            meshFilter = GetComponent<MeshFilter>();
            InitializeMeshIfNeeded();
            needsMeshUpdate = true;
            isAnimating = false;
        }

        void Update()
        {
            // Check if we should animate (only in play mode)
            var shouldAnimate = animateSize && Application.isPlaying;

            // Update mesh if needed or if animating
            if (needsMeshUpdate || shouldAnimate)
            {
                GenerateMesh();
                needsMeshUpdate = false;
                isAnimating = shouldAnimate;
            }
        }

        void GenerateMesh()
        {
            using (s_GenerateMeshMarker.Auto())
            {
                MeshData meshData;

                // Generate mesh with Generator profiler marker
                using (s_GeneratorMarker.Auto())
                {
                    switch (generatorType)
                    {
                        case GeneratorType.Box:
                            meshData = GenerateBox();
                            break;
                        case GeneratorType.Sphere:
                            meshData = GenerateSphere();
                            break;
                        case GeneratorType.Cylinder:
                            meshData = GenerateCylinder();
                            break;
                        case GeneratorType.Plane:
                            meshData = GeneratePlane();
                            break;
                        case GeneratorType.Cone:
                            meshData = GenerateCone();
                            break;
                        case GeneratorType.Torus:
                            meshData = GenerateTorus();
                            break;
                        case GeneratorType.Tetrahedron:
                            meshData = Tetrahedron.Generate(platonicSize, Allocator.Persistent);
                            break;
                        case GeneratorType.Octahedron:
                            meshData = Octahedron.Generate(platonicSize, Allocator.Persistent);
                            break;
                        case GeneratorType.Dodecahedron:
                            meshData = Dodecahedron.Generate(platonicSize, Allocator.Persistent);
                            break;
                        case GeneratorType.Icosphere:
                            meshData = GenerateIcosphere();
                            break;
                        default:
                            return;
                    }
                }

                // Apply modifiers with Modifier profiler marker
                using (s_ModifierMarker.Auto())
                {
                    meshData = ApplyModifiers(meshData);
                }

                try
                {
                    // Ensure we have a managed mesh
                    if (generatedMesh == null)
                    {
                        generatedMesh = new Mesh();
                        generatedMesh.name = "Generated Mesh";
                        generatedMesh.hideFlags = HideFlags.DontSave;
                        meshFilter.sharedMesh = generatedMesh;
                    }

                    using (s_UpdateUnityMeshMarker.Auto())
                    {
                        meshData.UpdateUnityMesh(generatedMesh, normalMode);
                    }
                }
                finally
                {
                    meshData.Dispose();
                }
            }
        }

        MeshData GenerateBox()
        {
            var currentSize = boxSize;
            var currentSegments = boxSegments;

            if (animateSize && Application.isPlaying)
            {
                var t = Time.time;
                var scale = math.lerp(0.9f, 1.0f, (math.sin(t) + 1.0f) * 0.5f);
                currentSize *= scale;
            }

            return Box.Generate(currentSize, currentSegments, Allocator.Persistent);
        }

        MeshData GenerateSphere()
        {
            var currentRadius = sphereRadius;
            var currentSegments = sphereSegments;

            if (animateSize && Application.isPlaying)
            {
                var t = Time.time;
                var scale = math.lerp(0.9f, 1.0f, (math.sin(t) + 1.0f) * 0.5f);
                currentRadius *= scale;
            }

            return Sphere.Generate(currentRadius, currentSegments, Allocator.Persistent);
        }

        MeshData GenerateCylinder()
        {
            var currentRadius = cylinderRadius;
            var currentHeight = cylinderHeight;

            if (animateSize && Application.isPlaying)
            {
                var t = Time.time;
                var scale = math.lerp(0.9f, 1.0f, (math.sin(t) + 1.0f) * 0.5f);
                currentRadius *= scale;
                currentHeight *= scale;
            }

            return Cylinder.Generate(currentRadius, currentHeight, cylinderSegments, cylinderCapped, Allocator.Persistent);
        }

        MeshData GeneratePlane()
        {
            var currentSize = planeSize;

            if (animateSize && Application.isPlaying)
            {
                var t = Time.time;
                var scale = math.lerp(0.9f, 1.0f, (math.sin(t) + 1.0f) * 0.5f);
                currentSize *= scale;
            }

            return PlaneGenerator.Generate(currentSize, planeSegments, Allocator.Persistent);
        }

        MeshData GenerateCone()
        {
            var currentRadius = coneRadius;
            var currentHeight = coneHeight;

            if (animateSize && Application.isPlaying)
            {
                var t = Time.time;
                var scale = math.lerp(0.9f, 1.0f, (math.sin(t) + 1.0f) * 0.5f);
                currentRadius *= scale;
                currentHeight *= scale;
            }

            return Cone.Generate(currentRadius, currentHeight, coneSegments, Allocator.Persistent);
        }

        MeshData GenerateTorus()
        {
            var currentMajorRadius = torusMajorRadius;
            var currentMinorRadius = torusMinorRadius;

            if (animateSize && Application.isPlaying)
            {
                var t = Time.time;
                var scale = math.lerp(0.9f, 1.0f, (math.sin(t) + 1.0f) * 0.5f);
                currentMajorRadius *= scale;
                currentMinorRadius *= scale;
            }

            return Torus.Generate(currentMajorRadius, currentMinorRadius, torusSegments, Allocator.Persistent);
        }

        MeshData GenerateIcosphere()
        {
            var currentRadius = icosphereRadius;

            if (animateSize && Application.isPlaying)
            {
                var t = Time.time;
                var scale = math.lerp(0.9f, 1.0f, (math.sin(t) + 1.0f) * 0.5f);
                currentRadius *= scale;
            }

            return Icosphere.Generate(currentRadius, icosphereSubdivisions, Allocator.Persistent);
        }

        void OnDestroy()
        {
            if (generatedMesh != null)
            {
                DestroyImmediate(generatedMesh);
                generatedMesh = null;
            }
        }

        MeshData ApplyModifiers(MeshData inputMesh)
        {
            var currentMesh = inputMesh;
            var needsDisposal = false;

            // Conway operators (topology-changing) - must be applied first
            if (applyConwayKis)
            {
                var newMesh = ConwayKis.Apply(currentMesh, kisHeight, Allocator.Persistent);
                if (needsDisposal)
                    currentMesh.Dispose();
                currentMesh = newMesh;
                needsDisposal = true;
            }

            if (applyConwayDual)
            {
                var newMesh = ConwayDual.Apply(currentMesh, Allocator.Persistent);
                if (needsDisposal)
                    currentMesh.Dispose();
                currentMesh = newMesh;
                needsDisposal = true;
            }

            if (applyConwayAmbo)
            {
                var newMesh = ConwayAmbo.Apply(currentMesh, Allocator.Persistent);
                if (needsDisposal)
                    currentMesh.Dispose();
                currentMesh = newMesh;
                needsDisposal = true;
            }

            if (applyConwayGyro)
            {
                var newMesh = ConwayGyro.Apply(currentMesh, gyroSpinRatio, Allocator.Persistent);
                if (needsDisposal)
                    currentMesh.Dispose();
                currentMesh = newMesh;
                needsDisposal = true;
            }

            if (applyConwayZip)
            {
                var newMesh = ConwayZip.Apply(currentMesh, zipHeight, Allocator.Persistent);
                if (needsDisposal)
                    currentMesh.Dispose();
                currentMesh = newMesh;
                needsDisposal = true;
            }

            if (applyConwayExpand)
            {
                var newMesh = ConwayExpand.Apply(currentMesh, Allocator.Persistent);
                if (needsDisposal)
                    currentMesh.Dispose();
                currentMesh = newMesh;
                needsDisposal = true;
            }

            if (applyConwayBevel)
            {
                var newMesh = ConwayBevel.Apply(currentMesh, bevelRatio, Allocator.Persistent);
                if (needsDisposal)
                    currentMesh.Dispose();
                currentMesh = newMesh;
                needsDisposal = true;
            }

            if (applyConwayOrtho)
            {
                var newMesh = ConwayOrtho.Apply(currentMesh, Allocator.Persistent);
                if (needsDisposal)
                    currentMesh.Dispose();
                currentMesh = newMesh;
                needsDisposal = true;
            }

            // Vertex-transform modifiers (in-place)
            // Apply expand modifier
            if (applyExpand)
            {
                ExpandVertices.Apply(currentMesh, expandDistance);
            }

            // Apply stretch modifier
            if (applyStretch)
            {
                StretchMesh.Apply(currentMesh, stretchScale);
            }

            // Apply twist modifier
            if (applyTwist)
            {
                TwistMesh.Apply(currentMesh, twistAxis, float3.zero, twistAngle);
            }

            // Apply skew modifier
            if (applySkew)
            {
                SkewMesh.Apply(currentMesh, skewAngle, skewDirection);
            }

            // Apply smoothing modifier
            if (applySmooth && smoothingFactor > 0 && smoothingIterations > 0)
            {
                SmoothVertices.Apply(currentMesh, smoothingFactor, smoothingIterations);
            }

            return currentMesh;
        }

        void OnValidate()
        {
            // Clamp values to valid ranges
            boxSegments = math.max(boxSegments, 1);
            boxSize = math.max(boxSize, 0.01f);

            sphereSegments = math.max(sphereSegments, 3);
            sphereRadius = math.max(sphereRadius, 0.01f);

            cylinderSegments = math.max(cylinderSegments, new int2(3, 1));
            cylinderRadius = math.max(cylinderRadius, 0.01f);
            cylinderHeight = math.max(cylinderHeight, 0.01f);

            planeSegments = math.max(planeSegments, 1);
            planeSize = math.max(planeSize, 0.01f);

            coneSegments = math.max(coneSegments, 3);
            coneRadius = math.max(coneRadius, 0.01f);
            coneHeight = math.max(coneHeight, 0.01f);

            torusSegments = math.max(torusSegments, 3);
            torusMajorRadius = math.max(torusMajorRadius, 0.01f);
            torusMinorRadius = math.max(torusMinorRadius, 0.01f);

            platonicSize = math.max(platonicSize, 0.01f);

            icosphereRadius = math.max(icosphereRadius, 0.01f);
            icosphereSubdivisions = math.clamp(icosphereSubdivisions, 0, 5);

            smoothingFactor = math.clamp(smoothingFactor, 0f, 1f);
            smoothingIterations = math.max(smoothingIterations, 1);

            stretchScale = math.max(stretchScale, 0.01f);
            twistAxis = math.normalize(twistAxis);
            skewDirection = math.normalize(skewDirection);

            // Conway operator parameters
            kisHeight = math.max(kisHeight, 0.01f);
            gyroSpinRatio = math.clamp(gyroSpinRatio, 0f, 1f);
            zipHeight = math.max(zipHeight, 0.01f);
            bevelRatio = math.clamp(bevelRatio, 0.1f, 0.4f);

            // Schedule mesh update for next Update
            needsMeshUpdate = true;
        }

        void InitializeMeshIfNeeded()
        {
            if (meshFilter == null)
                meshFilter = GetComponent<MeshFilter>();

            if (generatedMesh == null)
            {
                generatedMesh = new Mesh();
                generatedMesh.name = "Generated Mesh";
                generatedMesh.hideFlags = HideFlags.DontSave;

                if (meshFilter != null)
                    meshFilter.sharedMesh = generatedMesh;
            }
        }
    }
}