using SpinningSwords.Data;
using Unity.Entities;
using UnityEngine;

namespace SpinningSwords.Authoring
{
    public class PickupSwordWeightAuthoring : MonoBehaviour
    {
        public float Value;
        public class Baker : Baker<PickupSwordWeightAuthoring>
        {
            public override void Bake(PickupSwordWeightAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new PickupSwordWeight { Value = authoring.Value });
            }
        }
    }
}
