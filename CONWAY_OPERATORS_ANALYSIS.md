# Conway Polyhedron Operators - Technical Analysis

## Correct Definitions (from research)

### **Kis (k)** - CORRECTLY IMPLEMENTED ✓
**Definition**: Raises a shallow pyramid on each face
- **New Vertices**: One vertex at each face center (offset along face normal)
- **New Faces**: Each n-sided face becomes n triangles radiating from center
- **Element counts**: V' = V + F, E' = E + 2F, F' = sum of face valences
- **Topology**: Original edges disappear, replaced by edges from face center to original vertices

**My Implementation**: ConwayKis.cs - CORRECT
- Creates vertex at face centroid with height offset along normal
- Creates n triangles per face
- Matches definition

---

### **Dual (d)** - IMPLEMENTED, HAS TRAVERSAL BUGS ⚠️
**Definition**: Swaps vertices and faces
- **New Vertices**: One vertex at each face center
- **New Faces**: Each original vertex becomes a face (connects centroids of adjacent faces)
- **Element counts**: V' = F, F' = V, E' = E
- **Topology**: Faces and vertices exchange roles

**My Implementation**: ConwayDual.cs - MOSTLY CORRECT
- Creates vertices at face centroids ✓
- Creates faces by traversing around original vertices ✓
- **BUG**: Fails on subdivided meshes due to vertex traversal issues
- Issue is with finding all faces around a vertex reliably

---

### **Ambo (a)** - IMPLEMENTED, HAS TRAVERSAL BUGS ⚠️
**Definition**: Rectification - places vertices at edge midpoints
- **New Vertices**: One vertex at midpoint of each edge
- **New Faces**:
  - Each original n-sided face becomes an n-sided face (of edge midpoints)
  - Each original vertex becomes a face (connecting midpoints of incident edges)
- **Element counts**: V' = E, F' = F + V
- **Topology**: Also called "full truncation" or "complete truncation"

**My Implementation**: ConwayAmbo.cs - MOSTLY CORRECT
- Creates vertices at edge midpoints (shared between twins) ✓
- Creates face faces ✓
- Creates vertex faces ✓
- **BUG**: Fails on subdivided meshes due to vertex traversal issues
- Same traversal problem as Dual

---

### **Gyro (g)** - UNKNOWN STATUS
**Definition**: Creates pentagons by rotating and subdividing faces
- **New Vertices**: Creates vertices by rotating face edges inward
- **New Faces**: Each n-sided face becomes n pentagons
- **Element counts**: Complex - creates many new elements
- **Topology**: Related to snub operation

**My Implementation**: ConwayGyro.cs - NOT VERIFIED
- Implementation exists but correctness not confirmed
- User hasn't tested it thoroughly

---

### **Zip (z)** - INCORRECTLY IMPLEMENTED ✗
**Definition**: **Not an original Conway operator**. Added later, related to "truncated dual" (td)
- **Relationship**: z = td (truncate then dual) or equivalent
- **New Vertices**: Complex combination
- **New Faces**: Creates specific face pattern
- **Note**: "Zip" is sometimes defined differently in different sources

**My Implementation**: ConwayZip.cs - WRONG
- Currently delegates to Kis (placeholder)
- Completely incorrect - needs proper implementation

---

### **Expand (e)** - INCORRECTLY SIMPLIFIED ✗
**Definition**: Cantellation - moves faces apart, fills gaps
- **Formula**: e = aa (ambo applied twice)
- **New Vertices**: Edge midpoints AND face centers
- **New Faces**:
  - Each original face becomes smaller face
  - Each original edge becomes a quadrilateral
  - Each original vertex becomes a face
- **Element counts**: V' = E + F, F' = F + E + V
- **Topology**: "Expansion" operation

**My Implementation**: ConwayExpand.cs - WRONG
- Currently delegates to Ambo (simplified)
- Should create more topology than just Ambo
- Needs vertices at BOTH edge midpoints AND face centers

---

### **Bevel (b)** - MAY BE CORRECT (needs verification)
**Definition**: Truncated ambo
- **Formula**: b = ta (truncate then ambo)
- **New Vertices**: Creates vertices along edges
- **New Faces**: Rectangles along edges, small faces at vertices
- **Element counts**: Complex
- **Topology**: Also called "truncated rectification"

**My Implementation**: ConwayBevel.cs - POSSIBLY CORRECT
- Creates two vertices per half-edge along each edge
- Creates rectangular faces
- User says "looks like truncate" which might be correct for b = ta

---

### **Ortho (o)** - IMPLEMENTED, MAY HAVE BUGS ⚠️
**Definition**: Medial/Orthogonal - creates all-quad mesh
- **Formula**: o = jj (join applied twice) OR o = aa (expand, since e = aa)
- **New Vertices**: Edge midpoints and face centers
- **New Faces**: All quadrilaterals
- **Element counts**: V' = E + F, F' = 2E
- **Topology**: Every face is a quad

**My Implementation**: ConwayOrtho.cs - UNCERTAIN
- Creates vertices at edge midpoints ✓
- Creates vertices at face centers ✓
- Creates quads connecting these ✓
- **Possible BUG**: User reports "totally broken"
- Likely issue with quad vertex ordering or topology

---

## Summary of Issues

### Critical Bugs:
1. **Dual & Ambo**: Vertex traversal fails on subdivided meshes (2x2+ boxes)
   - Root cause: Circular traversal around vertices not finding all incident faces/edges
   - May be due to vertex.halfEdge not being set correctly by generators

2. **Expand**: Wrong implementation - just delegates to Ambo
   - Should be: aa (ambo twice) or proper expansion with face centers + edge midpoints
   - Needs complete rewrite

3. **Zip**: Wrong implementation - just delegates to Kis
   - Should be: td (truncate-dual) or proper zip definition
   - Needs complete rewrite

4. **Ortho**: User reports "totally broken"
   - Implementation looks theoretically correct but may have vertex ordering issues
   - Needs testing and debugging

### Likely Correct:
- **Kis**: Appears to work correctly
- **Gyro**: Unknown status, not tested
- **Bevel**: May be correct (user says looks like truncate, which matches b = ta)

### Root Cause Analysis:
The main issue appears to be **vertex traversal in half-edge structures**. The pattern used:
```csharp
// Find starting half-edge with preference for interior edges
var startHe = -1;
for (var heIdx = 0; heIdx < input.halfEdgeCount; heIdx++)
{
    if (input.halfEdges[heIdx].vertex == vertIdx)
    {
        if (startHe == -1 || input.halfEdges[heIdx].twin != -1)
        {
            startHe = heIdx;
            if (input.halfEdges[heIdx].twin != -1)
                break;
        }
    }
}

// Circular traversal
var he = startHe;
do {
    // collect faces/edges
    he = input.halfEdges[input.halfEdges[he].twin].next;
} while (he != startHe);
```

This works on simple meshes but fails on subdivided ones, suggesting the generators may not be setting `vertex.halfEdge` reliably, or the circular traversal assumptions are invalid.

## Recommended Fixes:

1. **Fix Dual & Ambo traversal first** - this is blocking testing of other operators
2. **Rewrite Expand** as proper e = aa or with face centers + edge midpoints
3. **Rewrite Zip** with correct td definition
4. **Debug Ortho** to find vertex ordering issue
5. **Investigate generator mesh building** - why does traversal fail on subdivided meshes?
