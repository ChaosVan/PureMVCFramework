using PureMVC.Interfaces;
using System;
using UnityEngine;

namespace PureMVCFramework.Entity
{
    public interface ISystem
    {
        void Update();
    }

    /// <summary>
    /// A system provides behavior in an ECS architecture.
    /// </summary>
    /// <remarks>
    /// System implementations should inherit <see cref="SystemBase"/>, which is a subclass of ComponentSystemBase.
    /// </remarks>
    public abstract class ComponentSystemBase : INotifier
    {
#if UNITY_EDITOR
        [SerializeField] protected bool showOdinInfo;
#endif

        internal SystemState m_State;

        internal ref SystemState CheckedState()
        {
            var state = m_State;
            if (!state.Enabled)
            {
                throw new InvalidOperationException("system state is not initialized or has already been destroyed");
            }
            return ref m_State;
        }

        public bool Enabled { get => m_State.Enabled; }

        public World World => (World) CheckedState().m_World.Target;

        public TimeData Time => World.Time;

        // ============

        internal void CreateInstance(World world, SystemState statePtr)
        {
            m_State = statePtr;
            OnBeforeCreateInternal(world);
            try
            {
                OnCreate();
            }
            catch
            {
                OnBeforeDestroyInternal();
                OnAfterDestroyInternal();
                throw;
            }
        }

        internal void DestroyInstance()
        {
            OnBeforeDestroyInternal();
            OnDestroy();
            OnAfterDestroyInternal();
        }

        protected virtual void OnCreate()
        {
        }

        /// <summary>
        /// Called before the first call to OnUpdate and when a system resumes updating after being stopped or disabled.
        /// </summary>
        /// <remarks>If the <see cref="EntityQuery"/> objects defined for a system do not match any existing entities
        /// then the system skips updates until a successful match is found. Likewise, if you set <see cref="Enabled"/>
        /// to false, then the system stops running. In both cases, <see cref="OnStopRunning"/> is
        /// called when a running system stops updating; OnStartRunning is called when it starts updating again.
        /// </remarks>
        protected virtual void OnStartRunning()
        {
        }

        /// <summary>
        /// Called when this system stops running because no entities match the system's <see cref="EntityQuery"/>
        /// objects or because you change the system <see cref="Enabled"/> property to false.
        /// </summary>
        /// <remarks>If the <see cref="EntityQuery"/> objects defined for a system do not match any existing entities
        /// then the system skips updating until a successful match is found. Likewise, if you set <see cref="Enabled"/>
        /// to false, then the system stops running. In both cases, <see cref="OnStopRunning"/> is
        /// called when a running system stops updating; OnStartRunning is called when it starts updating again.
        /// </remarks>
        protected virtual void OnStopRunning()
        {
        }

        internal virtual void OnStopRunningInternal()
        {
            OnStopRunning();
        }

        /// <summary>
        /// Called when this system is destroyed.
        /// </summary>
        /// <remarks>Systems are destroyed when the application shuts down, the World is destroyed, or you
        /// call <see cref="World.DestroySystem"/>. In the Unity Editor, system destruction occurs when you exit
        /// Play Mode and when scripts are reloaded.</remarks>
        protected virtual void OnDestroy()
        {
        }

        internal void OnDestroy_Internal()
        {
            OnDestroy();
        }

        /// <summary>
        /// Executes the system immediately.
        /// </summary>
        /// <remarks>The exact behavior is determined by this system's specific subclass.</remarks>
        /// <seealso cref="ComponentSystemGroup"/>
        /// <seealso cref="EntityCommandBufferSystem"/>
        public abstract void Update();

        internal ComponentSystemBase GetSystemFromSystemID(World world, int systemID)
        {
            foreach (var system in world.Systems)
            {
                if (system == null) continue;

                if (system.CheckedState().m_SystemID == systemID)
                {
                    return system;
                }
            }

            return null;
        }

        public virtual bool ShouldRunSystem() => CheckedState().ShouldRunSystem();

        internal virtual void OnBeforeCreateInternal(World world)
        {
        }

        internal void OnAfterDestroyInternal()
        {
            m_State = default;
        }

        internal virtual void OnBeforeDestroyInternal()
        {
            CheckedState();
            if (m_State.PreviouslyEnabled)
            {
                m_State.PreviouslyEnabled = false;
                OnStopRunning();
            }
        }

        protected IFacade Facade => PureMVC.Patterns.Facade.Facade.GetInstance(() => new PureMVC.Patterns.Facade.Facade());
        public void SendNotification(string notificationName, object body = null, string type = null)
        {
            Facade.SendNotification(notificationName, body, type);
        }

#if THREAD_SAFE
        public void SendNotificationSafe(string notificationName, object body = null, string type = null)
        {
            Facade.SendNotificationSafe(notificationName, body, type);
        }
#endif
    }

    // Updating before or after a system constrains the scheduler ordering of these systems within a ComponentSystemGroup.
    // Both the before & after system must be a members of the same ComponentSystemGroup.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
    public class UpdateBeforeAttribute : Attribute
    {
        public UpdateBeforeAttribute(Type systemType)
        {
            if (systemType == null)
                throw new ArgumentNullException(nameof(systemType));

            SystemType = systemType;
        }

        public Type SystemType { get; }
    }

    // Updating before or after a system constrains the scheduler ordering of these systems within a ComponentSystemGroup.
    // Both the before & after system must be a members of the same ComponentSystemGroup.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
    public class UpdateAfterAttribute : Attribute
    {
        public UpdateAfterAttribute(Type systemType)
        {
            if (systemType == null)
                throw new ArgumentNullException(nameof(systemType));

            SystemType = systemType;
        }

        public Type SystemType { get; }
    }

    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
    public class AlwaysUpdateSystemAttribute : Attribute
    {
    }
}
