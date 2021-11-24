using PureMVCFramework.Advantages;
using Unity.Mathematics;

namespace PureMVCFramework.Entity
{
    public class Scale : IComponent, IInitializeable
    {
        public float3 Value;

        public void OnInitialized()
        {
            Value = 1;
        }
    }
}
