using System;
using Unity.Entities;

namespace SpinningSwords.Data
{
    [Serializable]
    public struct Puller : IComponentData
    {
        public float Range;
        public float Strength;
    }

    public struct PullableTag : IComponentData { }
}
