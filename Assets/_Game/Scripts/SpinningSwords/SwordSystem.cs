using BovineLabs.Core.Extensions;
using BovineLabs.Core.Iterators;
using BovineLabs.Core.LifeCycle;
using BovineLabs.Core.PhysicsStates;
using SpinningSwords.Data;
using SpinningSwords.Extensions;
using Unity.Burst;
using Unity.CharacterController;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace SpinningSwords
{
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    public partial struct SwordSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            SwordEquidistantJob swordEquidistantJob = new SwordEquidistantJob
            {
                ElapsedTime = SystemAPI.Time.ElapsedTime,
                SwordEquidistantFinishLookup = SystemAPI.GetComponentLookup<SwordEquidistantFinish>(),
                SwordLookup = SystemAPI.GetComponentLookup<Sword>(),
                SwordEquidistantLookup = SystemAPI.GetComponentLookup<SwordEquidistant>(),
            };
            Unity.Jobs.JobHandle swordEquidistantJobHandle = swordEquidistantJob.ScheduleParallel(state.Dependency);

            SwordEquidistantFinishJob swordEquidistantFinishJob = new SwordEquidistantFinishJob
            {
                ElapsedTime = SystemAPI.Time.ElapsedTime,
                SwordEquidistantFinishLookup = SystemAPI.GetComponentLookup<SwordEquidistantFinish>(),
            };
            Unity.Jobs.JobHandle swordEquidistantFinishJobHandle = swordEquidistantFinishJob.ScheduleParallel(swordEquidistantJobHandle);

            SwordOrbitJob swordOrbitJob = new SwordOrbitJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(false),
                PostTransformMatrixLookup = SystemAPI.GetComponentLookup<PostTransformMatrix>(true),
                KinematicCharacterBodyLookup = SystemAPI.GetComponentLookup<KinematicCharacterBody>(true),
                SwordControllerLookup = SystemAPI.GetComponentLookup<SwordController>(true),
                ParentLookup = SystemAPI.GetComponentLookup<Parent>(true),
            };
            state.Dependency = swordOrbitJob.ScheduleParallel(swordEquidistantFinishJobHandle);
        }

        [WithAll(typeof(SwordEquidistant))]
        public partial struct SwordEquidistantJob : IJobEntity
        {
            public double ElapsedTime;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<Sword> SwordLookup;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<SwordEquidistantFinish> SwordEquidistantFinishLookup;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<SwordEquidistant> SwordEquidistantLookup;

            public void Execute(Entity entity, in DynamicBuffer<SwordBuffer> swordBuffers, in SwordController swordController)
            {
                if (swordBuffers.Length < 2) return;

                SwordEquidistant swordEquidistant = SwordEquidistantLookup[entity];
                SwordEquidistantLookup.SetComponentEnabled(entity, false);

                float angleStep = math.PI2 / swordBuffers.Length;
                RefRW<Sword> firstSword = SwordLookup.GetRefRW(swordBuffers[0].Value);
                firstSword.ValueRW.OrbitSpeed = swordController.OrbitSpeed; // first sword remain ordinary speed
                float step = math.radians(swordController.OrbitSpeed * swordEquidistant.Duration); // góc quét 
                float3 firstSwordPlannarForwardFinish = math.rotate(quaternion.Euler(math.up() * step), firstSword.ValueRO.PlanarForward);
                for (int i = 1; i < swordBuffers.Length; i++) // skip first sword
                {
                    RefRW<Sword> sword = SwordLookup.GetRefRW(swordBuffers[i].Value);
                    float3 plannarForward = math.rotate(quaternion.Euler(math.up() * step), sword.ValueRO.PlanarForward);
                    float3 plannarForwardFinish = math.rotate(quaternion.Euler(math.up() * (angleStep * i)), firstSwordPlannarForwardFinish);
                    float deltaAngle = step + math.sign(math.cross(plannarForward, plannarForwardFinish).y) * MathUtilities.AngleRadians(plannarForward, plannarForwardFinish);
                    sword.ValueRW.OrbitSpeed = math.degrees(deltaAngle) / swordEquidistant.Duration;
                    SwordEquidistantFinishLookup.SetComponentEnabled(swordBuffers[i].Value, true);
                    SwordEquidistantFinishLookup.GetRefRW(swordBuffers[i].Value).ValueRW.FinishTime = ElapsedTime + swordEquidistant.Duration;
                    SwordEquidistantFinishLookup.GetRefRW(swordBuffers[i].Value).ValueRW.FinishSpeed = swordController.OrbitSpeed;
                }
            }
        }

        [WithAll(typeof(SwordEquidistantFinish))]
        public partial struct SwordEquidistantFinishJob : IJobEntity
        {
            public double ElapsedTime;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<SwordEquidistantFinish> SwordEquidistantFinishLookup;

            public void Execute(Entity entity, ref Sword sword)
            {
                SwordEquidistantFinish swordEquidistantFinish = SwordEquidistantFinishLookup[entity];
                if (ElapsedTime >= swordEquidistantFinish.FinishTime)
                {
                    SwordEquidistantFinishLookup.SetComponentEnabled(entity, false);
                    sword.OrbitSpeed = swordEquidistantFinish.FinishSpeed;
                }
            }
        }

        public partial struct SwordOrbitJob : IJobEntity
        {
            public float DeltaTime;
            [NativeDisableParallelForRestriction]
            public ComponentLookup<LocalTransform> LocalTransformLookup;
            [ReadOnly] public ComponentLookup<PostTransformMatrix> PostTransformMatrixLookup;
            [ReadOnly] public ComponentLookup<KinematicCharacterBody> KinematicCharacterBodyLookup;
            [ReadOnly] public ComponentLookup<SwordController> SwordControllerLookup;
            [ReadOnly] public ComponentLookup<Parent> ParentLookup;

            public void Execute(in Entity entity, ref Sword sword, in SwordOrbitTarget orbitTarget)
            {
                RefRW<LocalTransform> localTransform = LocalTransformLookup.GetRefRW(entity);
                TransformHelpers.ComputeWorldTransformMatrix(
                                    orbitTarget.Target,
                                    out float4x4 targetWorldTransform,
                                    ref LocalTransformLookup,
                                    ref ParentLookup,
                                    ref PostTransformMatrixLookup);

                SwordController swordController = SwordControllerLookup[orbitTarget.TargetParent];
                float3 targetUp = targetWorldTransform.Up();
                float3 targetPosition = targetWorldTransform.Translation();
                // Update planar forward
                {
                    quaternion tempPlanarRotation = MathUtilities.CreateRotationWithUpPriority(targetUp, sword.PlanarForward);
                    // Rotation from character parent
                    if (
                        KinematicCharacterBodyLookup.TryGetComponent(orbitTarget.TargetParent, out KinematicCharacterBody characterBody))
                    {
                        // Only consider rotation around the character up, since the sword is already adjusting itself to character up
                        quaternion planarRotationFromParent = characterBody.RotationFromParent;
                        KinematicCharacterUtilities.AddVariableRateRotationFromFixedRateRotation(ref tempPlanarRotation, planarRotationFromParent, DeltaTime, characterBody.LastPhysicsUpdateDeltaTime);
                    }

                    sword.PlanarForward = MathUtilities.GetForwardFromRotation(tempPlanarRotation);
                }

                // Yaw
                float yawAngleChange = sword.OrbitSpeed * DeltaTime;
                quaternion yawRotation = quaternion.Euler(targetUp * math.radians(yawAngleChange));
                sword.PlanarForward = math.rotate(yawRotation, sword.PlanarForward);

                // Calculate final rotation
                quaternion swordRotation = CalculateRotation(targetUp, sword.PlanarForward);

                // Calculate sword position (no smoothing yet; this is done in the sword late update)
                float3 swordPosition = targetPosition + (-MathUtilities.GetForwardFromRotation(swordRotation) * swordController.OrbitDistance);

                // Write back to component
                localTransform.ValueRW = LocalTransform.FromPositionRotationScale(swordPosition, swordRotation, localTransform.ValueRO.Scale);
            }

            private quaternion CalculateRotation(float3 targetUp, float3 planarForward)
            {
                quaternion pitchRotation = quaternion.Euler(math.right() * math.radians(0));
                quaternion rotation = MathUtilities.CreateRotationWithUpPriority(targetUp, planarForward);
                rotation = math.mul(rotation, pitchRotation);
                return rotation;
            }
        }
    }

    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(PhysicsSystemGroup))]
    public partial struct SwordCollisionSystem : ISystem
    {
        private SharedComponentLookup<SwordOrbitTarget> swordOrbitTargetLookup;
        private EntityQuery swordOrbitTargetQuery;


        public void OnCreate(ref SystemState state)
        {
            swordOrbitTargetLookup = state.GetSharedComponentLookup<SwordOrbitTarget>();
            swordOrbitTargetQuery = SystemAPI.QueryBuilder().WithAll<SwordOrbitTarget>().Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            swordOrbitTargetLookup.Update(ref state);
            EntityCommandBuffer ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            SwordCollisionJob swordCollisionJob = new SwordCollisionJob
            {
                Ecb = ecb.AsParallelWriter(),
                SwordBufferLookup = SystemAPI.GetBufferLookup<SwordBuffer>(),
                SwordCollidersLookup = SystemAPI.GetComponentLookup<SwordColliders>(),
                SwordControllerLookup = SystemAPI.GetComponentLookup<SwordController>(),
                SwordEquidistantLookup = SystemAPI.GetComponentLookup<SwordEquidistant>(),
                SwordLookup = SystemAPI.GetComponentLookup<Sword>(),
                SwordOrbitTargetLookup = swordOrbitTargetLookup,
            };

            Unity.Jobs.JobHandle swordCollisionJobHandle = swordCollisionJob.ScheduleParallel(state.Dependency);
            swordCollisionJobHandle.Complete();
            ecb.Playback(state.EntityManager);

            #region Update Sword Counts

            foreach ((RefRW<SwordController> swordController, Entity entity) in SystemAPI.Query<RefRW<SwordController>>().WithEntityAccess())
            {
                swordOrbitTargetQuery.ResetFilter();
                swordOrbitTargetQuery.SetSharedComponentFilter(new SwordOrbitTarget { Target = swordController.ValueRO.OrbitTargetEntity, TargetParent = entity });
                int swordCount = swordOrbitTargetQuery.CalculateEntityCount();
                swordController.ValueRW.SwordCount = swordCount;
            }

            #endregion
        }

        public void OnDestroy(ref SystemState state)
        {

        }
        public partial struct SwordCollisionJob : IJobEntity
        {
            [NativeDisableParallelForRestriction]
            public BufferLookup<SwordBuffer> SwordBufferLookup;
            [NativeDisableParallelForRestriction]
            public ComponentLookup<SwordEquidistant> SwordEquidistantLookup;

            [ReadOnly]
            public SharedComponentLookup<SwordOrbitTarget> SwordOrbitTargetLookup;
            [ReadOnly]
            public ComponentLookup<Sword> SwordLookup;
            [ReadOnly]
            public ComponentLookup<SwordController> SwordControllerLookup;
            [ReadOnly]
            public ComponentLookup<SwordColliders> SwordCollidersLookup;

            public EntityCommandBuffer.ParallelWriter Ecb;


            public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, in DynamicBuffer<StatefulCollisionEvent> collisionEvents, in Sword sword, in SwordOrbitTarget orbitTarget,
                 ref PhysicsVelocity velocity, ref PhysicsGravityFactor physicsGravityFactor, ref PhysicsCollider collider, ref PhysicsMass mass)
            {
                for (int i = 0; i < collisionEvents.Length; i++)
                {
                    StatefulCollisionEvent collision = collisionEvents[i];
                    Entity otherEntity = collision.EntityB;

                    if (collision.State != StatefulEventState.Enter) continue;

                    bool isACollisionWithAnotherSword = SwordOrbitTargetLookup.TryGetComponent(otherEntity, out SwordOrbitTarget otherOrbitTarget);
                    if (!isACollisionWithAnotherSword) continue;

                    if (!SwordLookup.TryGetComponent(otherEntity, out Sword otherSword)) continue;

                    if (!SwordOrbitTargetLookup.TryGetComponent(otherEntity, out SwordOrbitTarget otherSwordOrbitTarget)) continue;

                    if (!SwordControllerLookup.TryGetComponent(orbitTarget.TargetParent, out SwordController swordController) || !SwordControllerLookup.TryGetComponent(otherSwordOrbitTarget.TargetParent, out SwordController otherSwordController)) continue;

                    // KE=0.5mv^2
                    float kineticEnergyOfA = 0.5f * swordController.Weight * sword.OrbitSpeed * sword.OrbitSpeed;
                    float kineticEnergyOfB = 0.5f * otherSwordController.Weight * otherSword.OrbitSpeed * otherSword.OrbitSpeed;

                    if (kineticEnergyOfA < kineticEnergyOfB)
                    {
                        float force = (kineticEnergyOfB - kineticEnergyOfA) * 0.0045f;
                        float3 forceDirection = math.cross(otherSword.PlanarForward, math.up());
                        velocity.Linear = forceDirection * force;

                        physicsGravityFactor.Value = 1;
                        mass = PhysicsMass.CreateDynamic(collider.MassProperties, 1);

                        SwordColliders swordColliders = SwordCollidersLookup[orbitTarget.TargetParent];
                        collider.Value = swordColliders.DetachedCollider;

                        DynamicBuffer<SwordBuffer> swordBuffer = SwordBufferLookup[orbitTarget.TargetParent];
                        int swordIndex = swordBuffer.IndexOf(new SwordBuffer { Value = entity });
                        swordBuffer.RemoveAt(swordIndex);
                        SwordEquidistantLookup.SetComponentEnabled(orbitTarget.TargetParent, true);

                        Ecb.RemoveComponent<SwordOrbitTarget>(sortKey, entity);
                    }
                    return; // chỉ xử lý va chạm cho 1 sword trong 1 frame
                }
            }
        }
    }


    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TransformSystemGroup))]
    public partial struct SwordSmoothSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            SwordSmoothOrbitJob swordOrbitJob = new SwordSmoothOrbitJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                LocalToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(false),
                SwordControllerLookup = SystemAPI.GetComponentLookup<SwordController>(true),
                KinematicCharacterBodyLookup = SystemAPI.GetComponentLookup<KinematicCharacterBody>(true),
            };

            state.Dependency = swordOrbitJob.ScheduleParallel(state.Dependency);
        }

        public partial struct SwordSmoothOrbitJob : IJobEntity
        {
            public float DeltaTime;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<LocalToWorld> LocalToWorldLookup;

            [ReadOnly] public ComponentLookup<SwordController> SwordControllerLookup;
            [ReadOnly] public ComponentLookup<KinematicCharacterBody> KinematicCharacterBodyLookup;

            private void Execute(Entity entity, in Sword sword, in SwordOrbitTarget orbitTarget, in LocalTransform localTransform)
            {
                LocalToWorld targetWorldTransform = LocalToWorldLookup[orbitTarget.Target];
                quaternion swordRotation = CalculateRotation(targetWorldTransform.Up, sword.PlanarForward);
                SwordController swordController = SwordControllerLookup[orbitTarget.TargetParent];
                float3 targetPosition = targetWorldTransform.Position;

                // Place Sword at the final distance (includes smoothing)
                float3 swordPosition = targetPosition + (-MathUtilities.GetForwardFromRotation(swordRotation) * swordController.OrbitDistance);

                // Write to LtW
                LocalToWorldLookup[entity] = new LocalToWorld { Value = float4x4.TRS(swordPosition, swordRotation, localTransform.Scale) };
            }

            private readonly quaternion CalculateRotation(float3 targetUp, float3 planarForward)
            {
                quaternion pitchRotation = quaternion.Euler(math.right() * math.radians(0));
                quaternion rotation = MathUtilities.CreateRotationWithUpPriority(targetUp, planarForward);
                rotation = math.mul(rotation, pitchRotation);
                return rotation;
            }
        }
    }


    [BurstCompile]
    [UpdateInGroup(typeof(InitializeSystemGroup))]
    public partial struct SwordInitSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {

        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            SwordInitJob swordInitJob = new SwordInitJob
            {
                SwordControllerLookup = SystemAPI.GetComponentLookup<SwordController>(true),
                SwordCollidersLookup = SystemAPI.GetComponentLookup<SwordColliders>(true),
                SwordBufferLookup = SystemAPI.GetBufferLookup<SwordBuffer>(),
                SwordLookup = SystemAPI.GetComponentLookup<Sword>(),
                SwordEquidistantLookup = SystemAPI.GetComponentLookup<SwordEquidistant>(),
            };
            state.Dependency = swordInitJob.Schedule(state.Dependency);
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        [WithAll(typeof(InitializeEntity))]
        public partial struct SwordInitJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<SwordController> SwordControllerLookup;
            [ReadOnly] public ComponentLookup<SwordColliders> SwordCollidersLookup;

            public ComponentLookup<Sword> SwordLookup;
            public ComponentLookup<SwordEquidistant> SwordEquidistantLookup;
            public BufferLookup<SwordBuffer> SwordBufferLookup;


            public void Execute(Entity entity, in SwordOrbitTarget orbitTarget, ref PhysicsCollider physicsCollider)
            {
                RefRW<Sword> sword = SwordLookup.GetRefRW(entity);
                sword.ValueRW.OrbitSpeed = SwordControllerLookup[orbitTarget.TargetParent].OrbitSpeed;

                physicsCollider.Value = SwordCollidersLookup[orbitTarget.TargetParent].OrbitCollider;

                DynamicBuffer<SwordBuffer> swordbuffer = SwordBufferLookup[orbitTarget.TargetParent];
                swordbuffer.Add(new SwordBuffer { Value = entity });
                sword.ValueRW.PlanarForward = math.forward();
                if (swordbuffer.Length > 1)
                {
                    SwordBuffer lasstSword = swordbuffer[^2];
                    sword.ValueRW.PlanarForward = SwordLookup[lasstSword.Value].PlanarForward;
                    SwordEquidistantLookup.SetComponentEnabled(orbitTarget.TargetParent, true);
                }
            }
        }
    }
}
