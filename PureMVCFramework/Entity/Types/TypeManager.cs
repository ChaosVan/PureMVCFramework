using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;

namespace PureMVCFramework.Entity
{
    /// <summary>
    /// [DisableAutoTypeRegistration] prevents a Component Type from being registered in the TypeManager
    /// during TypeManager.Initialize(). Types that are not registered will not be recognized by EntityManager.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
    public class DisableAutoTypeRegistrationAttribute : Attribute
    {
    }

    public static partial class TypeManager
    {
        public const int ClearFlagsMask = 0x007FFFFF;

        static bool s_Initialized;

        static bool s_AppDomainUnloadRegistered;
        static Dictionary<Type, int> s_ManagedTypeToIndex;

        static int s_TypeCount;
        static List<TypeInfo> s_TypeInfos;
#if UNITY_2021_3_OR_NEWER
        static Dictionary<ulong, int> s_StableTypeHashToTypeIndex;
#else
        static ConcurrentDictionary<ulong, int> s_StableTypeHashToTypeIndex;
#endif
        static List<Type> s_Types;
        static List<string> s_TypeNames;

        public static int TypeCount => s_TypeCount;

        internal static Type UnityEngineObjectType;

        [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
        public class TypeVersionAttribute : Attribute
        {
            public TypeVersionAttribute(int version)
            {
                TypeVersion = version;
            }

            public int TypeVersion;
        }

        public static TypeInfo[] GetAllTypes()
        {
            var res = new TypeInfo[s_TypeCount];

            for (var i = 0; i < s_TypeCount; i++)
            {
                res[i] = s_TypeInfos[i];
            }

            return res;
        }

        public readonly struct TypeInfo
        {
            public TypeInfo(int typeIndex, ulong stableTypeHash)
            {
                TypeIndex = typeIndex;
                StableTypeHash = stableTypeHash;
            }

            public readonly int TypeIndex;
            public readonly ulong StableTypeHash;

            // NOTE: We explicitly exclude Type as a member of TypeInfo so the type can remain a ValueType
            public Type Type => TypeManager.GetType(TypeIndex);

            public string DebugTypeName
            {
                get
                {
                    if (Type != null)
                        return Type.FullName;
                    else
                        return "<unavailable>";
                }
            }
        }
        public static Type GetType(int typeIndex)
        {
            var typeIndexNoFlags = typeIndex & ClearFlagsMask;
            Assert.IsTrue(typeIndexNoFlags >= 0 && typeIndexNoFlags < s_Types.Count);
            return s_Types[typeIndexNoFlags];
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Initialize()
        {
#if !UNITY_DOTSRUNTIME
            if (s_Initialized)
                return;

            s_Initialized = true;

            if (!s_AppDomainUnloadRegistered)
            {
                // important: this will always be called from a special unload thread (main thread will be blocking on this)
                AppDomain.CurrentDomain.DomainUnload += (_, __) =>
                {
                    if (s_Initialized)
                        Shutdown();

                };
                s_AppDomainUnloadRegistered = true;
            }

            s_ManagedTypeToIndex = new Dictionary<Type, int>(1000);

            s_TypeCount = 0;
            s_TypeInfos = new List<TypeInfo>(1000);
#if UNITY_2021_3_OR_NEWER
            s_StableTypeHashToTypeIndex = new Dictionary<ulong, int>();
#else
            s_StableTypeHashToTypeIndex = new ConcurrentDictionary<ulong, int>();
#endif
            s_Types = new List<Type>();
            s_TypeNames = new List<string>();

            InitializeSystemsState();
            InitializeAllComponentTypes();
#endif
        }

        public static void Shutdown()
        {
            if (!s_Initialized)
                return;

            s_Initialized = false;

            s_Types.Clear();
            s_TypeNames.Clear();

            ShutdownSystemsState();

            s_ManagedTypeToIndex.Clear();

            s_TypeInfos.Clear();
            s_StableTypeHashToTypeIndex.Clear();

        }

        private static void InitializeAllComponentTypes()
        {
#if UNITY_EDITOR
            var stopWatch = new Stopwatch();
            stopWatch.Start();
#endif
            try
            {
                UnityEngine.Profiling.Profiler.BeginSample("InitializeAllComponentTypes");
                var componentTypeSet = new HashSet<Type>();
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                UnityEngineObjectType = typeof(UnityEngine.MonoBehaviour);

#if UNITY_EDITOR
                //foreach (var type in UnityEditor.TypeCache.GetTypesDerivedFrom<UnityEngine.MonoBehaviour>())
                //    AddUnityEngineObjectTypeToListIfSupported(componentTypeSet, type);
                foreach (var type in UnityEditor.TypeCache.GetTypesDerivedFrom<IComponentData>())
                    AddComponentTypeToListIfSupported(componentTypeSet, type);
#else
                foreach (var assembly in assemblies)
                {
                    IsAssemblyReferencingEntitiesOrUnityEngine(assembly, out var isAssemblyReferencingEntities,
                        out var isAssemblyReferencingUnityEngine);
                    var isAssemblyRelevant = isAssemblyReferencingEntities/* || isAssemblyReferencingUnityEngine*/;

                    if (!isAssemblyRelevant)
                        continue;

                    var assemblyTypes = assembly.GetTypes();

                    // Register UnityEngine types (Hybrid)
                    //if (isAssemblyReferencingUnityEngine)
                    //{
                    //    foreach (var type in assemblyTypes)
                    //    {
                    //        if (UnityEngineObjectType.IsAssignableFrom(type))
                    //            AddUnityEngineObjectTypeToListIfSupported(componentTypeSet, type);

                    //    }
                    //}

                    // Register ComponentData types
                    if (isAssemblyReferencingEntities)
                    {
                        foreach (var type in assemblyTypes)
                        {
                            if (typeof(IComponentData).IsAssignableFrom(type))
                                AddComponentTypeToListIfSupported(componentTypeSet, type);
                        }
                    }
                }
#endif

                var componentTypeCount = componentTypeSet.Count;
                var componentTypes = new Type[componentTypeCount];
                componentTypeSet.CopyTo(componentTypes);

                var typeIndexByType = new Dictionary<Type, int>();
                var startTypeIndex = s_TypeCount;

                for (int i = 0; i < componentTypes.Length; i++)
                {
                    typeIndexByType[componentTypes[i]] = startTypeIndex + i;
                }

                AddAllComponentTypes(componentTypes, startTypeIndex);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                UnityEngine.Profiling.Profiler.EndSample();
            }
        }

        static void AddUnityEngineObjectTypeToListIfSupported(HashSet<Type> componentTypeSet, Type type)
        {
            if (type.ContainsGenericParameters)
                return;
            if (type.IsAbstract)
                return;
            componentTypeSet.Add(type);
        }

        static void AddComponentTypeToListIfSupported(HashSet<Type> typeSet, Type type)
        {
            if (!IsInstantiableComponentType(type))
                return;

            typeSet.Add(type);
        }

        static bool IsInstantiableComponentType(Type type)
        {
            if (type.IsAbstract)
                return false;

            if (!type.IsValueType && !typeof(IComponentData).IsAssignableFrom(type))
                return false;

            // Don't register open generics here.  It's an open question
            // on whether we should support them for components at all,
            // as with them we can't ever see a full set of component types
            // in use.
            if (type.ContainsGenericParameters)
                return false;

            if (type.GetCustomAttribute(typeof(DisableAutoTypeRegistrationAttribute)) != null)
                return false;

            return true;
        }

        internal static void IsAssemblyReferencingEntitiesOrUnityEngine(Assembly assembly, out bool referencesEntities, out bool referencesUnityEngine)
        {
            const string kEntitiesAssemblyName = "PureMVCFramework.Entity";
            //const string kUnityEngineAssemblyName = "UnityEngine";
            var assemblyName = assembly.GetName().Name;

            referencesEntities = false;
            referencesUnityEngine = false;

            if (assemblyName.Contains(kEntitiesAssemblyName))
                referencesEntities = true;

            //if (assemblyName.Contains(kUnityEngineAssemblyName))
            //    referencesUnityEngine = true;

            var referencedAssemblies = assembly.GetReferencedAssemblies();
            foreach (var referencedAssembly in referencedAssemblies)
            {
                var referencedAssemblyName = referencedAssembly.Name;

                if (!referencesEntities && referencedAssemblyName.Contains(kEntitiesAssemblyName))
                    referencesEntities = true;
                //if (!referencesUnityEngine && referencedAssemblyName.Contains(kUnityEngineAssemblyName))
                //    referencesUnityEngine = true;
            }
        }

        internal static TypeInfo BuildComponentType(Type type, int[] writeGroups)
        {
            // The stable type hash is the same as the memory order if the user hasn't provided a custom memory ordering
            var stableTypeHash = TypeHash.CalculateStableTypeHash(type);
            int typeIndex = s_TypeCount;

            return new TypeInfo(typeIndex, stableTypeHash);
        }

        private static void AddAllComponentTypes(Type[] componentTypes, int startTypeIndex)
        {
            var expectedTypeIndex = startTypeIndex;

            for (int i = 0; i < componentTypes.Length; i++)
            {
                var type = componentTypes[i];
                try
                {
                    var index = FindTypeIndex(type);
                    if (index != -1)
                        throw new InvalidOperationException("ComponentType cannot be initialized more than once.");

                    TypeInfo typeInfo = BuildComponentType(type, null);

                    var typeIndex = typeInfo.TypeIndex & TypeManager.ClearFlagsMask;
                    if (expectedTypeIndex != typeIndex)
                        throw new InvalidOperationException("ComponentType.TypeIndex does not match precalculated index.");

                    AddTypeInfoToTables(type, typeInfo, type.FullName);
                    expectedTypeIndex += 1;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        public static TypeInfo GetTypeInfo(int typeIndex)
        {
            return s_TypeInfos[typeIndex & ClearFlagsMask];
        }

        public static TypeInfo GetTypeInfo<T>()
        {
            return s_TypeInfos[GetTypeIndex<T>() & ClearFlagsMask];
        }

        private static int FindTypeIndex(Type type)
        {
#if !UNITY_DOTSRUNTIME
            if (type == null)
                return 0;

            int res;
            if (s_ManagedTypeToIndex.TryGetValue(type, out res))
                return res;
            else
                return -1;
#else
            // skip 0 since it is always null
            for (var i = 1; i < s_Types.Count; i++)
                if (type == s_Types[i])
                    return s_TypeInfos[i].TypeIndex;

            throw new ArgumentException("Tried to GetTypeIndex for type that has not been set up by the static type registry.");
#endif
        }

        public static int GetTypeIndex<T>()
        {
            return GetTypeIndex(typeof(T));
        }

        public static int GetTypeIndex(Type type)
        {
            var index = FindTypeIndex(type);

            if (index == -1)
                throw new Exception();

            return index;
        }

        public static int GetTypeIndexFromStableTypeHash(ulong stableTypeHash)
        {
            if (s_StableTypeHashToTypeIndex.TryGetValue(stableTypeHash, out var typeIndex))
                return typeIndex;
            return -1;
        }

        private static void AddTypeInfoToTables(Type type, TypeInfo typeInfo, string typeName)
        {
            if (!s_StableTypeHashToTypeIndex.TryAdd(typeInfo.StableTypeHash, typeInfo.TypeIndex))
            {
                int previousTypeIndex = s_StableTypeHashToTypeIndex[typeInfo.StableTypeHash] & ClearFlagsMask;
                throw new ArgumentException($"{type} and {s_Types[previousTypeIndex]} have a conflict in the stable type hash. Use the [TypeVersion(...)] attribute to force a different stable type hash for one of them.");
            }

            // Debug.Log($"{type} -> {typeInfo.StableTypeHash}");

            s_TypeInfos.Insert(typeInfo.TypeIndex & ClearFlagsMask, typeInfo);
            s_Types.Add(type);
            s_TypeNames.Add(typeName);
            Assert.AreEqual(s_TypeCount, typeInfo.TypeIndex & ClearFlagsMask);
            s_TypeCount++;

            if (type != null)
            {
                s_ManagedTypeToIndex.Add(type, typeInfo.TypeIndex);
            }
        }

        public static bool IsAssemblyReferencingEntities(Assembly assembly)
        {
            const string kEntitiesAssemblyName = "PureMVCFramework.Entity";
            if (assembly.GetName().Name.Contains(kEntitiesAssemblyName))
                return true;

            var referencedAssemblies = assembly.GetReferencedAssemblies();
            foreach (var referenced in referencedAssemblies)
                if (referenced.Name.Contains(kEntitiesAssemblyName))
                    return true;
            return false;
        }
    }
}
