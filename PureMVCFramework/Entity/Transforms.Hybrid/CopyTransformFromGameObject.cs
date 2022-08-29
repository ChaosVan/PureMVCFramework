using System;

namespace PureMVCFramework.Entity
{
    [Obsolete("Use CopyTransformFromGameObject")]
    public class CopyFromTransformComponent : CopyTransformFromGameObject { }

    public class CopyTransformFromGameObject : IComponentData
    {
    }
}
