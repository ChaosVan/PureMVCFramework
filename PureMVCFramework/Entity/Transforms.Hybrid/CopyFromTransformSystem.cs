using UnityEngine;

namespace PureMVCFramework.Entity
{
    [UpdateInGroup(typeof(TransformSystemGroup))]
    [UpdateBefore(typeof(TRSToLocalToWorldSystem))]
    public class CopyFromTransformSystem : HybridSystemBase<Transform, LocalToWorld, CopyFromTransformComponent>
    {
        protected override void OnUpdate(int index, Entity entity, Transform component1, LocalToWorld component2, CopyFromTransformComponent component3)
        {
            if (component1 != null)
                component2.Value = component1.localToWorldMatrix;
        }
    }
}
