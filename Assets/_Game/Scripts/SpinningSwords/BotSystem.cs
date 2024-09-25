using _GENERATED;
using BovineLabs.Core.LifeCycle;
using SpinningSwords.Data;
using Unity.Burst;
using Unity.CharacterController;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Collider = Unity.Physics.Collider;

namespace SpinningSwords
{
    [BurstCompile]
    public partial struct BotSystem : ISystem
    {
        private EntityQuery potentialTargetQuery;
        public void OnCreate(ref SystemState state)
        {
            potentialTargetQuery = SystemAPI.QueryBuilder().WithAny<PlayerTag, FakePlayerTag>().WithAll<LocalTransform>().WithNone<DestroyEntity>().Build();
            state.RequireForUpdate(potentialTargetQuery);
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            UpdateFindNewTargetJob updateFindNewTargetJob = new UpdateFindNewTargetJob
            {
                BotFindNewTargetLookup = SystemAPI.GetComponentLookup<BotFindNewTarget>(),
                Time = SystemAPI.Time.ElapsedTime,
            };
            Unity.Jobs.JobHandle updateFindNewTargetJobHandle = updateFindNewTargetJob.ScheduleParallel(state.Dependency);

            NativeList<LocalTransform> potentialTargetTransforms = potentialTargetQuery.ToComponentDataListAsync<LocalTransform>(state.WorldUpdateAllocator, out Unity.Jobs.JobHandle getPotentialTargetTransformsJobHandle);
            NativeList<Entity> potentialTargets = potentialTargetQuery.ToEntityListAsync(state.WorldUpdateAllocator, out Unity.Jobs.JobHandle getpotentialTargetsJobHandle);
            JobHandle combineJobHandle = JobHandle.CombineDependencies(getPotentialTargetTransformsJobHandle, getpotentialTargetsJobHandle);
            combineJobHandle = JobHandle.CombineDependencies(combineJobHandle, updateFindNewTargetJobHandle);
            FindClosestTargetJob findClosestTargetJob = new FindClosestTargetJob
            {
                Time = SystemAPI.Time.ElapsedTime,
                BotFindNewTargetLookup = SystemAPI.GetComponentLookup<BotFindNewTarget>(),
                PotentialTargetTransforms = potentialTargetTransforms.AsDeferredJobArray(),
                PotentialTargets = potentialTargets.AsDeferredJobArray(),
            };
            JobHandle findClosestTargetJobHanlde = findClosestTargetJob.ScheduleParallel(combineJobHandle);

            ChaseTargetJob chaseTargetJob = new ChaseTargetJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(),
            };
            state.Dependency = chaseTargetJob.ScheduleParallel(findClosestTargetJobHanlde);
        }

