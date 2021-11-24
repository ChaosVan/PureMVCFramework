using PureMVCFramework.Advantages;
using Unity.Mathematics;

namespace PureMVCFramework.Entity
{
    public class Rotation : IComponent, IInitializeable
    {
        public quaternion Value;

        public void OnInitialized()
        {
            Value = quaternion.identity;
        }
    }
}
