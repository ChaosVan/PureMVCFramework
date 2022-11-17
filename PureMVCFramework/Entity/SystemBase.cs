#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using System.Collections.Generic;

namespace PureMVCFramework.Entity
{
    public abstract class SystemBase : ComponentSystemBase
    {
#if ODIN_INSPECTOR
        [ShowIf("showOdinInfo"), ShowInInspector, ListDrawerSettings(IsReadOnly = true)]
#endif
        protected readonly List<Entity> Entities = new List<Entity>();

        protected EntityCommandBufferSystem CommandBufferSystem;
        protected EntityCommandBuffer CommandBuffer;

        private EntityQuery queries;

        protected void OverwriteEntityQuery(params ComponentType[] types)
        {
            queries = new EntityQuery(types);
        }

        protected virtual bool CheckEntityVaild(Entity entity, out IComponentData[] components)
        {
            return EntityManager.GetComponentData(entity, queries, out components);
        }

        public void InjectEntity(Entity entity)
        {
            bool tf = CheckEntityVaild(entity, out var components);
            if (Entities.Contains(entity))
            {
                if (!tf)
                {
                    var i = Entities.IndexOf(entity);
                    ClearComponents(i);
                    Entities.RemoveAt(i);
                }
            }
            else if (tf)
            {
                CollectComponents(entity, components);
                Entities.Add(entity);
            }
        }

        protected virtual void CollectComponents(Entity entity, IComponentData[] components)
        {

        }

        protected virtual void ClearComponents(int index)
        {

        }

        protected virtual void PreUpdate()
        {
            if (CommandBufferSystem != null)
                CommandBuffer = CommandBufferSystem.CreateCommandBuffer();
        }

        protected virtual void PostUpdate()
        {
            CommandBuffer = null;
        }

        protected override void OnStopRunning()
        {
            base.OnStopRunning();

            CommandBufferSystem = null;
        }

        public sealed override bool ShouldRunSystem()
        {
            if (Entities.Count == 0)
                return false;

            return base.ShouldRunSystem();
        }

        public sealed override void Update()
        {
            CheckedState();
            if (Enabled && ShouldRunSystem())
            {
                if (!m_State.PreviouslyEnabled)
                {
                    m_State.PreviouslyEnabled = true;
                    OnStartRunning();
                }

                var world = World.Unmanaged;
                var oldExecutingSystem = world.ExecutingSystem;
                world.ExecutingSystem = m_State.m_Handle;

                try
                {
                    PreUpdate();
                    OnUpdate();
                }
                finally
                {
                    PostUpdate();
                    world.ExecutingSystem = oldExecutingSystem;
                }
            }
            else if (m_State.PreviouslyEnabled)
            {
                m_State.PreviouslyEnabled = false;
                OnStopRunningInternal();
            }
        }

        internal sealed override void OnBeforeDestroyInternal()
        {
            base.OnBeforeDestroyInternal();
        }

        internal sealed override void OnBeforeCreateInternal(World world)
        {
            base.OnBeforeCreateInternal(world);
        }

        internal sealed override void OnStopRunningInternal()
        {
            base.OnStopRunningInternal();
            Entities.Clear();
        }

        protected abstract void OnUpdate();
    }
}
