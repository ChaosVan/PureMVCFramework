namespace PureMVCFramework.Entity
{
    public class LifeTime : IComponentData
    {
        public float Value;
    }

    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    public class LifeTimeSystem : SystemBase<LifeTime>
    {
        BeginSimulationEntityCommandBufferSystem m_EntityCommandBufferSystem;
        EntityCommandBuffer ecb;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        protected override void PreUpdate()
        {
            ecb = m_EntityCommandBufferSystem.CreateCommandBuffer();
        }

        protected override void OnUpdate(int index, Entity entity, LifeTime component)
        {
            component.Value -= Time.DeltaTime;

            if (component.Value <= 0)
            {
                ecb.DestroyEntity(entity);
            }
        }

        protected override void PostUpdate()
        {
            ecb.Dispose();
        }
    }
}
