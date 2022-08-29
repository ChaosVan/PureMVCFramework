using PureMVCFramework.Advantages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.Burst;
using UnityEngine;
using UnityEngine.Assertions;

namespace PureMVCFramework.Entity
{
    static public partial class TypeManager
    {
#pragma warning disable 414
        static int s_SystemCount;
#pragma warning restore 414
        static List<Type> s_SystemTypes = new List<Type>();
        static List<string> s_SystemTypeNames = new List<string>();
        static List<int> s_SystemTypeSizes = new List<int>();
        static List<long> s_SystemTypeHashes = new List<long>();

        struct LookupFlags
        {
            public WorldSystemFilterFlags OptionalFlags;
            public WorldSystemFilterFlags RequiredFlags;
        }

        static Dictionary<LookupFlags, IReadOnlyList<Type>> s_SystemFilterTypeMap;

        private static void InitializeSystemsState()
        {
            s_SystemTypes = new List<Type>();
            s_SystemTypeNames = new List<string>();
            s_SystemFilterTypeMap = new Dictionary<LookupFlags, IReadOnlyList<Type>>();
            s_SystemCount = 0;
        }

        private static void ShutdownSystemsState()
        {
            s_SystemTypes.Clear();
            s_SystemTypeNames.Clear();
            s_SystemCount = 0;
        }

        /// <summary>
        /// Construct a System from a Type. Uses the same list in GetSystems()
        /// </summary>
        ///
        public static ComponentSystemBase ConstructSystem(Type systemType)
        {
#if !NET_DOTS
            if (!typeof(ComponentSystemBase).IsAssignableFrom(systemType))
                throw new ArgumentException($"'{systemType.FullName}' cannot be constructed as it does not inherit from ComponentSystemBase");
            return (ComponentSystemBase)ReferencePool.SpawnInstance(systemType);
#else
            throw new();
#endif
        }

        public static T ConstructSystem<T>() where T : ComponentSystemBase
        {
            return (T)ConstructSystem(typeof(T));
        }

        public static T ConstructSystem<T>(Type systemType) where T : ComponentSystemBase
        {
            return (T)ConstructSystem(systemType);
        }

        /// <summary>
        /// Return an array of all System types available to the runtime matching the WorldSystemFilterFlags. By default,
        /// all systems available to the runtime is returned.
        /// </summary>
        public static IReadOnlyList<Type> GetSystems(WorldSystemFilterFlags filterFlags = WorldSystemFilterFlags.All, WorldSystemFilterFlags requiredFlags = WorldSystemFilterFlags.Default)
        {
            Assert.IsTrue(requiredFlags > 0, "Must use a 'requiredFlags' greater than 0. If you want to get all systems with any flag, pass a filterFlag of WorldSystemFilterFlags.All");
            LookupFlags lookupFlags = new LookupFlags() { OptionalFlags = filterFlags, RequiredFlags = requiredFlags };

            if (s_SystemFilterTypeMap.TryGetValue(lookupFlags, out var systemTypes))
                return systemTypes;

#if !UNITY_DOTSRUNTIME
            var filteredSystemTypes = new List<Type>();
            foreach (var systemType in GetTypesDerivedFrom(typeof(ComponentSystemBase)))
            {
                if (FilterSystemType(systemType, lookupFlags))
                    filteredSystemTypes.Add(systemType);
            }

            s_SystemFilterTypeMap[lookupFlags] = filteredSystemTypes;
            return filteredSystemTypes;
#else
            throw new();
#endif
        }

        public static bool IsSystemType(Type t)
        {
            return GetSystemTypeIndexNoThrow(t) != -1;
        }

        public static string GetSystemName(Type t)
        {
#if !NET_DOTS
            return t.FullName;
#else
            throw new();
#endif
        }

        internal static long GetSystemTypeHash(Type t)
        {
#if !NET_DOTS 
            return BurstRuntime.GetHashCode64(t);
#else
            throw new();
#endif
        }

        internal static int GetSystemTypeIndexNoThrow(Type t)
        {
            Assert.IsTrue(s_Initialized, "The TypeManager must be initialized before the TypeManager can be used.");

            for (int i = 0; i < s_SystemTypes.Count; ++i)
            {
                if (t == s_SystemTypes[i]) return i;
            }
            return -1;
        }

