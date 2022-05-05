using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using PureMVCFramework.Providers;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace PureMVCFramework.Advantages
{
    public interface IInitializeable
    {
        void OnInitialized(params object[] args);
    }

    public interface IRecycleable
    {
        void OnRecycle();
    }

    public interface IReflectionProvider
    {
        object Spawn(string typeName, params object[] args);
        object Recycle(object obj, out string typeName);
        void LoadTypes(string assemblyString);
        Type GetType(string fullTypeName);
        void InvokeConstructor(object inst, string typeName, params object[] args);
    }

    public class ReferencePool : SingletonBehaviour<ReferencePool>
    {
        private readonly ConcurrentDictionary<string, ConcurrentQueue<object>> m_Cache = new ConcurrentDictionary<string, ConcurrentQueue<object>>();

#if UNITY_EDITOR
#if ODIN_INSPECTOR
        [ShowInInspector, ShowIf("showOdinInfo"), DictionaryDrawerSettings(IsReadOnly = true, DisplayMode = DictionaryDisplayOptions.Foldout)]
#endif
        private readonly Dictionary<string, int> m_Counter = new Dictionary<string, int>();
#endif


        private IReflectionProvider provider;

        public IReflectionProvider Provider { get => provider; }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            provider = new ReflectionProvider();
        }

        protected override void OnDelete()
        {
            provider = null;

            Clear();
            base.OnDelete();
        }

        public void Clear()
        {
            foreach (var pair in m_Cache)
            {
                while (pair.Value.TryDequeue(out var result))
                {
                    
                }
            }

            m_Cache.Clear();
#if UNITY_EDITOR
            m_Counter.Clear();
#endif
        }

        public void LoadTypes(string assemblyString)
        {
            provider.LoadTypes(assemblyString);
        }

        public Type GetType(string fullTypeName)
        {
            return provider.GetType(fullTypeName);
        }

        public object SpawnInstance(string typeName, params object[] args)
        {
            if (m_Cache.TryGetValue(typeName, out var list) && list.TryDequeue(out var result))
            {
#if UNITY_EDITOR
                int count = m_Cache[typeName].Count;
                m_Counter[typeName] = count;
#endif
                provider.InvokeConstructor(result, typeName, args);
            }
            else
            {
                result = provider.Spawn(typeName, args);
            }

            if (result is IInitializeable o)
                o.OnInitialized(args);

            return result;
        }

        public object SpawnInstance(Type type, params object[] args)
        {
            return SpawnInstance(type.FullName, args);
        }

        public T SpawnInstance<T>(params object[] args) where T : new()
        {
            return (T)SpawnInstance(typeof(T).FullName, args);
        }

        public void RecycleInstance(object inst)
        {
            var obj = provider.Recycle(inst, out var typeName);

            if (obj != null)
            {
                if (obj is IRecycleable o)
                    o.OnRecycle();

                if (!m_Cache.ContainsKey(typeName))
                    m_Cache[typeName] = new ConcurrentQueue<object>();

                m_Cache[typeName].Enqueue(obj);
#if UNITY_EDITOR
                int count = m_Cache[typeName].Count;
                m_Counter[typeName] = count;
#endif
            }

        }
    }
}
