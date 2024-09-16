using SpinningSwords.Data;
using Unity.Entities;
using UnityEngine;

namespace SpinningSwords.Authoring
{
    public class PickupSwordAuthoring : MonoBehaviour
    {
        public class Baker : Baker<PickupSwordAuthoring>
        {
            public override void Bake(PickupSwordAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<PickupSword>(entity);
            }
        }
    }
}
