using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace PureMVCFramework
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class DomainReloadAttribute : Attribute
    {
        public object defaultValue;

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

            var assemblies = new string[] { "Assembly-CSharp", "PureMVCFramework", "Entity" };
            foreach (var assemblyName in assemblies)
            {
                var assembly = Assembly.Load(assemblyName);

                List<MemberInfo> list = new List<MemberInfo>();
                foreach (var t in assembly.GetTypes())
                {
                    if (t.IsGenericType)
                        continue;

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

    [AttributeUsage(AttributeTargets.Field)]
    public class DomainReloadListAttribute : Attribute
    {
        public string assemblyName;

        public DomainReloadListAttribute(string assemblyName = "Assembly-CSharp")
        {
            this.assemblyName = assemblyName;
        }

        public string AssemblyName => assemblyName;

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
        public static void Run()
        {
            var assemblies = new string[] { "Assembly-CSharp", "PureMVCFramework", "Entity" };
            List<FieldInfo> list = new List<FieldInfo>();
            foreach (var assemblyName in assemblies)
            {
                var assembly = Assembly.Load(assemblyName);
                foreach (var t in assembly.GetTypes())
                {
                    list.AddRange(t.GetFields()
                        .Where(m => m.GetCustomAttribute<DomainReloadListAttribute>() != null));
                }
            }

            if (list.Count > 0)
            {
                foreach (var field in list)
                {
                    var attribute = field.GetCustomAttribute<DomainReloadListAttribute>();
                    foreach (var obj in field.GetValue(field.Name) as Dictionary<string, object>)
                    {
                        try
                        {
                            var assembly = Assembly.Load(attribute.AssemblyName);
                            var typeName = obj.Key.Substring(0, obj.Key.LastIndexOf("."));
                            var memberName = obj.Key.Substring(obj.Key.LastIndexOf(".") + 1);

                            var type = assembly.GetType(typeName);
                            if (type != null)
                            {
                                var members = GetMember(assembly.GetType(typeName), memberName);

                                foreach (var m in members)
                                {
                                    switch (m.MemberType)
                                    {
                                        case MemberTypes.Field:
                                            ((FieldInfo)m).SetValue(assembly, obj.Value);
                                            break;
                                        case MemberTypes.Property:
                                            ((PropertyInfo)m).SetValue(assembly, obj.Value);
                                            break;
                                    }
                                }
                            }
                            else
                            {
                                throw new Exception(typeName + " is null");
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e.StackTrace);
                        }
                    }
                }
            }
        }

        private static IEnumerable<MemberInfo> GetMember(Type type, string memberName)
        {
            if (type.BaseType == null)
                return type.GetMember(memberName, BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);

            return type.GetMember(memberName, BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public).Concat(GetMember(type.BaseType, memberName));
        }
    }
}
