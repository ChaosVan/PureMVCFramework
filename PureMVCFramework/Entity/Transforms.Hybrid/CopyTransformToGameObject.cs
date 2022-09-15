using System;

namespace PureMVCFramework.Entity
{
    public class CopyTransformToGameObject : IComponentData
    {
    }

    [Obsolete("Use CopyTransformToGameObject")]
    public class CopyToTransformComponent : CopyTransformToGameObject { }
}
