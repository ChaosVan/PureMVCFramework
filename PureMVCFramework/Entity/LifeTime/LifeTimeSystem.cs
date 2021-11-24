using PureMVCFramework.Advantages;
using PureMVCFramework.Entity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PureMVCFramework.Entity
{
    public class LifeTime : IComponent
    {
        public float Value;
    }

    public class LifeTimeSystem : ComponentSystem<LifeTime>
    {
        public readonly List<Entity> willDestroy = new List<Entity>();
        protected override void OnUpdate(int index, Entity entity, LifeTime component)
        {
            component.Value -= ((World)World).DeltaTime;

            if (component.Value <= 0)
            {
                willDestroy.Add(entity);
            }
        }

        public override void PostUpdate()
        {
            for (int i = 0; i < willDestroy.Count; i++)
            {
                EntityManager.Instance.DestroyEntity(willDestroy[i]);
            }
            willDestroy.Clear();
        }
    }
}
