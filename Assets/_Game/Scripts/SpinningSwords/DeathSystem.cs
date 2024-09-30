using BovineLabs.Core.LifeCycle;
using BovineLabs.Core.PhysicsStates;
using SpinningSwords.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace SpinningSwords
{
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial struct DeathSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            ActorDeathJob actorDeathJob = new ActorDeathJob
            {
                ActorKillableLookup = SystemAPI.GetComponentLookup<ActorKillable>(),
                DestroyEntityLookup = SystemAPI.GetComponentLookup<DestroyEntity>(),
                SwordControllerLookup = SystemAPI.GetComponentLookup<SwordController>(),
            };
            actorDeathJob.ScheduleParallel();
        }

        public partial struct ActorDeathJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<SwordController> SwordControllerLookup;
            [ReadOnly]
            public ComponentLookup<ActorKillable> ActorKillableLookup;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<DestroyEntity> DestroyEntityLookup;


            public void Execute(Entity entity, DynamicBuffer<StatefulCollisionEvent> collisionEvents)
            {
                foreach (StatefulCollisionEvent colEvent in collisionEvents)
                {
                    if (SwordControllerLookup.TryGetComponent(colEvent.EntityB, out SwordController swordController))
                    {
                        if (swordController.SwordCount == 0 && ActorKillableLookup.HasComponent(colEvent.EntityB))
                        {
                            DestroyEntityLookup.SetComponentEnabled(colEvent.EntityB, true);
                        }
                    }
                }
            }
        }
    }
}
