using UnityEngine.Scripting;

namespace PureMVCFramework.Entity
{
    [UnityEngine.ExecuteAlways]
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    public class BeginInitializationEntityCommandBufferSystem : EntityCommandBufferSystem { }

    [UnityEngine.ExecuteAlways]
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
    public class EndInitializationEntityCommandBufferSystem : EntityCommandBufferSystem { }

    public class InitializationSystemGroup : ComponentSystemGroup
    {
        [Preserve]
        public InitializationSystemGroup()
        {
        }
    }

    [UnityEngine.ExecuteAlways]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public class BeginSimulationEntityCommandBufferSystem : EntityCommandBufferSystem { }

    [UnityEngine.ExecuteAlways]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    public class EndSimulationEntityCommandBufferSystem : EntityCommandBufferSystem { }

    [UnityEngine.ExecuteAlways]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    [UpdateBefore(typeof(EndSimulationEntityCommandBufferSystem))]
    public class LateSimulationSystemGroup : ComponentSystemGroup { }

    public class SimulationSystemGroup : ComponentSystemGroup
    {
        [Preserve]
        public SimulationSystemGroup()
        {
        }
    }
}
