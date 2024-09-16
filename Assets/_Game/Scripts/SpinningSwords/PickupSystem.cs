using BovineLabs.Core.LifeCycle;
using BovineLabs.Core.PhysicsStates;
using SpinningSwords.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace SpinningSwords
{
    [BurstCompile]
    public partial struct PickupSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            PickupSwordJob pickupSwordJob = new PickupSwordJob
            {
                DestroyEntityLookup = SystemAPI.GetComponentLookup<DestroyEntity>(),
                SwordControllerLookup = SystemAPI.GetComponentLookup<SwordController>(),
                SwordPrefabLookup = SystemAPI.GetComponentLookup<SwordPrefab>(true),
                Ecb = SystemAPI.GetSingleton<InstantiateCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
            };
            state.Dependency = pickupSwordJob.Schedule(state.Dependency);
        }

        [WithAll(typeof(PickupSword))]
        [WithNone(typeof(DestroyEntity))]
        public partial struct PickupSwordJob : IJobEntity
        {
            public ComponentLookup<DestroyEntity> DestroyEntityLookup;
            public ComponentLookup<SwordController> SwordControllerLookup;
            [ReadOnly]
            public ComponentLookup<SwordPrefab> SwordPrefabLookup;

            public EntityCommandBuffer Ecb;

            public void Execute(Entity entity, DynamicBuffer<StatefulTriggerEvent> collisionEvents)
            {
                if (collisionEvents.Length == 0) return;

                for (int i = 0; i < collisionEvents.Length; i++)
                {
                    StatefulTriggerEvent collisionEvent = collisionEvents[i];
                    if (SwordControllerLookup.TryGetComponent(collisionEvent.EntityB, out SwordController swordController))
                    {
                        if (!swordController.ReachMaxSwordCount && SwordPrefabLookup.TryGetComponent(collisionEvent.EntityB, out SwordPrefab swordPrefab))
                        {
                            swordController.SwordCount++;
                            SwordControllerLookup[collisionEvent.EntityB] = swordController;

                            Ecb.Instantiate(swordPrefab.Value);

                            DestroyEntityLookup.SetComponentEnabled(entity, true);

                            return;
                        }
                    }
                }
            }
        }
    }
}
