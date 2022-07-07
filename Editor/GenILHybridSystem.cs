using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace PureMVCFramework.Editor
{
    public static class GenILHybridSystem
    {
        [MenuItem("Tools/ECS/Generate/ILHybridSystem")]
        public static void GenerateILHybridSystem()
        {
            CodeFormatter cf = new CodeFormatter();

            cf.AppendLine(new CodeFormatter.USING { namespaces = "PureMVCFramework.Entity;System.Collections.Generic;UnityEngine" });

            var namespaceFmt = new CodeFormatter.NAMESPACE { name = "GameIL.ECS" };
            for (int i = 0; i < 2; ++i)
            {
                namespaceFmt.AppendFormat(GenerateILHybridSystem(i + 2));
            }

            cf.Append(namespaceFmt);

            var steam = FileUtils.CreateFile("Assets/../GameIL/ECS/ILHybridSystem.cs");

            using (StreamWriter writer = new StreamWriter(steam))
            {
                writer.Write(cf.ToString());
                writer.Flush();
            }

            AssetDatabase.Refresh();
        }

        private static CodeFormatter.CLASS GenerateILHybridSystem(int componentCount)
        {
            var classFmt = new CodeFormatter.CLASS
            {
                name = "ILHybridSystem",
                scope = "public",
                keyword = "abstract",
                inherits = "SystemBase",
                genericCount = componentCount,
            };

            List<string> list = new List<string>();
            for (int i = 1; i <= componentCount; ++i)
            {
                if (i == 1)
                    list.Add("Component");
                else
                    list.Add("IComponent");
            }
            classFmt.genericInherits = string.Join(";", list);

            for (int i = 1; i <= componentCount; ++i)
            {
                classFmt.AppendFormat(new CodeFormatter.ATTRIBUTE
                {
                    tags = "SerializeField",
                });
                classFmt.AppendFormat(new CodeFormatter.FIELD
                {
                    typeName = "List<T" + i + ">",
                    name = "Components" + i,
                    scope = "private",
                    keyword = "readonly",
                    value = "new List<T" + i + ">()",
                });
            }

            for (int i = 2; i <= componentCount; ++i)
            {
                classFmt.AppendFormat(new CodeFormatter.FIELD
                {
                    typeName = "long",
                    name = "hash" + i,
                    scope = "private",
                });
            }

            classFmt.AppendFormat(GenerateOnInitialized(componentCount));
            classFmt.AppendFormat(GenerateOnRecycle(componentCount));
            classFmt.AppendFormat(GenerateInjectEntity(componentCount));
            classFmt.AppendFormat(GenerateUpdate(componentCount));
            classFmt.AppendFormat(GenerateOnUpdate(componentCount));
            classFmt.AppendFormat(GenerateOnEject(componentCount));

            return classFmt;
        }

        private static CodeFormatter.FUNC GenerateOnInitialized(int componentCount)
        {
            var func = new CodeFormatter.FUNC
            {
                name = "OnInitialized",
                args = "params object[] args",
                scope = "public",
                keyword = "override",
                returnVal = "void",
            };

            func.AppendFormat(new CodeFormatter.STATEMENT { content = "base.OnInitialized(args);" });
            for (int i = 2; i <= componentCount; ++i)
            {
                func.AppendFormat(new CodeFormatter.STATEMENT { content = $"hash{i} = Entity.StringToHash(typeof(T{i}).FullName);" });
            }

            return func;
        }

        private static CodeFormatter.FUNC GenerateOnRecycle(int componentCount)
        {
            var func = new CodeFormatter.FUNC
            {
                name = "OnRecycle",
                scope = "public",
                keyword = "override",
                returnVal = "void",
            };

            for (int i = 1; i <= componentCount; ++i)
            {
                func.AppendFormat(new CodeFormatter.STATEMENT { content = $"Components{i}.Clear();" });
            }
            func.AppendFormat(new CodeFormatter.STATEMENT { content = "base.OnRecycle();" });

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

            var if1 = new CodeFormatter.STATEMENT_IF { conditions = "entity.gameObject == null" };
            var if2 = new CodeFormatter.STATEMENT_IF { conditions = "Entities.Contains(entity)" };
            if2.AppendFormat(new CodeFormatter.STATEMENT { content = "var i = Entities.IndexOf(entity);" });
            if2.AppendFormat(new CodeFormatter.STATEMENT { content = "Entities.RemoveAt(i);" });
            for (int i = 0; i < componentCount; ++i)
                if2.AppendFormat(new CodeFormatter.STATEMENT { content = $"Components{i + 1}.RemoveAt(i);" });
            if1.AppendFormat(if2);
            if1.AppendFormat(new CodeFormatter.STATEMENT { content = "return;" });

            func.AppendFormat(if1);

            func.AppendFormat(new CodeFormatter.STATEMENT { content = "var c1 = EntityManager.Instance.GetComponentObject<T1>(entity);" });
            for (int i = 2; i <= componentCount; ++i)
            {
                func.AppendFormat(new CodeFormatter.STATEMENT { content = $"var c{i} = (T{i})EntityManager.Instance.GetComponentData(entity, hash{i});" });
            }

            List<string> list = new List<string>();
            for (int i = 1; i <= componentCount; ++i)
            {
                list.Add($"c{i} != null");
            }
            func.AppendFormat(new CodeFormatter.STATEMENT { content = $"bool tf = {string.Join(" && ", list)};" });
            list.Clear();

            var if3 = new CodeFormatter.STATEMENT_IF { conditions = "Entities.Contains(entity)" };
            var if4 = new CodeFormatter.STATEMENT_IF { conditions = "!tf" };
            if4.AppendFormat(new CodeFormatter.STATEMENT { content = "var i = Entities.IndexOf(entity);" });
            if4.AppendFormat(new CodeFormatter.STATEMENT { content = "Entities.RemoveAt(i);" });
            for (int i = 0; i < componentCount; ++i)
                if4.AppendFormat(new CodeFormatter.STATEMENT { content = $"Components{i + 1}.RemoveAt(i);" });
            if4.AppendFormat(new CodeFormatter.STATEMENT { content = "OnEject(entity, c1);" });
            if3.AppendFormat(if4);
            func.AppendFormat(if3);

            var elif = new CodeFormatter.STATEMENT_ELSEIF { conditions = "tf" };
            elif.AppendFormat(new CodeFormatter.STATEMENT { content = "Entities.Add(entity);" });
            for (int i = 0; i < componentCount; ++i)
                elif.AppendFormat(new CodeFormatter.STATEMENT { content = $"Components{i + 1}.Add(c{i + 1});" });
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

        private static CodeFormatter.FUNC GenerateOnEject(int componentCount)
        {
            var func = new CodeFormatter.FUNC
            {
                name = "OnEject",
                scope = "protected",
                keyword = "virtual",
                returnVal = "void",
            };
            func.args = "Entity entity;T1 component";

            return func;
        }
    }
}