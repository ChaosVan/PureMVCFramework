using System;
using Unity.Mathematics;

namespace PureMVCFramework.Entity
{
    public class LocalToWorld : IComponentData, IDisposable
    {
        public float4x4 Value;
        public float3 Right => new float3(Value.c0.x, Value.c0.y, Value.c0.z);
        public float3 Up => new float3(Value.c1.x, Value.c1.y, Value.c1.z);
        public float3 Forward => new float3(Value.c2.x, Value.c2.y, Value.c2.z);
        public float3 Position => new float3(Value.c3.x, Value.c3.y, Value.c3.z);
        public quaternion Rotation => new quaternion(Value);

        public LocalToWorld()
        {
            Value = float4x4.identity;
        }

        public void Dispose()
        {
            Value = float4x4.identity;
        }
    }
}
