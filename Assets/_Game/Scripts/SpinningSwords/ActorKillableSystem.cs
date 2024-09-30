using SpinningSwords.Data;
using Unity.Burst;
using Unity.Entities;


namespace SpinningSwords
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial struct ActorKillableSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            foreach ((RefRO<SwordController> swordController, Entity entity) in SystemAPI.Query<RefRO<SwordController>>().WithAll<ThirdPersonCharacterControl>().WithEntityAccess())
            {
                if (swordController.ValueRO.SwordCount == 0 && !SystemAPI.HasComponent<ActorKillable>(entity))
                {
                    ecb.AddComponent<ActorKillable>(entity);
                }
                else if (swordController.ValueRO.SwordCount != 0 && SystemAPI.HasComponent<ActorKillable>(entity))
                {
                    ecb.RemoveComponent<ActorKillable>(entity);
                }
            }
            ecb.Playback(state.EntityManager);
        }
    }
}