using _GENERATED;
using BovineLabs.Core.LifeCycle;
using SpinningSwords.Data;
using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Extensions;


namespace SpinningSwords
{
    [BurstCompile]
    public partial struct FakePlayerSystem : ISystem
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
    public partial struct FakePlayerInitSystem : ISystem
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
                in SystemAPI.Query<RefRO<SwordPrefab>, RefRW<SwordColliders>>().WithAll<FakePlayerTag>().WithAny<InitializeSubSceneEntity, InitializeEntity>().WithEntityAccess()) // mặc dù là sẽ chỉ có 1 player thôi
            {
                // Về sau sẽ phải thay đổi giá trị của collider của từng fake player nên cần để collider của từng fake player thành unique
                SystemAPI.GetComponentRW<PhysicsCollider>(entity).ValueRW.MakeUnique(entity, ecb);

                #region Set SwordColliders

                PhysicsCollider swordPrefabCollider = SystemAPI.GetComponent<PhysicsCollider>(swordPrefab.ValueRO.Value);
                // Orbitting collider
                BlobAssetReference<Collider> orbitCollider = swordPrefabCollider.Value.Value.Clone();
                CollisionFilter orbitColFilter = orbitCollider.Value.GetCollisionFilter();
                orbitColFilter.BelongsTo = 0u | (PhysicsCategory.Sword);
                orbitColFilter.CollidesWith = 0u | (PhysicsCategory.Sword | PhysicsCategory.Fake_Player | PhysicsCategory.Bot | PhysicsCategory.Player);
                orbitColFilter.GroupIndex = -entity.Index; // sword of the same orbit target don't collider with each other
                orbitCollider.Value.SetCollisionFilter(orbitColFilter);
                swordCollider.ValueRW.OrbitCollider = orbitCollider;
                // Detached collider
                BlobAssetReference<Collider> detachedCollider = swordPrefabCollider.Value.Value.Clone();
                CollisionFilter detachedColFilter = detachedCollider.Value.GetCollisionFilter();
                detachedColFilter.BelongsTo = 0u | (PhysicsCategory.Sword | PhysicsCategory.Player);
                detachedColFilter.CollidesWith = 0u | PhysicsCategory.Ground;
                detachedCollider.Value.SetCollisionFilter(detachedColFilter);
                swordCollider.ValueRW.DetachedCollider = detachedCollider;

                #endregion
            }
        }
    }
}