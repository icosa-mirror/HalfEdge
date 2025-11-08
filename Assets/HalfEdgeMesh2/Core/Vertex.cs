using Unity.Mathematics;

namespace HalfEdgeMesh2
{
    public struct Vertex
    {
        public float3 position;
        public float2 uv;
        public int halfEdge;

        public Vertex(float3 position) => (this.position, this.uv, halfEdge) = (position, float2.zero, -1);

        public Vertex(float3 position, int halfEdge) => (this.position, this.uv, this.halfEdge) = (position, float2.zero, halfEdge);

        public Vertex(float3 position, float2 uv) => (this.position, this.uv, halfEdge) = (position, uv, -1);

        public Vertex(float3 position, float2 uv, int halfEdge) => (this.position, this.uv, this.halfEdge) = (position, uv, halfEdge);
    }
}