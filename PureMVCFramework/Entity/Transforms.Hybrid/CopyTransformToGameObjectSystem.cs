using UnityEngine;
using UnityEngine.Scripting;

namespace PureMVCFramework.Entity
{
    [Preserve]
    [UpdateInGroup(typeof(TransformSystemGroup))]
    [UpdateAfter(typeof(TRSToLocalToWorldSystem))]
    public class CopyTransformToGameObjectSystem : HybridSystemBase<Transform, LocalToWorld, CopyTransformToGameObject>
    {
        protected override void OnUpdate(int index, Entity entity, Transform component1, LocalToWorld component2, CopyTransformToGameObject component3)
        {
            if (component1 != null)
            {
                var mat = (Matrix4x4)component2.Value;
                component1.position = component2.Position;
                component1.rotation = mat.rotation;
                component1.localScale = mat.lossyScale;
            }
        }
    }
}
