using UnityEngine;
using UnityEngine.Scripting;

namespace PureMVCFramework.Entity
{
    [Preserve]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class CopyInitialTransformFromGameObjectSystem : HybridSystemBase<Transform, LocalToWorld, CopyInitialTransformFromGameObject>
    {
        protected override void OnStartRunning()
        {
            base.OnStartRunning(); 
            
            CommandBufferSystem = World.GetExistingSystem<EndInitializationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate(int index, Entity entity, Transform component1, LocalToWorld component2, CopyInitialTransformFromGameObject component3)
        {
            if (component1 != null)
                component2.Value = component1.localToWorldMatrix;

            CommandBuffer.RemoveComponentData<CopyInitialTransformFromGameObject>(entity);

            var position = EntityManager.GetComponentData<Position>(entity);
            if (position != null)
                position.Value = component2.Position;

            var rotation = EntityManager.GetComponentData<Rotation>(entity);
            if (rotation != null)
                rotation.Value = component2.Rotation;

            var scale = EntityManager.GetComponentData<Scale>(entity);
            if (scale != null)
                scale.Value = component1.lossyScale;
        }
    }
}
