using System.Collections.Generic;
using UnityEngine;
using PureMVCFramework.Advantages;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace PureMVCFramework.Entity
{
    public interface IWorld
    {
        void InjectEntity(Entity entity);
    }

    public class World : SingletonBehaviour<World>, IWorld
    {
        public static List<IWorld> All = new List<IWorld>();

        public static World DefaultWorld { get; private set; }

        public static void InjectEntityToWorlds(Entity entity)
        {
            foreach (var world in All)
            {
                world.InjectEntity(entity);
            }
        }

        public static void RegisterWorld(IWorld world)
        {
            All.Add(world);
        }


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RuntimeOnDisableDomainReload()
        {
            applicationIsQuitting = false;
            All.Clear();
            DefaultWorld = null;
        }

#if ODIN_INSPECTOR
        [ShowInInspector, ShowIf("showOdinInfo"), ListDrawerSettings(IsReadOnly = true)]
#endif
        private readonly List<ISystemBase> m_Systems = new List<ISystemBase>();

        public float DeltaTime { get; private set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            DefaultWorld = this;
            RegisterWorld(this);
        }

        protected override void OnDelete()
        {
            Destroy();

            base.OnDelete();
        }

        public void Destroy()
        {
            for (int i = 0; i < m_Systems.Count; ++i)
            {
                m_Systems[i].OnRelease();
            }

            m_Systems.Clear();
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
