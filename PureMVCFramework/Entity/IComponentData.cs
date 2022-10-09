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
            entity = EntityManager.Create(gameObject, out var buffer);
            self = entity.GetOrAddComponentData<T>(buffer);
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
            entity = EntityManager.Create(gameObject, out var buffer);
            t1 = entity.GetOrAddComponentData<T1>(buffer);
            t2 = entity.GetOrAddComponentData<T2>(buffer);
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
