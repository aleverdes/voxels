using Unity.Jobs;
using Unity.Mathematics;

namespace AleVerDes.Voxels
{
    public struct CalculateFacesCountJob : IJob
    {
        public VoxelTerrain VoxelTerrain;
        public int2 ChunkPosition;
        public int3 ChunkSize;

        public int FacesCount;

        public void Execute()
        {
            var chunk = VoxelTerrain.GetChunk(VoxelTerrain.GetChunkIndex(ChunkPosition));
            var firstBlockGlobalPosition = new int3(ChunkPosition.x * ChunkSize.x, 0, ChunkPosition.y * ChunkSize.z);

            for (var x = 0; x < ChunkSize.x; x++)
            for (var y = 0; y < ChunkSize.y; y++)
            for (var z = 0; z < ChunkSize.z; z++)
            {
                if (chunk.GetBlock(GetBlockIndex(new int3(x, y, z))).VoxelIndex == 0)
                    continue;

                var left = firstBlockGlobalPosition + new int3(x - 1, y, z);
                if (!VoxelTerrain.IsSolidBlock(left) && VoxelTerrain.IsBlockExistsInChunks(left))
                    FacesCount++;

                var right = firstBlockGlobalPosition + new int3(x + 1, y, z);
                if (!VoxelTerrain.IsSolidBlock(right) && VoxelTerrain.IsBlockExistsInChunks(right))
                    FacesCount++;

                var bottom = firstBlockGlobalPosition + new int3(x, y - 1, z);
                if (!VoxelTerrain.IsSolidBlock(bottom) && VoxelTerrain.IsBlockExistsInChunks(bottom))
                    FacesCount++;

                var top = firstBlockGlobalPosition + new int3(x, y + 1, z);
                if (!VoxelTerrain.IsSolidBlock(top) && VoxelTerrain.IsBlockExistsInChunks(top))
                    FacesCount++;

                var back = firstBlockGlobalPosition + new int3(x, y, z - 1);
                if (!VoxelTerrain.IsSolidBlock(back) && VoxelTerrain.IsBlockExistsInChunks(back))
                    FacesCount++;

                var front = firstBlockGlobalPosition + new int3(x, y, z + 1);
                if (!VoxelTerrain.IsSolidBlock(front) && VoxelTerrain.IsBlockExistsInChunks(front))
                    FacesCount++;
            }
        }
    }
}