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
}
