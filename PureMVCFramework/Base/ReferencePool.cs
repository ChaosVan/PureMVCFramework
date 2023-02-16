using PureMVCFramework.Providers;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace PureMVCFramework
{
    public interface IInitializable
    {
        void OnInitialized(params object[] args);
    }

    public interface IReflectionProvider
    {
        object Spawn(string typeName, params object[] args);
        object Recycle(object obj, out string typeName);
        void LoadTypes(string assemblyString);
        Type GetType(string fullTypeName);
        //void InvokeConstructor(object inst, string typeName, params object[] args);
    }

    public static class ReferencePool
    {
        internal static event Action<string> OnChanged;
        internal static event Action OnCleaned;

        internal static readonly ConcurrentDictionary<string, ConcurrentQueue<object>> m_Cache = new ConcurrentDictionary<string, ConcurrentQueue<object>>();

        private static IReflectionProvider provider;
        public static IReflectionProvider Provider { get => provider; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        public static void Initialize()
        {
            if (provider == null)
            {
                provider = new ReflectionProvider();

                provider.LoadTypes("PureMVCFramework");
                provider.LoadTypes("PureMVCFramework.Entity");
                provider.LoadTypes("Assembly-CSharp");
            }

            m_Cache.Clear();

#if UNITY_EDITOR
            ReferencePoolDebugger.Instance.updateMode = SingletonBehaviour<ReferencePoolDebugger>.UpdateMode.LATE_UPDATE;
#endif
        }

        public static void ClearCache(string rootNameSpace = "")
        {
            if (string.IsNullOrEmpty(rootNameSpace))
            {
                m_Cache.Clear();
                OnCleaned?.Invoke();
            }
            else
            {
                if (!rootNameSpace.EndsWith("."))
                    rootNameSpace = rootNameSpace + ".";

                foreach (var typeName in m_Cache.Keys)
                {
                    if (typeName.StartsWith(rootNameSpace) && m_Cache.TryGetValue(typeName, out var queue))
                    {
                        queue.Clear();
                        OnChanged?.Invoke(typeName);
                    }
                }
            }
        }

        public static void LoadTypes(string assemblyString)
        {
            provider.LoadTypes(assemblyString);
        }

        public static Type GetType(string fullTypeName)
        {
            return provider.GetType(fullTypeName);
        }

        private static object InternalSpawnInstance(string typeName, bool recallCtor, params object[] args)
        {
            if (m_Cache.TryGetValue(typeName, out var queue) && queue.TryDequeue(out var result))
            {
                OnChanged?.Invoke(typeName);
            }
            else
            {
                result = provider.Spawn(typeName, args);
            }

            if (result is IInitializable o)
                o.OnInitialized(args);

            return result;
        }

        public static object SpawnInstance(string typeName, params object[] args)
        {
            return InternalSpawnInstance(typeName, false, args);
        }

        public static object SpawnInstance(Type type, params object[] args)
        {
            return SpawnInstance(type.FullName, args);
        }

        public static T SpawnInstance<T>(params object[] args)
        {
            return (T)SpawnInstance(typeof(T).FullName, args);
        }

        public static void RecycleInstance(object inst)
        {
            var obj = provider.Recycle(inst, out var typeName);

            if (obj != null)
            {
                if (obj is IDisposable d)
                    d.Dispose();

                if (!m_Cache.ContainsKey(typeName))
                    m_Cache[typeName] = new ConcurrentQueue<object>();

                m_Cache[typeName].Enqueue(obj);
                OnChanged?.Invoke(typeName);
            }

        }
    }

    public class ReferencePoolDebugger : SingletonBehaviour<ReferencePoolDebugger>
    {

#if ODIN_INSPECTOR
        [ShowInInspector, ShowIf("showOdinInfo"), DictionaryDrawerSettings(IsReadOnly = true, DisplayMode = DictionaryDisplayOptions.Foldout)]
#endif
        private readonly Dictionary<string, int> m_Counter = new Dictionary<string, int>();

        protected override void OnInitialized()
        {
            base.OnInitialized();

            ReferencePool.OnChanged += ReferencePool_OnSpawned;
            ReferencePool.OnCleaned += ReferencePool_OnCleaned;
        }

        protected override void OnDelete()
        {
            m_Counter.Clear();

            ReferencePool.OnChanged -= ReferencePool_OnSpawned;
            ReferencePool.OnCleaned -= ReferencePool_OnCleaned;

            base.OnDelete();
        }

        private void ReferencePool_OnCleaned()
        {
            m_Counter.Clear();
        }

        private void ReferencePool_OnSpawned(string typeName)
        {
            int count = ReferencePool.m_Cache[typeName].Count;
            m_Counter[typeName] = count;
        }
    }
}
