using PureMVCFramework.Advantages;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PureMVCFramework.Entity
{
    public class EntityCommandBuffer : IDisposable
    {
        public struct EntityCommandBufferData
        {

        }

        public struct EntityData
        {
            public int index;
        }

        internal int SystemID;
        internal SystemHandleUntyped OriginSystemHandle;

        internal readonly List<EntityCommandBufferData> m_Data = new List<EntityCommandBufferData>();
        internal int index;

        public bool IsCreated => m_Data != null;

        public EntityCommandBuffer()
        {
            m_Data.Clear();
            index = 1;
        }

        public void Dispose()
        {
            ReferencePool.Instance.RecycleInstance(this);
        }

        public EntityData CreateEntity()
        {
            EntityArchetype archetype = new EntityArchetype();
            return _CreateEntity(archetype);
        }

        private EntityData _CreateEntity(EntityArchetype archetype)
        {
            EntityData entity = new EntityData { index = index++ };
            AddCreateCommand(ECBCommand.CreateEntity, entity, archetype);
            return entity;
        }

        public void DestroyEntity(Entity entity)
        {

        }

        internal void AddCreateCommand(ECBCommand op, EntityData entity, EntityArchetype archetype)
        {

        }

        public void Playback()
        {
            PlaybackInternal();
        }

        void PlaybackInternal()
        {
            
        }
    }

    internal enum ECBCommand
    {
        InstantiateEntity,

        CreateEntity,
        DestroyEntity,

        AddComponent,
        RemoveComponent,
        SetComponent,

        AddSharedComponentData,
        SetSharedComponentData,

    }
}
