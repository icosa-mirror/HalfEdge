using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace HalfEdgeMesh2.Unity
{
    public enum NormalGenerationMode
    {
        Smooth,
        Flat
    }

    public static class MeshConversion
    {
        // Static buffers for memory reuse
        static NativeArray<Vector3> s_vertexBuffer;
        static NativeArray<Vector2> s_uvBuffer;
        static NativeArray<int> s_triangleBuffer;
        static bool s_buffersInitialized;
        public static Mesh ToUnityMesh(ref MeshData meshData, NormalGenerationMode mode = NormalGenerationMode.Smooth)
        {
            var mesh = new Mesh();
            if (mode == NormalGenerationMode.Smooth)
                UpdateMeshDataSmooth(mesh, ref meshData);
            else
                UpdateMeshDataFlat(mesh, ref meshData);
            return mesh;
        }

        public static void UpdateUnityMesh(Mesh mesh, ref MeshData meshData, NormalGenerationMode mode = NormalGenerationMode.Smooth)
        {
            mesh.Clear();
            if (mode == NormalGenerationMode.Smooth)
                UpdateMeshDataSmooth(mesh, ref meshData);
            else
                UpdateMeshDataFlat(mesh, ref meshData);
        }

        static void UpdateMeshDataSmooth(Mesh mesh, ref MeshData meshData)
        {
            var triangleCount = GetTriangleCount(ref meshData);
            EnsureBufferCapacity(meshData.vertexCount, triangleCount);

            // Extract vertices and UVs into static buffers
            ExtractVerticesSmooth(ref meshData, ref s_vertexBuffer);
            ExtractUVsSmooth(ref meshData, ref s_uvBuffer);
            ExtractTrianglesSmooth(ref meshData, ref s_triangleBuffer);

            // Set mesh data using buffer slices
            var vertexSlice = s_vertexBuffer.GetSubArray(0, meshData.vertexCount);
            var uvSlice = s_uvBuffer.GetSubArray(0, meshData.vertexCount);
            var triangleSlice = s_triangleBuffer.GetSubArray(0, triangleCount);

            mesh.SetVertices(vertexSlice);
            mesh.SetUVs(0, uvSlice);
            mesh.SetIndices(triangleSlice, MeshTopology.Triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        static void EnsureBufferCapacity(int vertexCount, int triangleCount)
        {
            if (!s_buffersInitialized || s_vertexBuffer.Length < vertexCount)
            {
                if (s_buffersInitialized) s_vertexBuffer.Dispose();
                s_vertexBuffer = new NativeArray<Vector3>(math.max(vertexCount, 64), Allocator.Persistent);
            }

            if (!s_buffersInitialized || s_uvBuffer.Length < vertexCount)
            {
                if (s_buffersInitialized) s_uvBuffer.Dispose();
                s_uvBuffer = new NativeArray<Vector2>(math.max(vertexCount, 64), Allocator.Persistent);
            }

            if (!s_buffersInitialized || s_triangleBuffer.Length < triangleCount)
            {
                if (s_buffersInitialized) s_triangleBuffer.Dispose();
                s_triangleBuffer = new NativeArray<int>(math.max(triangleCount, 192), Allocator.Persistent);
            }

            s_buffersInitialized = true;
        }

        [BurstCompile]
        static int GetTriangleCount(ref MeshData meshData)
        {
            var triangleCount = 0;
            for (var faceIndex = 0; faceIndex < meshData.faceCount; faceIndex++)
            {
                var face = meshData.faces[faceIndex];
                var vertexCount = CountFaceVertices(ref meshData, face.halfEdge);
                triangleCount += (vertexCount - 2) * 3;
            }
            return triangleCount;
        }

        [BurstCompile]
        static void ExtractVerticesSmooth(ref MeshData meshData, ref NativeArray<Vector3> buffer)
        {
            for (var i = 0; i < meshData.vertexCount; i++)
                buffer[i] = meshData.vertices[i].position;
        }

        [BurstCompile]
        static void ExtractUVsSmooth(ref MeshData meshData, ref NativeArray<Vector2> buffer)
        {
            for (var i = 0; i < meshData.vertexCount; i++)
                buffer[i] = meshData.vertices[i].uv;
        }

        [BurstCompile]
        static void ExtractTrianglesSmooth(ref MeshData meshData, ref NativeArray<int> buffer)
        {
            var triangleIndex = 0;
            for (var faceIndex = 0; faceIndex < meshData.faceCount; faceIndex++)
            {
                var face = meshData.faces[faceIndex];
                ExtractFaceTrianglesDirectSmooth(ref meshData, face.halfEdge, ref buffer, ref triangleIndex);
            }
        }

        [BurstCompile]
        static void ExtractFaceTrianglesDirectSmooth(ref MeshData meshData, int startHalfEdge, ref NativeArray<int> triangles, ref int triangleIndex)
        {
            // Directly triangulate without intermediate storage for better performance
            var firstVertex = -1;
            var prevVertex = -1;
            var vertexIndex = 0;
            var currentHe = startHalfEdge;

            do
            {
                var he = meshData.halfEdges[currentHe];
                var vertex = he.vertex;

                if (vertexIndex == 0)
                    firstVertex = vertex;
                else if (vertexIndex > 1)
                {
                    // Create triangle: first, prev, current
                    triangles[triangleIndex++] = firstVertex;
                    triangles[triangleIndex++] = prevVertex;
                    triangles[triangleIndex++] = vertex;
                }

                prevVertex = vertex;
                vertexIndex++;
                currentHe = he.next;
            } while (currentHe != startHalfEdge);
        }


        [BurstCompile]
        static int CountFaceVertices(ref MeshData meshData, int startHalfEdge)
        {
            var count = 0;
            var currentHe = startHalfEdge;

            do
            {
                count++;
                var he = meshData.halfEdges[currentHe];
                currentHe = he.next;
            } while (currentHe != startHalfEdge);

            return count;
        }

        static void UpdateMeshDataFlat(Mesh mesh, ref MeshData meshData)
        {
            var triangleCount = GetTriangleCount(ref meshData);
            var vertexCount = triangleCount; // For flat shading, each triangle vertex is unique

            EnsureBufferCapacity(vertexCount, triangleCount);

            // Extract vertices, UVs for flat shading
            ExtractVerticesFlat(ref meshData, ref s_vertexBuffer, ref s_uvBuffer, out var actualVertexCount);
            ExtractTrianglesFlat(triangleCount, ref s_triangleBuffer);

            // Set mesh data using buffer slices
            var vertexSlice = s_vertexBuffer.GetSubArray(0, actualVertexCount);
            var uvSlice = s_uvBuffer.GetSubArray(0, actualVertexCount);
            var triangleSlice = s_triangleBuffer.GetSubArray(0, triangleCount);

            mesh.SetVertices(vertexSlice);
            mesh.SetUVs(0, uvSlice);
            mesh.SetIndices(triangleSlice, MeshTopology.Triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        [BurstCompile]
        static void ExtractVerticesFlat(ref MeshData meshData, ref NativeArray<Vector3> vertexBuffer, ref NativeArray<Vector2> uvBuffer, out int actualVertexCount)
        {
            var vertexIndex = 0;

            for (var faceIndex = 0; faceIndex < meshData.faceCount; faceIndex++)
            {
                var face = meshData.faces[faceIndex];
                ExtractFaceVerticesFlat(ref meshData, face.halfEdge, ref vertexBuffer, ref uvBuffer, ref vertexIndex);
            }

            actualVertexCount = vertexIndex;
        }

        [BurstCompile]
        static void ExtractFaceVerticesFlat(ref MeshData meshData, int startHalfEdge, ref NativeArray<Vector3> vertices, ref NativeArray<Vector2> uvs, ref int vertexIndex)
        {
            // Collect face vertices
            var faceVertexCount = CountFaceVertices(ref meshData, startHalfEdge);
            var faceVertices = new NativeArray<Vector3>(faceVertexCount, Allocator.TempJob);
            var faceUVs = new NativeArray<Vector2>(faceVertexCount, Allocator.TempJob);

            var currentHe = startHalfEdge;
            for (var i = 0; i < faceVertexCount; i++)
            {
                var he = meshData.halfEdges[currentHe];
                var vertex = meshData.vertices[he.vertex];
                faceVertices[i] = vertex.position;
                faceUVs[i] = vertex.uv;
                currentHe = he.next;
            }

            // Triangulate face with duplicated vertices for flat shading
            for (var i = 1; i < faceVertexCount - 1; i++)
            {
                vertices[vertexIndex] = faceVertices[0];
                uvs[vertexIndex] = faceUVs[0];
                vertexIndex++;

                vertices[vertexIndex] = faceVertices[i];
                uvs[vertexIndex] = faceUVs[i];
                vertexIndex++;

                vertices[vertexIndex] = faceVertices[i + 1];
                uvs[vertexIndex] = faceUVs[i + 1];
                vertexIndex++;
            }

            faceVertices.Dispose();
            faceUVs.Dispose();
        }

        [BurstCompile]
        static void ExtractTrianglesFlat(int triangleCount, ref NativeArray<int> buffer)
        {
            for (var i = 0; i < triangleCount; i++)
                buffer[i] = i;
        }
    }
}