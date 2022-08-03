using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PureMVCFramework.Entity
{
    public abstract class ComponentSystem : ComponentSystemBase
    {

        public bool ShouldRunSystem() => CheckedState().ShouldRunSystem();

        public sealed override void InjectEntity(Entity entity)
        {

        }

        public sealed override void PreUpdate()
        {
            base.PreUpdate();
        }

        public sealed override void PostUpdate()
        {
            base.PostUpdate();
        }

        public sealed override void Update()
        {
            CheckedState();
            if (Enabled && ShouldRunSystem())
            {
                if (!m_StatePtr.PreviouslyEnabled)
                {
                    m_StatePtr.PreviouslyEnabled = true;
                    OnStartRunning();
                }
                PreUpdate();

                try
                {
                    OnUpdate();
                }
                finally
                {
                    PostUpdate();
                }
            }
            else if (m_StatePtr.PreviouslyEnabled)
            {
                m_StatePtr.PreviouslyEnabled = false;
                OnStopRunningInternal();
            }
        }

        protected virtual void OnUpdate() { }
    }
}
