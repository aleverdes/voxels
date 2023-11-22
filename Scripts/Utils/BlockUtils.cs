using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace AleVerDes.Voxels
{
    public static class BlockUtils
    {
        public static int3[] GetHorizontalNeighboursWithTarget(int3 blockPosition)
        {
            return new[]
            {
                blockPosition,
                blockPosition + new int3(-1, 0, 0),
                blockPosition + new int3(1, 0, 0),
                blockPosition + new int3(0, 0, 1),
                blockPosition + new int3(0, 0, -1),
                blockPosition + new int3(-1, 0, -1),
                blockPosition + new int3(1, 0, 1),
                blockPosition + new int3(-1, 0, 1),
                blockPosition + new int3(1, 0, -1),
            };
        }
        
        public static int3[] GetAllNeighboursWithTarget(int3 blockPosition)
        {
            return new[]
            {
                blockPosition,
                blockPosition + new int3(-1, 0, 0),
                blockPosition + new int3(1, 0, 0),
                blockPosition + new int3(0, 0, 1),
                blockPosition + new int3(0, 0, -1),
                blockPosition + new int3(-1, 0, -1),
                blockPosition + new int3(1, 0, 1),
                blockPosition + new int3(-1, 0, 1),
                blockPosition + new int3(1, 0, -1),
                blockPosition + new int3(-1, 1, 0),
                blockPosition + new int3(1, 1, 0),
                blockPosition + new int3(0, 1, 1),
                blockPosition + new int3(0, 1, -1),
                blockPosition + new int3(-1, 1, -1),
                blockPosition + new int3(1, 1, 1),
                blockPosition + new int3(-1, 1, 1),
                blockPosition + new int3(1, 1, -1),
                blockPosition + new int3(-1, -1, 0),
                blockPosition + new int3(1, -1, 0),
                blockPosition + new int3(0, -1, 1),
                blockPosition + new int3(0, -1, -1),
                blockPosition + new int3(-1, -1, -1),
                blockPosition + new int3(1, -1, 1),
                blockPosition + new int3(-1, -1, 1),
                blockPosition + new int3(1, -1, -1),
            };
        }
        
        public static int3[] GetHorizontalNeighbours(int3 blockPosition)
        {
            return new[]
            {
                blockPosition + new int3(-1, 0, 0),
                blockPosition + new int3(1, 0, 0),
                blockPosition + new int3(0, 0, 1),
                blockPosition + new int3(0, 0, -1),
                blockPosition + new int3(-1, 0, -1),
                blockPosition + new int3(1, 0, 1),
                blockPosition + new int3(-1, 0, 1),
                blockPosition + new int3(1, 0, -1),
            };
        }
        
        public static int3[] GetAllNeighbours(int3 blockPosition)
        {
            return new[]
            {
                blockPosition + new int3(-1, 0, 0),
                blockPosition + new int3(1, 0, 0),
                blockPosition + new int3(0, 0, 1),
                blockPosition + new int3(0, 0, -1),
                blockPosition + new int3(-1, 0, -1),
                blockPosition + new int3(1, 0, 1),
                blockPosition + new int3(-1, 0, 1),
                blockPosition + new int3(1, 0, -1),
                blockPosition + new int3(-1, 1, 0),
                blockPosition + new int3(1, 1, 0),
                blockPosition + new int3(0, 1, 1),
                blockPosition + new int3(0, 1, -1),
                blockPosition + new int3(-1, 1, -1),
                blockPosition + new int3(1, 1, 1),
                blockPosition + new int3(-1, 1, 1),
                blockPosition + new int3(1, 1, -1),
                blockPosition + new int3(-1, -1, 0),
                blockPosition + new int3(1, -1, 0),
                blockPosition + new int3(0, -1, 1),
                blockPosition + new int3(0, -1, -1),
                blockPosition + new int3(-1, -1, -1),
                blockPosition + new int3(1, -1, 1),
                blockPosition + new int3(-1, -1, 1),
                blockPosition + new int3(1, -1, -1),
            };
        }
    }
}