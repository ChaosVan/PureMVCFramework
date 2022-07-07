using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class CodeFormatter : IFormatProvider, ICustomFormatter
{
    public interface ICodeNestor
    {
        public List<object> Formatables { get; set; }
        void AppendFormat(object obj);
    }

    public interface INestableFormatter
    {
        public string Indent { get; }
        public ICodeNestor Nestor { get; set; }
    }

    public abstract class CodeNestor : ICodeNestor
    {
        public List<object> Formatables { get; set; } = new List<object>();

        public void AppendFormat(object obj)
        {
            Formatables.Add(obj);

            if (obj is INestableFormatter format)
                format.Nestor = this;
        }
    }

    public class USING : IFormattable, INestableFormatter
    {
        public string namespaces;
        public ICodeNestor Nestor { get; set; }
        public string Indent { get; }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            StringBuilder sb = new StringBuilder();

            string[] array = namespaces.Split(';');
            if (array.Length > 0)
            {
                List<string> list = new List<string>();
                list.AddRange(array);
                list.Sort(string.Compare);

                for (int i = 0; i < list.Count; ++i)
                    sb.AppendLine($"using {list[i]};");
            }

            return sb.ToString();
        }
    }

    public class MACRO_DEFINE : CodeNestor, IFormattable, INestableFormatter
    {
        public string name;
        public ICodeNestor Nestor { get; set; }
        public string Indent
        {
            get
            {
                if (Nestor != null)
                {
                    if (Nestor is INestableFormatter c)
                        return c.Indent;

                    return string.Empty;
                }

                return string.Empty;
            }
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("#if " + name);

            if (Formatables.Count > 0)
            {
                foreach (var f in Formatables)
                {
                    sb.AppendFormat(formatProvider, "{0}", f);
                }
            }
            sb.AppendLine("#endif");

            return sb.ToString();
        }
    }

    public class MACRO_ELSE : CodeNestor, IFormattable, INestableFormatter
    {
        public ICodeNestor Nestor { get; set; }
        public string Indent
        {
            get
            {
                if (Nestor != null)
                {
                    if (Nestor is INestableFormatter c)
                        return c.Indent;

                    return string.Empty;
                }

                return string.Empty;
            }
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("#else ");

            if (Formatables.Count > 0)
            {
                foreach (var f in Formatables)
                {
                    sb.AppendFormat(formatProvider, "{0}", f);
                }
            }

            return sb.ToString();
        }
    }

    public class MACRO_ELSEIF : CodeNestor, IFormattable, INestableFormatter
    {
        public string name;
        public ICodeNestor Nestor { get; set; }
        public string Indent
        {
            get
            {
                if (Nestor != null)
                {
                    if (Nestor is INestableFormatter c)
                        return c.Indent;

                    return string.Empty;
                }

                return string.Empty;
            }
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("#elif " + name);

            if (Formatables.Count > 0)
            {
                foreach (var f in Formatables)
                {
                    sb.AppendFormat(formatProvider, "{0}", f);
                }
            }

            return sb.ToString();
        }
    }

    public class NAMESPACE : CodeNestor, IFormattable
    {
        public string name;

        public string ToString(string format, IFormatProvider formatProvider)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"namespace {name}");
            sb.AppendLine("{");
            if (Formatables.Count > 0)
            {
                foreach (var f in Formatables)
                {
                    sb.AppendFormat(formatProvider, "{0}", f);
                }
            }
            sb.AppendLine("}");

            return sb.ToString();
        }
    }

    public class ENUM : IFormattable, INestableFormatter
    {
        public string name;
        public string scope;
        public string values;
        public ICodeNestor Nestor { get; set; }
        public string Indent
        {
            get
            {
                if (Nestor != null)
                {
                    if (Nestor is INestableFormatter c)
                        return c.Indent + "\t";

                    return "\t";
                }

                return string.Empty;
            }
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(Indent);
            if (!string.IsNullOrEmpty(scope))
                sb.Append(scope).Append(" ");

            sb.Append("enum ").AppendLine(name);

            sb.Append(Indent).AppendLine("{");
            string[] array = values.Split(';');
            if (array.Length > 0)
            {
                for (int i = 0; i < array.Length; ++i)
                    sb.Append(Indent).AppendLine($"\t{array[i]},");
            }
            sb.Append(Indent).AppendLine("}");

            return sb.ToString();
        }
    }

    public class CLASS : CodeNestor, IFormattable, INestableFormatter
    {
        public string name;
        public string scope;
        public string keyword;
        public string inherits;
        public int genericCount;
        public string genericInherits;
        public ICodeNestor Nestor { get; set; }
        public string Indent
        {
            get
            {
                if (Nestor != null)
                {
                    if (Nestor is INestableFormatter c)
                        return c.Indent + "\t";

                    return "\t";
                }

                return string.Empty;
            }
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(Indent);
            if (!string.IsNullOrEmpty(scope))
                sb.Append(scope).Append(" ");

            if (!string.IsNullOrEmpty(keyword))
                sb.Append(keyword).Append(" ");

            // class name
            sb.Append("class ").Append(name);
            if (genericCount > 0)
            {
                List<string> t = new List<string>();
                for (int i = 1; i <= genericCount; ++i)
                {
                    t.Add("T" + i);
                }
                sb.Append($"<{string.Join(", ", t)}>");
            }

            if (!string.IsNullOrEmpty(inherits))
            {
                string[] array = inherits.Split(';');
                if (array.Length > 0)
                    sb.Append($" : {string.Join(", ", array)} ");
            }

            if (genericCount > 0 && !string.IsNullOrEmpty(genericInherits))
            {
                string[] array = genericInherits.Split(';');
                if (array.Length > 0)
                {
                    for (int i = 1; i <= array.Length; ++i)
                    {
                        sb.Append($"where T{i} : {array[i - 1]} ");
                    }
                }
            }

            sb.AppendLine("");
            sb.Append(Indent).AppendLine("{");
            if (Formatables.Count > 0)
            {
                foreach (var f in Formatables)
                {
                    sb.AppendFormat(formatProvider, "{0}", f);
                }
            }
            sb.Append(Indent).AppendLine("}");

            return sb.ToString();
        }
    }

    public class ATTRIBUTE : IFormattable, INestableFormatter
    {
        public string tags;
        public ICodeNestor Nestor { get; set; }
        public string Indent
        {
            get
            {
                if (Nestor != null)
                {
                    if (Nestor is INestableFormatter c)
                        return c.Indent + "\t";

                    return "\t";
                }

                return string.Empty;
            }
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            StringBuilder sb = new StringBuilder();

            string[] array = tags.Split(';');
            sb.Append(Indent).AppendLine($"[{string.Join(", ", array)}]");

            return sb.ToString();
        }
    }

    public class FIELD : IFormattable, INestableFormatter
    {
        public string typeName;
        public string name;
        public string scope;
        public string keyword;
        public string value;
        public ICodeNestor Nestor { get; set; }
        public string Indent
        {
            get
            {
                if (Nestor != null)
                {
                    if (Nestor is INestableFormatter c)
                        return c.Indent + "\t";

                    return "\t";
                }

                return string.Empty;
            }
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Indent);

            if (!string.IsNullOrEmpty(scope))
                sb.Append(scope).Append(" ");

            if (!string.IsNullOrEmpty(keyword))
                sb.Append(keyword).Append(" ");

            sb.Append(typeName).Append(" ").Append(name);
            if (!string.IsNullOrEmpty(value))
                sb.Append(" = ").Append(value).Append(";");
            else
                sb.Append(";");
            sb.Append("\n");

            return sb.ToString();
        }
    }

    public class PROPERTY : IFormattable, INestableFormatter
    {
        public string typeName;
        public string name;
        public string keyword;

        private PROPERTY_GET get;
        private PROPERTY_SET set;

        public ICodeNestor Nestor { get; set; }
        public string Indent
        {
            get
            {
                if (Nestor != null)
                {
                    if (Nestor is INestableFormatter c)
                        return c.Indent + "\t";

                    return "\t";
                }

                return string.Empty;
            }
        }

        public bool Inline => (get == null || get.Inline) && (set == null || set.Inline);

        public void AppendGet(PROPERTY_GET fmt)
        {
            get = fmt;
            get.property = this;
        }

        public void AppendGet(PROPERTY_GET fmt, params INestableFormatter[] statements)
        {
            AppendGet(fmt);
            foreach (var statement in statements)
            {
                fmt.AppendFormat(statement);
            }
        }

        public void AppendSet(PROPERTY_SET fmt)
        {
            set = fmt;
            set.property = this;
        }

        public void AppendSet(PROPERTY_SET fmt, params INestableFormatter[] statements)
        {
            AppendSet(fmt);
            foreach (var statement in statements)
            {
                fmt.AppendFormat(statement);
            }
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(Indent).Append("public").Append(" ");

            if (!string.IsNullOrEmpty(keyword))
                sb.Append(keyword).Append(" ");

            sb.Append(typeName).Append(" ");

            string head = name.Substring(0, 1);
            name = head.ToUpper() + name.Substring(1);
            sb.Append(name);

            if (Inline)
            {
                if (get != null && set != null)
                    sb.AppendFormat(formatProvider, " {{ {0:INLINE} {1:INLINE} }}\n", get, set);
                else if (get != null)
                    sb.AppendFormat(formatProvider, "{0:INLINE_GET}\n", get);
                else if (set != null)
                {
                    sb.AppendFormat(formatProvider, " {{ {0:INLINE} }}\n", set);
                    if (set.isPrivate)
                        Debug.LogError("仅当同时具有get和set访问器时，才能使用修饰符");
                    else if (set.Formatables.Count == 0)
                        Debug.LogError("自动实现的属性必须具有get访问器");
                }
            }
            else
            {
                sb.AppendLine("");
                sb.Append(Indent).AppendLine("{");
                if (get != null)
                    sb.AppendFormat(formatProvider, "{0}", get);
                if (set != null)
                {
                    sb.AppendFormat(formatProvider, "{0}", set);
                    if (get == null && set.isPrivate)
                        Debug.LogError("仅当同时具有get和set访问器时，才能使用修饰符");
                    else if (get == null && set.Formatables.Count == 0)
                        Debug.LogError("自动实现的属性必须具有get访问器");
                }
                sb.Append(Indent).AppendLine("}");
            }

            return sb.ToString();
        }
    }

    public class PROPERTY_GET : CodeNestor, IFormattable, INestableFormatter
    {
        public bool inline;
        public PROPERTY property;

        public ICodeNestor Nestor { get; set; }

        public string Indent
        {
            get
            {
                if (property != null)
                {
                    return property.Indent + "\t";
                }

                return string.Empty;
            }
        }

        public bool Inline => inline && Formatables.Count <= 1;

        public string ToString(string format, IFormatProvider formatProvider)
        {
            StringBuilder sb = new StringBuilder();

            if (Formatables.Count > 0)
            {
                if (Inline)
                {
                    if (format == "INLINE")
                        sb.AppendFormat(formatProvider, "get => {0:INLINE}", Formatables[0]);
                    else if (format == "INLINE_GET")
                        sb.AppendFormat(formatProvider, " => {0:INLINE}", Formatables[0]);
                    else
                        sb.Append(Indent).AppendFormat(formatProvider, "get => {0:INLINE}", Formatables[0]);
                }
                else
                {
                    sb.Append(Indent).AppendLine("get");
                    sb.Append(Indent).AppendLine("{");
                    foreach (var f in Formatables)
                    {
                        sb.AppendFormat(formatProvider, "{0}", f);
                    }
                    sb.Append(Indent).AppendLine("}");
                }
            }
            else
            {
                if (inline)
                {
                    if (format == "INLINE")
                        sb.Append("get;");
                    else
                        sb.Append(Indent).Append("get;");
                }
                else
                {
                    sb.Append(Indent).AppendLine("get;");
                }
            }

            return sb.ToString();
        }
    }

    public class PROPERTY_SET : CodeNestor, IFormattable, INestableFormatter
    {
        public bool inline;
        public bool isPrivate;
        public PROPERTY property;

        public ICodeNestor Nestor { get; set; }
        public string Indent
        {
            get
            {
                if (property != null)
                {
                    return property.Indent + "\t";
                }

                return string.Empty;
            }
        }

        public bool Inline => inline && Formatables.Count <= 1;

        public string ToString(string format, IFormatProvider formatProvider)
        {
            StringBuilder sb = new StringBuilder();

            if (Formatables.Count > 0)
            {
                if (Inline)
                {
                    if (format != "INLINE")
                        sb.Append(Indent);

                    if (isPrivate)
                        sb.Append("private ");

                    sb.AppendFormat(formatProvider, "set => {0:INLINE}", Formatables[0]);
                }
                else
                {
                    sb.Append(Indent);
                    if (isPrivate)
                        sb.Append("private ");
                    sb.AppendLine("set");
                    sb.Append(Indent).AppendLine("{");
                    foreach (var f in Formatables)
                    {
                        sb.AppendFormat(formatProvider, "{0}", f);
                    }
                    sb.Append(Indent).AppendLine("}");
                }
            }
            else
            {
                if (inline)
                {
                    if (format != "INLINE")
                        sb.Append(Indent);

                    if (isPrivate)
                        sb.Append("private ");

                    sb.Append("set;");
                }
                else
                {
                    sb.Append(Indent);
                    if (isPrivate)
                        sb.Append("private ");
                    sb.AppendLine("set;");
                }
            }

            return sb.ToString();
        }
    }

    public class FUNC : CodeNestor, IFormattable, INestableFormatter
    {
        public string name;
        public string scope;
        public string keyword;
        public string returnVal;
        public string args;
        public ICodeNestor Nestor { get; set; }
        public string Indent
        {
            get
            {
                if (Nestor != null)
                {
                    if (Nestor is INestableFormatter c)
                        return c.Indent + "\t";

                    return "\t";
                }

                return string.Empty;
            }
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(Indent);
            if (!string.IsNullOrEmpty(scope))
                sb.Append(scope).Append(" ");

            if (!string.IsNullOrEmpty(keyword))
                sb.Append(keyword).Append(" ");

            if (!string.IsNullOrEmpty(returnVal))
                sb.Append(returnVal).Append(" ");

            if (!string.IsNullOrEmpty(args))
            {
                string[] array = args.Split(';');
                sb.Append($"{name}({string.Join(", ", array)})");
            }
            else
                sb.Append($"{name}()");

            if (keyword == "abstract")
            {
                sb.AppendLine(";");
            }
            else if (Formatables.Count > 0)
            {
                sb.Append("\n");
                sb.Append(Indent).AppendLine("{");
                foreach (var f in Formatables)
                {
                    sb.AppendFormat(formatProvider, "{0}", f);
                }
                sb.Append(Indent).AppendLine("}");
            }
            else
            {
                sb.AppendLine(" { }");
            }

            return sb.ToString();
        }
    }

    public class STATEMENT : IFormattable, INestableFormatter
    {
        public string content;
        public ICodeNestor Nestor { get; set; }
        public string Indent
        {
            get
            {
                if (Nestor != null)
                {
                    if (Nestor is INestableFormatter c)
                        return c.Indent + "\t";

                    return "\t";
                }

                return string.Empty;
            }
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            StringBuilder sb = new StringBuilder();

            if (format == "INLINE")
            {
                sb.Append(content);
            }
            else
            {
                sb.Append(Indent).AppendLine(content);
            }

            return sb.ToString();
        }
    }

    public class STATEMENT_IF : CodeNestor, IFormattable, INestableFormatter
    {
        public bool inline;
        public string conditions;
        public ICodeNestor Nestor { get; set; }
        public string Indent
        {
            get
            {
                if (Nestor != null)
                {
                    if (Nestor is INestableFormatter c)
                        return c.Indent + "\t";

                    return "\t";
                }

                return string.Empty;
            }
        }

        public bool Inline => inline && Formatables.Count <= 1;

        public string ToString(string format, IFormatProvider formatProvider)
        {
            StringBuilder sb = new StringBuilder();

            if (Inline)
            {
                if (Formatables.Count == 1)
                    sb.Append(Indent).AppendLine($"if ({conditions}) {{ {string.Format(formatProvider, "{0:INLINE}", Formatables[0])} }}");
                else
                    sb.Append(Indent).AppendLine($"if ({conditions}) {{ }}");
            }
            else
            {
                sb.Append(Indent).AppendLine($"if ({conditions})");
                sb.Append(Indent).AppendLine("{");
                foreach (var f in Formatables)
                {
                    sb.AppendFormat(formatProvider, "{0}", f);
                }
                sb.Append(Indent).AppendLine("}");
            }

            return sb.ToString();
        }
    }

    public class STATEMENT_ELSEIF : CodeNestor, IFormattable, INestableFormatter
    {
        public bool inline;
        public string conditions;
        public ICodeNestor Nestor { get; set; }
        public string Indent
        {
            get
            {
                if (Nestor != null)
                {
                    if (Nestor is INestableFormatter c)
                        return c.Indent + "\t";

                    return "\t";
                }

                return string.Empty;
            }
        }

        public bool Inline => inline && Formatables.Count <= 1;

        public string ToString(string format, IFormatProvider formatProvider)
        {
            StringBuilder sb = new StringBuilder();

            if (Inline)
            {
                if (Formatables.Count == 1)
                    sb.Append(Indent).AppendLine($"else if ({conditions}) {{ {string.Format(formatProvider, "{0:INLINE}", Formatables[0])} }}");
                else
                    sb.Append(Indent).AppendLine($"else if ({conditions}) {{ }}");
            }
            else
            {
                sb.Append(Indent).AppendLine($"else if ({conditions})");
                sb.Append(Indent).AppendLine("{");
                foreach (var f in Formatables)
                {
                    sb.AppendFormat(formatProvider, "{0}", f);
                }
                sb.Append(Indent).AppendLine("}");
            }

            return sb.ToString();
        }
    }

    public class STATEMENT_ELSE : CodeNestor, IFormattable, INestableFormatter
    {
        public bool inline;
        public ICodeNestor Nestor { get; set; }
        public string Indent
        {
            get
            {
                if (Nestor != null)
                {
                    if (Nestor is INestableFormatter c)
                        return c.Indent + "\t";

                    return "\t";
                }

                return string.Empty;
            }
        }

        public bool Inline => inline && Formatables.Count <= 1;

        public string ToString(string format, IFormatProvider formatProvider)
        {
            StringBuilder sb = new StringBuilder();
            if (Inline)
            {
                if (Formatables.Count == 1)
                    sb.Append(Indent).AppendLine($"else {{ {string.Format(formatProvider, "{0:INLINE}", Formatables[0])} }}");
                else
                    sb.Append(Indent).AppendLine("else { }");
            }
            else
            {
                sb.Append(Indent).AppendLine("else");
                sb.Append(Indent).AppendLine("{");
                foreach (var f in Formatables)
                {
                    sb.AppendFormat(formatProvider, "{0}", f);
                }
                sb.Append(Indent).AppendLine("}");
            }

            return sb.ToString();
        }
    }

    public class STATEMENT_FOR : CodeNestor, IFormattable, INestableFormatter
    {
        public string conditions;
        public ICodeNestor Nestor { get; set; }
        public string Indent
        {
            get
            {
                if (Nestor != null)
                {
                    if (Nestor is INestableFormatter c)
                        return c.Indent + "\t";

                    return "\t";
                }

                return string.Empty;
            }
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(Indent).AppendLine($"for ({conditions})");
            sb.Append(Indent).AppendLine("{");
            foreach (var f in Formatables)
            {
                sb.AppendFormat(formatProvider, "{0}", f);
            }
            sb.Append(Indent).AppendLine("}");

            return sb.ToString();
        }
    }


    private StringBuilder builder;

    private void CheckBuilder()
    {
        if (builder == null)
        {
            builder = new StringBuilder();
            builder.AppendLine("////////////////////////////////");
            builder.AppendLine("// Generated by CodeFormatter v1.0");
            builder.AppendLine($"// {DateTime.Now:yyyy-MM-dd, HH:mm:ss}");
            builder.AppendLine("////////////////////////////////");
        }
    }

    public void Append(object arg0)
    {
        CheckBuilder();
        builder.AppendFormat(this, "{0}", arg0);
    }

    public void AppendLine(object arg0)
    {
        CheckBuilder();
        builder.AppendFormat(this, "{0}\n", arg0);
    }

    public override string ToString()
    {
        return builder.ToString(); ;
    }

    public string Format(string format, object arg, IFormatProvider formatProvider)
    {
        if (arg is IFormattable f)
        {
            return f.ToString(format, formatProvider);
        }
        else
        {
            if (string.IsNullOrEmpty(format))
            {
                return arg.ToString();
            }
            else
            {
                return arg.ToString();
            }
        }
    }

    public object GetFormat(Type formatType)
    {
        if (formatType == typeof(ICustomFormatter))
            return this;

        return null;
    }
}
