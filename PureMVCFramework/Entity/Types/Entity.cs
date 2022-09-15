#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace PureMVCFramework.Entity
{
    public sealed class Entity : IDisposable
    {
        public GameObject gameObject;

#if ODIN_INSPECTOR
        [ShowInInspector]
#endif
        public ulong GUID { get; internal set; }    // Generic Unique Identifier  本地ID

#if ODIN_INSPECTOR
        [ShowInInspector]
#endif
        public bool IsAlive { get; internal set; }

#if ODIN_INSPECTOR
        [ShowInInspector]
#endif
        internal readonly IComponentData[] m_AllComponentData;

        public EntityArchetype archetype;

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
                m_AllComponentData[i] = null;
            }
            archetype = default;
            gameObject = null;
        }

        internal bool InternalAddComponentData(ComponentType type, IComponentData component)
        {
            if (!IsAlive)
                return false;

            Assert.IsNull(m_AllComponentData[type.TypeIndex], $"Entity({GUID}) already has type: {TypeManager.GetType(type.TypeIndex).FullName}");
            archetype.AddComponentType(type);
            m_AllComponentData[type.TypeIndex] = component;
            return true;
        }

        //internal bool InternalSetComponentData(ComponentType type, IComponentData component, out IComponentData removed)
        //{
        //    removed = null;
        //    if (!IsAlive)
        //        return false;

        //    Assert.IsNotNull(m_AllComponentData[type.TypeIndex]);

        //    if (component == null)
        //        return InternalRemoveComponentData(type, out removed);

        //    if (!archetype.TryGetComponentType(type.TypeIndex, out var t))
        //        return false;

        //    removed = m_AllComponentData[type.TypeIndex];
        //    m_AllComponentData[type.TypeIndex] = component;
        //    return true;
        //}

        internal bool InternalRemoveComponentData(ComponentType type, out IComponentData removed)
        {
            removed = m_AllComponentData[type.TypeIndex];
            if (!IsAlive)
                return false;

            Assert.IsNotNull(removed, $"Entity({GUID}) doesn't has type: {TypeManager.GetType(type.TypeIndex).FullName}");
            archetype.RemoveComponentType(type);
            m_AllComponentData[type.TypeIndex] = null;
            return true;
        }

        internal bool InternalGetComponentData(ComponentType type, out IComponentData ret)
        {
            ret = m_AllComponentData[type.TypeIndex];
            if (!IsAlive)
                return false;

            return ret != null;
        }

        internal bool InternalGetComponentData(EntityQuery query, out IComponentData[] ret)
        {
            ret = null;
            if (!IsAlive)
                return false;

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

            return false;
        }
    }

}
