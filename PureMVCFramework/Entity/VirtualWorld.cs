using PureMVC.Patterns.Observer;
using PureMVCFramework.Advantages;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace PureMVCFramework.Entity
{
    public interface IWorld
    {
        public float TimePerFrame { get; set; }

        void CheckModifiedEntities();
        void ModifyEntity(Entity entity);
        void Initialize();
        void Destroy();
    }

    public class VirtualWorld : Notifier, IWorld
    {

#if ODIN_INSPECTOR
        [ShowInInspector, ListDrawerSettings(IsReadOnly = true)]
#endif
        protected readonly List<ISystemBase> m_Systems = new List<ISystemBase>();

        private List<Entity> modifiedEntities = new List<Entity>();

        public float TimePerFrame { get; set; }

        public virtual void Initialize()
        {
            WorldManager.Instance.RegisterWorld(this);
        }

        public virtual void Destroy()
        {
            if (ReferencePool.applicationIsQuitting)
                return;

            for (int i = 0; i < m_Systems.Count; ++i)
            {
                ReferencePool.Instance.RecycleInstance(m_Systems[i]);
            }

            m_Systems.Clear();
        }

        public void CheckModifiedEntities()
        {
            for (int i = 0; i < m_Systems.Count; ++i)
            {
                for (int j = 0; j < modifiedEntities.Count; ++j)
                {
                    m_Systems[i].InjectEntity(modifiedEntities[j]);
                }
            }

            modifiedEntities.Clear();
        }

        public void ModifyEntity(Entity entity)
        {
            if (!modifiedEntities.Contains(entity))
                modifiedEntities.Add(entity);
        }

        public ISystemBase RegisterSystem<T>() where T : ISystemBase, new()
        {
            var system = ReferencePool.Instance.SpawnInstance<T>();
            m_Systems.Add(system);
            system.World = this;

            return system;
        }

        public ISystemBase RegisterSystem(System.Type type)
        {
            var system = (ISystemBase)ReferencePool.Instance.SpawnInstance(type);
            m_Systems.Add(system);
            system.World = this;

            return system;
        }

        public ISystemBase RegisterSystem(string typeName)
        {
            var system = (ISystemBase)ReferencePool.Instance.SpawnInstance(typeName);
            m_Systems.Add(system);
            system.World = this;

            return system;
        }

        public void RemoveSystem(ISystemBase system)
        {
            if (m_Systems.Contains(system))
            {
                m_Systems.Remove(system);
                ReferencePool.Instance.RecycleInstance(system);
            }
        }
    }
}
