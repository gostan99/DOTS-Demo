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
        public float InitialOrbitSpeed;
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
        public float InitialOrbitSpeed;
        public float OrbitDistance;
        public float MaxOrbitSpeed;
        public float Weight;
        public float MaxWeight;
        public int MaxSwordCount;
        public int SwordCount;

        public bool StopPickupSwordCollision;
        public bool ReachMaxSwordCount => SwordCount >= MaxSwordCount;

        public void DefaultValue()
        {
            OrbitTargetEntity = Entity.Null;
            InitialOrbitSpeed = 125;
            OrbitDistance = 1.75f;
            MaxOrbitSpeed = 500;
            MaxWeight = 50;
            MaxSwordCount = 50;
            SwordCount = 0;
            Weight = 1;
        }
    }

    public struct SwordEquidistant : IComponentData, IEnableableComponent
    {
        public float Duration;
        public float DurationAfterReinitializedMultiplier;
        public double FinishTime;
        public bool SwordSpeedIsCalculated;
        public bool NeedReinitializeOrbitSwords;

        public void DefaultValue()
        {
            Duration = 1.45f;
            DurationAfterReinitializedMultiplier = 1.68f;
        }
    }

    public struct SwordColliders : IComponentData
    {
        public BlobAssetReference<Collider> OrbitCollider;
        public BlobAssetReference<Collider> DetachedCollider;
    }

    public struct PickupSword : IComponentData
    {
    }
}