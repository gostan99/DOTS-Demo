using System;
using Trove.UtilityAI;
using Unity.Entities;
using Unity.Mathematics;

namespace SpinningSwords.Data
{
    public struct FakePlayerTag : IComponentData
    {
    }

    [Serializable]
    public enum FakePlayerAIAction
    {
        CollectItem,
        Attack,
        Avoidance
    }

    public struct FakePlayerAI : IComponentData
    {
        // Store the selected action, so we can see it in the inspector for debugging
        public FakePlayerAIAction SelectedAction;

        // Store references to our action instances
        public ActionReference CollectItemActionRef;
        public ActionReference AttackActionRef;
        public ActionReference AvoidanceActionRef;

        // Store references to our consideration instances
        public ConsiderationReference SwordKineticEnergyRef;
        public ConsiderationReference RunSpeedRef;
        public ConsiderationReference NearestActorRef; // Actor: Player or Bot
        public ConsiderationReference NearestActorAvoidanceRef; // Actor: Player or Bot
        public ConsiderationReference NearestPickupRef;

        // Characteristics of this AI
        public float DecisionInertia;
        public float NearestActorConsiderationFLoor;
        public float NearestPickupConsiderationFloor;
        public float StopChasingDst;

        // State of this AI
        public double TimeToMadeDecision;
        public bool ShouldUpdateReasoner;
        public Entity AttackTarget;
        public Entity AvoidantTarget;
        public Entity PickupTarget;
        public float3 AvoidanceDir;
    }
}
