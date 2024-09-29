using Unity.Entities;

namespace SpinningSwords.Data
{
    public struct Neighbours : IBufferElementData
    {
        public Entity Entity;
    }

    public struct GatherNeighbourRadius : IComponentData
    {
        public float Radius;
        public float Increment;
        public float MaxRadius;
        public float NormalRadius;
    }
}
