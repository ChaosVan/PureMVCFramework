using PureMVCFramework.Entity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PureMVCFramework.Editor
{
    public static class ECSMenus
    {
        [MenuItem("Tools/ECS/FindDuplicateHash")]
        [RuntimeInitializeOnLoadMethod()]
        public static void FindDuplicateHash()
        {
            var types = System.AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes().Where(t => t.GetInterfaces().Contains(typeof(IComponent)))).ToArray();

            var typeList = new List<long>();

            foreach (var item in types)
            {
                long hash = Entity.Entity.StringToHash(item.FullName);
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

        [MenuItem("Tools/ECS/GenerateEntityComponentsHash")]
        public static void GenerateEntityComponentsHash()
        {
            var cf = new CodeFormatter();

            var namespaceFmt = new CodeFormatter.NAMESPACE { name = "Game" };
            var classFmt = new CodeFormatter.CLASS { name = "EntityComponentsHash", scope = "public", keyword = "static" };

            var types = System.AppDomain.CurrentDomain.GetAssemblies().Where(t => t.GetName().Name != "Adapters")
                .SelectMany(a => a.GetTypes().Where(t => t.GetInterfaces().Contains(typeof(IComponent)))).ToArray();

            foreach (var item in types)
            {
                classFmt.AppendFormat(new CodeFormatter.FIELD
                {
                    name = item.FullName.Replace(".", "_") + "Hash",
                    scope = "public",
                    keyword = "const",
                    typeName = "long",
                    value = Entity.Entity.StringToHash(item.FullName).ToString(),
                });
            }

            namespaceFmt.AppendFormat(classFmt);

            cf.Append(namespaceFmt);

            var steam = FileUtils.CreateFile("Assets/Scripts/Game/EntityComponentsHash.cs");
            using (StreamWriter writer = new StreamWriter(steam))
            {
                writer.Write(cf.ToString());
                writer.Flush();
            }

            AssetDatabase.Refresh();
        }
    }
}