        [WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
        [WithAll(typeof(BotFindNewTarget))]
        public partial struct UpdateFindNewTargetJob : IJobEntity
        {
            public double Time;
            [NativeDisableParallelForRestriction]
            public ComponentLookup<BotFindNewTarget> BotFindNewTargetLookup;

            public void Execute(Entity entity)
            {
                RefRW<BotFindNewTarget> cmp = BotFindNewTargetLookup.GetRefRW(entity);
                if (Time >= cmp.ValueRO.TimeToFindNewTarget)
                {
                    BotFindNewTargetLookup.SetComponentEnabled(entity, true);
                }
            }
        }

        [WithAll(typeof(BotFindNewTarget))]
        public partial struct FindClosestTargetJob : IJobEntity
        {
            public double Time;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<BotFindNewTarget> BotFindNewTargetLookup;
            [ReadOnly]
            public NativeArray<Entity> PotentialTargets;
            [ReadOnly]
            public NativeArray<LocalTransform> PotentialTargetTransforms;

            public void Execute(Entity entity, in LocalTransform transform, ref BotTarget target)
            {
                target.Target = Entity.Null;
                BotFindNewTargetLookup.SetComponentEnabled(entity, false);
                RefRW<BotFindNewTarget> botFindNewTarget = BotFindNewTargetLookup.GetRefRW(entity);
                botFindNewTarget.ValueRW.TimeToFindNewTarget = Time + botFindNewTarget.ValueRO.Interval;

                float closest = float.MaxValue;
                for (int i = 0; i < PotentialTargetTransforms.Length; i++)
                {
                    float3 targetPosition = PotentialTargetTransforms[i].Position;
                    float distancesq = math.distancesq(transform.Position, targetPosition);
                    if (distancesq < closest)
                    {
                        closest = distancesq;
                        target.Target = PotentialTargets[i];
                    }
                }
            }
        }

        public partial struct ChaseTargetJob : IJobEntity
        {
            public float DeltaTime;
            [NativeDisableParallelForRestriction]
            public ComponentLookup<LocalTransform> LocalTransformLookup;

            public void Execute(Entity entity, BotTarget target, ref ThirdPersonCharacterControl character, in ThirdPersonCharacterComponent characterComponent)
            {
                if (target.Target == Entity.Null)
                {
                    character.MoveVector = float3.zero;
                    return;
                }

                RefRW<LocalTransform> transform = LocalTransformLookup.GetRefRW(entity);
                LocalTransform targetTransform = LocalTransformLookup[target.Target];
                float dstqr = math.distancesq(transform.ValueRO.Position, targetTransform.Position);
                float3 dir = math.normalize(LocalTransformLookup[target.Target].Position - transform.ValueRO.Position);

                //Handle chasing target
                if (dstqr <= target.TargetReachDstSq)
                {
                    character.MoveVector = float3.zero;
                }
                else
                {
                    character.MoveVector = dir;
                }

                CharacterControlUtilities.SlerpRotationTowardsDirectionAroundUp(ref transform.ValueRW.Rotation, DeltaTime, dir, MathUtilities.GetUpFromRotation(transform.ValueRO.Rotation), characterComponent.RotationSharpness);
            }
        }
    }


    [BurstCompile]
    [UpdateInGroup(typeof(InitializeSystemGroup))]
    public partial struct BotInitSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = SystemAPI.GetSingleton<InstantiateCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            foreach ((RefRO<SwordPrefab> swordPrefab, RefRW<SwordColliders> swordCollider, Entity entity)
                in SystemAPI.Query<RefRO<SwordPrefab>, RefRW<SwordColliders>>().WithAll<BotTag>().WithAny<InitializeSubSceneEntity, InitializeEntity>().WithEntityAccess())
            {
                #region Set SwordColliders

                PhysicsCollider swordPrefabCollider = SystemAPI.GetComponent<PhysicsCollider>(swordPrefab.ValueRO.Value);
                // Orbitting collider
                BlobAssetReference<Collider> orbitCollider = swordPrefabCollider.Value.Value.Clone();
                CollisionFilter orbitColFilter = orbitCollider.Value.GetCollisionFilter();
                orbitColFilter.BelongsTo = 0u | (PhysicsCategory.Bot);
                orbitColFilter.CollidesWith = 0u | (PhysicsCategory.Fake_Player | PhysicsCategory.Player);
                orbitColFilter.GroupIndex = -entity.Index; // sword of the same orbit target don't collider with each other
                orbitCollider.Value.SetCollisionFilter(orbitColFilter);
                swordCollider.ValueRW.OrbitCollider = orbitCollider;
                // Detached collider
                BlobAssetReference<Collider> detachedCollider = swordPrefabCollider.Value.Value.Clone();
                CollisionFilter detachedColFilter = detachedCollider.Value.GetCollisionFilter();
                detachedColFilter.BelongsTo = 0u | PhysicsCategory.Sword;
                detachedColFilter.CollidesWith = 0u | PhysicsCategory.Ground;
                detachedCollider.Value.SetCollisionFilter(detachedColFilter);
                swordCollider.ValueRW.DetachedCollider = detachedCollider;

                #endregion

                #region Init sword

                BotInitialSwordCount initSwordCount = SystemAPI.GetComponent<BotInitialSwordCount>(entity);
                SwordController swordController = SystemAPI.GetComponent<SwordController>(entity);
                NativeArray<Entity> instantiatedSwords = new NativeArray<Entity>(initSwordCount.Count, Allocator.Temp);
                ecb.Instantiate(swordPrefab.ValueRO.Value, instantiatedSwords);
                ecb.AddSharedComponent(instantiatedSwords, new SwordOrbitTarget { TargetParent = entity, Target = swordController.OrbitTargetEntity });

                #endregion
            }
        }
    }
}
