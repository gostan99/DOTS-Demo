// <copyright file="GameSettings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace SpinningSwords.Authoring
{
    using BovineLabs.Core.Authoring.Settings;
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.Settings;
    using SpinningSwords.Data;
    using Unity.Entities;
    using UnityEngine;

    [SettingsGroup("Core")]
    [SettingsWorld("Shared")]
    public class GameSettings : BovineLabs.Core.Authoring.GameSettings
    {
        [Tooltip("In seconds")]
        public float PlayTimeMax = 30;
        public int PlayerCount = 5;

        public override void Bake(Baker<SettingsAuthoring> baker)
        {
            base.Bake(baker);

            Entity entity = baker.GetEntity(TransformUsageFlags.None);
            GameState gameState = new GameState { Value = new BitArray256 { [0] = true } };

            // States
            baker.AddComponent(entity, gameState);
            baker.AddComponent<GameStatePrevious>(entity);
            baker.AddComponent(entity, new GameSession
            {
                PlayTimeMax = this.PlayTimeMax,
                PlayerCount = PlayerCount
            });
        }
    }
}
