using PureMVCFramework.Advantages;
using System.Diagnostics;
using UnityEngine.Scripting;

namespace PureMVCFramework.Entity
{
    public class LifeTime : IComponentData, IInitializable
    {
        public float Value;

        public void OnInitialized(params object[] args)
        {
            if (args.Length > 0)
                Value = (float)args[0];
        }
    }

    [Preserve]
    [UpdateInGroup(typeof(LateSimulationSystemGroup), OrderLast = true)]
    public class LifeTimeSystem : SystemBase<LifeTime>
    {
        EndSimulationEntityCommandBufferSystem m_EntityCommandBufferSystem;
        EntityCommandBuffer commandBuffer;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_EntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void PreUpdate()
        {
            commandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer();
        }

        protected override void PostUpdate()
        {
            commandBuffer = null;
        }

        protected override void OnUpdate(int index, Entity entity, LifeTime component)
        {
            component.Value -= Time.DeltaTime;

            if (component.Value <= 0)
            {
                commandBuffer.DestroyEntity(entity);
            }
        }
    }
}
