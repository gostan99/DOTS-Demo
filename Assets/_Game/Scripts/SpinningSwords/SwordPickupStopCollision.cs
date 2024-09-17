using SpinningSwords.Data;
using Unity.Burst;
using Unity.Entities;
using Unity.Physics;


namespace SpinningSwords
{
    [BurstCompile]
    public partial struct SwordPickupStopCollision : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SwordController>();
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            StopCollisionJob stopCollisionJob = new StopCollisionJob
            {
            };

            stopCollisionJob.ScheduleParallel();
        }

        public partial struct StopCollisionJob : IJobEntity
        {
            public void Execute(ref SwordController swordController, ref PhysicsCollider physicsCollider)
            {
                if (!swordController.StopPickupSwordCollision && swordController.ReachMaxSwordCount)
                {
                    swordController.StopPickupSwordCollision = true;

                    ref Unity.Physics.Collider collider = ref physicsCollider.Value.Value;
                    CollisionFilter colFilter = collider.GetCollisionFilter();
                    colFilter.GroupIndex = -1;
                    collider.SetCollisionFilter(colFilter);
                }
            }
        }
    }
}