using System;

namespace PureMVCFramework.Entity
{
    public struct EntityArchetype
    {
        public EntityArchetype(params string[] typeNames)
        {
            this.typeNames = typeNames;

            hash = new int[typeNames.Length];
            for (int i = 0; i < typeNames.Length; ++i)
            {
                hash[i] = Entity.StringToHash(typeNames[i]);
            }
        }

        public EntityArchetype(params Type[] types)
        {
            typeNames = new string[types.Length];
            for (int i = 0; i < types.Length; ++i)
            {
                typeNames[i] = types[i].FullName;
            }
            hash = new int[typeNames.Length];
            for (int i = 0; i < typeNames.Length; ++i)
            {
                hash[i] = Entity.StringToHash(typeNames[i]);
            }
        }

        public string[] typeNames;
        public int[] hash;
    }
}
