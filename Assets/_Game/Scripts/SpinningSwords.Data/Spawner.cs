using Unity.Entities;
using Unity.Mathematics;

namespace SpinningSwords.Data
{
    public struct SpawnEntityConfig : IComponentData
    {
        public Entity Prefab;
        public int Count;
        public float MinSpawnRadius;
        public float MaxSpawnRadius;
    }

    public struct TimeToSpawn : ISharedComponentData
    {
        public float Value;
    }

    public struct Schedule : IBufferElementData
    {
        public float Time;
    }

    public struct ScheduleIndex : IComponentData
    {
        public int Value;
    }

    public struct ArenaCenter : IComponentData
    {
        public float3 Value;
    }
}