        public static bool IsSystemAGroup(Type t)
        {
#if !NET_DOTS
            return t.IsSubclassOf(typeof(ComponentSystemGroup));
#else
            throw new();
#endif
        }
        public static Attribute[] GetSystemAttributes(Type systemType, Type attributeType)
        {
            Assert.IsTrue(s_Initialized, "The TypeManager must be initialized before the TypeManager can be used.");

#if !NET_DOTS
            Attribute[] attributes;
            var kDisabledCreationAttribute = typeof(DisableAutoCreationAttribute);
            if (attributeType == kDisabledCreationAttribute)
            {
                // We do not want to inherit DisableAutoCreation from some parent type (as that attribute explicitly states it should not be inherited)
                var objArr = systemType.GetCustomAttributes(attributeType, false);
                var attrList = new List<Attribute>();

                var alreadyDisabled = false;
                for (int i = 0; i < objArr.Length; i++)
                {
                    var attr = objArr[i] as Attribute;
                    attrList.Add(attr);

                    if (attr.GetType() == kDisabledCreationAttribute)
                        alreadyDisabled = true;
                }

                if (!alreadyDisabled && systemType.Assembly.GetCustomAttribute(attributeType) != null)
                {
                    attrList.Add(new DisableAutoCreationAttribute());
                }
                attributes = attrList.ToArray();
            }
            else
            {
                var objArr = systemType.GetCustomAttributes(attributeType, true);
                attributes = new Attribute[objArr.Length];
                for (int i = 0; i < objArr.Length; i++)
                {
                    attributes[i] = objArr[i] as Attribute;
                }
            }

            return attributes;
#else
            throw new();
#endif
        }

        internal static IEnumerable<Type> GetTypesDerivedFrom(Type type)
        {
#if UNITY_EDITOR
            return UnityEditor.TypeCache.GetTypesDerivedFrom(type);
#else

            var types = new List<Type>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!TypeManager.IsAssemblyReferencingEntities(assembly))
                    continue;

                try
                {
                    var assemblyTypes = assembly.GetTypes();
                    foreach (var t in assemblyTypes)
                    {
                        if (type.IsAssignableFrom(t))
                            types.Add(t);
                    }
                }
                catch (ReflectionTypeLoadException e)
                {
                    foreach (var t in e.Types)
                    {
                        if (t != null && type.IsAssignableFrom(t))
                            types.Add(t);
                    }

                    Debug.LogWarning($"DefaultWorldInitialization failed loading assembly: {(assembly.IsDynamic ? assembly.ToString() : assembly.Location)}");
                }
            }

            return types;
#endif
        }

        static bool FilterSystemType(Type type, LookupFlags lookupFlags)
        {
            // IMPORTANT: keep this logic in sync with SystemTypeGen.cs for DOTS Runtime
            WorldSystemFilterFlags systemFlags = WorldSystemFilterFlags.Default;

            // the entire assembly can be marked for no-auto-creation (test assemblies are good candidates for this)
            var disableAllAutoCreation = Attribute.IsDefined(type.Assembly, typeof(DisableAutoCreationAttribute));
            var disableTypeAutoCreation = Attribute.IsDefined(type, typeof(DisableAutoCreationAttribute), false);

            // these types obviously cannot be instantiated
            if (type.IsAbstract || type.ContainsGenericParameters)
            {
                if (disableTypeAutoCreation)
                    Debug.LogWarning($"Invalid [DisableAutoCreation] on {type.FullName} (only concrete types can be instantiated)");

                return false;
            }

            // only derivatives of ComponentSystemBase and structs implementing ISystem are systems
            if (!type.IsSubclassOf(typeof(ComponentSystemBase)) && !typeof(ISystem).IsAssignableFrom(type))
                throw new System.ArgumentException($"{type} must already be filtered by ComponentSystemBase or ISystem");

            // the auto-creation system instantiates using the default ctor, so if we can't find one, exclude from list
            if (type.IsClass && type.GetConstructor(Type.EmptyTypes) == null)
            {
                // we want users to be explicit
                if (!disableTypeAutoCreation && !disableAllAutoCreation)
                    Debug.LogWarning($"Missing default ctor on {type.FullName} (or if you don't want this to be auto-creatable, tag it with [DisableAutoCreation])");

                return false;
            }

            if (lookupFlags.OptionalFlags == WorldSystemFilterFlags.All)
                return true;

            if (disableTypeAutoCreation || disableAllAutoCreation)
            {
                if (disableTypeAutoCreation && disableAllAutoCreation)
                    Debug.LogWarning($"Redundant [DisableAutoCreation] on {type.FullName} (attribute is already present on assembly {type.Assembly.GetName().Name}");

                return false;
            }

            if (Attribute.IsDefined(type, typeof(WorldSystemFilterAttribute), true))
                systemFlags = type.GetCustomAttribute<WorldSystemFilterAttribute>(true).FilterFlags;

            return (lookupFlags.OptionalFlags & systemFlags) >= lookupFlags.RequiredFlags;
        }
    }
}
