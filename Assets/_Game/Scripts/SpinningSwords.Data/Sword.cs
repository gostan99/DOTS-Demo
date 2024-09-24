using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace SpinningSwords.Data
{
    public struct SwordPrefab : IComponentData
    {
        public Entity Value;
    }

    public struct Sword : IComponentData
    {
        public float3 PlanarForward;
        public float OrbitSpeed;
    }

    [ChunkSerializable]
    public struct SwordOrbitTarget : ISharedComponentData
    {
        public Entity Target;
        public Entity TargetParent;
    }

    public struct SwordController : IComponentData
    {
        public Entity OrbitTargetEntity;
        public float OrbitDistance;
        public float OrbitSpeed;
        public float OrbitMaxSpeed;
        public float Weight;
        public float MaxWeight;
        public int MaxSwordCount;
        public int SwordCount;

        public bool StopPickupSwordCollision;
        public bool ReachMaxSwordCount => SwordCount >= MaxSwordCount;

        public void DefaultValue()
        {
            OrbitTargetEntity = Entity.Null;
            OrbitDistance = 1.75f;
            OrbitSpeed = 125;
            OrbitMaxSpeed = 500;
            MaxWeight = 50;
            MaxSwordCount = 50;
            SwordCount = 0;
            Weight = 1;
        }
    }

    public struct SwordEquidistant : IComponentData, IEnableableComponent
    {
        public float Duration;

        public void DefaultValue()
        {
            Duration = 1.45f;
        }
    }

    public struct SwordEquidistantFinish : IComponentData, IEnableableComponent
    {
        public double FinishTime;
        public float FinishSpeed;
    }

    public struct SwordColliders : IComponentData
    {
        public BlobAssetReference<Collider> OrbitCollider;
        public BlobAssetReference<Collider> DetachedCollider;
    }

    public struct PickupSword : IComponentData
    {
    }

    [InternalBufferCapacity(50)]
    public struct SwordBuffer : IBufferElementData
    {
        public Entity Value;
    }
}