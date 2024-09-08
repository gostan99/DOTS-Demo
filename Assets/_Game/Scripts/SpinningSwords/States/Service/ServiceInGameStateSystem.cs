// <copyright file="ServiceInGameStateSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace SpinningSwords.States.Service
{
    using BovineLabs.Core;
    using BovineLabs.Core.States;
    using SpinningSwords.Data;
    using SpinningSwords.States;
    using Unity.Entities;

    [UpdateInGroup(typeof(GameStateSystemGroup))]
    [WorldSystemFilter(Worlds.Service)]
    public partial class ServiceInGameStateSystem : SystemBase
    {
        protected override void OnCreate()
        {
            StateAPI.Register<GameState, StateGame, GameStates>(ref this.CheckedStateRef, "game");
        }

        /// <inheritdoc/>
        protected override void OnStartRunning()
        {
            BovineLabsBootstrap.CreateGameWorld();
        }

        /// <inheritdoc/>
        protected override void OnStopRunning()
        {
            BovineLabsBootstrap.DestroyGameWorld();
        }

        protected override void OnUpdate()
        {
        }

        private struct StateGame : IComponentData
        {
        }
    }
}
