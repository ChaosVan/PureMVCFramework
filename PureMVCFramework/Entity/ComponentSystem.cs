using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PureMVCFramework.Entity
{
    public abstract class ComponentSystem : ComponentSystemBase
    {
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
                    OnUpdate();
                }
                finally
                {
                    world.ExecutingSystem = oldExecutingSystem;
                }
            }
            else if (m_State.PreviouslyEnabled)
            {
                m_State.PreviouslyEnabled = false;
                OnStopRunningInternal();
            }
        }

        internal sealed override void OnBeforeCreateInternal(World world)
        {
            base.OnBeforeCreateInternal(world);
        }

        internal sealed override void OnBeforeDestroyInternal()
        {
            base.OnBeforeDestroyInternal();
        }

        /// <summary>Implement OnUpdate to perform the major work of this system.</summary>
        /// <remarks>
        /// The system invokes OnUpdate once per frame on the main thread when any of this system's
        /// EntityQueries match existing entities, or if the system has the <see cref="AlwaysUpdateSystemAttribute"/>.
        /// </remarks>
        /// <seealso cref="ComponentSystemBase.ShouldRunSystem"/>
        protected abstract void OnUpdate();
    }
}
