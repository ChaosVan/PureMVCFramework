using PureMVCFramework.Advantages;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Runtime.InteropServices;
using PureMVCFramework.Extensions;
using static UnityEngine.EventSystems.EventTrigger;
using static PureMVCFramework.Entity.ComponentType;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace PureMVCFramework.Entity
{
    public struct EntityData
    {
        public ulong index;

        public static implicit operator EntityData(ulong index)
        {
            return new EntityData { index = index};
        }

        public static implicit operator EntityData(uint index)
        {
            return new EntityData { index = index };
        }
    }

    public partial class EntityManager
    {
        [DomainReload(1000000UL)]
        internal static ulong GUID_COUNT = 1000000UL;

        internal static event Action<Entity> OnEntityCreated;
        internal static event Action<ulong> OnEntityDestroyed;

        internal static readonly SortedDictionary<ulong, Entity> Entities = new SortedDictionary<ulong, Entity>();

        internal static GCHandle m_World;

        public static World World => (World) m_World.Target;

        internal static void Initialize(World world)
        {
            GameObjectEntities.Clear();
            Entities.Clear();

            m_World = GCHandle.Alloc(world);
        }


        public static SortedDictionary<ulong, Entity> GetAllEntities()
        {
            return Entities;
        }

        public static bool TryGetEntity(ulong key, out Entity entity)
        {
            return Entities.TryGetValue(key, out entity);
        }

        internal static Entity[] BeginStructual(int count, params Entity[] entities)
        {
            var array = new Entity[count];
            if (entities != null && entities.Length >= count)
                Array.Copy(entities, array, count);

            return array;
        }

        internal static void EndStructual(IEnumerable<Entity> entities)
        {
            var e = World.Systems.GetEnumerator();
            while (e.MoveNext())
            {
                if (e.Current is SystemBase system)
                {
                    foreach (var entity in entities)
                    {
                        system.InjectEntity(entity);
                    }
                }
            }
        }

        public static Entity Create()
        {
            EntityArchetype archetype = default;
            return Create(archetype);
        }

        public static Entity Create(params ComponentType[] componentTypes)
        {
            EntityArchetype archetype = new EntityArchetype(componentTypes);
            return Create(archetype);
        }

        public static Entity Create(EntityArchetype archetype)
        {
            Create(archetype, 1, out var entities);
            return entities[0];
        }

        public static void Create(EntityArchetype archetype, int count, out Entity[] entities)
        {
            entities = BeginStructual(count);
            for (int i = 0; i < count; i++)
            {
                var entity = InternalCreate(GUID_COUNT++, archetype);
                entities[i] = entity;
            }
            EndStructual(entities);
        }

        public static void DestroyAll()
        {
            var list = BeginStructual(Entities.Count, Entities.Values.ToArray());
            foreach (var entity in list)
            {
                if (InternalDestroyEntity(entity.GUID, out _, out var gameObject))
                {
                    if (gameObject != null)
                        gameObject.Recycle();
                }
            }
            EndStructual(list);
        }

        public static void DestroyEntity(Entity entity)
        {
            DestroyEntity(entity, out var gameObject);
            if (gameObject != null)
                gameObject.Recycle();
        }

        public static void DestroyEntity(Entity entity, out GameObject gameObject)
        {
            var entities = BeginStructual(1, entity);
            if (InternalDestroyEntity(entity.GUID, out _, out gameObject))
                EndStructual(entities);

        }

        public static void AddComponentData(Entity entity, params ComponentType[] components)
        {
            if (entity.IsAlive)
            {
                var entities = BeginStructual(1, entity);
                for (int i = 0; i < components.Length; i++)
                {
                    InternalAddComponentData(entity.GUID, (IComponentData)ReferencePool.SpawnInstance(TypeManager.GetType(components[i].TypeIndex)), out _);
                }
                EndStructual(entities);
            }
        }

        public static T AddComponentData<T>(Entity entity) where T : IComponentData
        {
            return (T)AddComponentData(entity, ReferencePool.SpawnInstance<T>());
        }

        public static IComponentData AddComponentData(Entity entity, IComponentData componentData)
        {
            if (entity.IsAlive)
            {
                var entities = BeginStructual(1, entity);
                if (InternalAddComponentData(entity.GUID, componentData, out _))
                {
                    EndStructual(entities);
                    return componentData;
                }
            }

            return null;
        }

        public static void RemoveComponentData<T>(Entity entity) where T : IComponentData
        {
            RemoveComponentData(entity, typeof(T));
        }

        public static void RemoveComponentData(Entity entity, ComponentType componentType)
        {
            var entities = BeginStructual(1, entity);
            if (InternalRemoveComponentData(entity.GUID, componentType, out _, out var componentData))
            {
                ReferencePool.RecycleInstance(componentData);
                EndStructual(entities);
            }
        }

        public static T GetComponentData<T>(Entity entity) where T : IComponentData
        {
            return (T)GetComponentData(entity, typeof(T));
        }

        public static IComponentData GetComponentData(Entity entity, ComponentType componentType)
        {
            if (entity.IsAlive && entity.InternalGetComponentData(componentType, out var componentData))
                return componentData;

            return null;
        }

        public static bool GetComponentData(Entity entity, EntityQuery query, out IComponentData[] componentData)
        {
            if (entity.IsAlive && entity.InternalGetComponentData(query, out componentData))
                return true;

            componentData = null;
            return false;
        }

        internal static Entity InternalCreate(EntityData data, EntityArchetype archetype)
        {
            var entity = ReferencePool.SpawnInstance<Entity>();
            entity.GUID = data.index;
            entity.IsAlive = true;
            Entities.Add(entity.GUID, entity);

            OnEntityCreated?.Invoke(entity);

            if (archetype.TypesCount > 0)
            {
                foreach (var t in archetype.componentTypes)
                {
                    entity.InternalAddComponentData(t, (IComponentData)ReferencePool.SpawnInstance(TypeManager.GetType(t.TypeIndex)));
                }
            }

            return entity;
        }

        internal static bool InternalDestroyEntity(EntityData data, out Entity entity, out GameObject gameObject)
        {
            gameObject = null;
            if (Entities.TryGetValue(data.index, out entity))
            {
                OnEntityDestroyed?.Invoke(data.index);

                if (entity.gameObject != null)
                {
                    GameObjectEntities.Remove(entity.gameObject);
                    gameObject = entity.gameObject;

                    OnEntityGameObjectDeleted?.Invoke(gameObject);
                }
                Entities.Remove(entity.GUID);
                ReferencePool.RecycleInstance(entity);

                return true;
            }

            return false;
        }

        internal static bool InternalAddComponentData(EntityData data, IComponentData componentData, out Entity entity)
        {
            if (Entities.TryGetValue(data.index, out entity))
            {
                return entity.InternalAddComponentData(componentData.GetType(), componentData);
            }

            return false;
        }

        internal static bool InternalRemoveComponentData(EntityData data, ComponentType type, out Entity entity, out IComponentData componentData)
        {
            if (Entities.TryGetValue(data.index, out entity))
            {
                return entity.InternalRemoveComponentData(type, out componentData);
            }

            componentData = null;
            return false;
        }
    }

    public static class EntityExtensions
    {
        public static T GetOrAddComponentData<T>(this Entity entity) where T : IComponentData, new()
        {
            var comp = EntityManager.GetComponentData<T>(entity);
            if (comp == null)
                return EntityManager.AddComponentData<T>(entity);

            return comp;
        }

        public static T GetOrAddObjectComponent<T>(this Entity entity) where T : Component
        {
            var comp = EntityManager.GetComponentObject<T>(entity);
            if (comp == null)
                comp = EntityManager.AddComponentObject<T>(entity);

            return comp;
        }
    }
}
