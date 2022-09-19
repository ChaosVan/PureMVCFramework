using PureMVCFramework.Advantages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace PureMVCFramework.Providers
{
    public class ReflectionProvider : IReflectionProvider
    {
        public delegate object SpawneDelegate(string typeName, params object[] args);
        public delegate object RecycleDelegate(object inst, out string typeName);
        public delegate void ConstructDelegate(object inst, string typeName, params object[] args);

        private readonly Dictionary<string, Type> loadedTypes = new Dictionary<string, Type>();

        public SpawneDelegate customSpawner;
        public RecycleDelegate customRecycler;
        public ConstructDelegate customConstructor;

        public void LoadTypes(string assemblyString)
        {
            var asmb = Assembly.Load(assemblyString);
            var types = asmb.GetTypes();
            foreach (var type in types)
            {
                loadedTypes.Add(type.FullName, type);
            }
        }

        public Type GetType(string fullTypeName)
        {
            if (loadedTypes.TryGetValue(fullTypeName, out var type))
                return type;

            return null;
        }

        //public void InvokeConstructor(object inst, string typeName, params object[] args)
        //{
        //    if (loadedTypes.TryGetValue(typeName, out var type))
        //    {
        //        var constructors = type.GetConstructors();
        //        if (constructors != null && constructors.Length > 0)
        //            constructors[0].Invoke(inst, args);
        //    }
        //    else
        //    {
        //        customConstructor?.Invoke(inst, typeName, args);
        //    }
            
        //}

        public object Recycle(object inst, out string typeName)
        {
            typeName = inst.GetType().FullName;
            if (!loadedTypes.TryGetValue(typeName, out var _))
            {
                return customRecycler?.Invoke(inst, out typeName);
            }

            return inst;
        }

        public object Spawn(string typeName, params object[] args)
        {
            if (loadedTypes.TryGetValue(typeName, out var classType))
            {
                return Activator.CreateInstance(classType, args);
            }

            return customSpawner?.Invoke(typeName, args);
        }
    }
}
