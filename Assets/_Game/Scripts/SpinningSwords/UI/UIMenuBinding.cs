// <copyright file="UIMenuBinding.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace SpinningSwords.UI
{
    using BovineLabs.Core.UI;
    using Unity.Properties;

    public class UIMenuBinding : IBindingObject<UIMenuBinding.Data>
    {
        private Data data;
        private bool quitConfirmation;

        public ref Data Value => ref this.data;

        [CreateProperty]
        public bool Play
        {
            get => this.data.Play.Value;
            set => this.data.Play.TryProduce(value);
        }

        [CreateProperty]
        public bool Quit
        {
            get => this.data.Quit.Value;
            set => this.data.Quit.TryProduce(value);
        }

        public struct Data : IBindingObject
        {
            public ButtonEvent Play;
            public ButtonEvent Quit;
        }
    }
}
