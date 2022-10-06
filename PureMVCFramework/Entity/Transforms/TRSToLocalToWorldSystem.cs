using Unity.Mathematics;
using UnityEngine.Scripting;

namespace PureMVCFramework.Entity
{
    [Preserve]
    [UpdateInGroup(typeof(TransformSystemGroup))]
    public class TRSToLocalToWorldSystem : SystemBase<LocalToWorld, Position, Rotation, Scale>
    {
        protected override void OnUpdate(int index, Entity entity, LocalToWorld localToWorld, Position position, Rotation rotation, Scale scale)
        {
            localToWorld.Value = float4x4.TRS(position, rotation, scale);
        }
    }
}
