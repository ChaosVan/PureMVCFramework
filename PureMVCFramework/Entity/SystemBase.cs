using PureMVC.Patterns.Observer;
using System.Collections.Generic;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace PureMVCFramework.Entity
{
    public interface ISystemBase
    {
        void OnInitialized();
        void OnRelease();
        void InjectEntity(Entity entity);
        void Update();
        void PreUpdate();
        void PostUpdate();

        IWorld World { get; set; }
    }

    public abstract class SystemBase : Notifier, ISystemBase
    {
#if ODIN_INSPECTOR && UNITY_EDITOR
        [SerializeField]
        protected bool showOdinInfo;
#endif

#if ODIN_INSPECTOR
        [ShowInInspector, ShowIf("showOdinInfo"), ListDrawerSettings(IsReadOnly = true)]
#endif
        protected readonly List<Entity> Entities = new List<Entity>();

        public IWorld World { get; set; }

        public virtual void OnInitialized() { }

        public virtual void OnRelease() { }

        public abstract void InjectEntity(Entity entity);

        public abstract void Update();

        public virtual void PreUpdate() { }

        public virtual void PostUpdate() { }
    }
}
