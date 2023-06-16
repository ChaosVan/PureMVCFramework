using Newtonsoft.Json;
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
        private readonly List<World> worlds = new List<World>();
#if ODIN_INSPECTOR
        [ShowInInspector, ShowIf("showOdinInfo"), DictionaryDrawerSettings(IsReadOnly = true)]
#endif
        private readonly Dictionary<World, List<ComponentSystemGroup>> groups = new Dictionary<World, List<ComponentSystemGroup>>();
#if ODIN_INSPECTOR
        [ShowInInspector, ShowIf("showOdinInfo"), DictionaryDrawerSettings(IsReadOnly = true, DisplayMode = DictionaryDisplayOptions.OneLine)]
#endif
        private readonly Dictionary<ulong, List<IComponentData>> Entities = new Dictionary<ulong, List<IComponentData>>();
#if ODIN_INSPECTOR
        [ShowInInspector, ShowIf("showOdinInfo"), DictionaryDrawerSettings(IsReadOnly = true, DisplayMode = DictionaryDisplayOptions.OneLine)]
#endif
        private readonly Dictionary<GameObject, Entity> GameObjectEntities = new Dictionary<GameObject, Entity>();

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
            EntityManager.OnEntityAddComponentData += OnEntityAddComponentData;
            EntityManager.OnEntityRemoveComponentData += OnEntityRemoveComponentData;
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
            EntityManager.OnEntityAddComponentData -= OnEntityAddComponentData;
            EntityManager.OnEntityRemoveComponentData -= OnEntityRemoveComponentData;
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

        void OnEntityCreated(ulong entity)
        {
            Entities.Add(entity, new List<IComponentData>());
        }

        void OnEntityDetroyed(ulong entity)
        {
            Entities.Remove(entity);
        }

        private void OnEntityAddComponentData(ulong entity, IComponentData component)
        {
            if (Entities.TryGetValue(entity, out var list))
            {
                list.Add(component);
                list.Sort(ComponentDataSorter);
            }
        }

        private void OnEntityRemoveComponentData(ulong entity, IComponentData component)
        {
            if (Entities.TryGetValue(entity, out var list))
            {
                list.Remove(component);
                list.Sort(ComponentDataSorter);
            }
        }

        private static int ComponentDataSorter(IComponentData a, IComponentData b)
        {
            return TypeManager.GetTypeIndex(a.GetType()) - TypeManager.GetTypeIndex(b.GetType());
        }

        void OnEntityGameObjectLoaded(GameObject go, Entity entity)
        {
            GameObjectEntities.Add(go, entity);
        }

        void OnEntityGameObjectDeleted(GameObject gameObject)
        {
            GameObjectEntities.Remove(gameObject);
        }

        private readonly Dictionary<object, Dictionary<string, IComponentData>> snapshot = new Dictionary<object, Dictionary<string, IComponentData>>();

        public string TakeSnapshot(JsonSerializerSettings settings, EntitySnapshotKey keyType = EntitySnapshotKey.EntityGUID)
        {
            snapshot.Clear();
            var e = Entities.GetEnumerator();
            while (e.MoveNext())
            {
                var current = e.Current;
                if (keyType == EntitySnapshotKey.EntityGUID)
                {
                    snapshot.Add(current.Key, new Dictionary<string, IComponentData>());
                    foreach (var c in current.Value)
                    {
                        snapshot[current.Key].Add(c.GetType().FullName, c);
                    }
                }
                else if (keyType == EntitySnapshotKey.EntityName)
                {
                    string name = current.Key.ToString();
                    if (EntityManager.TryGetEntity(current.Key, out var entity) && entity.gameObject != null)
                    {
                        name = entity.gameObject.name;
                    }

                    snapshot.Add(name, new Dictionary<string, IComponentData>());
                    foreach (var c in current.Value)
                    {
                        snapshot[name].Add(c.GetType().FullName, c);
                    }
                }
            }

            return JsonConvert.SerializeObject(snapshot, settings);
        }
    }

    public enum EntitySnapshotKey
    {
        EntityGUID,
        EntityName,
    }
}
