using _GENERATED;
using BovineLabs.Core.Collections;
using BovineLabs.Core.Entropy;
using BovineLabs.Core.LifeCycle;
using BovineLabs.Core.Spatial;
using SpinningSwords.Data;
using Trove.UtilityAI;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Transforms;
using Action = Trove.UtilityAI.Action;
using Collider = Unity.Physics.Collider;


namespace SpinningSwords
{
    [BurstCompile]
    public partial struct FakePlayerSystem : ISystem
    {
        private PositionBuilder positionBuilder;
        private SpatialMap<SpatialPosition> spatialMap;
        private EntityQuery query;

        public void OnCreate(ref SystemState state)
        {
            query = SystemAPI.QueryBuilder().WithAll<LocalTransform>().WithAny<PullableTag, PlayerTag, BotTag, FakePlayerTag>().Build();
            this.positionBuilder = new PositionBuilder(ref state, query);

            const int size = 256;
            const int quantizeStep = 4;

            this.spatialMap = new SpatialMap<SpatialPosition>(quantizeStep, size);
        }

        public void OnDestroy(ref SystemState state)
        {
            this.spatialMap.Dispose();
        }

        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = this.positionBuilder.Gather(ref state, state.Dependency, out NativeArray<SpatialPosition> positions);
            state.Dependency = this.spatialMap.Build(positions, state.Dependency);

            // The entities in this will match the indices from the spatial map
            NativeList<Entity> entities = this.query.ToEntityListAsync(state.WorldUpdateAllocator, state.Dependency, out Unity.Jobs.JobHandle dependency);
            state.Dependency = dependency;

            GatherNeighboursJob gatherNeighboursJob = new GatherNeighboursJob
            {
                Entities = entities.AsDeferredJobArray(),
                Positions = positions,
                SpatialMap = this.spatialMap.AsReadOnly(),
            };
            Unity.Jobs.JobHandle gatherNeighboursJobHandle = gatherNeighboursJob.ScheduleParallel(state.Dependency);

            FakePlayerSelectActionJob fakePlayerSelectActionJob = new FakePlayerSelectActionJob
            {
                BotTagLookup = SystemAPI.GetComponentLookup<BotTag>(),
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(),
                PlayerTagLookup = SystemAPI.GetComponentLookup<PlayerTag>(),
                PullableTagLookup = SystemAPI.GetComponentLookup<PullableTag>(),
                SwordControllerLookup = SystemAPI.GetComponentLookup<SwordController>(),
                ThirdPersonCharacterComponentLookup = SystemAPI.GetComponentLookup<ThirdPersonCharacterComponent>(),
                Time = SystemAPI.Time.ElapsedTime,
                Random = SystemAPI.GetSingleton<Entropy>().Random,
            };
            Unity.Jobs.JobHandle fakePlayerSelectActionJobHandle = fakePlayerSelectActionJob.ScheduleParallel(gatherNeighboursJobHandle);

            FakePlayerAIJob fakePlayerAIJob = new FakePlayerAIJob
            {
                EntityStorageInfoLookup = SystemAPI.GetEntityStorageInfoLookup(),
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(),
            };
            state.Dependency = fakePlayerAIJob.ScheduleParallel(fakePlayerSelectActionJobHandle);
        }

        public partial struct GatherNeighboursJob : IJobEntity
        {
            [ReadOnly]
            public NativeArray<Entity> Entities;

            [ReadOnly]
            public NativeArray<SpatialPosition> Positions;

            [ReadOnly]
            public SpatialMap.ReadOnly SpatialMap;

