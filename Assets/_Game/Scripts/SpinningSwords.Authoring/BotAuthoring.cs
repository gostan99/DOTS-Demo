using SpinningSwords.Data;
using Unity.Entities;
using UnityEngine;

namespace SpinningSwords.Authoring
{
    public class BotAuthoring : MonoBehaviour
    {
        public float TargetReachDistance;

        public BotInitialSwordCount InitialSwordCount;

        [Header("Bot Find New Target")]
        public float BotFindNewTargetInterval;

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

                AddComponent(entity, new BotTarget { TargetReachDstSq = authoring.TargetReachDistance * authoring.TargetReachDistance });

                AddComponent(entity, new BotFindNewTarget { Interval = authoring.BotFindNewTargetInterval });

                AddComponent(entity, new SwordPrefab { Value = GetEntity(authoring.SwordPrefab, TransformUsageFlags.Dynamic) });

                AddComponent<SwordColliders>(entity);

                AddComponent(entity, authoring.InitialSwordCount);

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