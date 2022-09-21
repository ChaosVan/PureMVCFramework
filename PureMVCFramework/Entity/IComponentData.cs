using System;
using UnityEngine;

namespace PureMVCFramework.Entity
{
    public interface IComponentData { }

    public class ComponentWrapper<T> : MonoBehaviour where T : IComponentData
    {
        private bool started;
        protected T self;
        protected Entity entity;

        protected virtual void Start()
        {
            entity = EntityManager.Create(gameObject);
            self = entity.GetOrAddComponentData<T>();
            started = true;
        }

        private void Update()
        {
            if (!started)
                Start();
        }

        private void OnDisable()
        {
            started = false;
        }
    }

    public class ComponentWrapper<T1, T2> : MonoBehaviour where T1: IComponentData where T2 : IComponentData
    {
        private bool started;
        protected T1 t1;
        protected T2 t2;
        protected Entity entity;

        protected virtual void Start()
        {
            entity = EntityManager.Create(gameObject);
            t1 = entity.GetOrAddComponentData<T1>();
            t2 = entity.GetOrAddComponentData<T2>();
            started = true;
        }

        private void Update()
        {
            if (!started)
                Start();
        }

        private void OnDisable()
        {
            started = false;
        }
    }
}
