using System;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;

namespace PureMVCFramework.Entity
{
    /// <summary>
    /// An identifier representing an unmanaged system struct instance in a particular world.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SystemHandleUntyped : IEquatable<SystemHandleUntyped>, IComparable<SystemHandleUntyped>
    {
        //internal ushort m_Handle;
        //internal ushort m_Version;
        internal int m_SystemId;
        internal uint m_WorldSeqNo;

        private ulong ToUlong()
        {
            return ((ulong)m_WorldSeqNo << 32) | (uint)m_SystemId;
        }

        internal SystemHandleUntyped(int systemId, uint worldSeqNo)
        {
            m_SystemId = systemId;
            m_WorldSeqNo = worldSeqNo;
        }

        public int CompareTo(SystemHandleUntyped other)
        {
            ulong a = ToUlong();
            ulong b = other.ToUlong();
            if (a < b)
                return -1;
            else if (a > b)
                return 1;
            return 0;
        }

        public override bool Equals(object obj)
        {
            if (obj is SystemHandleUntyped foo)
                return Equals(foo);
            return false;
        }

        public bool Equals(SystemHandleUntyped other)
        {
            return ToUlong() == other.ToUlong();
        }

        public override int GetHashCode()
        {
            int hashCode = -116238775;
            hashCode = hashCode * -1521134295 + m_SystemId.GetHashCode();
            hashCode = hashCode * -1521134295 + m_WorldSeqNo.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(SystemHandleUntyped a, SystemHandleUntyped b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(SystemHandleUntyped a, SystemHandleUntyped b)
        {
            return !a.Equals(b);
        }
    }

    public class WorldUnmanaged
    {
        internal readonly ulong SequenceNumber;
        public WorldFlags Flags;
        public TimeData CurrentTime;
        public SystemHandleUntyped ExecutingSystem;

        /// <summary>
        /// The maximum DeltaTime that will be applied to a World in a single call to Update().
        /// If the actual elapsed time since the previous frame exceeds this value, it will be clamped.
        /// This helps maintain a minimum frame rate after a large frame time spike, by spreading out the recovery over
        /// multiple frames.
        /// The value is expressed in seconds. The default value is 1/3rd seconds. Recommended values are 1/10th and 1/3rd seconds.
        /// </summary>
        public float MaximumDeltaTime;

        public int Version { get; private set; }
        internal void BumpVersion() => Version++;

        public bool AllowGetSystem { get; private set; }
        internal void DisallowGetSystem() => AllowGetSystem = false;

        internal static readonly SharedStatic<ulong> ms_NextSequenceNumber = SharedStatic<ulong>.GetOrCreate<World>();

        internal WorldUnmanaged(World world, WorldFlags flags, AllocatorManager.AllocatorHandle backingAllocatorHandle)
        {
            CurrentTime = default;
            AllowGetSystem = true;

            SequenceNumber = ++ms_NextSequenceNumber.Data;
            MaximumDeltaTime = 1.0f / 3.0f;
            Flags = flags;

            Version = 0;
            ExecutingSystem = default;
        }

        internal SystemState AllocateSystemStateForManagedSystem(World self, ComponentSystemBase system)
        {
            var type = system.GetType();
            SystemState state = new SystemState();
            state.InitManaged(self, type, system, (uint)SequenceNumber);
            ++Version;
            return state;
        }
    }


}
