using PureMVCFramework.Advantages;
using Unity.Mathematics;

namespace PureMVCFramework.Entity
{
    public class Position : IComponent, IInitializeable
    {
        public float3 Value;

        public void OnInitialized()
        {
            Value = 0;
        }
    }
}
