
namespace BovineLabs.Core.Authoring
{
    using BovineLabs.Core.Authoring.Settings;
    using BovineLabs.Core.Settings;
    using SpinningSwords.Data;
    using System;
    using System.Linq;
    using Unity.Entities;
    using UnityEngine;

    [SettingsGroup("Core")]
    [SettingsWorld("Game")]
    public class SpawnerSettings : Settings.SettingsBase
    {
        /// <inheritdoc />
        public override void Bake(Baker<SettingsAuthoring> baker)
        {
            Entity entity = baker.GetEntity(TransformUsageFlags.None);

            baker.AddComponent(entity, new ArenaCenter { Value = ArenaCenter });

            float[] schedules = Array.ConvertAll(Schedules, item => item.TimeToSpawn);
            schedules = schedules.Distinct().ToArray();
            // sắp xếp lại theo thứ tự thời gian
            Array.Sort(schedules, new Comparison<float>(
              (i1, i2) => i1.CompareTo(i2)));

            DynamicBuffer<SpinningSwords.Data.Schedule> schedulesBuffer = baker.AddBuffer<SpinningSwords.Data.Schedule>(entity);
            foreach (float time in schedules)
            {
                schedulesBuffer.Add(new SpinningSwords.Data.Schedule { Time = time });
            }
            baker.AddComponent(entity, new ScheduleIndex { Value = 0 });

            foreach (Schedule schedule in Schedules)
            {
                foreach (Config spawnEntity in schedule.Entities)
                {
                    Entity prefab = baker.GetEntity(spawnEntity.Prefab, TransformUsageFlags.Dynamic);
                    Entity configHolder = baker.CreateAdditionalEntity(TransformUsageFlags.None);
                    baker.AddComponent(configHolder, new SpinningSwords.Data.SpawnEntityConfig
                    {
                        Prefab = baker.GetEntity(spawnEntity.Prefab, TransformUsageFlags.Dynamic),
                        Count = spawnEntity.Count,
                        MaxSpawnRadius = spawnEntity.MaxSpawnRadius,
                        MinSpawnRadius = spawnEntity.MinSpawnRadius,
                    });
                    baker.AddSharedComponent(configHolder, new TimeToSpawn { Value = schedule.TimeToSpawn });
                }
            }
        }


        [Serializable]
        public struct Config
        {
            public GameObject Prefab;
            public int Count;
            public float MinSpawnRadius;
            public float MaxSpawnRadius;
        }

        [Serializable]
        public struct Schedule
        {
            [Tooltip("Time since start playing in seconds")]
            public float TimeToSpawn;
            public Config[] Entities;
        }

        [SerializeField]
        private Vector3 ArenaCenter;

        [SerializeField]
        private Schedule[] Schedules;
    }
}
