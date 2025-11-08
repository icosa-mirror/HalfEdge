using Unity.Collections;
using Unity.Mathematics;

namespace HalfEdgeMesh2.Generators
{
    public static class Icosphere
    {
        public static MeshData Generate(float radius, int subdivisions, Allocator allocator)
        {
            var clampedSubdivisions = math.max(subdivisions, 0);

            // Create initial icosahedron
            var vertices = new NativeList<float3>(Allocator.Temp);
            var faces = new NativeList<int3>(Allocator.Temp);

            CreateIcosahedron(ref vertices, ref faces, radius);

            // Apply subdivisions
            for (var i = 0; i < clampedSubdivisions; i++)
            {
                Subdivide(ref vertices, ref faces, radius);
            }

            // Build mesh
            var builder = new MeshBuilder(Allocator.TempJob, faces.Length * 2);

            var vertexIndices = new NativeArray<int>(vertices.Length, Allocator.Temp);
            for (var i = 0; i < vertices.Length; i++)
            {
                var pos = vertices[i];
                var uv = CalculateSphericalUV(pos, radius);
                vertexIndices[i] = builder.AddVertex(pos, uv);
            }

            for (var i = 0; i < faces.Length; i++)
            {
                var face = faces[i];
                builder.AddFace(vertexIndices[face.x], vertexIndices[face.y], vertexIndices[face.z]);
            }

            var result = builder.Build(allocator);

            builder.Dispose();
            vertices.Dispose();
            faces.Dispose();
            vertexIndices.Dispose();

            return result;
        }

        static float2 CalculateSphericalUV(float3 position, float radius)
        {
            var normalized = math.normalize(position);
            var u = 0.5f + math.atan2(normalized.z, normalized.x) / (2f * math.PI);
            var v = 0.5f - math.asin(normalized.y) / math.PI;
            return new float2(u, v);
        }

        static void CreateIcosahedron(ref NativeList<float3> vertices, ref NativeList<int3> faces, float radius)
        {
            var t = (1f + math.sqrt(5f)) / 2f;

            // 12 vertices of icosahedron
            vertices.Add(math.normalize(new float3(-1,  t,  0)) * radius);
            vertices.Add(math.normalize(new float3( 1,  t,  0)) * radius);
            vertices.Add(math.normalize(new float3(-1, -t,  0)) * radius);
            vertices.Add(math.normalize(new float3( 1, -t,  0)) * radius);
            vertices.Add(math.normalize(new float3( 0, -1,  t)) * radius);
            vertices.Add(math.normalize(new float3( 0,  1,  t)) * radius);
            vertices.Add(math.normalize(new float3( 0, -1, -t)) * radius);
            vertices.Add(math.normalize(new float3( 0,  1, -t)) * radius);
            vertices.Add(math.normalize(new float3( t,  0, -1)) * radius);
            vertices.Add(math.normalize(new float3( t,  0,  1)) * radius);
            vertices.Add(math.normalize(new float3(-t,  0, -1)) * radius);
            vertices.Add(math.normalize(new float3(-t,  0,  1)) * radius);

            // 20 faces of icosahedron
            faces.Add(new int3(0, 11, 5));
            faces.Add(new int3(0, 5, 1));
            faces.Add(new int3(0, 1, 7));
            faces.Add(new int3(0, 7, 10));
            faces.Add(new int3(0, 10, 11));
            faces.Add(new int3(1, 5, 9));
            faces.Add(new int3(5, 11, 4));
            faces.Add(new int3(11, 10, 2));
            faces.Add(new int3(10, 7, 6));
            faces.Add(new int3(7, 1, 8));
            faces.Add(new int3(3, 9, 4));
            faces.Add(new int3(3, 4, 2));
            faces.Add(new int3(3, 2, 6));
            faces.Add(new int3(3, 6, 8));
            faces.Add(new int3(3, 8, 9));
            faces.Add(new int3(4, 9, 5));
            faces.Add(new int3(2, 4, 11));
            faces.Add(new int3(6, 2, 10));
            faces.Add(new int3(8, 6, 7));
            faces.Add(new int3(9, 8, 1));
        }

        static void Subdivide(ref NativeList<float3> vertices, ref NativeList<int3> faces, float radius)
        {
            var newFaces = new NativeList<int3>(faces.Length * 4, Allocator.Temp);
            var midpointCache = new NativeHashMap<long, int>(faces.Length * 3, Allocator.Temp);

            for (var i = 0; i < faces.Length; i++)
            {
                var face = faces[i];
                var a = face.x;
                var b = face.y;
                var c = face.z;

                // Get midpoints
                var ab = GetMidpoint(a, b, ref vertices, ref midpointCache, radius);
                var bc = GetMidpoint(b, c, ref vertices, ref midpointCache, radius);
                var ca = GetMidpoint(c, a, ref vertices, ref midpointCache, radius);

                // Create 4 new triangular faces
                newFaces.Add(new int3(a, ab, ca));
                newFaces.Add(new int3(b, bc, ab));
                newFaces.Add(new int3(c, ca, bc));
                newFaces.Add(new int3(ab, bc, ca));
            }

            faces.Clear();
            faces.AddRange(newFaces);

            newFaces.Dispose();
            midpointCache.Dispose();
        }

        static int GetMidpoint(int v1, int v2, ref NativeList<float3> vertices, ref NativeHashMap<long, int> cache, float radius)
        {
            var key = v1 < v2 ? ((long)v1 << 32) | (uint)v2 : ((long)v2 << 32) | (uint)v1;

            if (cache.TryGetValue(key, out var existing))
                return existing;

            var point1 = vertices[v1];
            var point2 = vertices[v2];
            var middle = math.normalize((point1 + point2) * 0.5f) * radius;

            vertices.Add(middle);
            var index = vertices.Length - 1;
            cache[key] = index;

            return index;
        }
    }
}
