using BovineLabs.Core.Entropy;
using BovineLabs.Core.Extensions;
using SpinningSwords.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

namespace SpinningSwords
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    [UpdateAfter(typeof(GameSessionSystem))]
    public partial struct SpawnerSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameSession>();
            state.RequireForUpdate<Schedule>();
        }

        public void OnUpdate(ref SystemState state)
        {
            GameSession sessionData = state.GetSingleton<GameSession>();
            ArenaCenter arenaCenter = state.GetSingleton<ArenaCenter>();
            DynamicBuffer<Schedule> schedules = SystemAPI.QueryBuilder().WithAll<Schedule>().Build().GetSingletonBufferNoSync<Schedule>(true);
            ScheduleIndex scheduleIndex = state.GetSingleton<ScheduleIndex>();

            if (scheduleIndex.Value < schedules.Length)
            {
                if (sessionData.PlayTime > schedules[scheduleIndex.Value].Time)
                {
                    /*
                     * Lấy tất cả index thỏa mãn điều kiện để spawn trong 1 khoảng delta time.
                     * Ví dụ: timeline để spawn là 2s, 2.1s mà delta time là 1s => trong khoảng 1s đó cần phải spawn cho cả 2s và 2.1s
                     */
                    int tempIndex = scheduleIndex.Value + 1;
                    NativeList<int> indicies = new NativeList<int>(Allocator.Temp);
                    indicies.Add(scheduleIndex.Value);
                    while (tempIndex < schedules.Length)
                    {
                        if (sessionData.PlayTime > schedules[tempIndex].Time)
                        {
                            indicies.Add(tempIndex);
                        }
                        tempIndex++;
                    }

                    ref Random random = ref SystemAPI.GetSingleton<Entropy>().Random.GetRandomRef(); // dùng Entropy random thay vì Unity random để không phải tạo seed
                    foreach (int index in indicies)
                    {
                        TimeToSpawn timeToSpawn = new TimeToSpawn { Value = schedules[index].Time };
                        foreach ((TimeToSpawn _, SpawnEntityConfig configs) in SystemAPI.Query<TimeToSpawn, SpawnEntityConfig>().WithSharedComponentFilter(timeToSpawn))
                        {
                            NativeArray<Entity> spawnedEntities = new NativeArray<Entity>(configs.Count, Allocator.Temp);
                            // Spawn entities
                            state.EntityManager.Instantiate(configs.Prefab, spawnedEntities);
                            // make positions and rotations in range
                            NativeArray<float3> positions = new NativeArray<float3>(configs.Count, Allocator.Temp);
                            NativeArray<quaternion> rotations = new NativeArray<quaternion>(configs.Count, Allocator.Temp);
                            RandomPointsInRange(arenaCenter.Value, quaternion.Euler(math.up()), configs.MinSpawnRadius, configs.MaxSpawnRadius, ref positions, ref rotations, ref random);
                            // set position and rotation
                            for (int i = 0; i < spawnedEntities.Length; i++)
                            {
                                Entity entity = spawnedEntities[i];
                                LocalTransform transform = state.EntityManager.GetComponentData<LocalTransform>(entity);
                                transform.Position = positions[i];
                                transform.Rotation = rotations[i];
                                state.EntityManager.SetComponentData(entity, transform);
                            }
                        }
                    }
                    scheduleIndex.Value = scheduleIndex.Value == indicies[^1] ? scheduleIndex.Value + 1 : indicies[^1];
                    state.EntityManager.SetSingleton<ScheduleIndex>(scheduleIndex);
                }
            }
        }

        private static void RandomPointsInRange(
           float3 center, quaternion orientation, float minRadius, float maxRadius,
           ref NativeArray<float3> positions, ref NativeArray<quaternion> rotations, ref Random random)
        {
            int count = positions.Length;
            for (int i = 0; i < count; i++)
            {
                quaternion rotation = quaternion.Euler(0, random.NextFloat(0, 360), 0); // random Y-axis rotation;
                float3 position = new float3(random.NextFloat(minRadius, maxRadius), 0, random.NextFloat(minRadius, maxRadius));
                position = center + math.mul(rotation, position);
                positions[i] = position;
                rotations[i] = rotation;
            }
        }
    }
}