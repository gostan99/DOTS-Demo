// <copyright file="UIGameSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace SpinningSwords.UI
{
    using BovineLabs.Core.States;
    using BovineLabs.Core.UI;
    using SpinningSwords;
    using SpinningSwords.Data;
    using Unity.Burst;
    using Unity.Entities;

    [UpdateInGroup(typeof(UISystemGroup))]
    public partial struct UIGameOverSystem : ISystem, ISystemStartStop
    {
        private UIHelper<UIGameOverBinding, UIGameOverBinding.Data> helper;

        public void OnCreate(ref SystemState state)
        {
            StateAPI.Register<UIState, State, UIStates>(ref state, "game over");
            this.helper = new UIHelper<UIGameOverBinding, UIGameOverBinding.Data>("game over");
        }

        public void OnStartRunning(ref SystemState state)
        {
            this.helper.Load();
            this.helper.Binding.PlayerCount = SystemAPI.GetSingleton<GameSession>().PlayerCount;
        }

        public void OnStopRunning(ref SystemState state)
        {
            this.helper.Unload();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (this.helper.Binding.Quit.TryConsume())
            {
                GameAPI.StateSet(ref state, "quit");
            }
        }

        private struct State : IComponentData
        {
        }
    }
}
