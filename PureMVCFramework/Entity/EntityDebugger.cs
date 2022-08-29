#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using System.Collections.Generic;
using UnityEngine;

namespace PureMVCFramework.Entity
{
    public class EntityDebugger : SingletonBehaviour<EntityDebugger>
    {
#if ODIN_INSPECTOR
        [ShowInInspector, ShowIf("showOdinInfo"), ListDrawerSettings(IsReadOnly = true)]
#endif
        private readonly List<World> worlds = new();
#if ODIN_INSPECTOR
        [ShowInInspector, ShowIf("showOdinInfo"), DictionaryDrawerSettings(IsReadOnly = true)]
#endif
        private readonly Dictionary<World, List<ComponentSystemGroup>> groups = new();
#if ODIN_INSPECTOR
        [ShowInInspector, ShowIf("showOdinInfo"), DictionaryDrawerSettings(IsReadOnly = true, DisplayMode = DictionaryDisplayOptions.OneLine)]
#endif
        private readonly Dictionary<ulong, Entity> Entities = new();
#if ODIN_INSPECTOR
        [ShowInInspector, ShowIf("showOdinInfo"), DictionaryDrawerSettings(IsReadOnly = true, DisplayMode = DictionaryDisplayOptions.OneLine)]
#endif
        internal static readonly Dictionary<GameObject, Entity> GameObjectEntities = new();

        private DefaultWorldInitialization.DefaultRootGroups rootGroup;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            World.WorldCreated += OnWorldCreated;
            World.WorldDestroyed += OnWorldDestroyed;
            World.SystemCreated += OnSystemCreated;
            World.SystemDestroyed += OnSystemDestroyed;

            EntityManager.OnEntityCreated += OnEntityCreated;
            EntityManager.OnEntityDestroyed += OnEntityDetroyed;
            EntityManager.OnEntityGameObjectLoaded += OnEntityGameObjectLoaded;
            EntityManager.OnEntityGameObjectDeleted += OnEntityGameObjectDeleted;

            rootGroup = new DefaultWorldInitialization.DefaultRootGroups();
        }

        protected override void OnDelete()
        {
            World.WorldCreated -= OnWorldCreated;
            World.WorldDestroyed -= OnWorldDestroyed;
            World.SystemCreated -= OnSystemCreated;
            World.SystemDestroyed -= OnSystemDestroyed;

            EntityManager.OnEntityCreated -= OnEntityCreated;
            EntityManager.OnEntityDestroyed -= OnEntityDetroyed;
            EntityManager.OnEntityGameObjectLoaded -= OnEntityGameObjectLoaded;
            EntityManager.OnEntityGameObjectDeleted -= OnEntityGameObjectDeleted;

            base.OnDelete();
        }

        void OnWorldCreated(World world)
        {
            worlds.Add(world);
        }

        void OnWorldDestroyed(World world)
        {
            worlds.Remove(world);
        }

        void OnSystemCreated(World world, ComponentSystemBase system)
        {
            if (rootGroup.IsRootGroup(system.GetType()))
            {
                if (!groups.TryGetValue(world, out var group))
                {
                    group = new List<ComponentSystemGroup>();
                    groups.Add(world, group);
                }
                group.Add(system as ComponentSystemGroup);
            }
        }

        void OnSystemDestroyed(World world, ComponentSystemBase system)
        {
            if (rootGroup.IsRootGroup(system.GetType()))
            {
                if (groups.TryGetValue(world, out var group))
                    group.Remove(system as ComponentSystemGroup);
            }
        }

        void OnEntityCreated(Entity entity)
        {
            Entities.Add(entity.GUID, entity);
        }

        void OnEntityDetroyed(ulong index)
        {
            Entities.Remove(index);
        }

        void OnEntityGameObjectLoaded(GameObject go, Entity entity)
        {
            GameObjectEntities.Add(go, entity);
        }

        void OnEntityGameObjectDeleted(GameObject gameObject)
        {
            GameObjectEntities.Remove(gameObject);
        }
    }
}
