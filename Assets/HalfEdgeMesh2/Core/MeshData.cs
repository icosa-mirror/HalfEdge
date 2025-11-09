using System;
using Unity.Collections;

namespace HalfEdgeMesh2
{
    public struct MeshData : IDisposable
    {
        public NativeArray<Vertex> vertices;
        public NativeArray<HalfEdge> halfEdges;
        public NativeArray<Face> faces;

        public int vertexCount;
        public int halfEdgeCount;
        public int faceCount;

        public bool IsCreated => vertices.IsCreated;

        public MeshData(int maxVertices, int maxHalfEdges, int maxFaces, Allocator allocator)
        {
            vertices = new NativeArray<Vertex>(maxVertices, allocator);
            halfEdges = new NativeArray<HalfEdge>(maxHalfEdges, allocator);
            faces = new NativeArray<Face>(maxFaces, allocator);

            vertexCount = 0;
            halfEdgeCount = 0;
            faceCount = 0;
        }

        public void Dispose()
        {
            if (vertices.IsCreated)
                vertices.Dispose();
            if (halfEdges.IsCreated)
                halfEdges.Dispose();
            if (faces.IsCreated)
                faces.Dispose();
        }

        public int AddVertex(Vertex vertex)
        {
            if (vertexCount >= vertices.Length)
                throw new InvalidOperationException("Vertex array is full");

            var index = vertexCount;
            vertices[index] = vertex;
            vertexCount++;
            return index;
        }

        public int AddHalfEdge(HalfEdge halfEdge)
        {
            if (halfEdgeCount >= halfEdges.Length)
                throw new InvalidOperationException("HalfEdge array is full");

            var index = halfEdgeCount;
            halfEdges[index] = halfEdge;
            halfEdgeCount++;
            return index;
        }

        public int AddFace(Face face)
        {
            if (faceCount >= faces.Length)
                throw new InvalidOperationException("Face array is full");

            var index = faceCount;
            faces[index] = face;
            faceCount++;
            return index;
        }

        // Try to create a face from vertex list, reversing order if needed
        public bool TryAddFaceFromVertices(NativeList<int> vertexIndices, bool tryReversed = true)
        {
            if (vertexIndices.Length < 3)
                return false;

            // Try normal order first
            if (TryAddFaceFromVerticesInternal(vertexIndices, false))
                return true;

            // If that didn't work and we should try reversed, try reversed order
            if (tryReversed && TryAddFaceFromVerticesInternal(vertexIndices, true))
                return true;

            return false;
        }

        bool TryAddFaceFromVerticesInternal(NativeList<int> vertexIndices, bool reversed)
        {
            if (halfEdgeCount + vertexIndices.Length > halfEdges.Length)
                return false; // Not enough space for half-edges

            if (faceCount >= faces.Length)
                return false; // Not enough space for face

            var faceStartHe = halfEdgeCount;
            var savedHalfEdgeCount = halfEdgeCount;
            var savedFaceCount = faceCount;

            var newFaceIdx = AddFace(new Face(faceStartHe));

            for (var i = 0; i < vertexIndices.Length; i++)
            {
                var vertIdx = reversed ? vertexIndices[vertexIndices.Length - 1 - i] : vertexIndices[i];
                var newHe = new HalfEdge(
                    next: faceStartHe + ((i + 1) % vertexIndices.Length),
                    twin: -1,
                    vertex: vertIdx,
                    face: newFaceIdx
                );
                AddHalfEdge(newHe);
            }

            // Validate: check that half-edges don't conflict with existing edges
            // Each half-edge from v0->v1 must not duplicate an existing v0->v1 edge
            // (but v1->v0 is fine - that becomes a twin pair)
            for (var i = 0; i < vertexIndices.Length; i++)
            {
                var v0 = reversed ? vertexIndices[vertexIndices.Length - 1 - i] : vertexIndices[i];
                var v1 = reversed ? vertexIndices[vertexIndices.Length - 1 - ((i + 1) % vertexIndices.Length)] : vertexIndices[(i + 1) % vertexIndices.Length];

                // Check all existing half-edges to see if any go from v0 to v1
                for (var existingHeIdx = 0; existingHeIdx < savedHalfEdgeCount; existingHeIdx++)
                {
                    var existingHe = halfEdges[existingHeIdx];
                    if (existingHe.next == -1)
                        continue; // Invalid half-edge

                    var existingNext = halfEdges[existingHe.next];

                    // Check if existing edge goes v0->v1 (same direction as our new edge)
                    if (existingHe.vertex == v0 && existingNext.vertex == v1)
                    {
                        // Bad orientation - already have an edge in this direction
                        halfEdgeCount = savedHalfEdgeCount;
                        faceCount = savedFaceCount;
                        return false;
                    }
                }
            }

            // Update vertex half-edge references
            for (var i = 0; i < vertexIndices.Length; i++)
            {
                var vertIdx = reversed ? vertexIndices[vertexIndices.Length - 1 - i] : vertexIndices[i];
                var v = vertices[vertIdx];
                if (v.halfEdge == -1)
                {
                    v.halfEdge = faceStartHe + i;
                    vertices[vertIdx] = v;
                }
            }

            return true;
        }

        public MeshData Compact(Allocator allocator)
        {
            var result = new MeshData
            {
                vertices = new NativeArray<Vertex>(vertexCount, allocator),
                halfEdges = new NativeArray<HalfEdge>(halfEdgeCount, allocator),
                faces = new NativeArray<Face>(faceCount, allocator),
                vertexCount = vertexCount,
                halfEdgeCount = halfEdgeCount,
                faceCount = faceCount
            };

            NativeArray<Vertex>.Copy(vertices, result.vertices, vertexCount);
            NativeArray<HalfEdge>.Copy(halfEdges, result.halfEdges, halfEdgeCount);
            NativeArray<Face>.Copy(faces, result.faces, faceCount);

            return result;
        }
    }
}