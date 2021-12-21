﻿using System.Collections.Generic;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace PureMVCFramework.Entity
{
    public sealed class Entity
    {
#if UNITY_EDITOR
        public string name;
#endif

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
        internal readonly SortedDictionary<long, IComponent> components = new SortedDictionary<long, IComponent>();

        public static long StringToHash(string str)
        {
            long hashcode = 0;
            if (!string.IsNullOrEmpty(str))
            {
                for (int i = 0; i < str.Length; ++i)
                {
                    hashcode = hashcode * 31 + str[i];
                }
            }

            return hashcode;
        }
    }

}
