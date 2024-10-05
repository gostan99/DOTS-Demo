using _GENERATED;
using SpinningSwords.Data;
using Trove.UtilityAI;
using Unity.Entities;
using UnityEngine;

namespace SpinningSwords.Authoring
{
    public class FakePlayerAuthoring : MonoBehaviour
    {

        [Header("Puller")]
        public Puller puller;

        [Header("Sword Controller")]
        public GameObject SwordPrefab;
        public GameObject OrbitTarget;
        public float InitialOrbitSpeed;
        public float OrbitDistance;
        public float MaxOrbitSpeed;
        public float MaxWeight;
        public int MaxSwordCount;

        [Header("Sword Equidistant")]
        public float SwordEquidistantDuration;

        [Header("Gather Neighbours")]
        public float Radius;
        public float Increment;
        public float MaxRadius;

        [Header("AI")]
        public FakePlayerConsiderationSetData ConsiderationSetData;
        public float DecisionInertia;
        public float NearestActorConsiderationFLoor;
        public float NearestPickupConsiderationFloor;
        public float StopChasingDst;

        public class Baker : Baker<FakePlayerAuthoring>
        {
            public override void Bake(FakePlayerAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent<FakePlayerTag>(entity);

                AddComponent(entity, authoring.puller);

                AddComponent(entity, new SwordPrefab { Value = GetEntity(authoring.SwordPrefab, TransformUsageFlags.Dynamic) });

                AddComponent<SwordColliders>(entity);

                AddComponent(entity, new SwordController
                {
                    OrbitTargetEntity = GetEntity(authoring.OrbitTarget, TransformUsageFlags.Dynamic),
                    OrbitSpeed = authoring.InitialOrbitSpeed,
                    OrbitDistance = authoring.OrbitDistance,
                    OrbitMaxSpeed = authoring.MaxOrbitSpeed,
                    MaxWeight = authoring.MaxWeight,
                    MaxSwordCount = authoring.MaxSwordCount,
                    SwordCount = 0,
                    Weight = 1,
                });

                AddComponent(entity, new SwordEquidistant
                {
                    Duration = authoring.SwordEquidistantDuration,
                });
                SetComponentEnabled<SwordEquidistant>(entity, false);

                AddBuffer<SwordBuffer>(entity);

                AddBuffer<Neighbours>(entity);

                AddComponent(entity, new GatherNeighbourRadius
                {
                    NormalRadius = authoring.Radius,
                    Radius = authoring.Radius,
                    MaxRadius = authoring.MaxRadius,
                    Increment = authoring.Increment
                });

                SetupAI(authoring);

                AddComponent<EnableSpeedBoost>(entity);
                SetComponentEnabled<EnableSpeedBoost>(entity, false);
            }

            public void SetupAI(FakePlayerAuthoring authoring)
            {
                // Create our Agent component, but don't add it just yet (because we must set data in it first)
                FakePlayerAI ai = new FakePlayerAI
                {
                    DecisionInertia = authoring.DecisionInertia,
                    NearestActorConsiderationFLoor = authoring.NearestActorConsiderationFLoor,
                    NearestPickupConsiderationFloor = authoring.NearestPickupConsiderationFloor,
                    StopChasingDst = authoring.StopChasingDst,
                    ShouldUpdateReasoner = true,
                };

                // We bake our consideration set definitions to the entity (these are blob asset references to each consideration curve)
                authoring.ConsiderationSetData.Bake(this, out FakePlayerConsiderationSet considerationSetComponent);

                // When we're ready to start adding actions and considerations, we call BeginBakeReasoner. This will give us
                // access to the components and buffers we need.
                ReasonerUtilities.BeginBakeReasoner(this, out Reasoner reasoner, out DynamicBuffer<Action> actionsBuffer, out DynamicBuffer<Consideration> considerationsBuffer, out DynamicBuffer<ConsiderationInput> considerationInputsBuffer);
                {
                    // Add our actions. We specify an action type using our enum, and we store the resulting
                    // "ActionReference" in our agent component.
                    ReasonerUtilities.AddAction(new ActionDefinition((int)FakePlayerAIAction.CollectItem), true, ref reasoner, actionsBuffer, out ai.CollectItemActionRef);
                    ReasonerUtilities.AddAction(new ActionDefinition((int)FakePlayerAIAction.Attack), true, ref reasoner, actionsBuffer, out ai.AttackActionRef);
                    ReasonerUtilities.AddAction(new ActionDefinition((int)FakePlayerAIAction.Avoidance), true, ref reasoner, actionsBuffer, out ai.AvoidanceActionRef);

                    // Add our considerations to our actions. We use the "ConsiderationDefinition"s from our consideration set
                    // in order to specify the type of consideration to add, and we also specify the "ActionReference" that we
                    // want to add this consideration to. Finally, we store the resulting "ConsiderationReference" in our 
                    // agent component.
                    ReasonerUtilities.AddConsideration(considerationSetComponent.NearestAttackDistance, ref ai.AttackActionRef, true, ref reasoner, actionsBuffer, considerationsBuffer, considerationInputsBuffer, out ai.NearestAttackDistanceRef);
                    ReasonerUtilities.AddConsideration(considerationSetComponent.RunSpeed, ref ai.AttackActionRef, true, ref reasoner, actionsBuffer, considerationsBuffer, considerationInputsBuffer, out ai.RunSpeedRef);
                    ReasonerUtilities.AddConsideration(considerationSetComponent.WantAttack, ref ai.AttackActionRef, true, ref reasoner, actionsBuffer, considerationsBuffer, considerationInputsBuffer, out ai.WantAttackRef);
                    ReasonerUtilities.AddConsideration(considerationSetComponent.NearestAvoidDistance, ref ai.AvoidanceActionRef, true, ref reasoner, actionsBuffer, considerationsBuffer, considerationInputsBuffer, out ai.NearestAvoidDistanceRef);
                    ReasonerUtilities.AddConsideration(considerationSetComponent.WantAvoid, ref ai.AvoidanceActionRef, true, ref reasoner, actionsBuffer, considerationsBuffer, considerationInputsBuffer, out ai.WantAvoidRef);
                    ReasonerUtilities.AddConsideration(considerationSetComponent.NearestPickupDistance, ref ai.CollectItemActionRef, true, ref reasoner, actionsBuffer, considerationsBuffer, considerationInputsBuffer, out ai.NearestPickupDistanceRef);
                }
                // Once we're finished setting everything up, we end baking for the reasoner
                ReasonerUtilities.EndBakeReasoner(this, reasoner);

                // Add our agent ai component, after it's been filled with all added action/consideration references
                AddComponent(GetEntity(TransformUsageFlags.Dynamic), ai);

                // Let the baking system know that we depend on that consideration set SriptableObject, so that
                // baking is properly re-triggered when it changes.
                DependsOn(authoring.ConsiderationSetData);
            }
        }
    }
}