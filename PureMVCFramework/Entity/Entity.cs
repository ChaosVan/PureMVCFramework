using System.Collections.Generic;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

#if UNITY_EDITOR
using System.Linq;
using System.Text;
using System.IO;
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
        internal readonly Dictionary<int, IComponent> components = new Dictionary<int, IComponent>();

        public static int StringToHash(string str)
        {
            int hashcode = 0;
            if (!string.IsNullOrEmpty(str))
            {
                for (int i = 0; i < str.Length; ++i)
                {
                    hashcode = hashcode * 31 + str[i];
                }
            }

            return hashcode;
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Tools/ECS/FindDuplicateHash")]
        [RuntimeInitializeOnLoadMethod()]
        public static void FindDuplicateHash()
        {
            var types = System.AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes().Where(t => t.GetInterfaces().Contains(typeof(IComponent)))).ToArray();

            var typeList = new List<int>();

            foreach (var item in types)
            {
                int hash = StringToHash(item.FullName);
                if (!typeList.Contains(hash))
                {
                    typeList.Add(hash);
                }
                else
                {
                    var typeIndex = typeList.IndexOf(hash);
                    throw new System.Exception($"The component stringID inherited from IComponent is duplicated, " +
                        $"{types[typeIndex].FullName} AND {item.FullName}");
                }
            }
        }

        [UnityEditor.MenuItem("Tools/ECS/GenerateEntityComponentsHash")]
        public static void GenerateEntityComponentsHash()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("public static class EntityComponentsHash\n");
            sb.Append("{\n");
            
            var types = System.AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes().Where(t => t.GetInterfaces().Contains(typeof(IComponent)))).ToArray();
            foreach (var item in types)
            {
                int hash = StringToHash(item.FullName);
                sb.AppendFormat("\tpublic const int {0}Hash = {1};\n", item.FullName.Replace(".", "_"), hash);
            }


            sb.Append("}\n");
            var steam = FileUtils.CreateFile("Assets/Scripts/EntityComponentsHash.cs");

            using (StreamWriter writer = new StreamWriter(steam))
            {
                writer.Write(sb.ToString());
                writer.Flush();
            }
        }
#endif
    }

}
