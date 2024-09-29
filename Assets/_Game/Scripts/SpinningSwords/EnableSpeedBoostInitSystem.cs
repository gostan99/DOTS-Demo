using BovineLabs.Core.LifeCycle;
using SpinningSwords.Data;
using Unity.Burst;
using Unity.Entities;

namespace SpinningSwords
{
    [BurstCompile]
    [UpdateInGroup(typeof(InitializeSystemGroup))]
    public partial struct EnableSpeedBoostInitSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            foreach ((ThirdPersonCharacterComponent thirdPersonCharacterComponent, RefRW<EnableSpeedBoost> enableSpeedBoost)
                in SystemAPI.Query<ThirdPersonCharacterComponent, RefRW<EnableSpeedBoost>>().WithPresent<EnableSpeedBoost>().WithAll<InitializeEntity>())
            {
                enableSpeedBoost.ValueRW.NormalSpeed = thirdPersonCharacterComponent.GroundMaxSpeed;
            }
        }
    }
}
