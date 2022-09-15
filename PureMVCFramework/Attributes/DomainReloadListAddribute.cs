using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace PureMVCFramework
{
    [AttributeUsage(AttributeTargets.Field)]
    public class DomainReloadListAttribute : Attribute
    {
        private string assemblyName;

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
            var assemblies = new string[] { "Assembly-CSharp", "PureMVCFramework" };
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
