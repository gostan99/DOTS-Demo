using SpinningSwords.Data;
using Unity.Entities;
using UnityEngine;

namespace SpinningSwords.Authoring
{
    public class PlayerAuthoring : MonoBehaviour
    {
        public Puller puller;

        public class Baker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, authoring.puller);
            }
        }
    }
}