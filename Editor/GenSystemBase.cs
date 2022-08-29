using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace PureMVCFramework.Editor
{
    public static class GenGenericSystemBaseHelper
    {
        [MenuItem("Tools/ECS/GenGenericSystemBase")]
        public static void GenGenericSystemBase()
        {
            CodeFormatter cf = new CodeFormatter();

            var def = new CodeFormatter.MACRO_DEFINE { name = "ODIN_INSPECTOR" };
            def.AppendFormat(new CodeFormatter.USING { namespaces = "Sirenix.OdinInspector" });
            cf.AppendLine(def);

            cf.Append(new CodeFormatter.USING { namespaces = "UnityEngine;System.Collections.Generic" });

            var namespaceFmt = new CodeFormatter.NAMESPACE { name = "PureMVCFramework.Entity" };
            for (int i = 0; i < 6; ++i)
            {
                namespaceFmt.AppendFormat(GenerateSystemBase(i + 1));
            }

            for (int i = 0; i < 3; ++i)
            {
                namespaceFmt.AppendFormat(GenerateHybridSystemBase(i + 2));
            }

            cf.Append(namespaceFmt);

            var steam = FileUtils.CreateFile("Assets/PureMVCFramework/PureMVCFramework/Entity/GenericSystemBase.cs");

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
                inherits = "SystemBase",
                genericCount = componentCount,
            };

            List<string> list = new List<string>();
            for (int i = 1; i <= componentCount; ++i)
                list.Add("IComponentData");
            classFmt.genericInherits = string.Join(";", list);

            for (int i = 1; i <= componentCount; ++i)
            {
                var d = new CodeFormatter.MACRO_DEFINE { name = "ODIN_INSPECTOR" };
                d.AppendFormat(new CodeFormatter.ATTRIBUTE
                {
                    tags = "ShowIf(\"showOdinInfo\");ShowInInspector;ListDrawerSettings(IsReadOnly = true)",
                });
                classFmt.AppendFormat(d);

                classFmt.AppendFormat(new CodeFormatter.FIELD
                {
                    typeName = "List<T" + i + ">",
                    name = "Components" + i,
                    scope = "private",
                    keyword = "readonly",
                    value = "new List<T" + i + ">()",
                });
            }

            var def = new CodeFormatter.MACRO_DEFINE { name = "ODIN_INSPECTOR" };
            def.AppendFormat(new CodeFormatter.ATTRIBUTE
            {
                tags = "ShowIf(\"showOdinInfo\");ShowInInspector",
            });
            classFmt.AppendFormat(def);
            classFmt.AppendFormat(new CodeFormatter.FIELD
            {
                typeName = "EntityQuery",
                name = "queries",
                scope = "protected"
            });

            //classFmt.AppendFormat(GenerateOnStopRunningInternal(componentCount));
            classFmt.AppendFormat(GenerateOnCreate(componentCount));
            classFmt.AppendFormat(GenerateInjectEntity(componentCount));
            classFmt.AppendFormat(GenerateOnUpdateOverride(componentCount));
            classFmt.AppendFormat(GenerateOnUpdate(componentCount));

            return classFmt;
        }

        private static CodeFormatter.CLASS GenerateHybridSystemBase(int componentCount)
        {
            var classFmt = new CodeFormatter.CLASS
            {
                name = "HybridSystemBase",
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
                    list.Add("IComponentData");
            }
            classFmt.genericInherits = string.Join(";", list);

            for (int i = 1; i <= componentCount; ++i)
            {
                var d = new CodeFormatter.MACRO_DEFINE { name = "ODIN_INSPECTOR" };
                d.AppendFormat(new CodeFormatter.ATTRIBUTE
                {
                    tags = "ShowIf(\"showOdinInfo\");ShowInInspector;ListDrawerSettings(IsReadOnly = true)",
                });
                classFmt.AppendFormat(d);

                classFmt.AppendFormat(new CodeFormatter.FIELD
                {
                    typeName = "List<T" + i + ">",
                    name = "Components" + i,
                    scope = "private",
                    keyword = "readonly",
                    value = "new List<T" + i + ">()",
                });
            }

            //for (int i = 2; i <= componentCount; ++i)
            //{
            //    classFmt.AppendFormat(new CodeFormatter.FIELD
            //    {
            //        typeName = "long",
            //        name = "hash" + i,
            //        scope = "private",
            //        keyword = "static",
            //        value = $"Entity.StringToHash(typeof(T{i}).FullName)"
            //    });
            //}

            var def = new CodeFormatter.MACRO_DEFINE { name = "ODIN_INSPECTOR" };
            def.AppendFormat(new CodeFormatter.ATTRIBUTE
            {
                tags = "ShowIf(\"showOdinInfo\");ShowInInspector",
            });
            classFmt.AppendFormat(def);
            classFmt.AppendFormat(new CodeFormatter.FIELD
            {
                typeName = "EntityQuery",
                name = "queries",
                scope = "protected"
            });

            //classFmt.AppendFormat(GenerateOnStopRunningInternal(componentCount));
            classFmt.AppendFormat(GenerateOnCreateHybrid(componentCount));
            classFmt.AppendFormat(GenerateInjectEntityHybrid(componentCount));
            classFmt.AppendFormat(GenerateOnUpdateOverride(componentCount));
            classFmt.AppendFormat(GenerateOnUpdate(componentCount));
            classFmt.AppendFormat(GenerateOnEject(componentCount));

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
            List<string> list = new List<string>();
            for (int i = 1; i <= componentCount; ++i)
                list.Add($"ComponentType.ReadWrite<T{i}>()");
            func.AppendFormat(new CodeFormatter.STATEMENT { content = $"queries = new EntityQuery({string.Join(", ", list)});" });

            return func;
        }

        private static CodeFormatter.FUNC GenerateOnCreateHybrid(int componentCount)
        {
            var func = new CodeFormatter.FUNC
            {
                name = "OnCreate",
                scope = "protected",
                keyword = "override",
                returnVal = "void",
            };

            func.AppendFormat(new CodeFormatter.STATEMENT { content = "base.OnCreate();" });
            List<string> list = new List<string>();
            for (int i = 2; i <= componentCount; ++i)
                list.Add($"ComponentType.ReadWrite<T{i}>()");
            func.AppendFormat(new CodeFormatter.STATEMENT { content = $"queries = new EntityQuery({string.Join(", ", list)});" });

            return func;
        }

        private static CodeFormatter.FUNC GenerateOnStopRunningInternal(int componentCount)
        {
            var func = new CodeFormatter.FUNC
            {
                name = "OnStopRunningInternal",
                scope = "internal",
                keyword = "sealed override",
                returnVal = "void",
            };

            func.AppendFormat(new CodeFormatter.STATEMENT { content = "base.OnStopRunningInternal();" });
            for (int i = 1; i <= componentCount; ++i)
            {
                func.AppendFormat(new CodeFormatter.STATEMENT { content = $"Components{i}.Clear();" });
            }

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

            func.AppendFormat(new CodeFormatter.STATEMENT { content = $"bool tf = entity.InternalGetComponentData(queries, out var components);" });

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
                elif.AppendFormat(new CodeFormatter.STATEMENT { content = $"Components{i + 1}.Add((T{i + 1})components[{i}]);" });
            func.AppendFormat(elif);

            return func;
        }

        private static CodeFormatter.FUNC GenerateInjectEntityHybrid(int componentCount)
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

            func.AppendFormat(new CodeFormatter.STATEMENT { content = "var co = entity.gameObject.GetComponent<T1>();" });
            func.AppendFormat(new CodeFormatter.STATEMENT { content = $"bool tf = entity.InternalGetComponentData(queries, out var components) && co;" });
            //func.AppendFormat(new CodeFormatter.STATEMENT { content = $"IComponentData[] c = new IComponentData[{componentCount - 1}];" });

            //List<string> list = new List<string>();
            //for (int i = 0; i < componentCount - 1; ++i)
            //    list.Add($"entity.components.TryGetValue(hash{i + 2}, out c[{i}])");

            //func.AppendFormat(new CodeFormatter.STATEMENT { content = $"bool tf = co && {string.Join(" && ", list)};" });
            //list.Clear();

            var if3 = new CodeFormatter.STATEMENT_IF { conditions = "Entities.Contains(entity)" };
            var if4 = new CodeFormatter.STATEMENT_IF { conditions = "!tf" };
            if4.AppendFormat(new CodeFormatter.STATEMENT { content = "var i = Entities.IndexOf(entity);" });
            if4.AppendFormat(new CodeFormatter.STATEMENT { content = "Entities.RemoveAt(i);" });
            for (int i = 0; i < componentCount; ++i)
                if4.AppendFormat(new CodeFormatter.STATEMENT { content = $"Components{i + 1}.RemoveAt(i);" });
            if4.AppendFormat(new CodeFormatter.STATEMENT { content = "OnEject(entity, co);" });
            if3.AppendFormat(if4);
            func.AppendFormat(if3);

            var elif = new CodeFormatter.STATEMENT_ELSEIF { conditions = "tf" };
            elif.AppendFormat(new CodeFormatter.STATEMENT { content = "Entities.Add(entity);" });
            elif.AppendFormat(new CodeFormatter.STATEMENT { content = "Components1.Add(co);" });
            for (int i = 0; i < componentCount - 1; ++i)
                elif.AppendFormat(new CodeFormatter.STATEMENT { content = $"Components{i + 2}.Add((T{i + 2})components[{i}]);" });
            func.AppendFormat(elif);

            return func;
        }

        private static CodeFormatter.FUNC GenerateOnUpdateOverride(int componentCount)
        {
            var func = new CodeFormatter.FUNC
            {
                name = "OnUpdate",
                scope = "protected",
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
