using UnityEngine;
using UnityEngine.Scripting;

namespace PureMVCFramework.Entity
{
    [Preserve]
    [UpdateInGroup(typeof(TransformSystemGroup))]
    [UpdateAfter(typeof(CopyTransformToGameObjectSystem))]
    public class CopyScaleToGameObjectSystem : HybridSystemBase<Transform, Scale, CopyScaleToGameObject>
    {
        protected override void OnUpdate(int index, Entity entity, Transform component1, Scale component2, CopyScaleToGameObject component3)
        {
            component1.localScale = component2.Value;
        }
    }
}
