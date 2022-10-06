using System;
using Unity.Mathematics;

namespace PureMVCFramework.Entity
{
    public class Scale : IComponentData, IDisposable
    {
        public float3 Value = 1;

        public void Dispose()
        {
            Value = 1;
        }

        public static implicit operator float3(Scale scale)
        {
            return scale.Value;
        }
    }
}
