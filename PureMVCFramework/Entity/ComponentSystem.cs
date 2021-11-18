using System.Collections.Generic;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace PureMVCFramework.Entity
{
    [System.Serializable]
    public abstract class ComponentSystem<T1> : SystemBase where T1 : IComponent
    {
#if ODIN_INSPECTOR
        [ShowInInspector, ShowIf("showOdinInfo"), ListDrawerSettings(IsReadOnly = true)]
#endif
        private readonly List<T1> Components = new List<T1>();

        private int hash;

        public override void OnInitialized()
        {
            hash = Entity.StringToHash(typeof(T1).FullName);
        }

        public sealed override void InjectEntity(Entity entity)
        {
            IComponent c;
            bool tf = entity.components.TryGetValue(hash, out c);

            if (Entities.Contains(entity))
            {
                if (!tf)
                {
                    var i = Entities.IndexOf(entity);
                    Entities.RemoveAt(i);
                    Components.RemoveAt(i);
                }
            }
            else if (tf)
            {
                Entities.Add(entity);
                Components.Add((T1)c);
            }
        }

        public sealed override void Update()
        {
            for (int i = 0; i < Entities.Count; ++i)
            {
                OnUpdate(i, Entities[i], Components[i]);
            }
        }

        protected abstract void OnUpdate(int index, Entity entity, T1 component);
    }

    public abstract class ComponentSystem<T1, T2> : SystemBase where T1 : IComponent where T2 : IComponent
    {
#if ODIN_INSPECTOR
        [ShowInInspector, ShowIf("showOdinInfo"), ListDrawerSettings(IsReadOnly = true)]
#endif
        private readonly List<T1> Components1 = new List<T1>();
#if ODIN_INSPECTOR
        [ShowInInspector, ShowIf("showOdinInfo"), ListDrawerSettings(IsReadOnly = true)]
#endif
        private readonly List<T2> Components2 = new List<T2>();

        private int hash1, hash2;

        public override void OnInitialized()
        {
            hash1 = Entity.StringToHash(typeof(T1).FullName);
            hash2 = Entity.StringToHash(typeof(T2).FullName);
        }

        public sealed override void InjectEntity(Entity entity)
        {
            IComponent[] c = new IComponent[2];
            bool tf = entity.components.TryGetValue(hash1, out c[0]) &&
                entity.components.TryGetValue(hash2, out c[1]);

            if (Entities.Contains(entity))
            {
                if (!tf)
                {
                    var i = Entities.IndexOf(entity);
                    Entities.RemoveAt(i);
                    Components1.RemoveAt(i);
                    Components2.RemoveAt(i);
                }
            }
            else if (tf)
            {
                Entities.Add(entity);
                Components1.Add((T1)c[0]);
                Components2.Add((T2)c[1]);
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
    }

    public abstract class ComponentSystem<T1, T2, T3> : SystemBase where T1 : IComponent where T2 : IComponent where T3 : IComponent
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

        private int hash1, hash2, hash3;

        public override void OnInitialized()
        {
            hash1 = Entity.StringToHash(typeof(T1).FullName);
            hash2 = Entity.StringToHash(typeof(T2).FullName);
            hash3 = Entity.StringToHash(typeof(T3).FullName);
        }

        public sealed override void InjectEntity(Entity entity)
        {
            IComponent[] c = new IComponent[3];
            bool tf = entity.components.TryGetValue(hash1, out c[0]) &&
                entity.components.TryGetValue(hash2, out c[1]) &&
                entity.components.TryGetValue(hash3, out c[2]);

            if (Entities.Contains(entity))
            {
                if (!tf)
                {
                    var i = Entities.IndexOf(entity);
                    Entities.RemoveAt(i);
                    Components1.RemoveAt(i);
                    Components2.RemoveAt(i);
                    Components3.RemoveAt(i);
                }
            }
            else if (tf)
            {
                Entities.Add(entity);
                Components1.Add((T1)c[0]);
                Components2.Add((T2)c[1]);
                Components3.Add((T3)c[2]);
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
    }

    public abstract class ComponentSystem<T1, T2, T3, T4> : SystemBase where T1 : IComponent where T2 : IComponent where T3 : IComponent where T4 : IComponent
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
#if ODIN_INSPECTOR
        [ShowInInspector, ShowIf("showOdinInfo"), ListDrawerSettings(IsReadOnly = true)]
#endif
        private readonly List<T4> Components4 = new List<T4>();

        private int hash1, hash2, hash3, hash4;

        public override void OnInitialized()
        {
            hash1 = Entity.StringToHash(typeof(T1).FullName);
            hash2 = Entity.StringToHash(typeof(T2).FullName);
            hash3 = Entity.StringToHash(typeof(T3).FullName);
            hash4 = Entity.StringToHash(typeof(T4).FullName);
        }

        public sealed override void InjectEntity(Entity entity)
        {
            IComponent[] c = new IComponent[4];
            bool tf = entity.components.TryGetValue(hash1, out c[0]) &&
                entity.components.TryGetValue(hash2, out c[1]) &&
                entity.components.TryGetValue(hash3, out c[2]) &&
                entity.components.TryGetValue(hash4, out c[3]);

            if (Entities.Contains(entity))
            {
                if (!tf)
                {
                    var i = Entities.IndexOf(entity);
                    Entities.RemoveAt(i);
                    Components1.RemoveAt(i);
                    Components2.RemoveAt(i);
                    Components3.RemoveAt(i);
                    Components4.RemoveAt(i);
                }
            }
            else if (tf)
            {
                Entities.Add(entity);
                Components1.Add((T1)c[0]);
                Components2.Add((T2)c[1]);
                Components3.Add((T3)c[2]);
                Components4.Add((T4)c[3]);
            }
        }

        public sealed override void Update()
        {
            for (int i = 0; i < Entities.Count; ++i)
            {
                OnUpdate(i, Entities[i], Components1[i], Components2[i], Components3[i], Components4[i]);
            }
        }

        protected abstract void OnUpdate(int index, Entity entity, T1 component1, T2 component2, T3 component3, T4 component4);
    }

    public abstract class ComponentSystem<T1, T2, T3, T4, T5> : SystemBase where T1 : IComponent where T2 : IComponent where T3 : IComponent where T4 : IComponent where T5 : IComponent
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
#if ODIN_INSPECTOR
        [ShowInInspector, ShowIf("showOdinInfo"), ListDrawerSettings(IsReadOnly = true)]
#endif
        private readonly List<T4> Components4 = new List<T4>();
#if ODIN_INSPECTOR
        [ShowInInspector, ShowIf("showOdinInfo"), ListDrawerSettings(IsReadOnly = true)]
#endif
        private readonly List<T5> Components5 = new List<T5>();

        private int hash1, hash2, hash3, hash4, hash5;

        public override void OnInitialized()
        {
            hash1 = Entity.StringToHash(typeof(T1).FullName);
            hash2 = Entity.StringToHash(typeof(T2).FullName);
            hash3 = Entity.StringToHash(typeof(T3).FullName);
            hash4 = Entity.StringToHash(typeof(T4).FullName);
            hash5 = Entity.StringToHash(typeof(T5).FullName);
        }

        public sealed override void InjectEntity(Entity entity)
        {
            IComponent[] c = new IComponent[5];
            bool tf = entity.components.TryGetValue(hash1, out c[0]) &&
                entity.components.TryGetValue(hash2, out c[1]) &&
                entity.components.TryGetValue(hash3, out c[2]) &&
                entity.components.TryGetValue(hash4, out c[3]) &&
                entity.components.TryGetValue(hash5, out c[4]);

            if (Entities.Contains(entity))
            {
                if (!tf)
                {
                    var i = Entities.IndexOf(entity);
                    Entities.RemoveAt(i);
                    Components1.RemoveAt(i);
                    Components2.RemoveAt(i);
                    Components3.RemoveAt(i);
                    Components4.RemoveAt(i);
                    Components5.RemoveAt(i);
                }
            }
            else if (tf)
            {
                Entities.Add(entity);
                Components1.Add((T1)c[0]);
                Components2.Add((T2)c[1]);
                Components3.Add((T3)c[2]);
                Components4.Add((T4)c[3]);
                Components5.Add((T5)c[4]);
            }
        }

        public sealed override void Update()
        {
            for (int i = 0; i < Entities.Count; ++i)
            {
                OnUpdate(i, Entities[i], Components1[i], Components2[i], Components3[i], Components4[i], Components5[i]);
            }
        }

        protected abstract void OnUpdate(int index, Entity entity, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5);
    }
}
