using System;
using Unity.Entities;

namespace SpinningSwords.Data
{
    public struct BotTarget : IComponentData
    {
        public Entity Target;
        public float TargetReachDstSq;
    }

    public struct BotFindNewTarget : IComponentData, IEnableableComponent
    {
        public float Interval;
        public double TimeToFindNewTarget;
    }

    public struct BotTag : IComponentData
    {
    }

    [Serializable]
    public struct BotInitialSwordCount : IComponentData
    {
        public int Count;
    }
}