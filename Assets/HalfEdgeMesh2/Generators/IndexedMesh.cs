using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace HalfEdgeMesh2.Generators
{
    // Converts indexed mesh data (vertices + face indices) to HalfEdgeMesh2 format
    public static class IndexedMesh
    {
        // Generate mesh from arrays of vertices and face indices
        // faces: array of arrays where each inner array contains vertex indices for one face
        public static MeshData Generate(float3[] vertices, int[][] faces, Allocator allocator)
        {
            var vertexCount = vertices.Length;
            var faceCount = faces.Length;

            // Estimate edge count (assuming mostly quads)
            var estimatedEdgeCount = faceCount * 4;
            var builder = new MeshBuilder(Allocator.TempJob, estimatedEdgeCount);

            // Add all vertices
            for (var i = 0; i < vertexCount; i++)
                builder.AddVertex(vertices[i]);

            // Add all faces
            for (var i = 0; i < faceCount; i++)
            {
                var faceIndices = faces[i];
                if (faceIndices.Length < 3)
                    continue; // Skip degenerate faces

                // Use appropriate AddFace method based on face size
                if (faceIndices.Length == 3)
                    builder.AddFace(faceIndices[0], faceIndices[1], faceIndices[2]);
                else if (faceIndices.Length == 4)
                    builder.AddFace(faceIndices[0], faceIndices[1], faceIndices[2], faceIndices[3]);
                else
                    builder.AddFace(new System.ReadOnlySpan<int>(faceIndices));
            }

            var result = builder.Build(allocator);
            builder.Dispose();
            return result;
        }

        // Generate mesh from NativeArrays (Burst-compatible version)
        public static MeshData Generate(NativeArray<float3> vertices, NativeArray<int> faceIndices, NativeArray<int> faceSizes, Allocator allocator)
        {
            var vertexCount = vertices.Length;
            var faceCount = faceSizes.Length;

            // Estimate edge count
            var totalFaceIndices = faceIndices.Length;
            var builder = new MeshBuilder(Allocator.TempJob, totalFaceIndices);

            // Add all vertices
            for (var i = 0; i < vertexCount; i++)
                builder.AddVertex(vertices[i]);

            // Add all faces
            var indexOffset = 0;
            for (var i = 0; i < faceCount; i++)
            {
                var faceSize = faceSizes[i];
                if (faceSize < 3)
                {
                    indexOffset += faceSize;
                    continue; // Skip degenerate faces
                }

                // Extract face indices
                if (faceSize == 3)
                {
                    builder.AddFace(
                        faceIndices[indexOffset],
                        faceIndices[indexOffset + 1],
                        faceIndices[indexOffset + 2]);
                }
                else if (faceSize == 4)
                {
                    builder.AddFace(
                        faceIndices[indexOffset],
                        faceIndices[indexOffset + 1],
                        faceIndices[indexOffset + 2],
                        faceIndices[indexOffset + 3]);
                }
                else
                {
                    // For n-gons, we need to use unsafe code
                    unsafe
                    {
                        var span = new System.ReadOnlySpan<int>((int*)faceIndices.GetUnsafeReadOnlyPtr() + indexOffset, faceSize);
                        builder.AddFace(span);
                    }
                }

                indexOffset += faceSize;
            }

            var result = builder.Build(allocator);
            builder.Dispose();
            return result;
        }
    }
}
