using UnityEngine;
using UnityEngine.Scripting;

namespace PureMVCFramework.Entity
{
    [Preserve]
    [UpdateInGroup(typeof(TransformSystemGroup))]
    [UpdateBefore(typeof(TRSToLocalToWorldSystem))]
    public class CopyTransformFromGameObjectSystem : HybridSystemBase<Transform, LocalToWorld, CopyTransformFromGameObject>
    {
        protected override void OnUpdate(int index, Entity entity, Transform component1, LocalToWorld component2, CopyTransformFromGameObject component3)
        {
            if (component1 != null)
                component2.Value = component1.localToWorldMatrix;
        }
    }
}
