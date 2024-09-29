using SpinningSwords.Data;
using Unity.Entities;
using UnityEngine;

namespace SpinningSwords.Authoring
{
    public class PlayerAuthoring : MonoBehaviour
    {
        [Header("Puller")]
        public Puller puller;

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

        public class Baker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent<PlayerTag>(entity);

                AddComponent(entity, authoring.puller);

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

                AddComponent<EnableSpeedBoost>(entity);
                SetComponentEnabled<EnableSpeedBoost>(entity, false);
            }
        }
    }
}