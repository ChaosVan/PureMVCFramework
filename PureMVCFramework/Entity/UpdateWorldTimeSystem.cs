using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace PureMVCFramework.Entity
{
    [Preserve]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class UpdateWorldTimeSystem : ComponentSystem
    {
        protected override void OnStartRunning()
        {
            // Ensure that the final elapsedTime of the very first OnUpdate call is the
            // original Time.ElapsedTime value (usually zero) without a deltaTime applied.
            // Effectively, this code preemptively counteracts the first OnUpdate call.
            var currentElapsedTime = Time.ElapsedTime;
            var deltaTime = math.min(UnityEngine.Time.deltaTime, World.MaximumDeltaTime);
            World.SetTime(new TimeData(
                elapsedTime: currentElapsedTime - deltaTime,
                deltaTime: deltaTime
            ));
        }

        protected override void OnUpdate()
        {
            var currentElapsedTime = Time.ElapsedTime;
            var deltaTime = math.min(UnityEngine.Time.deltaTime, World.MaximumDeltaTime);
            World.SetTime(new TimeData(
                elapsedTime: currentElapsedTime + deltaTime,
                deltaTime: deltaTime
            ));

            Debug.Log(Time.ElapsedTime);
        }
    }
}
