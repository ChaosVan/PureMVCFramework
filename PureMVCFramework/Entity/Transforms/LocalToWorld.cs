using System;
using Unity.Mathematics;

namespace PureMVCFramework.Entity
{
    public class LocalToWorld : IComponentData, IDisposable
    {
        public float4x4 Value;
        public float3 Right => Value.c0.xyz;
        public float3 Up => Value.c1.xyz;
        public float3 Forward => Value.c2.xyz;
        public float3 Position => Value.c3.xyz;
        public quaternion Rotation => new quaternion(Value);
        public float3 Scale
        {
            get
            {
                var m = math.mul(Value, Compose(Position, math.inverse(Rotation), 1));
                return new float3(m.c0.x, m.c1.y, m.c2.z);
            }
        }

        public LocalToWorld()
        {
            Value = float4x4.identity;
        }

        public void Dispose()
        {
            Value = float4x4.identity;
        }

        public static float4x4 Compose(float3 position, quaternion rotation, float3 scale)
        {
            return math.mul(new float4x4(rotation, position), float4x4.Scale(scale));
        }

        public static implicit operator float4x4(LocalToWorld matrix)
        {
            return matrix.Value;
        }
    }
}
