using BovineLabs.Core.LifeCycle;
using BovineLabs.Core.PhysicsStates;
using SpinningSwords.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Systems;

namespace SpinningSwords
{
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(PhysicsSystemGroup))]
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
            Unity.Jobs.JobHandle pickupSwordJobHandle = pickupSwordJob.Schedule(state.Dependency);

            PickupSwordWeightJob pickupSwordWeightJob = new PickupSwordWeightJob
            {
                DestroyEntityLookup = SystemAPI.GetComponentLookup<DestroyEntity>(),
                SwordControllerLookup = SystemAPI.GetComponentLookup<SwordController>(),
                Ecb = SystemAPI.GetSingleton<InstantiateCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
            };
            state.Dependency = pickupSwordWeightJob.Schedule(pickupSwordJobHandle); //todo: tách chỉ số SwordCount và SwordWeight để 2 job này không bị phụ thuộc vào nhau
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

            public void Execute(Entity entity, in DynamicBuffer<StatefulTriggerEvent> collisionEvents)
            {
                for (int i = 0; i < collisionEvents.Length; i++)
                {
                    StatefulTriggerEvent collisionEvent = collisionEvents[i];
                    if (SwordControllerLookup.TryGetComponent(collisionEvent.EntityB, out SwordController swordController))
                    {
                        if (!swordController.ReachMaxSwordCount && SwordPrefabLookup.TryGetComponent(collisionEvent.EntityB, out SwordPrefab swordPrefab))
                        {
                            swordController.SwordCount++;
                            SwordControllerLookup[collisionEvent.EntityB] = swordController;

                            Entity newEntity = Ecb.Instantiate(swordPrefab.Value);
                            Ecb.AddSharedComponent(newEntity, new SwordOrbitTarget { TargetParent = collisionEvent.EntityB, Target = swordController.OrbitTargetEntity });

                            DestroyEntityLookup.SetComponentEnabled(entity, true);

                            return;
                        }
                    }
                }
            }
        }


        [WithAll(typeof(PickupSwordWeight))]
        [WithNone(typeof(DestroyEntity))]
        public partial struct PickupSwordWeightJob : IJobEntity
        {
            public ComponentLookup<DestroyEntity> DestroyEntityLookup;
            public ComponentLookup<SwordController> SwordControllerLookup;

            public EntityCommandBuffer Ecb;

            public void Execute(Entity entity, in DynamicBuffer<StatefulTriggerEvent> collisionEvents, in PickupSwordWeight pickupSwordWeight)
            {
                for (int i = 0; i < collisionEvents.Length; i++)
                {
                    StatefulTriggerEvent collisionEvent = collisionEvents[i];
                    if (SwordControllerLookup.TryGetComponent(collisionEvent.EntityB, out SwordController swordController))
                    {
                        if (!swordController.ReachMaxSwordWeight)
                        {
                            swordController.Weight += pickupSwordWeight.Value;
                            swordController.Weight = math.clamp(swordController.Weight + pickupSwordWeight.Value, 0, swordController.MaxWeight);
                            SwordControllerLookup[collisionEvent.EntityB] = swordController;

                            DestroyEntityLookup.SetComponentEnabled(entity, true);

                            return;
                        }
                    }
                }
            }
        }
    }
}
