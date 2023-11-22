using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace AleVerDes.Voxels
{
    public static class ChunkUtils
    {
        public static int2[] GetHorizontalNeighboursWithTarget(int2 blockPosition)
        {
            return new[]
            {
                blockPosition,
                blockPosition + new int2(-1, 0),
                blockPosition + new int2(1, 0),
                blockPosition + new int2(0, 1),
                blockPosition + new int2(0, -1),
                blockPosition + new int2(-1, -1),
                blockPosition + new int2(1, 1),
                blockPosition + new int2(-1, 1),
                blockPosition + new int2(1, -1),
            };
        }

        public static int2[] GetHorizontalNeighbours(int2 blockPosition)
        {
            return new[]
            {
                blockPosition + new int2(-1, 0),
                blockPosition + new int2(1, 0),
                blockPosition + new int2(0, 1),
                blockPosition + new int2(0, -1),
                blockPosition + new int2(-1, -1),
                blockPosition + new int2(1, 1),
                blockPosition + new int2(-1, 1),
                blockPosition + new int2(1, -1),
            };
        }
    }
}