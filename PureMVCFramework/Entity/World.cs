#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;

namespace PureMVCFramework.Entity
{
    /// <summary>
    /// Specify all traits a <see cref="World"/> can have.
    /// </summary>
    [Flags]
    public enum WorldFlags : int
    {
        /// <summary>
        /// Default WorldFlags value.
        /// </summary>
        None = 0,

        /// <summary>
        /// The main <see cref="World"/> for a game/application.
        /// This flag is combined with <see cref="Editor"/>, <see cref="Game"/> and <see cref="Simulation"/>.
        /// </summary>
        Live = 1,

        /// <summary>
        /// Main <see cref="Live"/> <see cref="World"/> running in the Editor.
        /// </summary>
        Editor = 1 << 1 | Live,

        /// <summary>
        /// Main <see cref="Live"/> <see cref="World"/> running in the Player.
        /// </summary>
        Game = 1 << 2 | Live,

        ///// <summary>
        ///// Any additional <see cref="Live"/> <see cref="World"/> running in the application for background processes that
        ///// queue up data for other <see cref="Live"/> <see cref="World"/> (ie. physics, AI simulation, networking, etc.).
        ///// </summary>
        //Simulation = 1 << 3 | Live,

        ///// <summary>
        ///// <see cref="World"/> on which conversion systems run to transform authoring data to runtime data.
        ///// </summary>
        //Conversion = 1 << 4,

        ///// <summary>
        ///// <see cref="World"/> in which temporary results are staged before being moved into a <see cref="Live"/> <see cref="World"/>.
        ///// Typically combined with <see cref="Conversion"/> to represent an intermediate step in the full conversion process.
        ///// </summary>
        //Staging = 1 << 5,

        ///// <summary>
        ///// <see cref="World"/> representing a previous state of another <see cref="World"/> typically to compute
        ///// a diff of runtime data - for example useful for undo/redo or Live Link.
        ///// </summary>
        //Shadow = 1 << 6,

        ///// <summary>
        ///// Dedicated <see cref="World"/> for managing incoming streamed data to the Player.
        ///// </summary>
        //Streaming = 1 << 7,

        ///// <summary>
        ///// <see cref="World"/> Enable world update allocator to free individual block.
        ///// </summary>
        //EnableBlockFree = 1 << 8,
    }

    /// <summary>
    /// When entering playmode or the game starts in the Player a default world is created.
    /// Sometimes you need multiple worlds to be setup when the game starts or perform some
    /// custom world initialization. This lets you override the bootstrap of game code world creation.
    /// </summary>
    public interface ICustomBootstrap
    {
        // Returns true if the bootstrap has performed initialization.
        // Returns false if default world initialization should be performed.
        bool Initialize(string defaultWorldName);
    }

    public partial class World : IDisposable
    {
        internal static readonly List<World> s_AllWorlds = new List<World>();

        public static World DefaultGameObjectInjectionWorld { get; set; }

        Dictionary<Type, ComponentSystemBase> m_SystemLookup = new Dictionary<Type, ComponentSystemBase>();
        public static NoAllocReadOnlyCollection<World> All { get; } = new NoAllocReadOnlyCollection<World>(s_AllWorlds);

        /// <summary>
        /// Event invoked after world has been fully constructed.
        /// </summary>
        internal static event Action<World> WorldCreated;

        /// <summary>
        /// Event invoked before world is disposed.
        /// </summary>
        internal static event Action<World> WorldDestroyed;

        /// <summary>
        /// Event invoked after system has been fully constructed.
        /// </summary>
        internal static event Action<World, ComponentSystemBase> SystemCreated;

        /// <summary>
        /// Event invoked before system is disposed.
        /// </summary>
        internal static event Action<World, ComponentSystemBase> SystemDestroyed;

        List<ComponentSystemBase> m_Systems = new List<ComponentSystemBase>();
        public NoAllocReadOnlyCollection<ComponentSystemBase> Systems { get; }

        private WorldUnmanaged m_Unmanaged;

        public WorldUnmanaged Unmanaged => m_Unmanaged;

        public WorldFlags Flags => m_Unmanaged.Flags;

#if ODIN_INSPECTOR
        [ShowInInspector]
#endif
        public string Name { get; }

        public override string ToString()
        {
            return Name;
        }

        public int Version => m_Unmanaged.Version;

        public bool IsCreated => m_Systems != null;

        public ulong SequenceNumber => m_Unmanaged.SequenceNumber;

#if ODIN_INSPECTOR
        [ShowInInspector]
#endif
        public TimeData Time { get => m_Unmanaged.CurrentTime; set => m_Unmanaged.CurrentTime = value; }

