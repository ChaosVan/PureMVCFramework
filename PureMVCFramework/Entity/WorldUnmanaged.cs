using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace PureMVCFramework.Entity
{
    public struct WorldUnmanaged
    {
//#if ENABLE_UNITY_COLLECTIONS_CHECKS
//        private AtomicSafetyHandle m_Safety;
//#endif

        private WorldUnmanagedImpl m_Impl;

        internal static readonly SharedStatic<ulong> ms_NextSequenceNumber = SharedStatic<ulong>.GetOrCreate<World>();

        public TimeData CurrentTime { get => m_Impl.CurrentTime; set => m_Impl.CurrentTime = value; }
        public WorldFlags Flags => m_Impl.Flags;
        public float MaximumDeltaTime
        {
            get => m_Impl.MaximumDeltaTime;
            set => m_Impl.MaximumDeltaTime = value;
        }
        public ulong SequenceNumber => m_Impl.SequenceNumber;
        public int Version => m_Impl.Version;
        internal void BumpVersion() => m_Impl.BumpVersion();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        [BurstCompatible(RequiredUnityDefine = "ENABLE_UNITY_COLLECTIONS_CHECKS", CompileTarget = BurstCompatibleAttribute.BurstCompatibleCompileTarget.Editor)]
        internal bool AllowGetSystem => m_Impl.AllowGetSystem;

        [BurstCompatible(RequiredUnityDefine = "ENABLE_UNITY_COLLECTIONS_CHECKS", CompileTarget = BurstCompatibleAttribute.BurstCompatibleCompileTarget.Editor)]
        internal void DisallowGetSystem() => m_Impl.DisallowGetSystem();
#endif

        [NotBurstCompatible]
        internal void Create(World world, WorldFlags flags, AllocatorManager.AllocatorHandle backingAllocatorHandle)
        {
            m_Impl = new WorldUnmanagedImpl(++ms_NextSequenceNumber.Data, flags);

//#if ENABLE_UNITY_COLLECTIONS_CHECKS
//            m_Safety = AtomicSafetyHandle.Create();
//#endif

            // The EntityManager itself is only a handle to a data access and already performs safety checks, so it is
            // OK to keep it on this handle itself instead of in the actual implementation.
            //m_EntityManager = default;
            //m_EntityManager.Initialize(world);
        }

        [NotBurstCompatible]
        internal void Dispose()
        {
//#if ENABLE_UNITY_COLLECTIONS_CHECKS
//            AtomicSafetyHandle.CheckDeallocateAndThrow(m_Safety);
//            AtomicSafetyHandle.Release(m_Safety);
//#endif
            //m_EntityManager.DestroyInstance();
            //m_EntityManager = default;
            m_Impl = default;
        }

//        private WorldUnmanagedImpl GetImpl()
//        {
//#if ENABLE_UNITY_COLLECTIONS_CHECKS
//            AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
//#endif
//            return m_Impl;
//        }

        internal SystemState AllocateSystemStateForManagedSystem(World self, ComponentSystemBase system) =>
            m_Impl.AllocateSystemStateForManagedSystem(self, system);


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
