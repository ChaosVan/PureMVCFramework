using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PureMVCFramework.Advantages;
using System.Reflection;
using System;
using System.Linq;
using UnityEngine.Assertions;
using UnityEngine.LowLevel;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace PureMVCFramework.Entity
{
    public class WorldManager : SingletonBehaviour<WorldManager>
    {
#if ODIN_INSPECTOR
        [ShowInInspector, ShowIf("showOdinInfo"), ListDrawerSettings(IsReadOnly = true)]
#endif
        private readonly List<World> worlds = new();
#if ODIN_INSPECTOR
        [ShowInInspector, ShowIf("showOdinInfo"), DictionaryDrawerSettings(IsReadOnly = true)]
#endif
        private readonly Dictionary<World, List<ComponentSystemGroup>> groups = new();

        private DefaultWorldInitialization.DefaultRootGroups rootGroup;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            World.WorldCreated += OnWorldCreated;
            World.WorldDestroyed += OnWorldDestroyed;
            World.SystemCreated += OnSystemCreated;
            World.SystemDestroyed += OnSystemDestroyed;

            rootGroup = new DefaultWorldInitialization.DefaultRootGroups();
        }

        protected override void OnDelete()
        {
            World.WorldCreated -= OnWorldCreated;
            World.WorldDestroyed -= OnWorldDestroyed;
            World.SystemCreated -= OnSystemCreated;
            World.SystemDestroyed -= OnSystemDestroyed;
            base.OnDelete();
        }

        void OnWorldCreated(World world)
        {
            worlds.Add(world);
        }

        void OnWorldDestroyed(World world)
        {
            worlds.Remove(world);
        }

        void OnSystemCreated(World world, ComponentSystemBase system)
        {
            if (rootGroup.IsRootGroup(system.GetType()))
            {
                if (!groups.TryGetValue(world, out var group))
                {
                    group = new List<ComponentSystemGroup>();
                    groups.Add(world, group);
                }
                group.Add(system as ComponentSystemGroup);
            }
        }

        void OnSystemDestroyed(World world, ComponentSystemBase system)
        {
            if (rootGroup.IsRootGroup(system.GetType()))
            {
                if (groups.TryGetValue(world, out var group))
                    group.Remove(system as ComponentSystemGroup);
            }
        }

        //#if ODIN_INSPECTOR
        //        [ShowInInspector, ShowIf("showOdinInfo"), ListDrawerSettings(IsReadOnly = true)]
        //#endif
        //        private readonly List<IWorld> worlds = new();
        //#if ODIN_INSPECTOR
        //        [ShowInInspector, ShowIf("showOdinInfo"), ListDrawerSettings(IsReadOnly = true)]
        //#endif
        //        private readonly List<Type> _updateGroups = new();

        //#if ODIN_INSPECTOR
        //        [ShowInInspector, ShowIf("showOdinInfo"), DictionaryDrawerSettings(IsReadOnly = true)]
        //#endif
        //        private readonly Dictionary<Type, ComponentSystemGroup> _groups = new();

        //        private ComponentSystemGroup defaultGroup;
        //        private bool groupSortDirty;

        //        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        //        public static void Initialize()
        //        {
        //            Instance.updateMode = UpdateMode.UPDATE;
        //        }

        //        protected override void OnInitialized()
        //        {
        //            base.OnInitialized();

        //            // create default group
        //            //defaultGroup = RegisterGroup(typeof(DefaultGroup));

        //            //var assemblies = new string[] { "Assembly-CSharp", "Entity" };
        //            //List<Type> system_collect = new();
        //            //List<Type> group_collect = new();

        //            //// Collections
        //            //foreach (var assemblyName in assemblies)
        //            //{
        //            //    group_collect.AddRange(Assembly.Load(assemblyName).GetTypes().Where(t => !t.IsAbstract && typeof(ComponentSystemGroup).IsAssignableFrom(t)));
        //            //    system_collect.AddRange(Assembly.Load(assemblyName).GetTypes().Where(t => !t.IsAbstract && typeof(ComponentSystemBase).IsAssignableFrom(t)));
        //            //}
        //            //// Initialize groups and systems
        //            //InitializeGroups(group_collect);
        //            ////InitializeSystems(system_collect);

        //        }

        //        protected override void OnDelete()
        //        {
        //            foreach (var world in worlds)
        //            {
        //                world.Destroy();
        //            }
        //            worlds.Clear();
        //            _updateGroups.Clear();

        //            LocalWorld = null;

        //            base.OnDelete();
        //        }

        //private void InitializeGroups(List<Type> collection)
        //{
        //    foreach (var t in collection)
        //    {
        //        RegisterGroup(t);
        //    }


        //    //Dictionary<Type, List<Type>> typeDep = new Dictionary<Type, List<Type>>();
        //    //for (int i = 0; i < collection.Count; i++)
        //    //{
        //    //    var t1 = collection[i].GetCustomAttribute<UpdateAfterAttribute>();
        //    //    if (t1 != null)
        //    //    {
        //    //        for (int j = i + 1; j < collection.Count; j++)
        //    //        {
        //    //            if (t1.GroupType == collection[j])
        //    //            {

        //    //            }
        //    //        }
        //    //    }
        //    //}
        //}

        //public ComponentSystemGroup RegisterGroup(Type groupType)
        //{
        //    if (!_groups.TryGetValue(groupType, out var group))
        //    {
        //        group = ReferencePool.Instance.SpawnInstance(groupType) as ComponentSystemGroup;
        //        _groups.Add(groupType, group);
        //        groupSortDirty = true;
        //    }

        //    return group;
        //}

        //private void SortGroups()
        //{
        //    _updateGroups.Clear();
        //    _updateGroups.AddRange(_groups.Keys);
        //    _updateGroups.Sort((a, b) =>
        //    {
        //        var aa = a.GetCustomAttribute<UpdateAfterAttribute>();
        //        var ba = b.GetCustomAttribute<UpdateAfterAttribute>();
        //        var ab = a.GetCustomAttribute<UpdateBeforeAttribute>();
        //        var bb = b.GetCustomAttribute<UpdateBeforeAttribute>();

        //        return 1;
        //    });

        //    groupSortDirty = false;
        //}

        //public bool TryGetGroup(Type groupType, out ComponentSystemGroup group)
        //{
        //    return _groups.TryGetValue(groupType, out group);
        //}

        protected override void OnUpdate(float delta)
        {

        }

        public void ModifyEntity(Entity entity)
        {
            //foreach (var world in worlds)
            //{
            //    world.ModifyEntity(entity);
            //}
        }

        //public void RegisterWorld(IWorld world)
        //{
        //    worlds.Add(world);
        //}

        //public void RemoveWorld(IWorld world)
        //{
        //    worlds.Remove(world);
        //}

        //public IWorld GetWorld<T>() where T : IWorld
        //{
        //    foreach (var world in worlds)
        //    {
        //        if (world.GetType() == typeof(T))
        //            return world;
        //    }

        //    var newWorld = ReferencePool.Instance.SpawnInstance<T>();
        //    RegisterWorld(newWorld);

        //    return newWorld;
        //}
    }

    static class AutomaticWorldBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            DefaultWorldInitialization.Initialize("Default World", false);
        }
    }

    public static class DefaultWorldInitialization
    {
#pragma warning disable 0067 // unused variable
        /// <summary>
        /// Invoked after the default World is initialized.
        /// </summary>
        internal static event Action<World> DefaultWorldInitialized;

        /// <summary>
        /// Invoked after the Worlds are destroyed.
        /// </summary>
        internal static event Action DefaultWorldDestroyed;
#pragma warning restore 0067 // unused variable

#if !UNITY_DOTSRUNTIME
        [DomainReload(true)]
        static bool s_UnloadOrPlayModeChangeShutdownRegistered = false;

        /// <summary>
        /// Destroys Editor World when entering Play Mode without Domain Reload.
        /// RuntimeInitializeOnLoadMethod is called before the new scene is loaded, before Awake and OnEnable of MonoBehaviour.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void CleanupWorldBeforeSceneLoad()
        {
            DomainUnloadOrPlayModeChangeShutdown();
        }
#endif

        internal static void DomainUnloadOrPlayModeChangeShutdown()
        {
#if !UNITY_DOTSRUNTIME
            if (!s_UnloadOrPlayModeChangeShutdownRegistered)
                return;

            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            foreach (var w in World.s_AllWorlds)
                ScriptBehaviourUpdateOrder.RemoveWorldFromPlayerLoop(w, ref playerLoop);
            PlayerLoop.SetPlayerLoop(playerLoop);

            //RuntimeApplication.UnregisterFrameUpdateToCurrentPlayerLoop();

            World.DisposeAllWorlds();

            s_UnloadOrPlayModeChangeShutdownRegistered = false;

            DefaultWorldDestroyed?.Invoke();
#endif
        }

        public static World Initialize(string defaultWorldName, bool editorWorld = false)
        {
#if UNITY_EDITOR
            WorldManager.Instance.updateMode = SingletonBehaviour<WorldManager>.UpdateMode.LATE_UPDATE;
#endif

            if (!editorWorld)
            {
                var bootStrap = CreateBootStrap();
                if (bootStrap != null && bootStrap.Initialize(defaultWorldName))
                {
                    Assert.IsTrue(World.DefaultGameObjectInjectionWorld != null,
                        $"ICustomBootstrap.Initialize() implementation failed to set " +
                        $"World.DefaultGameObjectInjectionWorld, despite returning true " +
                        $"(indicating the World has been properly initialized)");
                    return World.DefaultGameObjectInjectionWorld;
                }
            }

            var world = new World(defaultWorldName, editorWorld ? WorldFlags.Editor : WorldFlags.Game);
            World.DefaultGameObjectInjectionWorld = world;

            var systemList = GetAllSystems(WorldSystemFilterFlags.Default);
            AddSystemToRootLevelSystemGroupsInternal(world, systemList);

#if !UNITY_DOTSRUNTIME
            ScriptBehaviourUpdateOrder.AppendWorldToCurrentPlayerLoop(world);
#endif

            DefaultWorldInitialized?.Invoke(world);
            return world;
        }

        static ICustomBootstrap CreateBootStrap()
        {
#if !UNITY_DOTSRUNTIME
            var bootstrapTypes = TypeManager.GetTypesDerivedFrom(typeof(ICustomBootstrap));
            Type selectedType = null;

            foreach (var bootType in bootstrapTypes)
            {
                if (bootType.IsAbstract || bootType.ContainsGenericParameters)
                    continue;

                if (selectedType == null)
                    selectedType = bootType;
                else if (selectedType.IsAssignableFrom(bootType))
                    selectedType = bootType;
                else if (!bootType.IsAssignableFrom(selectedType))
                    Debug.LogError("Multiple custom ICustomBootstrap specified, ignoring " + bootType);
            }
            ICustomBootstrap bootstrap = null;
            if (selectedType != null)
                bootstrap = Activator.CreateInstance(selectedType) as ICustomBootstrap;

            return bootstrap;
#else
            throw new Exception("This method should have been replaced by code-gen.");
#endif
        }

        /// <summary>
        /// Calculates a list of all systems filtered with WorldSystemFilterFlags, [DisableAutoCreation] etc.
        /// </summary>
        /// <param name="filterFlags"></param>
        /// <param name="requireExecuteAlways">Optionally require that [ExecuteAlways] is present on the system. This is used when creating edit mode worlds.</param>
        /// <returns>The list of filtered systems</returns>
        public static IReadOnlyList<Type> GetAllSystems(WorldSystemFilterFlags filterFlags)
        {
            return TypeManager.GetSystems(filterFlags, WorldSystemFilterFlags.Default);
        }

        /// <summary>
        /// This internal interface is used when adding systems to the default world to identify the root groups in your
        /// setup. They will then be skipped when we try to find the parent of each system (because they don't need a
        /// parent).
        /// </summary>
        internal interface IIdentifyRootGroups
        {
            bool IsRootGroup(Type type);
        }

        internal struct DefaultRootGroups : IIdentifyRootGroups
        {
            public bool IsRootGroup(Type type) =>
                type == typeof(InitializationSystemGroup) ||
                type == typeof(SimulationSystemGroup);
        }

        internal static void AddSystemToRootLevelSystemGroupsInternal<T>(World world, IEnumerable<Type> systemTypesOrig, ComponentSystemGroup defaultGroup, T rootGroups)
            where T : struct, IIdentifyRootGroups
        {
            var managedTypes = new List<Type>();
            var unmanagedTypes = new List<Type>();

            foreach (var stype in systemTypesOrig)
            {
                if (typeof(ComponentSystemBase).IsAssignableFrom(stype))
                    managedTypes.Add(stype);
                else if (typeof(ISystem).IsAssignableFrom(stype))
                    unmanagedTypes.Add(stype);
                else
                    throw new InvalidOperationException("Bad type");
            }

            var systems = world.GetOrCreateSystemsAndLogException(managedTypes, managedTypes.Count);

            // Add systems to their groups, based on the [UpdateInGroup] attribute.
            foreach (var system in systems)
            {
                if (system == null)
                    continue;

                // Skip the built-in root-level system groups
                var type = system.GetType();
                if (rootGroups.IsRootGroup(type))
                {
                    continue;
                }

                var updateInGroupAttributes = TypeManager.GetSystemAttributes(system.GetType(), typeof(UpdateInGroupAttribute));
                if (updateInGroupAttributes.Length == 0)
                {
                    defaultGroup.AddSystemToUpdateList(system);
                }

                foreach (var attr in updateInGroupAttributes)
                {
                    var group = FindGroup(world, type, attr);
                    if (group != null)
                    {
                        group.AddSystemToUpdateList(system);
                    }
                }
            }
        }

        private static void AddSystemToRootLevelSystemGroupsInternal(World world, IEnumerable<Type> systemTypesOrig)
        {
            var initializationSystemGroup = world.GetOrCreateSystem<InitializationSystemGroup>();
            var simulationSystemGroup = world.GetOrCreateSystem<SimulationSystemGroup>();

            AddSystemToRootLevelSystemGroupsInternal(world, systemTypesOrig, simulationSystemGroup, new DefaultRootGroups());

            // Update player loop
            initializationSystemGroup.SortSystems();
            simulationSystemGroup.SortSystems();
        }

        private static ComponentSystemGroup FindGroup(World world, Type systemType, Attribute attr)
        {
            var uga = attr as UpdateInGroupAttribute;

            if (uga == null)
                return null;

            if (!TypeManager.IsSystemAGroup(uga.GroupType))
            {
                throw new InvalidOperationException($"Invalid [UpdateInGroup] attribute for {systemType}: {uga.GroupType} must be derived from ComponentSystemGroup.");
            }
            if (uga.OrderFirst && uga.OrderLast)
            {
                throw new InvalidOperationException($"The system {systemType} can not specify both OrderFirst=true and OrderLast=true in its [UpdateInGroup] attribute.");
            }

            var groupSys = world.GetExistingSystem(uga.GroupType);
            if (groupSys == null)
            {
                // Warn against unexpected behaviour combining DisableAutoCreation and UpdateInGroup
                var parentDisableAutoCreation = TypeManager.GetSystemAttributes(uga.GroupType, typeof(DisableAutoCreationAttribute)).Length > 0;
                if (parentDisableAutoCreation)
                {
                    Debug.LogWarning($"A system {systemType} wants to execute in {uga.GroupType} but this group has [DisableAutoCreation] and {systemType} does not. The system will not be added to any group and thus not update.");
                }
                else
                {
                    Debug.LogWarning(
                        $"A system {systemType} could not be added to group {uga.GroupType}, because the group was not created. Fix these errors before continuing. The system will not be added to any group and thus not update.");
                }
            }

            return groupSys as ComponentSystemGroup;
        }


    }
}
