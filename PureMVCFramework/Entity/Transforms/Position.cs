using System;
using Unity.Mathematics;

namespace PureMVCFramework.Entity
{
    public class Position : IComponentData, IDisposable
    {
        public float3 Value;

        public void Dispose()
        {
            Value = float3.zero;
        }

        public static implicit operator float3(Position position)
        {
            return position.Value;
        }
    }
}
