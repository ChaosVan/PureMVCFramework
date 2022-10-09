using PureMVCFramework.Advantages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.AccessControl;
using Unity.Mathematics;
using UnityEngine;

namespace PureMVCFramework.Entity
{
    public class EntityCommandBuffer : IDisposable
    {
        internal struct EntityCommandBufferData
        {
            internal ECBCommand commandType;
            internal EntityArchetype archetype;
            internal EntityData entity;
            internal bool destroyImmediately;
            internal ComponentType componentType;
            internal IComponentData componentData;
            internal EntityQuery query;

            internal float3 position;
            internal quaternion rotation;
            internal Transform parent;

            internal string asset;
            internal Action<Entity, object> callback;
            internal object userdata;
        }

        internal int SystemID;
        internal SystemHandleUntyped OriginSystemHandle;

        internal readonly List<EntityCommandBufferData> m_Data = new List<EntityCommandBufferData>();

        public bool IsCreated => m_Data != null;

        public void Dispose()
        {
            m_Data.Clear();
        }

        public EntityData CreateEntity()
        {
            EntityArchetype archetype = new EntityArchetype();
            return CreateEntity(archetype);
        }

        public EntityData CreateEntity(EntityArchetype archetype)
        {
            EntityData entity = new EntityData { index = EntityManager.GUID_COUNT++ };
            AddEntityArchetypeCommand(ECBCommand.CreateEntity, entity, archetype);
            return entity;
        }

        public void DestroyEntity(EntityData entity, bool destroy = true)
        {
            AddEntityDestroyCommand(ECBCommand.DestroyEntity, entity, destroy);
        }

        public void DestroyEntity(Entity entity, bool destroy = true)
        {
            DestroyEntity(entity.GUID, destroy);
        }

        public T AddComponentData<T>(EntityData entity) where T : IComponentData
        {
            var componentData = ReferencePool.SpawnInstance<T>();
            AddEntityComponentDataCommand(ECBCommand.AddComponent, entity, componentData);
            return componentData;
        }

        public T AddComponentData<T>(Entity entity) where T : IComponentData
        {
            return AddComponentData<T>(entity.GUID);
        }

        public void AddComponentData(EntityData entity, EntityArchetype archetype)
        {
            AddEntityArchetypeCommand(ECBCommand.AddComponentWithEntityArchetype, entity, archetype);
        }

        public void AddComponentData(Entity entity, EntityArchetype archetype)
        {
            AddComponentData(entity.GUID, archetype);
        }

        public void RemoveComponentData<T>(EntityData entity) where T : IComponentData
        {
            AddEntityComponentTypeCommand(ECBCommand.RemoveComponent, entity, typeof(T));
        }

        public void RemoveComponentData<T>(Entity entity) where T : IComponentData
        {
            RemoveComponentData<T>(entity.GUID);
        }

        public void RemoveComponentData(EntityData entity, EntityQuery query)
        {
            AddEntityQueryCommand(ECBCommand.RemoveComponentWithEntityQuery, entity, query);
        }

        public void RemoveComponentData(Entity entity, EntityQuery query)
        {
            RemoveComponentData(entity.GUID, query);
        }

        public void LoadGameObject(EntityData entity, string asset, Transform parent = null, Action<Entity, object> callback = null, object userdata = null)
        {
            AddLoadGameObjectCommand(ECBCommand.LoadGameObjectWithParent, entity, asset, parent, float3.zero, quaternion.identity, callback, userdata);
        }

        public void LoadGameObject(EntityData entity, string asset, Vector3 position, Quaternion rotation, Action<Entity, object> callback = null, object userdata = null)
        {
            AddLoadGameObjectCommand(ECBCommand.LoadGameObjectWithCoordinates, entity, asset, null, position, rotation, callback, userdata);
        }

        public void UpdateGameObject(EntityData entity)
        {
            UpdateGameObjectCommand(ECBCommand.UpdateGameObject, entity);
        }

        public void UpdateGameObject(Entity entity)
        {
            UpdateGameObject(entity.GUID);
        }

        internal void AddEntityDestroyCommand(ECBCommand op, EntityData entity, bool destroy)
        {
            if (EntityManager.TryGetEntity(entity, out var e) && e.IsAlive)
            {
                e.IsAlive = false; 
                
                var ecbd = new EntityCommandBufferData();
                ecbd.commandType = op;
                ecbd.entity = entity;
                ecbd.destroyImmediately = destroy;

                m_Data.Add(ecbd);
            }
        }

