using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace AleVerDes.Voxels
{
    public static class UnsafeListHelper
    {
        public static unsafe int AddWithIndex<T>(ref NativeList<T>.ParallelWriter list, in T element) where T : unmanaged
        {
            var listData = list.ListData;
            var idx = Interlocked.Increment(ref listData->m_length) - 1;
            UnsafeUtility.WriteArrayElement(listData->Ptr, idx, element);
            return idx;
        }

        public static unsafe void Add<T>(ref NativeList<T>.ParallelWriter list, in T element) where T : unmanaged
        {
            var listData = list.ListData;
            var idx = Interlocked.Increment(ref listData->m_length) - 1;
            UnsafeUtility.WriteArrayElement(listData->Ptr, idx, element);
        }
    }
}