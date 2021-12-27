using PureMVCFramework.Advantages;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace PureMVCFramework.Entity
{
    public class EntityManager : SingletonBehaviour<EntityManager>
    {
        private static ulong GUID_COUNT = 1000000;

        public static Entity Create()
        {
            var entity = ReferencePool.Instance.SpawnInstance<Entity>();

            entity.GUID = GUID_COUNT++;

            entity.IsAlive = true;
            Instance.Entities.Add(entity.GUID, entity);

            return entity;
        }

        public static Entity Create(GameObject gameObject)
        {
            Assert.IsNotNull(gameObject);

            if (Instance.GameObjectEntities.TryGetValue(gameObject, out var entity))
            {
                return entity;
            }

            entity = Create();
            Instance.OnGameObjectLoaded(entity, gameObject);

            return entity;
        }


#if ODIN_INSPECTOR
        [ShowInInspector, ShowIf("showOdinInfo"), DictionaryDrawerSettings(IsReadOnly = true, DisplayMode = DictionaryDisplayOptions.Foldout)]
#endif
        internal readonly Dictionary<GameObject, Entity> GameObjectEntities = new Dictionary<GameObject, Entity>();
#if ODIN_INSPECTOR
        [ShowInInspector, ShowIf("showOdinInfo"), ListDrawerSettings(IsReadOnly = true)]
#endif
        internal readonly SortedDictionary<ulong, Entity> Entities = new SortedDictionary<ulong, Entity>();

        public bool IsDataMode { get; private set; }
 
        protected override void OnDelete()
        {
            GameObjectEntities.Clear();
            Entities.Clear();
            base.OnDelete();
        }

        public void EnableDataMode(bool tf)
        {
            IsDataMode = tf;
        }


        public SortedDictionary<ulong, Entity> GetAllEntities()
        {
            return Entities;
        }

        public SortedDictionary<long, IComponent> GetAllComponentDatas(Entity entity)
        {
            return entity.components;
        }

        public bool TryGetEntity(GameObject obj, out Entity entity)
        {
            return GameObjectEntities.TryGetValue(obj, out entity);
        }

        public bool TryGetEntity(ulong key, out Entity entity)
        {
            return Entities.TryGetValue(key, out entity);
        }

        public List<Entity> QueryEntities(Func<Entity, bool> query)
        {
            return Entities.Values.Where(query).ToList();
        }

        public List<T> QueryEntities<T>(Func<Entity, bool> query, Func<Entity, T> select)
        {
            return Entities.Values.Where(query).Select(select).ToList();
        }

        public List<GameObject> QueryEntityGameObjects(Func<Entity, bool> query)
        {
            return QueryEntities(query, (entity) => entity.gameObject);
        }

        internal void InternalAddComponentData(Entity entity, long typeHash, IComponent comp)
        {
            if (entity.components.ContainsKey(typeHash))
            {
                throw new Exception("添加了重复类型的组件，或者组件的hash重复：" + comp.GetType().FullName);
            }

            entity.components.Add(typeHash, comp);
        }

        internal bool InternalRemoveComponentData(Entity entity, long typeHash, out IComponent comp)
        {
            if (entity.components.TryGetValue(typeHash, out comp) && entity.components.Remove(typeHash))
            {
                return true;
            }

            return false;
        }

        public T AddComponentData<T>(Entity entity) where T : IComponent, new()
        {
            T comp = ReferencePool.Instance.SpawnInstance<T>();
            InternalAddComponentData(entity, Entity.StringToHash(typeof(T).FullName), comp);
            WorldManager.Instance.ModifyEntity(entity);

            return comp;
        }

        public IComponent AddComponentData(Entity entity, Type type)
        {
            IComponent comp = (IComponent)ReferencePool.Instance.SpawnInstance(type);
            InternalAddComponentData(entity, Entity.StringToHash(type.FullName), comp);
            WorldManager.Instance.ModifyEntity(entity);

            return comp;
        }

        public IComponent AddComponentData(Entity entity, string typeName)
        {
            IComponent comp = (IComponent)ReferencePool.Instance.SpawnInstance(typeName);
            InternalAddComponentData(entity, Entity.StringToHash(typeName), comp);
            WorldManager.Instance.ModifyEntity(entity);

            return comp;
        }

        public void AddComponentDataByArchetype(Entity entity, EntityArchetype archetype, out IComponent[] components)
        {
            components = new IComponent[archetype.typeNames.Length];
            for (int i = 0; i < archetype.typeNames.Length; ++i)
            {
                IComponent comp = (IComponent)ReferencePool.Instance.SpawnInstance(archetype.typeNames[i]);
                InternalAddComponentData(entity, archetype.hash[i], comp);
                components[i] = comp;
            }

            WorldManager.Instance.ModifyEntity(entity);
        }

        public T GetComponentData<T>(Entity entity) where T : IComponent
        {
            return (T)GetComponentData(entity, Entity.StringToHash(typeof(T).FullName));
        }

        public IComponent GetComponentData(Entity entity, Type type)
        {
            return GetComponentData(entity, Entity.StringToHash(type.FullName));
        }

        public IComponent GetComponentData(Entity entity, string typeName)
        {
            return GetComponentData(entity, Entity.StringToHash(typeName));
        }

        public IComponent GetComponentData(Entity entity, long typeHash)
        {
            if (entity.components.TryGetValue(typeHash, out var c))
                return c;

            return null;
        }

        public void RemoveComponentData<T>(Entity entity) where T : IComponent
        {
            RemoveComponentData(entity, Entity.StringToHash(typeof(T).FullName));
        }

        public void RemoveComponentData(Entity entity, Type type)
        {
            RemoveComponentData(entity, Entity.StringToHash(type.FullName));
        }

        public void RemoveComponentData(Entity entity, string typeName)
        {
            RemoveComponentData(entity, Entity.StringToHash(typeName));
        }

        public void RemoveComponentData(Entity entity, long typeHash)
        {
            if (InternalRemoveComponentData(entity, typeHash, out var comp))
                ReferencePool.Instance.RecycleInstance(comp);

            WorldManager.Instance.ModifyEntity(entity);
        }

        public void DestroyEntity(ulong key)
        {
            if (Entities.TryGetValue(key, out Entity e))
            {
                DestroyEntity(e);
            }
        }

        public void DestroyAll()
        {
            var list = Entities.Values.ToArray();
            foreach (var entity in list)
            {
                DestroyEntity(entity);
            }
        }

        public void DestroyEntity(Entity entity, float delay = 0)
        {
            if (entity.IsAlive)
            {
                entity.IsAlive = false;

                while (entity.components.Count > 0)
                {
                    var e = entity.components.GetEnumerator();
                    if (e.MoveNext())
                    {
                        InternalRemoveComponentData(entity, e.Current.Key, out var comp);
                        ReferencePool.Instance.RecycleInstance(comp);
                    }
                }

                WorldManager.Instance.ModifyEntity(entity);

                if (entity.gameObject != null)
                {
                    GameObjectEntities.Remove(entity.gameObject);

                    if (delay <= 0)
                    {
                        entity.gameObject.Recycle();
                    }
                    else
                    {
                        TimerManager.Instance.AddOneShotTask(delay, entity.gameObject.Recycle);
                    }
                }

                Entities.Remove(entity.GUID);

                entity.GUID = 0;
                entity.gameObject = null;

                if (!ReferencePool.applicationIsQuitting)
                    ReferencePool.Instance.RecycleInstance(entity);
            }
        }

        public void LoadGameObject(Entity entity, string assetPath, Transform parent = null, Action<Entity, object> callback = null, object userdata = null)
        {
            if (IsDataMode)
                return;

            AutoReleaseManager.Instance.LoadGameObjectAsync(assetPath, parent, (go, data) =>
            {
                if (entity.IsAlive)
                {
                    if (go != null)
                    {
                        OnGameObjectLoaded(entity, go);
                        callback?.Invoke(entity, data);
                    }
                }
                else
                {
                    go.Recycle();
                }
            }, userdata);
        }

        public void LoadGameObject(Entity entity, string assetPath, Vector3 position, Quaternion rotation, Action<Entity, object> callback = null, object userdata = null)
        {
            if (IsDataMode)
                return;

            AutoReleaseManager.Instance.LoadGameObjectAsync(assetPath, position, rotation, (go, data) =>
            {
                if (entity.IsAlive)
                {
                    if (go != null)
                    {
                        OnGameObjectLoaded(entity, go);
                        callback?.Invoke(entity, data);
                    }
                }
                else
                {
                    go.Recycle();
                }
            }, userdata);
        } 

        private void OnGameObjectLoaded(Entity entity, GameObject go)
        {
            if (go != null)
            {
                entity.gameObject = go;
#if UNITY_EDITOR
                entity.name = go.name = go.name.Replace("(Spawn)", entity.GUID.ToString());
#endif

                Instance.GameObjectEntities[entity.gameObject] = entity;
                WorldManager.Instance.ModifyEntity(entity);
            }
        }

        public T AddComponentObject<T>(Entity entity) where T : Component
        {
            if (entity.gameObject != null)
            {
                var comp = entity.gameObject.AddComponent<T>();
                WorldManager.Instance.ModifyEntity(entity);

                return comp;
            }

            return null;
        }

        public T GetComponentObject<T>(Entity entity) where T : Component
        {
            if (entity.gameObject != null)
                return entity.gameObject.GetComponent<T>();

            return null;
        }

        public void RemoveComponentObject<T>(Entity entity) where T : Component
        {
            var comp = GetComponentObject<T>(entity);
            if (comp != null)
            {
                UnityEngine.Object.Destroy(comp);
                WorldManager.Instance.ModifyEntity(entity);
            }
        }
    }

    public static class EntityExtensions
    {
        public static T GetOrAddComponentData<T>(this Entity entity) where T : IComponent, new()
        {
            var comp = EntityManager.Instance.GetComponentData<T>(entity);
            if (comp == null)
                comp = EntityManager.Instance.AddComponentData<T>(entity);

            return comp;
        }

        public static T GetOrAddObjectComponent<T>(this Entity entity) where T : Component
        {
            var comp = EntityManager.Instance.GetComponentObject<T>(entity);
            if (comp == null)
                comp = EntityManager.Instance.AddComponentObject<T>(entity);

            return comp;
        }
    }
}
