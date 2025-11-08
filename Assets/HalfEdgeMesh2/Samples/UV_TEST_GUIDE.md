# UV Test Guide

This guide explains how to verify UV mapping on all HalfEdgeMesh2 generators using the provided UV testing tools.

## Quick Start

### Method 1: Using the UV Gallery Sample (Recommended)

1. **Create a new GameObject** in your scene
2. **Add the UVGallerySample component** to it
3. **Configure the settings** in the Inspector:
   - Texture Type: Choose Checkerboard, UV Gradient, or Colored Grid
   - Grid Size: Number of squares (for checkerboard/grid)
   - Texture Resolution: Texture size (512 recommended)
   - Spacing: Distance between meshes in the gallery
   - Items Per Row: Number of meshes per row
4. **Enter Play Mode** or right-click the component and select **"Create Gallery"**

The gallery will automatically create all 10 generators with UV textures applied.

### Method 2: Manual Setup with GeneratorSample

1. **Open the HalfEdgeMesh2.unity scene**
2. **Select the GameObject** with the GeneratorSample component
3. **Create a material** with a UV test texture:
   - In code: Use `UVTextureGenerator.CreateCheckerboard()`
   - Or use any checker/grid texture from the Asset Store
4. **Apply the material** to the MeshRenderer
5. **Cycle through different generators** using the Generator Type dropdown
6. **Observe the UV mapping** on each shape

## UV Texture Types

### Checkerboard Pattern
- **Best for**: Quick visual verification of UV layout
- **Shows**: Distortion, stretching, and seam issues
- **Usage**: `UVTextureGenerator.CreateCheckerboard(gridSize, textureSize)`

### UV Gradient
- **Best for**: Precise UV coordinate verification
- **Shows**: Red=U coordinate (0-1), Green=V coordinate (0-1)
- **Usage**: `UVTextureGenerator.CreateUVGradient(textureSize)`

### Colored Grid
- **Best for**: Identifying distinct UV regions
- **Shows**: Different colored squares for visual appeal
- **Usage**: `UVTextureGenerator.CreateColoredGrid(gridSize, textureSize)`

## UV Mapping Strategies by Generator

### Parametric Shapes

**Box**
- Grid-based UV with coordinate averaging
- Each face has consistent UV layout

**Sphere**
- Spherical/equirectangular mapping (longitude/latitude)
- Pole singularities at top/bottom

**Cylinder**
- Cylindrical unwrap for sides (u=angle, v=height)
- Radial mapping for top/bottom caps

**Plane**
- Simple planar 0-1 mapping
- Uniform distribution across grid

**Cone**
- Conical unwrap with apex at center
- Side faces wrap around base

**Torus**
- Toroidal unwrap using major/minor angles
- Smooth wrapping around both radii

### Platonic Solids

**Tetrahedron, Octahedron, Dodecahedron, Icosphere**
- All use spherical UV mapping
- Calculated from normalized vertex positions
- Formula: u = 0.5 + atan2(z, x) / (2π), v = 0.5 - asin(y) / π

### Advanced Generators

**Lathe**
- Cylindrical unwrap based on rotation angle and profile
- u = rotation angle (0-1), v = profile position (0-1)
- Perfect for rotationally symmetric objects

**Extrusion**
- Planar mapping along extrusion axis
- u = profile position, v = height (0-1)
- Side faces wrap around profile

**IndexedMesh**
- Default (0,0) UVs
- Extensible for custom UV arrays in future

## Expected Results

When viewing the UV textures on each generator:

✅ **Good UV Mapping:**
- Checkerboard squares appear roughly uniform in size
- Minimal distortion or stretching
- Textures wrap smoothly without visible seams
- No UV coordinate values outside 0-1 range

⚠️ **Acceptable Artifacts:**
- Pole singularities on Sphere (unavoidable with spherical mapping)
- Slight distortion on curved surfaces (geometric limitation)
- Seam visibility at UV boundaries (can be mitigated with proper texturing)

❌ **Problematic UV Mapping:**
- Extreme stretching or compression
- Completely black or white areas (missing UVs)
- Inverted or mirrored textures
- Overlapping UV coordinates

## Troubleshooting

**Problem: All meshes appear white or black**
- Solution: Check that material has the UV texture assigned
- Verify texture was created successfully

**Problem: Texture appears but is not mapped correctly**
- Solution: Check normalMode setting (Smooth vs Flat)
- Verify UV coordinates are in 0-1 range

**Problem: Gallery doesn't create meshes**
- Solution: Ensure you're in Play Mode or use "Create Gallery" context menu
- Check Console for errors

**Problem: Texture looks blurry**
- Solution: Increase textureResolution (try 1024 or 2048)
- Set texture filterMode to Point for sharp edges

## Code Example: Custom UV Test

```csharp
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using HalfEdgeMesh2.Generators;
using HalfEdgeMesh2.Samples;
using HalfEdgeMesh2.Unity;

public class CustomUVTest : MonoBehaviour
{
    void Start()
    {
        // Create UV test texture
        var texture = UVTextureGenerator.CreateCheckerboard(8, 512);

        // Create material with texture
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.mainTexture = texture;

        // Generate mesh with UVs
        var meshData = Sphere.Generate(1f, new int2(32, 24), Allocator.Persistent);

        // Convert to Unity mesh
        var mesh = new Mesh();
        meshData.UpdateUnityMesh(mesh, NormalGenerationMode.Smooth);

        // Apply to GameObject
        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshRenderer>().material = material;

        // Clean up
        meshData.Dispose();
    }
}
```

## Performance Notes

- UV coordinate generation adds minimal overhead (<5% in most cases)
- UVs are stored per-vertex, so flat shading duplicates UV data like positions
- All UV generation is Burst-compatible where supported
- No runtime GC allocation for UV data

## Further Reading

- See `IMPLEMENTATION_SUMMARY.md` for technical details on UV implementation
- Check generator source code for specific UV calculation formulas
- Unity UV documentation: https://docs.unity3d.com/Manual/UVMapping.html
