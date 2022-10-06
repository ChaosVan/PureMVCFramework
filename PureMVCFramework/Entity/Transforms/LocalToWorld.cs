using System;
using Unity.Mathematics;

namespace PureMVCFramework.Entity
{
    public class LocalToWorld : IComponentData, IDisposable
    {
        public float4x4 Value = float4x4.identity;
        public float3 Right => Value.c0.xyz;
        public float3 Up => Value.c1.xyz;
        public float3 Forward => Value.c2.xyz;
        public float3 Position => Value.c3.xyz;
        public quaternion Rotation => new quaternion(Value);

        public void Dispose()
        {
            Value = float4x4.identity;
        }

        public static implicit operator float4x4(LocalToWorld matrix)
        {
            return matrix.Value;
        }
    }
}
