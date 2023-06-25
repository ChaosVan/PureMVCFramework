using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.LowLevel;

namespace PureMVCFramework.Entity
{
    static class AutomaticWorldBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
#if !UNITY_DOTSRUNTIME
            DefaultWorldInitialization.Initialize("Default World", false);
#endif
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

        [DomainReload(true)]
        static bool s_UnloadOrPlayModeChangeShutdownRegistered = true;

        /// <summary>
        /// Destroys Editor World when entering Play Mode without Domain Reload.
        /// RuntimeInitializeOnLoadMethod is called before the new scene is loaded, before Awake and OnEnable of MonoBehaviour.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void CleanupWorldBeforeSceneLoad()
        {
            DomainUnloadOrPlayModeChangeShutdown();
        }

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
            EntityDebugger.Instance.updateMode = SingletonBehaviour<EntityDebugger>.UpdateMode.LATE_UPDATE;
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
