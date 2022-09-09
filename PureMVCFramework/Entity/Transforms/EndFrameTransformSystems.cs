namespace PureMVCFramework.Entity
{
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    [UpdateBefore(typeof(LateSimulationSystemGroup))]
    public class TransformSystemGroup : ComponentSystemGroup
    {
    }
}
