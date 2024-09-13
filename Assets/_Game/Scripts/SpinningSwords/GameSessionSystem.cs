using SpinningSwords.Data;
using Unity.Burst;
using Unity.Entities;

namespace SpinningSwords
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial struct GameSessionSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameSession>();
            state.RequireForUpdate<GameState>();

            state.EntityManager.CreateSingleton<GameSession>();
        }

        public void OnUpdate(ref SystemState state)
        {
            GameState gameState = SystemAPI.GetSingleton<GameState>();

            GameSession gameSession = SystemAPI.GetSingleton<GameSession>();
            gameSession.PlayTime += SystemAPI.Time.DeltaTime;
            SystemAPI.SetSingleton(gameSession);
        }
    }
}
