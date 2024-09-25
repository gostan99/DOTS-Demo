using _GENERATED;
using BovineLabs.Core.LifeCycle;
using SpinningSwords.Data;
using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Collider = Unity.Physics.Collider;

namespace SpinningSwords
{
    [BurstCompile]
    public partial struct PlayerSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
        }
    }


    [BurstCompile]
    [UpdateInGroup(typeof(InitializeSystemGroup))]
    public partial struct PlayerInitSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            foreach ((RefRO<SwordPrefab> swordPrefab, RefRW<SwordColliders> swordCollider, Entity entity)
                in SystemAPI.Query<RefRO<SwordPrefab>, RefRW<SwordColliders>>().WithAll<PlayerTag>().WithAny<InitializeSubSceneEntity, InitializeEntity>().WithEntityAccess()) // mặc dù là sẽ chỉ có 1 player thôi
            {
                #region Set SwordColliders

                PhysicsCollider swordPrefabCollider = SystemAPI.GetComponent<PhysicsCollider>(swordPrefab.ValueRO.Value);

                BlobAssetReference<Collider> orbitCollider = swordPrefabCollider.Value.Value.Clone();
                CollisionFilter orbitColFilter = orbitCollider.Value.GetCollisionFilter();
                orbitColFilter.BelongsTo = 0u | (PhysicsCategory.Player);
                orbitColFilter.CollidesWith = 0u | (PhysicsCategory.Fake_Player | PhysicsCategory.Bot);
                orbitColFilter.GroupIndex = -entity.Index;
                orbitCollider.Value.SetCollisionFilter(orbitColFilter);
                swordCollider.ValueRW.OrbitCollider = orbitCollider;

                BlobAssetReference<Collider> detachedCollider = swordPrefabCollider.Value.Value.Clone();
                CollisionFilter detachedColFilter = detachedCollider.Value.GetCollisionFilter();
                detachedColFilter.BelongsTo = 0u | PhysicsCategory.Sword;
                detachedColFilter.CollidesWith = 0u | PhysicsCategory.Ground;
                detachedCollider.Value.SetCollisionFilter(detachedColFilter);
                swordCollider.ValueRW.DetachedCollider = detachedCollider;

                #endregion
            }
        }
    }
}
