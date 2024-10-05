
using Unity.Entities;

namespace SpinningSwords.Data
{
    public struct GameSession : IComponentData
    {
        public float PlayTime;
        public float PlayTimeMax;
        public int PlayerCount;
    }
}
