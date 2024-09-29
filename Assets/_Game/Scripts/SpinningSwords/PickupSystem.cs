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

            PickupSpeedBoostJob pickupSpeedBoostJob = new PickupSpeedBoostJob
            {
                DestroyEntityLookup = SystemAPI.GetComponentLookup<DestroyEntity>(),
                ThirdPersonCharacterComponentLookup = SystemAPI.GetComponentLookup<ThirdPersonCharacterComponent>(),
                Ecb = SystemAPI.GetSingleton<InstantiateCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                Time = SystemAPI.Time.ElapsedTime
            };
            pickupSpeedBoostJob.ScheduleParallel();

            StopSpeedBoostJob stopSpeedBoostJob = new StopSpeedBoostJob
            {
                Ecb = SystemAPI.GetSingleton<InstantiateCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                Time = SystemAPI.Time.ElapsedTime
            };
            stopSpeedBoostJob.ScheduleParallel();
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
            [NativeDisableParallelForRestriction]
            public ComponentLookup<DestroyEntity> DestroyEntityLookup;
            [NativeDisableParallelForRestriction]
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

        [WithAll(typeof(PickupSpeedBoost))]
        [WithNone(typeof(DestroyEntity))]
        public partial struct PickupSpeedBoostJob : IJobEntity
        {
            [NativeDisableParallelForRestriction]
            public ComponentLookup<DestroyEntity> DestroyEntityLookup;
            [NativeDisableParallelForRestriction]
            public ComponentLookup<ThirdPersonCharacterComponent> ThirdPersonCharacterComponentLookup;

            public EntityCommandBuffer.ParallelWriter Ecb;
            public double Time;

            public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, in DynamicBuffer<StatefulTriggerEvent> collisionEvents, in PickupSpeedBoost pickupSpeedBoost)
            {
                for (int i = 0; i < collisionEvents.Length; i++)
                {
                    StatefulTriggerEvent collisionEvent = collisionEvents[i];
                    if (ThirdPersonCharacterComponentLookup.TryGetComponent(collisionEvent.EntityB, out ThirdPersonCharacterComponent thirdPersonCharacterComponent))
                    {
                        Ecb.AddComponent(sortKey, collisionEvent.EntityB, new EnableSpeedBoost
                        {
                            NormalSpeed = thirdPersonCharacterComponent.GroundMaxSpeed,
                            DisableAt = Time + pickupSpeedBoost.Duration
                        });
                        Ecb.SetComponentEnabled<EnableSpeedBoost>(sortKey, collisionEvent.EntityB, true);
                        thirdPersonCharacterComponent.GroundMaxSpeed = pickupSpeedBoost.BoostValue;
                        ThirdPersonCharacterComponentLookup[collisionEvent.EntityB] = thirdPersonCharacterComponent;

                        DestroyEntityLookup.SetComponentEnabled(entity, true);

                        return;
                    }
                }
            }
        }

        public partial struct StopSpeedBoostJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            public double Time;

            public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, in EnableSpeedBoost enableSpeedBoost, ref ThirdPersonCharacterComponent thirdPersonCharacterComponent)
            {
                if (Time < enableSpeedBoost.DisableAt) return;

                thirdPersonCharacterComponent.GroundMaxSpeed = enableSpeedBoost.NormalSpeed;
                Ecb.SetComponentEnabled<EnableSpeedBoost>(sortKey, entity, false);
            }
        }
    }
}