        internal Stack<TimeData> m_WorldTimeQueue;

        public float MaximumDeltaTime
        {
            get => m_Unmanaged.MaximumDeltaTime;
            set => m_Unmanaged.MaximumDeltaTime = value;
        }

        public World(string name, WorldFlags flags = WorldFlags.Game)
        {
            Name = name;
            Systems = new NoAllocReadOnlyCollection<ComponentSystemBase>(m_Systems);

            Init(flags, Allocator.Persistent);
        }

        void Init(WorldFlags flags, AllocatorManager.AllocatorHandle backingAllocatorHandle)
        {
            m_Unmanaged = new WorldUnmanaged(this, flags, backingAllocatorHandle);

            s_AllWorlds.Add(this);

            m_WorldTimeQueue = new Stack<TimeData>();

            // The EntityManager itself is only a handle to a data access and already performs safety checks, so it is
            // OK to keep it on this handle itself instead of in the actual implementation.
            EntityManager.Initialize(this);

            WorldCreated?.Invoke(this);
        }

        public void Dispose()
        {
            if (!IsCreated)
                throw new ArgumentException("The World has already been Disposed.");

            WorldDestroyed?.Invoke(this);

            m_Unmanaged.DisallowGetSystem();

            DestroyAllSystemsAndLogException();

            s_AllWorlds.Remove(this);

            m_SystemLookup.Clear();
            m_SystemLookup = null;

            if (DefaultGameObjectInjectionWorld == this)
                DefaultGameObjectInjectionWorld = null;

            m_Unmanaged = default;
        }

        public static void DisposeAllWorlds()
        {
            while (s_AllWorlds.Count != 0)
            {
                s_AllWorlds[0].Dispose();
            }
        }

        public void SetTime(TimeData newTimeData)
        {
            Time = newTimeData;
        }

        public void PushTime(TimeData newTimeData)
        {
            m_WorldTimeQueue.Push(Time);
            SetTime(newTimeData);
        }

        public void PopTime()
        {
            Assert.IsTrue(m_WorldTimeQueue.Count > 0, "PopTime without a matching PushTime");
            var prevTime = m_WorldTimeQueue.Pop();
            SetTime(prevTime);
        }

        ComponentSystemBase CreateSystemInternal(Type type)
        {
            var system = AllocateSystemInternal(type);
            AddSystem_Add_Internal(system);
            AddSystem_OnCreate_Internal(system);
            return system;
        }

        ComponentSystemBase AllocateSystemInternal(Type type)
        {
            if (!m_Unmanaged.AllowGetSystem)
                throw new ArgumentException("During destruction of a system you are not allowed to create more systems.");

            return TypeManager.ConstructSystem(type);
        }

        ComponentSystemBase GetExistingSystemInternal(Type type)
        {
            ComponentSystemBase system;
            if (m_SystemLookup.TryGetValue(type, out system))
                return system;

            return null;
        }

        void AddTypeLookupInternal(Type type, ComponentSystemBase system)
        {
            while (type != typeof(ComponentSystemBase))
            {
                if (!m_SystemLookup.ContainsKey(type))
                    m_SystemLookup.Add(type, system);

                type = type.BaseType;
            }
        }

        void AddSystem_Add_Internal(ComponentSystemBase system)
        {
            m_Systems.Add(system);
            AddTypeLookupInternal(system.GetType(), system);
        }

        void AddSystem_OnCreate_Internal(ComponentSystemBase system)
        {
            try
            {
                var state = m_Unmanaged.AllocateSystemStateForManagedSystem(this, system);
                system.CreateInstance(this, state);
            }
            catch
            {
                RemoveSystemInternal(system);
                throw;
            }

            m_Unmanaged.BumpVersion();

            SystemCreated?.Invoke(this, system);
        }

        void RemoveSystemInternal(ComponentSystemBase system)
        {
            if (!m_Systems.Remove(system))
                throw new ArgumentException($"System does not exist in the world");

            m_Unmanaged.BumpVersion();

            var type = system.GetType();
            while (type != typeof(ComponentSystemBase))
            {
                if (m_SystemLookup[type] == system)
                {
                    m_SystemLookup.Remove(type);

                    foreach (var otherSystem in m_Systems)
                        // Equivalent to otherSystem.isSubClassOf(type) but compatible with NET_DOTS
                        if (type != otherSystem.GetType() && type.IsAssignableFrom(otherSystem.GetType()))
                            AddTypeLookupInternal(otherSystem.GetType(), otherSystem);
                }

                type = type.BaseType;
            }
        }

