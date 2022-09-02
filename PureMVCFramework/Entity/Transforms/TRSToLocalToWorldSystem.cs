using Unity.Mathematics;
using UnityEngine.Scripting;

namespace PureMVCFramework.Entity
{
    [Preserve]
    [UpdateInGroup(typeof(TransformSystemGroup))]
    public class TRSToLocalToWorldSystem : SystemBase<LocalToWorld, Position, Rotation, Scale>
    {
        protected override void OnUpdate(int index, Entity entity, LocalToWorld matrix, Position position, Rotation rotation, Scale scale)
        {
            matrix.Value = LocalToWorld.Compose(position, rotation, scale);
        }
    }
}
