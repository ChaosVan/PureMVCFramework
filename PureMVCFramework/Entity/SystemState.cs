using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace PureMVCFramework.Entity
{
    [Serializable]
    public struct SystemState
    {
        internal WorldUnmanaged m_WorldUnmanaged;
        internal SystemHandleUntyped m_Handle;

        private const uint kEnabledMask = 0x1;
        private const uint kAlwaysUpdateSystemMask = 0x2;
        private const uint kPreviouslyEnabledMask = 0x4;

        private uint m_Flags;

        private void SetFlag(uint mask, bool value) => m_Flags = value ? m_Flags | mask : m_Flags & ~mask;

        internal int m_SystemID;
        internal long m_typeHash;


        /// <summary>
        /// Return the unmanaged type index of the system (>= 0 for ISystem-type systems), or -1 for managed systems.
        /// </summary>
        public int UnmanagedMetaIndex { get; private set; }

        internal GCHandle m_World;
        // used by managed systems to store a reference to the actual system
        internal GCHandle m_ManagedSystem;

        public bool Enabled { get => (m_Flags & kEnabledMask) != 0; set => SetFlag(kEnabledMask, value); }

        private bool AlwaysUpdateSystem { get => (m_Flags & kAlwaysUpdateSystemMask) != 0; set => SetFlag(kAlwaysUpdateSystemMask, value); }
        internal bool PreviouslyEnabled { get => (m_Flags & kPreviouslyEnabledMask) != 0; set => SetFlag(kPreviouslyEnabledMask, value); }

        // Managed systems call this function to initialize their backing system state
        internal void InitManaged(World world, Type managedType, ComponentSystemBase system, uint worldSeqNo)
        {
            UnmanagedMetaIndex = -1;
            m_ManagedSystem = GCHandle.Alloc(system, GCHandleType.Normal);

            CommonInit(world);

            if (managedType != null)
            {
                AlwaysUpdateSystem = Attribute.IsDefined(managedType, typeof(AlwaysUpdateSystemAttribute), true);
            }

            //m_typeHash = BurstRuntime.GetHashCode64(managedType);
            m_Handle = new SystemHandleUntyped(m_SystemID, worldSeqNo);
        }

        static int ms_SystemIDAllocator = 0;
        private void CommonInit(World world)
        {
            Enabled = true;
            m_SystemID = ++ms_SystemIDAllocator;
            m_World = GCHandle.Alloc(world);

            AlwaysUpdateSystem = false;
        }

        internal bool ShouldRunSystem()
        {
            if (AlwaysUpdateSystem)
                return true;

            return Application.isPlaying;

            //ref var required = ref RequiredEntityQueries;

            //if (required.Length > 0)
            //{
            //    for (int i = 0; i != required.Length; i++)
            //    {
            //        EntityQuery query = required[i];
            //        if (query.IsEmptyIgnoreFilter)
            //            return false;
            //    }

            //    return true;
            //}
            //else
            //{
            //    // Systems without queriesDesc should always run.
            //    ref var eqs = ref EntityQueries;
            //    var length = eqs.Length;
            //    if (length == 0)
            //        return true;

            //    // If all the queriesDesc are empty, skip it.
            //    // (There¡¯s no way to know what the key value is without other markup)
            //    for (int i = 0; i != length; i++)
            //    {
            //        EntityQuery query = eqs[i];
            //        if (!query.IsEmptyIgnoreFilter)
            //            return true;
            //    }

            //    return false;
            //}
        }
    }
}
