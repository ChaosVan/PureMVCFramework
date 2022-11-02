using PureMVCFramework.Advantages;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace PureMVCFramework.Entity
{
    public static partial class EntityManager
    {
        [DomainReload(1000000UL)]
        internal static ulong GUID_COUNT = 1000000UL;

        internal static event Action<ulong> OnEntityCreated;
        internal static event Action<ulong> OnEntityDestroyed;
        internal static event Action<ulong, IComponentData> OnEntityAddComponentData;
        internal static event Action<ulong, IComponentData> OnEntityRemoveComponentData;

        internal static readonly Dictionary<ulong, Entity> Entities = new Dictionary<ulong, Entity>();
        internal static GCHandle m_World;

        internal static EntityCommandBufferSystem ecbs_begin, ecbs_end;

        public static World World => (World)m_World.Target;

        internal static void Initialize(World world)
        {
            GameObjectEntities.Clear();
            Entities.Clear();

            if (m_World.IsAllocated)
                m_World.Free();

            m_World = GCHandle.Alloc(world);

            ecbs_begin = world.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
            ecbs_end = world.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
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
            foreach (var entity in Entities.Values)
            {
                if (GetComponentData(entity, query, out var components))
                {
                    entities.Add(entity.GUID, components);
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
            var commandBuffer = ecbs_end.CreateCommandBuffer();
            foreach (var entity in Entities.Values)
            {
                commandBuffer.DestroyEntity(entity);
            }
        }

        public static void DestroyEntities(IEnumerable<EntityData> entities)
        {
            var commandBuffer = ecbs_end.CreateCommandBuffer();
            foreach (var entity in entities)
            {
                commandBuffer.DestroyEntity(entity);
            }
        }

        public static void DestroyEntities(EntityQuery query)
        {
            if (TryGetEntities(query, out var entities))
            {
                var commandBuffer = ecbs_end.CreateCommandBuffer();
                foreach (var entity in entities.Keys)
                {
                    commandBuffer.DestroyEntity(entity);
                }
            }
        }

        public static void DestroyEntities(EntityQuery query, out GameObject[] gameObjects)
        {
            gameObjects = null;
            if (TryGetEntities(query, out var entities))
            {
                var commandBuffer = ecbs_end.CreateCommandBuffer();
                gameObjects = new GameObject[entities.Count];
                int index = 0;
                foreach (var entity in entities.Keys)
                {
                    commandBuffer.DestroyEntity(entity, out gameObjects[index++]);
                }
            }
        }

        public static void DestroyEntity(EntityData data)
        {
            if (TryGetEntity(data, out var entity))
                DestroyEntity(entity);
        }

        public static void DestroyEntity(EntityData data, out GameObject gameObject)
        {
            gameObject = null;
            if (TryGetEntity(data, out var entity))
                DestroyEntity(entity, out gameObject);
        }

        public static void DestroyEntity(Entity entity)
        {
            DestroyEntity(entity, out var gameObject);
            if (gameObject != null)
                gameObject.Recycle();
        }

        public static void DestroyEntity(Entity entity, out GameObject gameObject)
        {
            gameObject = null;
            if (entity.IsAlive)
            {
                ecbs_end.CreateCommandBuffer().DestroyEntity(entity, out gameObject);
            }
        }

        public static EntityData Create(out EntityCommandBuffer commandBuffer)
        {
            EntityArchetype archetype = default;
            return Create(archetype, out commandBuffer);
        }

        public static EntityData Create(out EntityCommandBuffer commandBuffer, params ComponentType[] componentTypes)
        {
            EntityArchetype archetype = new EntityArchetype(componentTypes);
            return Create(archetype, out commandBuffer);
        }

        public static EntityData Create(EntityArchetype archetype, out EntityCommandBuffer commandBuffer)
        {
            Create(archetype, 1, out var entities, out commandBuffer);
            return entities[0];
        }

        public static void Create(EntityArchetype archetype, int count, out EntityData[] entities, out EntityCommandBuffer commandBuffer)
        {
            commandBuffer = ecbs_begin.CreateCommandBuffer();
            entities = new EntityData[count];
            for (int i = 0; i < count; i++)
            {
                entities[i] = commandBuffer.CreateEntity(archetype);
            }
        }

        public static T AddComponentData<T>(EntityData entity) where T : IComponentData
        {
            return ecbs_begin.CreateCommandBuffer().AddComponentData<T>(entity);
        }

        public static T AddComponentData<T>(Entity entity) where T : IComponentData
        {
            return ecbs_begin.CreateCommandBuffer().AddComponentData<T>(entity);
        }

        public static void AddComponentData(EntityData entity, EntityArchetype archetype)
        {
            ecbs_begin.CreateCommandBuffer().AddComponentData(entity, archetype);
        }

        public static void AddComponentData(Entity entity, EntityArchetype archetype)
        {
            ecbs_begin.CreateCommandBuffer().AddComponentData(entity, archetype);
        }

        public static void RemoveComponentData<T>(EntityData entity) where T : IComponentData
        {
            ecbs_end.CreateCommandBuffer().RemoveComponentData<T>(entity);
        }

        public static void RemoveComponentData<T>(Entity entity) where T : IComponentData
        {
            ecbs_end.CreateCommandBuffer().RemoveComponentData<T>(entity);
        }

        public static void RemoveComponentData(EntityData entity, EntityQuery query)
        {
            ecbs_end.CreateCommandBuffer().RemoveComponentData(entity, query);
        }

        public static void RemoveComponentData(Entity entity, EntityQuery query)
        {
            ecbs_end.CreateCommandBuffer().RemoveComponentData(entity, query);
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

        internal static bool InternalDestroyEntity(EntityData data, out Entity entity)
        {
            if (TryGetEntity(data, out entity))
            {
                OnEntityDestroyed?.Invoke(data);
                Entities.Remove(data);
                ReferencePool.RecycleInstance(entity);

                return true;
            }

            return false;
        }

        internal static void InternalAddComponentData(EntityData data, IComponentData componentData, out Entity entity)
        {
            if (TryGetEntity(data, out entity) && entity.IsAlive)
            {
                if (entity.InternalAddComponentData(componentData.GetType(), componentData))
                {
                    OnEntityAddComponentData?.Invoke(data, componentData);
                }
            }
        }

        internal static void InternalAddComponentData(EntityData data, EntityArchetype archetype, out Entity entity)
        {
            if (TryGetEntity(data, out entity) && entity.IsAlive && archetype.TypesCount > 0)
            {
                foreach (var t in archetype.componentTypes)
                {
                    if (entity.InternalAddComponentData(t, (IComponentData)ReferencePool.SpawnInstance(TypeManager.GetType(t.TypeIndex))))
                    {
                        OnEntityAddComponentData?.Invoke(data, entity.m_AllComponentData[t.TypeIndex]);
                    }
                }
            }
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

        internal static bool InternalRemoveComponentData(EntityData data, EntityQuery query, out Entity entity, out IComponentData[] componentDatas)
        {
            if (TryGetEntity(data, out entity) && entity.IsAlive && query.TypesCount > 0)
            {
                componentDatas = new IComponentData[query.TypesCount];
                for (int i = 0; i < query.TypesCount; i++)
                {
                    if (entity.InternalRemoveComponentData(query.types[i], out componentDatas[i]))
                    {
                        OnEntityRemoveComponentData?.Invoke(data, componentDatas[i]);
                    }
                }
            }

            componentDatas = null;
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

        public static T GetOrAddComponentData<T>(this Entity entity, EntityCommandBuffer commandBuffer) where T : IComponentData
        {
            var comp = EntityManager.GetComponentData<T>(entity);
            if (comp == null)
                return commandBuffer.AddComponentData<T>(entity);

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
