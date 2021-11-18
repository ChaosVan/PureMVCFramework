using System.Collections.Generic;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace PureMVCFramework.Entity
{
    public abstract class HybridSystem<T1, T2> : SystemBase where T1 : Component where T2 : IComponent
    {
#if ODIN_INSPECTOR
        [ShowInInspector, ShowIf("showOdinInfo"), ListDrawerSettings(IsReadOnly = true)]
#endif
        private readonly List<T1> Components1 = new List<T1>();
#if ODIN_INSPECTOR
        [ShowInInspector, ShowIf("showOdinInfo"), ListDrawerSettings(IsReadOnly = true)]
#endif
        private readonly List<T2> Components2 = new List<T2>();

        private int hash2;

        public override void OnInitialized()
        {
            hash2 = Entity.StringToHash(typeof(T2).FullName);
        }

        public sealed override void InjectEntity(Entity entity)
        {
            if (entity.gameObject == null)
                return;

            var c1 = entity.gameObject.GetComponent<T1>();
            IComponent c2 = null;
            bool tf = c1 && entity.components.TryGetValue(hash2, out c2);

            if (Entities.Contains(entity))
            {
                if (!tf)
                {
                    var i = Entities.IndexOf(entity);
                    Entities.RemoveAt(i);
                    Components1.RemoveAt(i);
                    Components2.RemoveAt(i);

                    OnEject(entity, c1);
                }
            }
            else if (tf)
            {
                Entities.Add(entity);
                Components1.Add((T1)c1);
                Components2.Add((T2)c2);
            }
        }

        public sealed override void Update()
        {
            for (int i = 0; i < Entities.Count; ++i)
            {
                OnUpdate(i, Entities[i], Components1[i], Components2[i]);
            }
        }

        protected abstract void OnUpdate(int index, Entity entity, T1 component1, T2 component2);

        protected virtual void OnEject(Entity entity, T1 component) { }
    }

    public abstract class HybridSystem<T1, T2, T3> : SystemBase where T1 : Component where T2 : IComponent where T3 : IComponent
    {
#if ODIN_INSPECTOR
        [ShowInInspector, ShowIf("showOdinInfo"), ListDrawerSettings(IsReadOnly = true)]
#endif
        private readonly List<T1> Components1 = new List<T1>();
#if ODIN_INSPECTOR
        [ShowInInspector, ShowIf("showOdinInfo"), ListDrawerSettings(IsReadOnly = true)]
#endif
        private readonly List<T2> Components2 = new List<T2>();
#if ODIN_INSPECTOR
        [ShowInInspector, ShowIf("showOdinInfo"), ListDrawerSettings(IsReadOnly = true)]
#endif
        private readonly List<T3> Components3 = new List<T3>();

        private int hash2, hash3;

        public override void OnInitialized()
        {
            hash2 = Entity.StringToHash(typeof(T2).FullName);
            hash3 = Entity.StringToHash(typeof(T3).FullName);
        }

        public sealed override void InjectEntity(Entity entity)
        {
            if (entity.gameObject == null)
                return;

            var c1 = entity.gameObject.GetComponent<T1>();
            IComponent c2 = null, c3 = null;
            bool tf = c1 && entity.components.TryGetValue(hash2, out c2) &&
                entity.components.TryGetValue(hash3, out c3);

            if (Entities.Contains(entity))
            {
                if (!tf)
                {
                    var i = Entities.IndexOf(entity);
                    Entities.RemoveAt(i);
                    Components1.RemoveAt(i);
                    Components2.RemoveAt(i);
                    Components3.RemoveAt(i);

                    OnEject(entity, c1);
                }
            }
            else if (tf)
            {
                Entities.Add(entity);
                Components1.Add((T1)c1);
                Components2.Add((T2)c2);
                Components3.Add((T3)c3);
            }
        }

        public sealed override void Update()
        {
            for (int i = 0; i < Entities.Count; ++i)
            {
                OnUpdate(i, Entities[i], Components1[i], Components2[i], Components3[i]);
            }
        }

        protected abstract void OnUpdate(int index, Entity entity, T1 component1, T2 component2, T3 component3);

        protected virtual void OnEject(Entity entity, T1 component) { }
    }
}
