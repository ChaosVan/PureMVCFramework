namespace PureMVCFramework.Entity
{
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    [UpdateBefore(typeof(LifeTimeSystem))]
    public class TransformSystemGroup : ComponentSystemGroup
    {
    }
}
