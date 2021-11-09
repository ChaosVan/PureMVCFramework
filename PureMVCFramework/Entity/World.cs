using System.Collections.Generic;
using UnityEngine;
using PureMVCFramework.Advantages;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace PureMVCFramework.Entity
{
    public class World : SingletonBehaviour<World>
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RuntimeOnDisableDomainReload()
        {
            applicationIsQuitting = false;
        }

#if ODIN_INSPECTOR
        [ShowInInspector, ShowIf("showOdinInfo"), ListDrawerSettings(IsReadOnly = true)]
#endif
        private readonly List<ISystemBase> m_Systems = new List<ISystemBase>();

        public float DeltaTime { get; private set; }

        protected override void OnDelete()
        {
            for (int i = 0; i < m_Systems.Count; ++i)
            {
                m_Systems[i].OnRelease();
            }

            m_Systems.Clear();

            base.OnDelete();
        }

        public void InjectEntity(Entity entity)
        {
            foreach (var system in m_Systems)
            {
                system.InjectEntity(entity);
            }
        }

        public void RegisterSystem<T>() where T : ISystemBase, new()
        {
            var system = ReferencePool.Instance.SpawnInstance<T>();
            m_Systems.Add(system);
            system.OnInitialized();
        }

        public void RegisterSystem(System.Type type)
        {
            var system = (ISystemBase)ReferencePool.Instance.SpawnInstance(type);
            m_Systems.Add(system);
            system.OnInitialized();
        }

        public void RegisterSystem(string typeName)
        {
            var system = (ISystemBase)ReferencePool.Instance.SpawnInstance(typeName);
            m_Systems.Add(system);
            system.OnInitialized();
        }

        protected override void OnUpdate(float delta)
        {
            DeltaTime = delta;
            int count = m_Systems.Count;

            for (int i = 0; i < count; ++i)
            {
                m_Systems[i].PreUpdate();
            }
            for (int i = 0; i < count; ++i)
            {
                m_Systems[i].Update();
            }
            for (int i = 0; i < count; ++i)
            {
                m_Systems[i].PostUpdate();
            }
        }
    }
}
