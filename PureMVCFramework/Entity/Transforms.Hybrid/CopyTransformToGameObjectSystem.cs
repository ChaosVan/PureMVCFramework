using UnityEngine;
using UnityEngine.Scripting;

namespace PureMVCFramework.Entity
{
    [Preserve]
    [UpdateInGroup(typeof(TransformSystemGroup))]
    [UpdateAfter(typeof(TRSToLocalToWorldSystem))]
    public class CopyTransformToGameObjectSystem : HybridSystemBase<Transform, LocalToWorld, CopyTransformToGameObject>
    {
#if ENABLE_JOBS
        [BurstCompile]
        struct CopyTransforms : IJobParallelForTransform
        {
            [DeallocateOnJobCompletion]
            [ReadOnly] public NativeArray<float3> positions;
            [DeallocateOnJobCompletion]
            [ReadOnly] public NativeArray<quaternion> rotations;

            public void Execute(int index, TransformAccess transform)
            {
                transform.position = positions[index];
                transform.rotation = rotations[index];
            }
        }

        private JobHandle handle;
        private CopyTransforms Job;

        private TransformAccessArray transofrms;

        protected override void PreUpdate()
        {
            base.PreUpdate();
            if (Entities.Count > 0)
            {
                handle = new JobHandle();
                Job = new CopyTransforms
                {
                    positions = new NativeArray<float3>(Entities.Count, Allocator.TempJob),
                    rotations = new NativeArray<quaternion>(Entities.Count, Allocator.TempJob),
                };

                transofrms = new TransformAccessArray(Entities.Count);
            }

        }

        protected override void PostUpdate()
        {
            base.PostUpdate();
            if (Entities.Count > 0)
            {
                Job.Schedule(transofrms, handle);
                handle.Complete();

                transofrms.Dispose();
            }
        }
#endif

        protected override void OnUpdate(int index, Entity entity, Transform component1, LocalToWorld component2, CopyTransformToGameObject component3)
        {
            if (component1 != null)
            {
                var mat = (Matrix4x4)component2.Value;
                component1.position = component2.Position;
                component1.rotation = mat.rotation;
                component1.localScale = mat.lossyScale;
            }
        }
    }
}
