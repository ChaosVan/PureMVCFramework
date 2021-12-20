using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace PureMVCFramework.Entity
{
    public class WorldManager : SingletonBehaviour<WorldManager>
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RuntimeOnDisableDomainReload()
        {
            applicationIsQuitting = false;
        }

        private World LocalWorld;

#if ODIN_INSPECTOR
        [ShowInInspector, ShowIf("showOdinInfo"), ListDrawerSettings(IsReadOnly = true)]
#endif
        private readonly List<IWorld> AllWorlds = new List<IWorld>();

        public void Initialize()
        {

        }

        public void ModifyEntity(Entity entity)
        {
            foreach (var world in AllWorlds)
            {
                world.ModifyEntity(entity);
            }
        }

        public void RegisterWorld(IWorld world)
        {
            AllWorlds.Add(world);
        }

        public void RemoveWorld(IWorld world)
        {
            AllWorlds.Remove(world);
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            LocalWorld = new World();
            LocalWorld.Initialize();
            RegisterWorld(LocalWorld);
        }

        protected override void OnDelete()
        {
            foreach (var world in AllWorlds)
            {
                world.Destroy();
            }

            LocalWorld = null;

            base.OnDelete();
        }

        protected override void OnUpdate(float delta)
        {
            if (LocalWorld != null)
                LocalWorld.OnUpdate(delta);
        }
    }
}
