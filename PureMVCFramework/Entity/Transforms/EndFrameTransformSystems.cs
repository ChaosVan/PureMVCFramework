using UnityEngine.Scripting;

namespace PureMVCFramework.Entity
{
    [Preserve]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    [UpdateBefore(typeof(LateSimulationSystemGroup))]
    public class TransformSystemGroup : ComponentSystemGroup
    {
    }
}
