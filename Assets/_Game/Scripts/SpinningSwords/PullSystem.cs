using SpinningSwords.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace SpinningSwords
{
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    public partial struct PullSystem : ISystem
    {
        private EntityQuery pullerQuery;

        public void OnCreate(ref SystemState state)
        {
            pullerQuery = SystemAPI.QueryBuilder().WithAll<LocalToWorld, Puller>().Build();

            state.RequireForUpdate<Puller>();
            state.RequireForUpdate<Pullable>();
        }

        public void OnUpdate(ref SystemState state)
        {
            NativeList<Entity> pullerEntities = pullerQuery.ToEntityListAsync(state.WorldUpdateAllocator, out JobHandle dep);

            PullJob pullJob = new PullJob
            {
                PullerEntities = pullerEntities,
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
                PullerLookup = SystemAPI.GetComponentLookup<Puller>(true),
                DeltaTime = SystemAPI.Time.fixedDeltaTime,
            };
            state.Dependency = pullJob.ScheduleParallel(dep);
        }

        [WithAll(typeof(Pullable))]
        public partial struct PullJob : IJobEntity
        {
            [ReadOnly]
            public NativeList<Entity> PullerEntities;
            [ReadOnly]
            public ComponentLookup<LocalTransform> LocalTransformLookup;
            [ReadOnly]
            public ComponentLookup<Puller> PullerLookup;

            public float DeltaTime;

            public void Execute(ref PhysicsVelocity velocity, in PhysicsCollider collider, in PhysicsMass mass,
                in LocalTransform localTransform)
            {
                for (int i = 0; i < PullerEntities.Length; i++)
                {
                    Entity pullerEntity = PullerEntities[i];
                    Puller Puller = PullerLookup[pullerEntity];
                    float3 pos = LocalTransformLookup[pullerEntity].Position;
                    velocity.ApplyExplosionForce(
                        mass, collider, localTransform.Position, localTransform.Rotation,
                        -Puller.Strength, pos, Puller.Range,
                        DeltaTime, math.up());
                }
            }
        }
    }
}
