using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace PureMVCFramework.Editor
{
    public static class GenSystemBase
    {
        [MenuItem("Tools/ECS/Generate/SystemBase")]
        public static void GenerateSystemBase()
        {
            CodeFormatter cf = new CodeFormatter();

            var def = new CodeFormatter.MACRO_DEFINE { name = "ODIN_INSPECTOR" };
            def.AppendFormat(new CodeFormatter.USING { namespaces = "Sirenix.OdinInspector" });
            cf.AppendLine(def);

            cf.Append(new CodeFormatter.USING { namespaces = "System.Collections.Generic" });

            var namespaceFmt = new CodeFormatter.NAMESPACE { name = "PureMVCFramework.Entity" };
            for (int i = 0; i < 6; ++i)
            {
                namespaceFmt.AppendFormat(GenerateSystemBase(i + 1));
            }

            cf.Append(namespaceFmt);

            var steam = FileUtils.CreateFile("Assets/PureMVCFramework/PureMVCFramework/Entity/SystemBase.cs");

            using (StreamWriter writer = new StreamWriter(steam))
            {
                writer.Write(cf.ToString());
                writer.Flush();
            }

            AssetDatabase.Refresh();
        }

        private static CodeFormatter.CLASS GenerateSystemBase(int componentCount)
        {
            var classFmt = new CodeFormatter.CLASS
            {
                name = "SystemBase",
                scope = "public",
                keyword = "abstract",
                inherits = "ComponentSystemBase",
                genericCount = componentCount,
            };

            List<string> list = new List<string>();
            for (int i = 1; i <= componentCount; ++i)
                list.Add("IComponentData");
            classFmt.genericInherits = string.Join(";", list);

            for (int i = 1; i <= componentCount; ++i)
            {
                var def = new CodeFormatter.MACRO_DEFINE { name = "ODIN_INSPECTOR" };
                def.AppendFormat(new CodeFormatter.ATTRIBUTE
                {
                    tags = "ShowIf(\"showOdinInfo\");ShowInInspector;ListDrawerSettings(IsReadOnly = true)",
                });
                classFmt.AppendFormat(def);

                classFmt.AppendFormat(new CodeFormatter.FIELD
                {
                    typeName = "List<T" + i + ">",
                    name = "Components" + i,
                    scope = "private",
                    keyword = "readonly",
                    value = "new List<T" + i + ">()",
                });
            }

            for (int i = 1; i <= componentCount; ++i)
            {
                classFmt.AppendFormat(new CodeFormatter.FIELD
                {
                    typeName = "long",
                    name = "hash" + i,
                    scope = "private",
                });
            }

            classFmt.AppendFormat(GenerateOnCreate(componentCount));
            classFmt.AppendFormat(GenerateOnDestroy(componentCount));
            classFmt.AppendFormat(GenerateInjectEntity(componentCount));
            classFmt.AppendFormat(GenerateUpdate(componentCount));
            classFmt.AppendFormat(GenerateOnUpdate(componentCount));

            return classFmt;
        }

        private static CodeFormatter.FUNC GenerateOnCreate(int componentCount)
        {
            var func = new CodeFormatter.FUNC
            {
                name = "OnCreate",
                scope = "protected",
                keyword = "override",
                returnVal = "void",
            };

            func.AppendFormat(new CodeFormatter.STATEMENT { content = "base.OnCreate();" });
            for (int i = 1; i <= componentCount; ++i)
            {
                func.AppendFormat(new CodeFormatter.STATEMENT { content = $"hash{i} = Entity.StringToHash(typeof(T{i}).FullName);" });
            }

            return func;
        }

        private static CodeFormatter.FUNC GenerateOnDestroy(int componentCount)
        {
            var func = new CodeFormatter.FUNC
            {
                name = "OnDestroy",
                scope = "protected",
                keyword = "override",
                returnVal = "void",
            };

            for (int i = 1; i <= componentCount; ++i)
            {
                func.AppendFormat(new CodeFormatter.STATEMENT { content = $"Components{i}.Clear();" });
            }
            func.AppendFormat(new CodeFormatter.STATEMENT { content = "base.OnDestroy();" });

            return func;
        }

        private static CodeFormatter.FUNC GenerateInjectEntity(int componentCount)
        {
            var func = new CodeFormatter.FUNC
            {
                name = "InjectEntity",
                scope = "public",
                keyword = "sealed override",
                returnVal = "void",
                args = "Entity entity",
            };

            func.AppendFormat(new CodeFormatter.STATEMENT { content = $"IComponentData[] c = new IComponentData[{componentCount}];" });
            List<string> list = new List<string>();
            for (int i = 0; i < componentCount; ++i)
                list.Add($"entity.components.TryGetValue(hash{i + 1}, out c[{i}])");
            func.AppendFormat(new CodeFormatter.STATEMENT { content = $"bool tf = {string.Join($" && ", list)};" });

            var if1 = new CodeFormatter.STATEMENT_IF { conditions = "Entities.Contains(entity)" };
            var if2 = new CodeFormatter.STATEMENT_IF { conditions = "!tf" };
            if2.AppendFormat(new CodeFormatter.STATEMENT { content = "var i = Entities.IndexOf(entity);" });
            if2.AppendFormat(new CodeFormatter.STATEMENT { content = "Entities.RemoveAt(i);" });
            for (int i = 0; i < componentCount; ++i)
                if2.AppendFormat(new CodeFormatter.STATEMENT { content = $"Components{i + 1}.RemoveAt(i);" });
            if1.AppendFormat(if2);
            func.AppendFormat(if1);

            var elif = new CodeFormatter.STATEMENT_ELSEIF { conditions = "tf" };
            elif.AppendFormat(new CodeFormatter.STATEMENT { content = "Entities.Add(entity);" });
            for (int i = 0; i < componentCount; ++i)
                elif.AppendFormat(new CodeFormatter.STATEMENT { content = $"Components{i + 1}.Add((T{i + 1})c[{i}]);" });
            func.AppendFormat(elif);

            return func;
        }

        private static CodeFormatter.FUNC GenerateUpdate(int componentCount)
        {
            var func = new CodeFormatter.FUNC
            {
                name = "Update",
                scope = "public",
                keyword = "sealed override",
                returnVal = "void",
            };

            List<string> list = new List<string>();
            for (int i = 0; i < componentCount; ++i)
                list.Add($"Components{i + 1}[i]");

            var for1 = new CodeFormatter.STATEMENT_FOR { conditions = "int i = 0; i < Entities.Count; ++i" };
            for1.AppendFormat(new CodeFormatter.STATEMENT { content = $"OnUpdate(i, Entities[i], {string.Join(", ", list)});" });
            func.AppendFormat(for1);

            return func;
        }

        private static CodeFormatter.FUNC GenerateOnUpdate(int componentCount)
        {
            var func = new CodeFormatter.FUNC
            {
                name = "OnUpdate",
                scope = "protected",
                keyword = "abstract",
                returnVal = "void",
            };
            func.args = "int index;Entity entity";
            for (int i = 1; i <= componentCount; ++i)
                func.args += $";T{i} component{i}";

            return func;
        }
    }
}