        void CheckGetOrCreateSystem()
        {
            if (!IsCreated)
            {
                throw new ArgumentException("The World has already been Disposed.");
            }

            if (!m_Unmanaged.AllowGetSystem)
            {
                throw new ArgumentException("You are not allowed to get or create more systems during destruction of a system.");
            }
        }

        // Public system management

        public T GetOrCreateSystem<T>() where T : ComponentSystemBase
        {
            CheckGetOrCreateSystem();

            var system = GetExistingSystemInternal(typeof(T));
            return (T)(system ?? CreateSystemInternal(typeof(T)));
        }

        public ComponentSystemBase GetOrCreateSystem(Type type)
        {
            CheckGetOrCreateSystem();

            var system = GetExistingSystemInternal(type);
            return system ?? CreateSystemInternal(type);
        }

        public T GetExistingSystem<T>() where T : ComponentSystemBase
        {
            CheckGetOrCreateSystem();

            return (T)GetExistingSystemInternal(typeof(T));
        }

        public ComponentSystemBase GetExistingSystem(Type type)
        {
            CheckGetOrCreateSystem();

            return GetExistingSystemInternal(type);
        }

        public void DestroySystem(ComponentSystemBase system)
        {
            CheckGetOrCreateSystem();

            SystemDestroyed?.Invoke(this, system);
            RemoveSystemInternal(system);
            system.DestroyInstance();
        }

        public void DestroyAllSystemsAndLogException()
        {
            if (m_Systems == null)
                return;

            // Systems are destroyed in reverse order from construction, in three phases:
            // 1. Stop all systems from running (if they weren't already stopped), to ensure OnStopRunning() is called.
            // 2. Call each system's OnDestroy() method
            // 3. Actually destroy each system
            for (int i = m_Systems.Count - 1; i >= 0; --i)
            {
                try
                {
                    SystemDestroyed?.Invoke(this, m_Systems[i]);
                    m_Systems[i].OnBeforeDestroyInternal();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            for (int i = m_Systems.Count - 1; i >= 0; --i)
            {
                try
                {
                    m_Systems[i].OnDestroy_Internal();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            for (int i = m_Systems.Count - 1; i >= 0; --i)
            {
                try
                {
                    m_Systems[i].OnAfterDestroyInternal();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            m_Systems.Clear();
            m_Systems = null;
        }

        internal ComponentSystemBase[] GetOrCreateSystemsAndLogException(IEnumerable<Type> types, int typesCount)
        {
            CheckGetOrCreateSystem();

            var toInitSystems = new ComponentSystemBase[typesCount];
            // start before 0 as we increment at the top of the loop to avoid
            // special cases for the various early outs in the loop below
            var i = -1;
            foreach (var type in types)
            {
                i++;
                try
                {
                    if (GetExistingSystemInternal(type) != null)
                        continue;

                    var system = AllocateSystemInternal(type);
                    if (system == null)
                        continue;

                    toInitSystems[i] = system;
                    AddSystem_Add_Internal(system);
                }
                catch (Exception exc)
                {
                    Debug.LogException(exc);
                }
            }

            for (i = 0; i != typesCount; i++)
            {
                if (toInitSystems[i] != null)
                {
                    try
                    {
                        AddSystem_OnCreate_Internal(toInitSystems[i]);
                    }
                    catch (Exception exc)
                    {
                        Debug.LogException(exc);
                    }
                }
            }

            i = 0;
            foreach (var type in types)
            {
                toInitSystems[i] = GetExistingSystemInternal(type);
                i++;
            }

            return toInitSystems;
        }

        public ComponentSystemBase[] GetOrCreateSystemsAndLogException(Type[] types)
        {
            return GetOrCreateSystemsAndLogException(types, types.Length);
        }

        public bool QuitUpdate { get; set; }
    }

    /// <summary>
    /// Read only collection that doesn't generate garbage when used in a foreach.
    /// </summary>
    public struct NoAllocReadOnlyCollection<T> : IEnumerable<T>
    {
        readonly List<T> m_Source;

        public NoAllocReadOnlyCollection(List<T> source) => m_Source = source;

        public int Count => m_Source.Count;

        public T this[int index] => m_Source[index];

        public List<T>.Enumerator GetEnumerator() => m_Source.GetEnumerator();

        public bool Contains(T item) => m_Source.Contains(item);

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
            => throw new NotSupportedException($"To avoid boxing, do not cast {nameof(NoAllocReadOnlyCollection<T>)} to IEnumerable<T>.");
        IEnumerator IEnumerable.GetEnumerator()
            => throw new NotSupportedException($"To avoid boxing, do not cast {nameof(NoAllocReadOnlyCollection<T>)} to IEnumerable.");
    }
}
