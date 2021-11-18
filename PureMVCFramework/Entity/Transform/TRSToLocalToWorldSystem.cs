using Unity.Mathematics;
using UnityEngine;

namespace PureMVCFramework.Entity
{
    public class TRSToLocalToWorldSystem : ComponentSystem<LocalToWorld, Position, Rotation, Scale>
    {
        protected override void OnUpdate(int index, Entity entity, LocalToWorld c1, Position c2, Rotation c3, Scale c4)
        {
            c1.Value = Matrix4x4.TRS(c2.Value, math.normalize(c3.Value), c4.Value);
        }
    }
}
