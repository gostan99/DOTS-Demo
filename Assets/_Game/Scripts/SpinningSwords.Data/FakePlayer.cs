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
        public ConsiderationReference WantAttackRef;
        public ConsiderationReference WantAvoidRef;
        public ConsiderationReference RunSpeedRef;
        public ConsiderationReference NearestAvoidDistanceRef; // Actor: Player or Bot
        public ConsiderationReference NearestAttackDistanceRef; // Actor: Player or Bot
        public ConsiderationReference NearestPickupDistanceRef;

        // Characteristics of this AI
        public float DecisionInertia;
        public float NearestActorConsiderationFLoor;
        public float NearestPickupConsiderationFloor;
        public float StopChasingDst;

        // State of this AI
        public double TimeToMadeDecision;
        public bool ShouldUpdateReasoner;
        public Entity NearestActor;
        public Entity PickupTarget;
        public float3 AvoidanceDir;
    }
}
