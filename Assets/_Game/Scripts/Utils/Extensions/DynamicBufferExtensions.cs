using Unity.Entities;

namespace SpinningSwords.Utils.Extensions
{
    public static class DynamicBufferExtensions
    {
        public static int IndexOf<T>(this ref DynamicBuffer<T> buffer, T element)
            where T : unmanaged, IBufferElementData
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                T e = buffer[i];
                if (e.Equals(element))
                    return i;
            }
            return -1;
        }
    }
}
