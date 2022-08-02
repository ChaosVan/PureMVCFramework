using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace PureMVCFramework.Entity
{
    public struct WorldUnmanaged
    {
        private WorldUnmanagedImpl m_Impl;

        internal static readonly SharedStatic<ulong> ms_NextSequenceNumber = SharedStatic<ulong>.GetOrCreate<World>();

        public TimeData CurrentTime => GetImpl().CurrentTime;
        public WorldFlags Flags => GetImpl().Flags;
        public float MaximumDeltaTime => GetImpl().MaximumDeltaTime;
        public ulong SequenceNumber => GetImpl().SequenceNumber;
        public int Version => GetImpl().Version;

        internal void Create(World world, WorldFlags flags, AllocatorManager.AllocatorHandle backingAllocatorHandle)
        {
            m_Impl = new WorldUnmanagedImpl(++ms_NextSequenceNumber.Data, flags);
        }

        private WorldUnmanagedImpl GetImpl()
        {
            return m_Impl;
        }

        internal SystemState AllocateSystemStateForManagedSystem(World self, ComponentSystemBase system) =>
            GetImpl().AllocateSystemStateForManagedSystem(self, system);


    }

    internal partial struct WorldUnmanagedImpl
    {
        internal readonly ulong SequenceNumber;
        public WorldFlags Flags;
        public TimeData CurrentTime;

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

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        public bool AllowGetSystem { get; private set; }
        internal void DisallowGetSystem() => AllowGetSystem = false;
#endif

        internal WorldUnmanagedImpl(ulong sequenceNumber, WorldFlags flags)
        {
            CurrentTime = default;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AllowGetSystem = true;
#endif
            SequenceNumber = sequenceNumber;
            MaximumDeltaTime = 1.0f / 3.0f;
            Flags = flags;

            Version = 0;
        }

        internal SystemState AllocateSystemStateForManagedSystem(World self, ComponentSystemBase system)
        {
            var type = system.GetType();
            SystemState state = new SystemState();
            state.InitManaged(self, type, system);
            ++Version;
            return state;
        }
    }
}
