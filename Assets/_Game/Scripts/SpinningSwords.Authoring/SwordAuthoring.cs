using SpinningSwords.Data;
using Unity.Entities;
using UnityEngine;

namespace SpinningSwords.Authoring
{
    public class SwordAuthoring : MonoBehaviour
    {
        public class Baker : Baker<SwordAuthoring>
        {
            public override void Bake(SwordAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent<Sword>(entity);
            }
        }
    }
}


