using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace PureMVCFramework
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class DomainReloadAttribute : Attribute
    {
        private object defaultValue;

        public DomainReloadAttribute(object defaultValue = null)
        {
            this.defaultValue = defaultValue;
        }

        public object DefaultValue => defaultValue;

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
        public static void Run()
        {
            Dictionary<Assembly, List<MemberInfo>> allMembers = new Dictionary<Assembly, List<MemberInfo>>();

            string allow = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Library", "ScriptAssemblies");
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic || !assembly.Location.StartsWith(allow))
                    continue;

                var assemblyName = assembly.GetName();
                if (assemblyName == null)
                    continue;

                if (assemblyName.Name.StartsWith("Unity.") ||
                    assemblyName.Name.StartsWith("UnityEngine.") ||
                    assemblyName.Name.StartsWith("UnityEditor."))
                    continue;

                List<MemberInfo> list = new List<MemberInfo>();
                foreach (var t in assembly.GetTypes())
                {
                    if (t.IsGenericType)
                    {
                        continue;
                    }

                    //Debug.LogWarning(t.FullName);

                    list.AddRange(GetFields(t)
                        .Where(m => m.GetCustomAttribute<DomainReloadAttribute>() != null));

                    list.AddRange(GetProperties(t)
                        .Where(m => m.GetCustomAttribute<DomainReloadAttribute>() != null));

                }

                if (list.Count > 0)
                    allMembers[assembly] = list;
            }

            foreach (var pair in allMembers)
            {
                foreach (var m in pair.Value)
                {
                    switch (m.MemberType)
                    {
                        case MemberTypes.Field:
                            ((FieldInfo)m).SetValue(pair.Key, m.GetCustomAttribute<DomainReloadAttribute>().DefaultValue);
                            break;
                        case MemberTypes.Property:
                            ((PropertyInfo)m).SetValue(pair.Key, m.GetCustomAttribute<DomainReloadAttribute>().DefaultValue);
                            break;
                    }
                }
            }
        }

        private static IEnumerable<MemberInfo> GetFields(Type t)
        {
            if (t.BaseType == null)
                return t.GetFields(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);

            return t.GetFields(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public).Concat(GetFields(t.BaseType));
        }

        private static IEnumerable<MemberInfo> GetProperties(Type t)
        {
            if (t.BaseType == null)
                return t.GetProperties(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);

            return t.GetProperties(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public).Concat(GetProperties(t.BaseType));
        }
    }
}
