using SpinningSwords.Data;
using Unity.Entities;
using UnityEngine;

namespace SpinningSwords.Authoring
{
    public class PickupSpeedBoostAuthoring : MonoBehaviour
    {
        public float BoostValue;
        public float Duration;
        public class Baker : Baker<PickupSpeedBoostAuthoring>
        {
            public override void Bake(PickupSpeedBoostAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new PickupSpeedBoost
                {
                    BoostValue = authoring.BoostValue,
                    Duration = authoring.Duration,
                });
            }
        }
    }
}
