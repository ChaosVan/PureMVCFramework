using System.Collections.Generic;

namespace PureMVCFramework.Entity
{
    public class LifeTime : IComponentData
    {
        public float Value;
    }

    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    public class LifeTimeSystem : SystemBase<LifeTime>
    {
        public readonly List<Entity> willDestroy = new List<Entity>();
        protected override void OnUpdate(int index, Entity entity, LifeTime component)
        {
            component.Value -= Time.DeltaTime;

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
