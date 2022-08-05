using System.Collections.Generic;

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


}
