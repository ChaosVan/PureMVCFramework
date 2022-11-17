using System;
using System.Text;

namespace PureMVCFramework.Entity
{
    [Serializable]
    public partial struct ComponentType : IEquatable<ComponentType>
    {
        public enum AccessMode
        {
            ReadWrite,
            //ReadOnly,
            Exclude
        }

        public int TypeIndex;
        public AccessMode AccessModeType;

        public static ComponentType ReadWrite<T>()
        {
            return FromTypeIndex(TypeManager.GetTypeIndex<T>());
        }

        public static ComponentType ReadWrite(Type type)
        {
            return FromTypeIndex(TypeManager.GetTypeIndex(type));
        }

        public static ComponentType ReadWrite(int typeIndex)
        {
            return FromTypeIndex(typeIndex);
        }

        public static ComponentType FromTypeIndex(int typeIndex)
        {
            ComponentType type;
            type.TypeIndex = typeIndex;
            type.AccessModeType = AccessMode.ReadWrite;
            return type;
        }

        //public static ComponentType ReadOnly(Type type)
        //{
        //    ComponentType t = FromTypeIndex(TypeManager.GetTypeIndex(type));
        //    t.AccessModeType = AccessMode.ReadOnly;
        //    return t;
        //}

        //public static ComponentType ReadOnly(int typeIndex)
        //{
        //    ComponentType t = FromTypeIndex(typeIndex);
        //    t.AccessModeType = AccessMode.ReadOnly;
        //    return t;
        //}

        //public static ComponentType ReadOnly<T>()
        //{
        //    ComponentType t = ReadWrite<T>();
        //    t.AccessModeType = AccessMode.ReadOnly;
        //    return t;
        //}

        public static ComponentType Exclude(Type type)
        {
            return Exclude(TypeManager.GetTypeIndex(type));
        }

        public static ComponentType Exclude(int typeIndex)
        {
            ComponentType t = FromTypeIndex(typeIndex);
            t.AccessModeType = AccessMode.Exclude;
            return t;
        }

        public static ComponentType Exclude<T>()
        {
            return Exclude(TypeManager.GetTypeIndex<T>());
        }

        public ComponentType(Type type, AccessMode accessModeType = AccessMode.ReadWrite)
        {
            TypeIndex = TypeManager.GetTypeIndex(type);
            AccessModeType = accessModeType;
        }

        public Type GetManagedType()
        {
            return TypeManager.GetType(TypeIndex);
        }

        public static implicit operator ComponentType(Type type)
        {
            return new ComponentType(type, AccessMode.ReadWrite);
        }

        public static bool operator <(ComponentType lhs, ComponentType rhs)
        {
            if (lhs.TypeIndex == rhs.TypeIndex)
                return lhs.AccessModeType < rhs.AccessModeType;

            return lhs.TypeIndex < rhs.TypeIndex;
        }

        public static bool operator >(ComponentType lhs, ComponentType rhs)
        {
            return rhs < lhs;
        }

        public static bool operator ==(ComponentType lhs, ComponentType rhs)
        {
            return lhs.TypeIndex == rhs.TypeIndex && lhs.AccessModeType == rhs.AccessModeType;
        }

        public static bool operator !=(ComponentType lhs, ComponentType rhs)
        {
            return lhs.TypeIndex != rhs.TypeIndex || lhs.AccessModeType != rhs.AccessModeType;
        }

        public override string ToString()
        {
            if (TypeIndex == 0)
                return "None";

            var info = TypeManager.GetTypeInfo(TypeIndex);
            StringBuilder ns = new StringBuilder();
            ns.Append(info.DebugTypeName);

            if (AccessModeType == AccessMode.Exclude)
                ns.Append(" [Exclude]");

            return ns.ToString();

        }

        public bool Equals(ComponentType other)
        {
            return TypeIndex == other.TypeIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is ComponentType && (ComponentType)obj == this;
        }

        public override int GetHashCode()
        {
            return (TypeIndex * 5813);
        }
    }
}
