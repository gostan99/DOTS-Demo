using SpinningSwords.Data;
using Unity.Entities;
using UnityEngine;

namespace SpinningSwords.Authoring
{
    public class PullableAuthoring : MonoBehaviour
    {
        public class Baker : Baker<PullableAuthoring>
        {
            public override void Bake(PullableAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<PullableTag>(entity);
            }
        }
    }
}