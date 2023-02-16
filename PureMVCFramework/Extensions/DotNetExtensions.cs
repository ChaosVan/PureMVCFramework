using System.Collections.Concurrent;

namespace PureMVCFramework.Extensions
{
    public static class DotNetExtensions
    {
#if !UNITY_2021_1_OR_NEWER
        public static void Clear<T>(this ConcurrentQueue<T> concurrentQueue)
        {
            while (concurrentQueue.TryDequeue(out _)) ;
        }
#endif
    }
}
