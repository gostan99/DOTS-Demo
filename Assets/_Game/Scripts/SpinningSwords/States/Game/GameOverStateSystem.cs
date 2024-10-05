// <copyright file="GameQuitStateSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace SpinningSwords.States.Game
{
    using BovineLabs.Core.States;
    using SpinningSwords.Data;
    using SpinningSwords.States;
    using Unity.Burst;
    using Unity.Entities;
    using Unity.Mathematics;

    [UpdateInGroup(typeof(GameStateSystemGroup))]
    public partial struct GameOverStateSystem : ISystem, ISystemStartStop
    {
        private EntityQuery playerQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            playerQuery = SystemAPI.QueryBuilder().WithAll<ThirdPersonPlayer, ThirdPersonPlayerInputs>().Build();

            StateAPI.Register<GameState, StateOver, GameStates>(ref state, "game over");
        }

        public void OnStartRunning(ref SystemState state)
        {
            GameAPI.UISet(ref state, "game over");

            // stop player input
            foreach (ThirdPersonPlayer player in playerQuery.ToComponentDataArray<ThirdPersonPlayer>(state.WorldUpdateAllocator))
            {
                if (state.EntityManager.Exists(player.ControlledCharacter))
                {
                    RefRW<ThirdPersonCharacterControl> characterControll = SystemAPI.GetComponentRW<ThirdPersonCharacterControl>(player.ControlledCharacter);
                    characterControll.ValueRW.MoveVector = float3.zero;
                }

                RefRW<OrbitCameraControl> cameraControll = SystemAPI.GetComponentRW<OrbitCameraControl>(player.ControlledCamera);
                cameraControll.ValueRW.ZoomDelta = 0;
                cameraControll.ValueRW.LookDegreesDelta = 0;
            }
            state.EntityManager.RemoveComponent<ThirdPersonPlayerInputs>(playerQuery);
        }

        public void OnStopRunning(ref SystemState state)
        {
        }

        private struct StateOver : IComponentData
        {
        }
    }
}
