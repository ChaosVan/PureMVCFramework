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
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup), OrderFirst = true)]
    public class BeginFixedStepSimulationEntityCommandBufferSystem : EntityCommandBufferSystem { }

    [UnityEngine.ExecuteAlways]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup), OrderLast = true)]
    public class EndFixedStepSimulationEntityCommandBufferSystem : EntityCommandBufferSystem { }

    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    [UpdateAfter(typeof(BeginSimulationEntityCommandBufferSystem))]
    public class FixedStepSimulationSystemGroup : ComponentSystemGroup
    {
        /// <summary>
        /// Set the timestep use by this group, in seconds. The default value is 1/60 seconds.
        /// This value will be clamped to the range [0.0001f ... 10.0f].
        /// </summary>
        public float Timestep
        {
            get => RateManager != null ? RateManager.Timestep : 0;
            set
            {
                if (RateManager != null)
                    RateManager.Timestep = value;
            }
        }

        [Preserve]
        public FixedStepSimulationSystemGroup()
        {
            float defaultFixedTimestep = 1.0f / 60.0f;
            RateManager = new RateUtils.FixedRateCatchUpManager(defaultFixedTimestep);
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
