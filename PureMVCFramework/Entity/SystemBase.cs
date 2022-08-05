#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PureMVCFramework.Entity
{
    public abstract class SystemBase : ComponentSystemBase
    {
#if ODIN_INSPECTOR
        [ShowIf("showOdinInfo"), ShowInInspector, ListDrawerSettings(IsReadOnly = true)]
#endif
        protected readonly List<Entity> Entities = new List<Entity>();

        public abstract void InjectEntity(Entity entity);

        protected virtual void PreUpdate()
        {

        }

        protected virtual void PostUpdate()
        {

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
                    world.ExecutingSystem = oldExecutingSystem;
                }

                PostUpdate();
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

        internal override void OnStopRunningInternal()
        {
            base.OnStopRunningInternal();
            Entities.Clear();
        }

        protected abstract void OnUpdate();
    }
}
