using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

namespace PureMVCFramework.Entity
{
    [Preserve]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class CopyInitialTransformFromGameObjectSystem : HybridSystemBase<Transform, LocalToWorld, CopyInitialTransformFromGameObject>
    {
        private EndInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;
        private EntityCommandBuffer commandBuffer;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_EntityCommandBufferSystem = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
        }

        protected override void PreUpdate()
        {
            commandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer();
        }

        protected override void PostUpdate()
        {
            commandBuffer = null;
        }

        protected override void OnUpdate(int index, Entity entity, Transform component1, LocalToWorld component2, CopyInitialTransformFromGameObject component3)
        {
            if (component1 != null)
                component2.Value = component1.localToWorldMatrix;

            commandBuffer.RemoveComponentData<CopyInitialTransformFromGameObject>(entity);

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
