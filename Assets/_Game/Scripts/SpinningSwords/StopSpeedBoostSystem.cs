using SpinningSwords.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics.Systems;

namespace SpinningSwords
{
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(PhysicsSystemGroup))]
    public partial struct StopSpeedBoostSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            StopSpeedBoostJob stopSpeedBoostJob = new StopSpeedBoostJob
            {
                EnableSpeedBoostLookup = SystemAPI.GetComponentLookup<EnableSpeedBoost>(),
                Time = SystemAPI.Time.ElapsedTime
            };
            stopSpeedBoostJob.ScheduleParallel();
        }

        [WithAll(typeof(EnableSpeedBoost))]
        public partial struct StopSpeedBoostJob : IJobEntity
        {
            [NativeDisableParallelForRestriction]
            public ComponentLookup<EnableSpeedBoost> EnableSpeedBoostLookup;
            public double Time;

            public void Execute(Entity entity, ref ThirdPersonCharacterComponent thirdPersonCharacterComponent)
            {
                EnableSpeedBoost enableSpeedBoost = EnableSpeedBoostLookup[entity];
                if (Time < enableSpeedBoost.DisableAt) return;

                thirdPersonCharacterComponent.GroundMaxSpeed = enableSpeedBoost.NormalSpeed;
                EnableSpeedBoostLookup.SetComponentEnabled(entity, false);
            }
        }
    }
}
