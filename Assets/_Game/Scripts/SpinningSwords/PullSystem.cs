using BovineLabs.Core.LifeCycle;
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
            state.RequireForUpdate<PullableTag>();
        }

        public void OnUpdate(ref SystemState state)
        {
            state.CompleteDependency();
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

        [WithAll(typeof(PullableTag))]
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


    [BurstCompile]
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(InitializeSystemGroup))]
    public partial struct PullableInitSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
        }

        [BurstCompile]
        public unsafe void OnUpdate(ref SystemState state)
        {
            foreach (RefRW<PhysicsCollider> physicsCollider in SystemAPI.Query<RefRW<PhysicsCollider>>().WithAll<InitializeEntity, PullableTag, Prefab>())
            {
                // set GroupIndex -1 để entity nào ban đầu collide được thì về sau có thể không muốn collide nữa thì chỉ cần entity đó cũng set GroupIndex -1
                ref Unity.Physics.Collider collider = ref physicsCollider.ValueRW.Value.Value;
                ColliderKey colliderKey = new ColliderKey(collider.TotalNumColliderKeyBits, 0);
                if (collider.GetChild(ref colliderKey, out ChildCollider child))
                {
                    CollisionFilter colFilter = child.Collider->GetCollisionFilter();
                    colFilter.GroupIndex = -1;
                    child.Collider->SetCollisionFilter(colFilter);
                }
            }
        }
    }

}
