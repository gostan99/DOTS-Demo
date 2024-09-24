using SpinningSwords.Data;
using Unity.Entities;
using UnityEngine;

namespace SpinningSwords.Authoring
{
    public class BotAuthoring : MonoBehaviour
    {
        [Header("Sword Controller")]
        public GameObject SwordPrefab;
        public GameObject OrbitTarget;
        public float InitialOrbitSpeed;
        public float OrbitDistance;
        public float MaxOrbitSpeed;
        public float MaxWeight;
        public int MaxSwordCount;

        [Header("Sword Equidistant")]
        public float SwordEquidistantDuration;

        public class Baker : Baker<BotAuthoring>
        {
            public override void Bake(BotAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent<BotTag>(entity);

                AddComponent(entity, new SwordPrefab { Value = GetEntity(authoring.SwordPrefab, TransformUsageFlags.Dynamic) });

                AddComponent<SwordColliders>(entity);

                AddComponent(entity, new SwordController
                {
                    OrbitTargetEntity = GetEntity(authoring.OrbitTarget, TransformUsageFlags.Dynamic),
                    OrbitSpeed = authoring.InitialOrbitSpeed,
                    OrbitDistance = authoring.OrbitDistance,
                    OrbitMaxSpeed = authoring.MaxOrbitSpeed,
                    MaxWeight = authoring.MaxWeight,
                    MaxSwordCount = authoring.MaxSwordCount,
                    SwordCount = 0,
                    Weight = 1,
                });

                AddComponent(entity, new SwordEquidistant
                {
                    Duration = authoring.SwordEquidistantDuration,
                });
                SetComponentEnabled<SwordEquidistant>(entity, false);

                AddBuffer<SwordBuffer>(entity);
            }
        }
    }
}