            private void Execute(Entity entity, in LocalTransform localTransform, DynamicBuffer<Neighbours> neighbours, ref GatherNeighbourRadius gatherNeighbourRadius)
            {
                neighbours.Clear();
                float radius = gatherNeighbourRadius.Radius;

                // Find the min and max boxes
                int2 min = this.SpatialMap.Quantized(localTransform.Position.xz - radius);
                int2 max = this.SpatialMap.Quantized(localTransform.Position.xz + radius);

                for (int j = min.y; j <= max.y; j++)
                {
                    for (int i = min.x; i <= max.x; i++)
                    {
                        int hash = this.SpatialMap.Hash(new int2(i, j));

                        if (!this.SpatialMap.Map.TryGetFirstValue(hash, out int item, out NativeParallelMultiHashMapIterator<int> it))
                        {
                            continue;
                        }

                        do
                        {
                            Entity otherEntity = this.Entities[item];

                            // Don't add ourselves
                            if (otherEntity.Equals(entity))
                            {
                                continue;
                            }

                            float3 otherPosition = this.Positions[item].Position;

                            // The spatialmap serves as the broad-phase but most of the time we still need to ensure entities are actually within range
                            if (math.distancesq(localTransform.Position.xz, otherPosition.xz) <= radius * radius)
                            {
                                neighbours.Add(new Neighbours { Entity = otherEntity });
                            }
                        }
                        while (this.SpatialMap.Map.TryGetNextValue(out item, ref it));
                    }
                }

                if (neighbours.Length == 0)
                {
                    gatherNeighbourRadius.Radius += gatherNeighbourRadius.Increment;
                    gatherNeighbourRadius.Radius = math.clamp(gatherNeighbourRadius.Radius, 0, gatherNeighbourRadius.MaxRadius);
                }
                else
                {
                    gatherNeighbourRadius.Radius = gatherNeighbourRadius.NormalRadius;
                }
            }
        }

        public partial struct FakePlayerSelectActionJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<LocalTransform> LocalTransformLookup;
            [ReadOnly]
            public ComponentLookup<PullableTag> PullableTagLookup;
            [ReadOnly]
            public ComponentLookup<PlayerTag> PlayerTagLookup;
            [ReadOnly]
            public ComponentLookup<BotTag> BotTagLookup;
            [ReadOnly]
            public ComponentLookup<SwordController> SwordControllerLookup;
            [ReadOnly]
            public ComponentLookup<ThirdPersonCharacterComponent> ThirdPersonCharacterComponentLookup;

            public double Time;
            public ThreadRandom Random;

