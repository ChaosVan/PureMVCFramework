﻿using PureMVCFramework.Advantages;
using System;
using UnityEngine;
using UnityEngine.Assertions;

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

    public sealed class Entity : IDisposable
    {
        public GameObject gameObject;

#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.ShowInInspector]
#endif
        public ulong GUID { get; internal set; }    // Generic Unique Identifier  本地ID

#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.ShowInInspector]
#endif
        public bool IsAlive { get; internal set; }

        internal readonly IComponentData[] m_AllComponentData;

        private EntityArchetype archetype;

        public Entity()
        {
            m_AllComponentData = new IComponentData[TypeManager.TypeCount];
        }

        public void Dispose()
        {
            IsAlive = false;
            GUID = 0UL;

            for (int i = 0; i < m_AllComponentData.Length; ++i)
            {
                if (m_AllComponentData[i] != null)
                    ReferencePool.RecycleInstance(m_AllComponentData[i]);
                m_AllComponentData[i] = null;
            }
            archetype = default;
            gameObject = null;


        }

        internal bool InternalAddComponentData(ComponentType type, IComponentData component)
        {
            Assert.IsNull(m_AllComponentData[type.TypeIndex], $"Entity({GUID}) already has type: {TypeManager.GetType(type.TypeIndex).FullName}");
            archetype.AddComponentType(type);
            m_AllComponentData[type.TypeIndex] = component;
            return true;
        }

        internal bool InternalRemoveComponentData(ComponentType type, out IComponentData removed)
        {
            removed = m_AllComponentData[type.TypeIndex];
            Assert.IsNotNull(removed, $"Entity({GUID}) doesn't has type: {TypeManager.GetType(type.TypeIndex).FullName}");
            archetype.RemoveComponentType(type);
            m_AllComponentData[type.TypeIndex] = null;
            return true;
        }

        internal bool InternalGetComponentData(ComponentType type, out IComponentData ret)
        {
            ret = m_AllComponentData[type.TypeIndex];
            return ret != null;
        }

        internal bool InternalGetComponentData(EntityQuery query, out IComponentData[] ret)
        {

            if (query.TypesCount > 0)
            {
                ret = new IComponentData[query.TypesCount];
                for (int i = 0; i < query.TypesCount; i++)
                {
                    ret[i] = m_AllComponentData[query.types[i].TypeIndex];

                    if (query.types[i].AccessModeType < ComponentType.AccessMode.Exclude)
                    {
                        if (ret[i] == null)
                            return false;
                    }
                    else if (query.types[i].AccessModeType == ComponentType.AccessMode.Exclude)
                    {
                        if (ret[i] != null)
                            return false;
                    }
                }

                return true;
            }

            ret = null;
            return false;
        }
    }

}
