// <copyright file="UIMenuBinding.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace SpinningSwords.UI
{
    using BovineLabs.Core.UI;
    using Unity.Properties;

    public class UIGameOverBinding : IBindingObject<UIGameOverBinding.Data>
    {
        private Data data;

        public ref Data Value => ref this.data;

        [CreateProperty]
        public bool Quit
        {
            get => this.data.Quit.Value;
            set => this.data.Quit.TryProduce(value);
        }

        [CreateProperty]
        public int PlayerCount
        {
            get => this.data.PlayerCount;
            set => this.data.PlayerCount = value;
        }

        public struct Data : IBindingObject
        {
            public ButtonEvent Quit;
            public int PlayerCount;
        }
    }
}
