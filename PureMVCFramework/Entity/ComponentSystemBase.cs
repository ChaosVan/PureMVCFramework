using PureMVC.Patterns.Observer;
using PureMVCFramework.Advantages;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PureMVCFramework.Entity
{
    public interface ISystem
    {
        void Update();
        void PreUpdate();
        void PostUpdate();
    }

    public abstract class ComponentSystemBase : Notifier, ISystem, IRecycleable, IInitializeable
    {
#if ODIN_INSPECTOR && UNITY_EDITOR
        [SerializeField] protected bool showOdinInfo;
#endif
        protected readonly List<Entity> Entities = new List<Entity>();

        internal SystemState m_StatePtr;

        internal SystemState CheckedState()
        {
            return m_StatePtr;
        }

#if ODIN_INSPECTOR
        [ShowIf("showOdinInfo"), ShowInInspector]
#endif
        public bool Enabled { get => CheckedState().Enabled; }

        public World World => (World)m_StatePtr.m_World.Target;

        public TimeData Time => World.Time;

        // ============

        internal void CreateInstance(World world, SystemState statePtr)
        {
            m_StatePtr = statePtr;
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

        internal virtual void OnBeforeCreateInternal(World world)
        {
        }

        internal void OnAfterDestroyInternal()
        {
            m_StatePtr = default;
        }

        internal virtual void OnBeforeDestroyInternal()
        {
            var state = CheckedState();

            if (state.PreviouslyEnabled)
            {
                state.PreviouslyEnabled = false;
                OnStopRunning();
            }
        }


        public abstract void InjectEntity(Entity entity);


        public virtual void PreUpdate() { }

        public virtual void PostUpdate() { }

        public virtual void OnRecycle()
        {
            
        }

        public virtual void OnInitialized(params object[] args)
        {
            
        }
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
