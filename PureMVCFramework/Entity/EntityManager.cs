using PureMVCFramework.Advantages;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace PureMVCFramework.Entity
{
    public partial class EntityManager
    {
        [DomainReload(1000000UL)]
        internal static ulong GUID_COUNT = 1000000UL;

        internal static event Action<ulong> OnEntityCreated;
        internal static event Action<ulong> OnEntityDestroyed;
        internal static event Action<ulong, IComponentData> OnEntityAddComponentData;
        internal static event Action<ulong, IComponentData> OnEntityRemoveComponentData;

        internal static readonly Dictionary<ulong, Entity> Entities = new Dictionary<ulong, Entity>();
        internal static GCHandle m_World;

        internal static EntityCommandBufferSystem BeginCommandBuffer, EndCommandBuffer;

        public static World World => (World)m_World.Target;

        internal static void Initialize(World world)
        {
            GameObjectEntities.Clear();
            Entities.Clear();

            if (m_World.IsAllocated)
                m_World.Free();

            m_World = GCHandle.Alloc(world);

            BeginCommandBuffer = world.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
            EndCommandBuffer = world.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }


        public static Dictionary<ulong, Entity> GetAllEntities()
        {
            return Entities;
        }

        public static bool TryGetEntity(ulong key, out Entity entity)
        {
            return Entities.TryGetValue(key, out entity);
        }

        public static bool TryGetEntities(EntityQuery query, out Dictionary<ulong, IComponentData[]> entities)
        {
            entities = new Dictionary<ulong, IComponentData[]>();
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

        public static void DestroyEntity(Entity entity, out GameObject gameObject)
        {
            EndCommandBuffer.CreateCommandBuffer().DestroyEntity(entity, out gameObject);
        }

        public static bool DestroyEntity(EntityData data)
        {
            if (TryGetEntity(data, out var entity) && entity.IsAlive)
            {
                EndCommandBuffer.CreateCommandBuffer().DestroyEntity(entity);
                return true;
            }

            return false;
        }

        public static bool DestroyEntity(EntityData data, out GameObject gameObject)
        {
            if (TryGetEntity(data, out var entity) && entity.IsAlive)
            {
                EndCommandBuffer.CreateCommandBuffer().DestroyEntity(entity, out gameObject);
                return true;
            }

            gameObject = null;
            return false;
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

            OnEntityCreated?.Invoke(data);

            if (archetype.TypesCount > 0)
            {
                foreach (var t in archetype.componentTypes)
                {
                    if (entity.InternalAddComponentData(t, (IComponentData)ReferencePool.SpawnInstance(TypeManager.GetType(t.TypeIndex))))
                    {
                        OnEntityAddComponentData?.Invoke(data, entity.m_AllComponentData[t.TypeIndex]);
                    }
                }
            }

            return entity;
        }

        internal static bool InternalDestroyEntity(EntityData data, out Entity entity, out GameObject gameObject)
        {
            gameObject = null;
            if (TryGetEntity(data, out entity))
            {
                if (entity.gameObject != null)
                {
                    gameObject = entity.gameObject;
                    GameObjectEntities.Remove(gameObject);
                    OnEntityGameObjectDeleted?.Invoke(gameObject);
                }
                Entities.Remove(data);
                ReferencePool.RecycleInstance(entity);

                OnEntityDestroyed?.Invoke(data);

                return true;
            }

            return false;
        }

        internal static bool InternalAddComponentData(EntityData data, IComponentData componentData, out Entity entity)
        {
            if (TryGetEntity(data, out entity) && entity.IsAlive)
            {
                if (entity.InternalAddComponentData(componentData.GetType(), componentData))
                {
                    OnEntityAddComponentData?.Invoke(data, componentData);
                    return true;
                }
            }

            return false;
        }

        internal static bool InternalRemoveComponentData(EntityData data, ComponentType type, out Entity entity, out IComponentData componentData)
        {
            if (TryGetEntity(data, out entity) && entity.IsAlive)
            {
                if (entity.InternalRemoveComponentData(type, out componentData))
                {
                    OnEntityRemoveComponentData?.Invoke(data, componentData);
                    return true;
                }
            }

            componentData = null;
            return false;
        }
    }

    public static class EntityExtensions
    {
        public static T GetOrAddComponentData<T>(this Entity entity) where T : IComponentData
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