            private void Execute(Entity entity, ref FakePlayerAI fakePlayerAI, ref Reasoner reasoner, ref DynamicBuffer<Action> actionsBuffer, ref DynamicBuffer<Consideration> considerationsBuffer, ref DynamicBuffer<ConsiderationInput> considerationInputsBuffer,
                ref ThirdPersonCharacterControl thirdPersonCharacterControl,
                in SwordController swordController, in ThirdPersonCharacterComponent thirdPersonCharacter, in LocalTransform localTransform, in DynamicBuffer<Neighbours> neighbours)
            {
                if (Time <= fakePlayerAI.TimeToMadeDecision && !fakePlayerAI.ShouldUpdateReasoner) return;

                if (neighbours.Length == 0)
                {
                    return;
                }

                // find the nearest pickup and nearest actor
                Entity nearestPickup = Entity.Null;
                float nearestPickupDstSq = float.MaxValue;
                float3 nearestPickupDir = float3.zero;
                Entity nearestActor = Entity.Null;
                float nearestActorDstSq = float.MaxValue;
                float3 nearestActorDir = float3.zero;
                foreach (Neighbours neighbour in neighbours)
                {
                    float3 neighbourPos = LocalTransformLookup[neighbour.Entity].Position;
                    float distanceSq = math.distancesq(neighbourPos, localTransform.Position);
                    if (BotTagLookup.HasComponent(neighbour.Entity) || PlayerTagLookup.HasComponent(neighbour.Entity))
                    {
                        if (distanceSq < nearestActorDstSq)
                        {
                            nearestActorDstSq = distanceSq;
                            nearestActor = neighbour.Entity;
                            nearestActorDir = math.normalize(neighbourPos - localTransform.Position);
                        }
                    }
                    else if (PullableTagLookup.HasComponent(neighbour.Entity))
                    {
                        if (distanceSq < nearestPickupDstSq)
                        {
                            nearestPickupDstSq = distanceSq;
                            nearestPickup = neighbour.Entity;
                            nearestPickupDir = math.normalize(neighbourPos - localTransform.Position);
                        }
                    }
                }

                if (nearestActor.Equals(Entity.Null)) // no avoidance or attacking
                {
                    ReasonerUtilities.SetConsiderationInput(ref fakePlayerAI.WantAttackRef, 0, in reasoner, considerationsBuffer, considerationInputsBuffer);
                    ReasonerUtilities.SetConsiderationInput(ref fakePlayerAI.WantAvoidRef, 0, in reasoner, considerationsBuffer, considerationInputsBuffer);
                    ReasonerUtilities.SetConsiderationInput(ref fakePlayerAI.RunSpeedRef, 0, in reasoner, considerationsBuffer, considerationInputsBuffer);
                    ReasonerUtilities.SetConsiderationInput(ref fakePlayerAI.NearestAvoidDistanceRef, 0, in reasoner, considerationsBuffer, considerationInputsBuffer);
                    ReasonerUtilities.SetConsiderationInput(ref fakePlayerAI.NearestAttackDistanceRef, 0, in reasoner, considerationsBuffer, considerationInputsBuffer);
                    ReasonerUtilities.SetConsiderationInput(ref fakePlayerAI.NearestPickupDistanceRef, math.saturate(fakePlayerAI.NearestPickupConsiderationFloor / math.sqrt(nearestPickupDstSq)), in reasoner, considerationsBuffer, considerationInputsBuffer);
                }
                else if (nearestPickup.Equals(Entity.Null)) // only avoidance or attacking
                {
                    SwordController enemySwordController = SwordControllerLookup[nearestActor];
                    float enemySwordKineticEnergy = enemySwordController.Weight * enemySwordController.OrbitSpeed;
                    float swordKineticEnergy = swordController.Weight * swordController.OrbitSpeed;

                    ThirdPersonCharacterComponent enemyCharacterCtrl = ThirdPersonCharacterComponentLookup[nearestActor];
                    float enemyRunSpeed = enemyCharacterCtrl.GroundMaxSpeed;

                    bool attackCon1 = swordKineticEnergy > enemySwordKineticEnergy && swordController.SwordCount > 0;
                    bool attackCon2 = swordController.SwordCount > 0 && enemySwordController.SwordCount == 0;

                    bool wantAttacking = attackCon1 || attackCon2;

                    ReasonerUtilities.SetConsiderationInput(ref fakePlayerAI.WantAttackRef, wantAttacking ? 1 : 0, in reasoner, considerationsBuffer, considerationInputsBuffer);
                    ReasonerUtilities.SetConsiderationInput(ref fakePlayerAI.WantAvoidRef, wantAttacking ? 0 : 1, in reasoner, considerationsBuffer, considerationInputsBuffer);
                    ReasonerUtilities.SetConsiderationInput(ref fakePlayerAI.RunSpeedRef, math.saturate(thirdPersonCharacter.GroundMaxSpeed / (enemyRunSpeed * 1.1f)), in reasoner, considerationsBuffer, considerationInputsBuffer);
                    ReasonerUtilities.SetConsiderationInput(ref fakePlayerAI.NearestAttackDistanceRef, math.saturate(fakePlayerAI.NearestActorConsiderationFLoor / math.sqrt(nearestActorDstSq)), in reasoner, considerationsBuffer, considerationInputsBuffer);
                    ReasonerUtilities.SetConsiderationInput(ref fakePlayerAI.NearestAvoidDistanceRef, math.saturate(fakePlayerAI.NearestActorConsiderationFLoor / math.sqrt(nearestActorDstSq)), in reasoner, considerationsBuffer, considerationInputsBuffer);
                    ReasonerUtilities.SetConsiderationInput(ref fakePlayerAI.NearestPickupDistanceRef, 0, in reasoner, considerationsBuffer, considerationInputsBuffer);
                }
                else // avoidance or attacking or collect pickup
                {
                    SwordController enemySwordController = SwordControllerLookup[nearestActor];
                    float enemySwordKineticEnergy = enemySwordController.Weight * enemySwordController.OrbitSpeed;
                    float swordKineticEnergy = swordController.Weight * swordController.OrbitSpeed;

                    ThirdPersonCharacterComponent enemyCharacterCtrl = ThirdPersonCharacterComponentLookup[nearestActor];
                    float enemyRunSpeed = enemyCharacterCtrl.GroundMaxSpeed;

                    bool attackCon1 = swordKineticEnergy > enemySwordKineticEnergy && swordController.SwordCount > 0;
                    bool attackCon2 = swordController.SwordCount > 0 && enemySwordController.SwordCount == 0;

                    bool wantAttacking = attackCon1 || attackCon2;

                    ReasonerUtilities.SetConsiderationInput(ref fakePlayerAI.WantAttackRef, wantAttacking ? 1 : 0, in reasoner, considerationsBuffer, considerationInputsBuffer);
                    ReasonerUtilities.SetConsiderationInput(ref fakePlayerAI.WantAvoidRef, wantAttacking ? 0 : 1, in reasoner, considerationsBuffer, considerationInputsBuffer);
                    ReasonerUtilities.SetConsiderationInput(ref fakePlayerAI.RunSpeedRef, math.saturate(thirdPersonCharacter.GroundMaxSpeed / (enemyRunSpeed * 1.1f)), in reasoner, considerationsBuffer, considerationInputsBuffer);
                    ReasonerUtilities.SetConsiderationInput(ref fakePlayerAI.NearestAttackDistanceRef, math.saturate(fakePlayerAI.NearestActorConsiderationFLoor / math.sqrt(nearestActorDstSq)), in reasoner, considerationsBuffer, considerationInputsBuffer);
                    ReasonerUtilities.SetConsiderationInput(ref fakePlayerAI.NearestAvoidDistanceRef, math.saturate(fakePlayerAI.NearestActorConsiderationFLoor / math.sqrt(nearestActorDstSq)), in reasoner, considerationsBuffer, considerationInputsBuffer);
                    ReasonerUtilities.SetConsiderationInput(ref fakePlayerAI.NearestPickupDistanceRef, math.saturate(fakePlayerAI.NearestPickupConsiderationFloor / math.sqrt(nearestPickupDstSq)), in reasoner, considerationsBuffer, considerationInputsBuffer);
                }

                // Create an action selector (determines how we pick the "best" action). There are various types of
                // selectors to choose from, and you can also create your own
                ActionSelectors.HighestScoring actionSelector = new ActionSelectors.HighestScoring();

                // Update the AI and select an action
                if (ReasonerUtilities.UpdateScoresAndSelectAction(ref actionSelector, ref reasoner, actionsBuffer, considerationsBuffer, considerationInputsBuffer, out Action selectedAction))
                {
                    // Handle switching actions
                    //if (selectedAction.Score > 0f) // Don't bother switching actions if the new one scored 0
                    {
                        FakePlayerAIAction previousAction = fakePlayerAI.SelectedAction;
                        fakePlayerAI.SelectedAction = (FakePlayerAIAction)selectedAction.Type;
                        fakePlayerAI.ShouldUpdateReasoner = false;
                        fakePlayerAI.TimeToMadeDecision = Time + (double)fakePlayerAI.DecisionInertia;

                        // What happens when an action is ended
                        switch (previousAction)
                        {
                            case FakePlayerAIAction.CollectItem:
                                break;
                            case FakePlayerAIAction.Attack:
                                break;
                            case FakePlayerAIAction.Avoidance:
                                break;
                        }

                        // What happens when a new action is started
                        switch (fakePlayerAI.SelectedAction)
                        {
                            case FakePlayerAIAction.CollectItem:
                                fakePlayerAI.PickupTarget = nearestPickup;
                                break;
                            case FakePlayerAIAction.Attack:
                                fakePlayerAI.NearestActor = nearestActor;
                                break;
                            case FakePlayerAIAction.Avoidance:
                                {
                                    fakePlayerAI.NearestActor = nearestActor;
                                    float3 dir = math.normalize(localTransform.Position - LocalTransformLookup[fakePlayerAI.NearestActor].Position);
                                    dir = math.rotate(quaternion.RotateY(Random.GetRandomRef().NextFloat(-90, 90)), dir);
                                    fakePlayerAI.AvoidanceDir = dir;
                                }
                                break;
                        }
                    }
                }
            }
        }

        public partial struct FakePlayerAIJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<LocalTransform> LocalTransformLookup;
            [ReadOnly]
            public EntityStorageInfoLookup EntityStorageInfoLookup;

            public void Execute(ref FakePlayerAI fakePlayerAI, ref ThirdPersonCharacterControl thirdPersonCharacterControl, in LocalTransform localTransform)
            {
                switch (fakePlayerAI.SelectedAction)
                {
                    case FakePlayerAIAction.CollectItem:
                        if (EntityStorageInfoLookup.Exists(fakePlayerAI.PickupTarget))
                            thirdPersonCharacterControl.MoveVector = math.normalize(LocalTransformLookup[fakePlayerAI.PickupTarget].Position - localTransform.Position);
                        else
                            fakePlayerAI.ShouldUpdateReasoner = true;
                        break;
                    case FakePlayerAIAction.Attack:
                        if (EntityStorageInfoLookup.Exists(fakePlayerAI.NearestActor))
                        {
                            if (math.distancesq(localTransform.Position, LocalTransformLookup[fakePlayerAI.NearestActor].Position) > fakePlayerAI.StopChasingDst * fakePlayerAI.StopChasingDst)
                                thirdPersonCharacterControl.MoveVector = math.normalize(LocalTransformLookup[fakePlayerAI.NearestActor].Position - localTransform.Position);
                            else
                                thirdPersonCharacterControl.MoveVector = float3.zero;
                        }
                        else
                            fakePlayerAI.ShouldUpdateReasoner = true;
                        break;
                    case FakePlayerAIAction.Avoidance:
                        if (EntityStorageInfoLookup.Exists(fakePlayerAI.NearestActor))
                            thirdPersonCharacterControl.MoveVector = fakePlayerAI.AvoidanceDir;
                        else
                            fakePlayerAI.ShouldUpdateReasoner = true;
                        break;
                }
            }
        }
    }

    [BurstCompile]
    [UpdateInGroup(typeof(InitializeSystemGroup))]
    public partial struct FakePlayerInitSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            foreach ((RefRO<SwordPrefab> swordPrefab, RefRW<SwordColliders> swordCollider, Entity entity)
                in SystemAPI.Query<RefRO<SwordPrefab>, RefRW<SwordColliders>>().WithAll<FakePlayerTag>().WithAny<InitializeSubSceneEntity, InitializeEntity>().WithEntityAccess())
            {
                // Về sau sẽ phải thay đổi giá trị của collider của từng fake player nên cần để collider của từng fake player thành unique
                SystemAPI.GetComponentRW<PhysicsCollider>(entity).ValueRW.MakeUnique(entity, ecb);

                #region Set SwordColliders

                PhysicsCollider swordPrefabCollider = SystemAPI.GetComponent<PhysicsCollider>(swordPrefab.ValueRO.Value);
                // Orbitting collider
                BlobAssetReference<Collider> orbitCollider = swordPrefabCollider.Value.Value.Clone();
                CollisionFilter orbitColFilter = orbitCollider.Value.GetCollisionFilter();
                orbitColFilter.BelongsTo = 0u | (PhysicsCategory.Fake_Player);
                orbitColFilter.CollidesWith = 0u | (PhysicsCategory.Fake_Player | PhysicsCategory.Bot | PhysicsCategory.Player);
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
            }
        }
    }
}
