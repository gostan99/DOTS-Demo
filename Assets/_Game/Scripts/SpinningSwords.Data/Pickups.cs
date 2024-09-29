using Unity.Entities;

namespace SpinningSwords.Data
{
    public struct PickupSword : IComponentData
    {
    }

    public struct PickupSwordWeight : IComponentData
    {
        public float Value;
    }

    public struct PickupSpeedBoost : IComponentData
    {
        public float BoostValue;
        public float Duration;
    }

    public struct EnableSpeedBoost : IComponentData, IEnableableComponent
    {
        public double DisableAt;
        public float NormalSpeed;
    }
}
