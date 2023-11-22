using System;

namespace AleVerDes.Voxels
{
    [Serializable]
    public struct Block
    {
        public byte VoxelIndex;
        public byte NoiseWeight;
    }
}