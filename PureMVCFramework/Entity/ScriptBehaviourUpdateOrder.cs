using System;
using System.Collections.Generic;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace PureMVCFramework.Entity
{
    public static class ScriptBehaviourUpdateOrder
    {
        delegate bool RemoveFromPlayerLoopDelegate(ref PlayerLoopSystem playerLoop);

        /// <summary>
        /// Append the update function to the matching player loop system type.
        /// </summary>
        /// <param name="updateType">The update function type.</param>
        /// <param name="updateFunction">The update function.</param>
        /// <param name="playerLoop">The player loop.</param>
        /// <param name="playerLoopSystemType">The player loop system type.</param>
        /// <returns><see langword="true"/> if successfully appended to player loop, <see langword="false"/> otherwise.</returns>
        internal static bool AppendToPlayerLoop(Type updateType, PlayerLoopSystem.UpdateFunction updateFunction, ref PlayerLoopSystem playerLoop, Type playerLoopSystemType)
        {
            return AppendToPlayerLoopList(updateType, updateFunction, ref playerLoop, playerLoopSystemType);
        }

        /// <summary>
        /// Add this World's three default top-level system groups to a PlayerLoopSystem object.
        /// </summary>
        /// <remarks>
        /// This function performs the following modifications to the provided PlayerLoopSystem:
        /// - If an instance of InitializationSystemGroup exists in this World, it is appended to the
        ///   Initialization player loop phase.
        /// - If an instance of SimulationSystemGroup exists in this World, it is appended to the
        ///   Update player loop phase.
        /// - If an instance of PresentationSystemGroup exists in this World, it is appended to the
        ///   PreLateUpdate player loop phase.
        /// If instances of any or all of these system groups don't exist in this World, then no entry is added to the player
        /// loop for that system group.
        ///
        /// This function does not change the currently active player loop. If this behavior is desired, it's necessary
        /// to call PlayerLoop.SetPlayerLoop(playerLoop) after the systems have been removed.
        /// </remarks>
        /// <param name="world">The three top-level system groups from this World will be added to the provided player loop.</param>
        /// <param name="playerLoop">Existing player loop to modify (e.g.  (e.g. PlayerLoop.GetCurrentPlayerLoop())</param>
        public static void AppendWorldToPlayerLoop(World world, ref PlayerLoopSystem playerLoop)
        {
            if (world == null)
                return;

            var initGroup = world.GetExistingSystem<InitializationSystemGroup>();
            if (initGroup != null)
                AppendSystemToPlayerLoop(initGroup, ref playerLoop, typeof(Initialization));

            var simGroup = world.GetExistingSystem<SimulationSystemGroup>();
            if (simGroup != null)
                AppendSystemToPlayerLoop(simGroup, ref playerLoop, typeof(Update));

            //var presGroup = world.GetExistingSystem<PresentationSystemGroup>();
            //if (presGroup != null)
            //    AppendSystemToPlayerLoop(presGroup, ref playerLoop, typeof(PreLateUpdate));
        }

        /// <summary>
        /// Append this World's three default top-level system groups to the current Unity player loop.
        /// </summary>
        /// <remarks>
        /// This is a convenience wrapper around AddWorldToPlayerLoop() that retrieves the current player loop,
        /// adds a World's top-level system groups to it, and sets the modified copy as the new active player loop.
        ///
        /// Note that modifications to the active player loop do not take effect until to the next iteration through the player loop.
        /// </remarks>
        /// <param name="world">The three top-level system groups from this World will be added to the provided player loop.</param>
        public static void AppendWorldToCurrentPlayerLoop(World world)
        {
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            AppendWorldToPlayerLoop(world, ref playerLoop);
            PlayerLoop.SetPlayerLoop(playerLoop);
        }

        /// <summary>
        /// Remove all of this World's systems from the specified player loop.
        /// </summary>
        /// <remarks>
        /// Only the systems from this World will be removed; other player loop modifications (including systems added
        /// by other Worlds) will not be affected.
        ///
        /// This function does not change the currently active player loop. If this behavior is desired, it's necessary
        /// to call PlayerLoop.SetPlayerLoop(playerLoop) after the systems have been removed.
        /// </remarks>
        /// <param name="world">All systems in the provided player loop owned by this World will be removed from the player loop.</param>
        /// <param name="playerLoop">Existing player loop to modify (e.g. PlayerLoop.GetCurrentPlayerLoop())</param>
        public static void RemoveWorldFromPlayerLoop(World world, ref PlayerLoopSystem playerLoop)
        {
            RemoveWorldFromPlayerLoopList(world, ref playerLoop);
        }

        /// <summary>
        /// Add an ECS system to a specific point in the Unity player loop, so that it is updated every frame.
        /// </summary>
        /// <remarks>
        /// This function does not change the currently active player loop. If this behavior is desired, it's necessary
        /// to call PlayerLoop.SetPlayerLoop(playerLoop) after the systems have been removed.
        /// </remarks>
        /// <param name="system">The ECS system to add to the player loop.</param>
        /// <param name="playerLoop">Existing player loop to modify (e.g. PlayerLoop.GetCurrentPlayerLoop())</param>
        /// <param name="playerLoopSystemType">The Type of the PlayerLoopSystem subsystem to which the ECS system should be appended.
        /// See the UnityEngine.PlayerLoop namespace for valid values.</param>
        public static void AppendSystemToPlayerLoop(ComponentSystemBase system, ref PlayerLoopSystem playerLoop, Type playerLoopSystemType)
        {
            var wrapper = new DummyDelegateWrapper(system);
            if (!AppendToPlayerLoop(system.GetType(), wrapper.TriggerUpdate, ref playerLoop, playerLoopSystemType))
                throw new ArgumentException($"Could not find PlayerLoopSystem with type={playerLoopSystemType}");
        }

        static bool AppendToPlayerLoopList(Type updateType, PlayerLoopSystem.UpdateFunction updateFunction, ref PlayerLoopSystem playerLoop, Type playerLoopSystemType)
        {
            if (updateType == null || updateFunction == null || playerLoopSystemType == null)
                return false;

            if (playerLoop.type == playerLoopSystemType)
            {
                var oldListLength = playerLoop.subSystemList != null ? playerLoop.subSystemList.Length : 0;
                var newSubsystemList = new PlayerLoopSystem[oldListLength + 1];
                for (var i = 0; i < oldListLength; ++i)
                    newSubsystemList[i] = playerLoop.subSystemList[i];
                newSubsystemList[oldListLength] = new PlayerLoopSystem
                {
                    type = updateType,
                    updateDelegate = updateFunction
                };
                playerLoop.subSystemList = newSubsystemList;
                return true;
            }

            if (playerLoop.subSystemList != null)
            {
                for (var i = 0; i < playerLoop.subSystemList.Length; ++i)
                {
                    if (AppendToPlayerLoopList(updateType, updateFunction, ref playerLoop.subSystemList[i], playerLoopSystemType))
                        return true;
                }
            }
            return false;
        }

        static bool RemoveFromPlayerLoopList(RemoveFromPlayerLoopDelegate removeDelegate, ref PlayerLoopSystem playerLoop)
        {
            if (removeDelegate == null || playerLoop.subSystemList == null || playerLoop.subSystemList.Length == 0)
                return false;

            var result = false;
            var newSubSystemList = new List<PlayerLoopSystem>(playerLoop.subSystemList.Length);
            for (var i = 0; i < playerLoop.subSystemList.Length; ++i)
            {
                ref var playerLoopSubSystem = ref playerLoop.subSystemList[i];
                result |= RemoveFromPlayerLoopList(removeDelegate, ref playerLoopSubSystem);
                if (!removeDelegate(ref playerLoopSubSystem))
                    newSubSystemList.Add(playerLoopSubSystem);
            }

            if (newSubSystemList.Count != playerLoop.subSystemList.Length)
            {
                playerLoop.subSystemList = newSubSystemList.ToArray();
                result = true;
            }
            return result;
        }

        static void RemoveWorldFromPlayerLoopList(World world, ref PlayerLoopSystem playerLoop)
        {
            RemoveFromPlayerLoopList((ref PlayerLoopSystem pl) => IsDelegateForWorldSystem(world, ref pl), ref playerLoop);
        }

        static bool IsDelegateForWorldSystem(World world, ref PlayerLoopSystem playerLoop)
        {
            if (typeof(ComponentSystemBase).IsAssignableFrom(playerLoop.type))
            {
                var wrapper = playerLoop.updateDelegate.Target as DummyDelegateWrapper;
                if (wrapper.System.World == world)
                    return true;
            }
            return false;
        }

        internal class DummyDelegateWrapper
        {
            internal ComponentSystemBase System => m_System;
            private readonly ComponentSystemBase m_System;

            public DummyDelegateWrapper(ComponentSystemBase sys)
            {
                m_System = sys;
            }

            public void TriggerUpdate()
            {
                if (m_System.m_State.Enabled)
                {
                    m_System.Update();
                }
            }
        }
    }
}
