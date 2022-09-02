using PureMVCFramework.Advantages;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace PureMVCFramework.Entity
{
    public struct EntityData
    {
        public ulong index;

        public static implicit operator EntityData(ulong index)
        {
            return new EntityData { index = index };
        }

        public static implicit operator EntityData(uint index)
        {
            return new EntityData { index = index };
        }

        public static implicit operator ulong(EntityData entity)
        {
            return entity.index;
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

        internal static EntityCommandBufferSystem BeginCommandBuffer, EndCommandBuffer;

        public static World World => (World)m_World.Target;

        internal static void Initialize(World world)
        {
            GameObjectEntities.Clear();
            Entities.Clear();

            m_World = GCHandle.Alloc(world);

            BeginCommandBuffer = world.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
            EndCommandBuffer = world.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }


        public static SortedDictionary<ulong, Entity> GetAllEntities()
        {
            return Entities;
        }

        public static bool TryGetEntity(ulong key, out Entity entity)
        {
            return Entities.TryGetValue(key, out entity);
        }

        public static bool TryGetEntities(EntityQuery query, out Dictionary<ulong, IComponentData[]> entities)
        {
            entities = new();
            foreach (var entity in Entities.Keys)
            {
                if (GetComponentData(entity, query, out var components))
                {
                    entities.Add(entity, components);
                }
            }

            return entities.Count > 0;
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

        public static void DestroyAll()
        {
            var commandBuffer = EndCommandBuffer.CreateCommandBuffer();
            foreach (var entity in Entities.Values)
            {
                commandBuffer.DestroyEntity(entity);
            }
        }

        public static void DestroyEntity(Entity entity)
        {
            EndCommandBuffer.CreateCommandBuffer().DestroyEntity(entity);
        }

        public static void DestroyEntity(EntityData data)
        {
            if (TryGetEntity(data, out var entity))
                EndCommandBuffer.CreateCommandBuffer().DestroyEntity(entity);
        }

        public static EntityData Create()
        {
            EntityArchetype archetype = default;
            return Create(archetype);
        }

        public static EntityData Create(params ComponentType[] componentTypes)
        {
            EntityArchetype archetype = new EntityArchetype(componentTypes);
            return Create(archetype);
        }

        public static EntityData Create(EntityArchetype archetype)
        {
            Create(archetype, 1, out var entities);
            return entities[0];
        }

        public static void Create(EntityArchetype archetype, int count, out EntityData[] entities)
        {
            var commandBuffer = BeginCommandBuffer.CreateCommandBuffer();
            entities = new EntityData[count];
            for (int i = 0; i < count; i++)
            {
                entities[i] = commandBuffer.CreateEntity(archetype);
            }
        }

        public static T AddComponentData<T>(EntityData entity) where T : IComponentData
        {
            return BeginCommandBuffer.CreateCommandBuffer().AddComponentData<T>(entity);
        }

        public static T AddComponentData<T>(Entity entity) where T : IComponentData
        {
            return BeginCommandBuffer.CreateCommandBuffer().AddComponentData<T>(entity);
        }

        public static void RemoveComponentData<T>(EntityData entity) where T : IComponentData
        {
            EndCommandBuffer.CreateCommandBuffer().RemoveComponentData<T>(entity);
        }

        public static void RemoveComponentData<T>(Entity entity) where T : IComponentData
        {
            EndCommandBuffer.CreateCommandBuffer().RemoveComponentData<T>(entity);
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

        public static bool GetComponentData(EntityData entity, EntityQuery query, out IComponentData[] componentData)
        {
            componentData = null;
            return TryGetEntity(entity, out var e) && GetComponentData(e, query, out componentData);
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
            entity.GUID = data;
            entity.IsAlive = true;
            Entities.Add(data, entity);

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
            if (TryGetEntity(data, out entity))
            {
                OnEntityDestroyed?.Invoke(data);

                if (entity.gameObject != null)
                {
                    gameObject = entity.gameObject;
                    GameObjectEntities.Remove(gameObject);
                    OnEntityGameObjectDeleted?.Invoke(gameObject);
                }
                Entities.Remove(data);
                ReferencePool.RecycleInstance(entity);

                return true;
            }

            return false;
        }

        internal static bool InternalAddComponentData(EntityData data, IComponentData componentData, out Entity entity)
        {
            if (TryGetEntity(data, out entity))
            {
                return entity.InternalAddComponentData(componentData.GetType(), componentData);
            }

            return false;
        }

        internal static bool InternalRemoveComponentData(EntityData data, ComponentType type, out Entity entity, out IComponentData componentData)
        {
            if (TryGetEntity(data, out entity))
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