        internal void AddEntityComponentDataCommand(ECBCommand op, EntityData entity, IComponentData componentData)
        {
            var ecbd = new EntityCommandBufferData();
            ecbd.commandType = op;
            ecbd.entity = entity;
            ecbd.componentData = componentData;

            m_Data.Add(ecbd);
        }

        internal void AddEntityComponentTypeCommand(ECBCommand op, EntityData entity, ComponentType componentType)
        {
            var ecbd = new EntityCommandBufferData();
            ecbd.commandType = op;
            ecbd.entity = entity;
            ecbd.componentType = componentType;

            m_Data.Add(ecbd);
        }

        internal void AddEntityArchetypeCommand(ECBCommand op, EntityData entity, EntityArchetype archetype)
        {
            var ecbd = new EntityCommandBufferData();
            ecbd.commandType = op;
            ecbd.entity = entity;
            ecbd.archetype = archetype;

            m_Data.Add(ecbd);
        }

        internal void AddEntityQueryCommand(ECBCommand op, EntityData entity, EntityQuery query)
        {
            var ecbd = new EntityCommandBufferData();
            ecbd.commandType = op;
            ecbd.entity = entity;
            ecbd.query = query;

            m_Data.Add(ecbd);
        }

        internal void AddLoadGameObjectCommand(ECBCommand op, EntityData entity, string asset, Transform transform, float3 position, quaternion rotation, Action<Entity, object> callback, object userdata)
        {
            var ecbd = new EntityCommandBufferData();
            ecbd.commandType = op;
            ecbd.entity = entity;
            ecbd.parent = transform;
            ecbd.position = position;
            ecbd.rotation = rotation;
            ecbd.asset = asset;
            ecbd.callback = callback;
            ecbd.userdata = userdata;

            m_Data.Add(ecbd);
        }

        internal void UpdateGameObjectCommand(ECBCommand op, EntityData entity)
        {
            var ecbd = new EntityCommandBufferData();
            ecbd.commandType = op;
            ecbd.entity = entity;

            m_Data.Add(ecbd);
        }

        public void Playback(ref Entity[] entities)
        {
            PlaybackInternal(ref entities);
        }

        void PlaybackInternal(ref Entity[] entities)
        {
            foreach (var data in m_Data)
            {
                try
                {
                    Entity entity = null;
                    switch (data.commandType)
                    {
                        case ECBCommand.CreateEntity:
                            entity = EntityManager.InternalCreate(data.entity, data.archetype);
                            break;
                        case ECBCommand.DestroyEntity:
                            if (EntityManager.InternalDestroyEntity(data.entity, out entity, out var gameObject))
                            {
                                if (data.destroyImmediately && gameObject != null)
                                    gameObject.Recycle();
                            }
                            break;
                        case ECBCommand.AddComponent:
                            EntityManager.InternalAddComponentData(data.entity, data.componentData, out entity);
                            break;
                        case ECBCommand.AddComponentWithEntityArchetype:
                            EntityManager.InternalAddComponentData(data.entity, data.archetype, out entity);
                            break;
                        case ECBCommand.RemoveComponent:
                            if (EntityManager.InternalRemoveComponentData(data.entity, data.componentType, out entity, out var componentData))
                            {
                                ReferencePool.RecycleInstance(componentData);
                            }
                            break;
                        case ECBCommand.RemoveComponentWithEntityQuery:
                            if (EntityManager.InternalRemoveComponentData(data.entity, data.query, out entity, out var componentDatas))
                            {
                                foreach (var c in componentDatas)
                                {
                                    ReferencePool.RecycleInstance(c);
                                }
                            }
                            break;
                        case ECBCommand.LoadGameObjectWithParent:
                            EntityManager.InternalLoadGameObject(data.entity, data.asset, data.parent, data.callback, data.userdata);
                            break;
                        case ECBCommand.LoadGameObjectWithCoordinates:
                            EntityManager.InternalLoadGameObject(data.entity, data.asset, data.position, data.rotation, data.callback, data.userdata);
                            break;
                        case ECBCommand.UpdateGameObject:
                            EntityManager.TryGetEntity(data.entity, out entity);
                            break;
                        default:
                            break;
                    }

                    if (entity != null && !entities.Contains(entity))
                        entities = entities.Expand(entity);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            m_Data.Clear();
        }
    }

    internal enum ECBCommand
    {
        CreateEntity,
        DestroyEntity,

        AddComponent,
        AddComponentWithEntityArchetype,
        RemoveComponent,
        RemoveComponentWithEntityQuery,

        LoadGameObjectWithParent,
        LoadGameObjectWithCoordinates,

        UpdateGameObject,
    }
}
