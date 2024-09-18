using BovineLabs.Core.LifeCycle;
using SpinningSwords.Data;
using Unity.Burst;
using Unity.CharacterController;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
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

            SwordOrbitJob swordOrbitJob = new SwordOrbitJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(false),
                PostTransformMatrixLookup = SystemAPI.GetComponentLookup<PostTransformMatrix>(true),
                KinematicCharacterBodyLookup = SystemAPI.GetComponentLookup<KinematicCharacterBody>(true),
                SwordControllerLookup = SystemAPI.GetComponentLookup<SwordController>(true),
                ParentLookup = SystemAPI.GetComponentLookup<Parent>(true),
            };

            state.Dependency = swordOrbitJob.ScheduleParallel(state.Dependency);
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
    [UpdateInGroup(typeof(InitializeSystemGroup))]
    public partial struct SwordInitSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {

        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            SwordInitJob swordInitJob = new SwordInitJob { SwordControllerLookup = SystemAPI.GetComponentLookup<SwordController>(true) };
            state.Dependency = swordInitJob.ScheduleParallel(state.Dependency);
        }

        public void OnDestroy(ref SystemState state)
        {

        }

        [WithAll(typeof(InitializeEntity))]
        public partial struct SwordInitJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<SwordController> SwordControllerLookup;
            public void Execute(Entity entity, ref Sword sword, in SwordOrbitTarget orbitTarget)
            {
                sword.InitialOrbitSpeed = SwordControllerLookup[orbitTarget.TargetParent].InitialOrbitSpeed;
                sword.OrbitSpeed = sword.InitialOrbitSpeed;
            }
        }
    }
}
