using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace AleVerDes.Voxels
{
    public struct GenerateChunkMeshJob : IJob
    {
        public VoxelTerrain VoxelTerrain;
        public int2 ChunkPosition;
        public Mesh Mesh;
        public int FacesCount;

        public Mesh.MeshDataArray MeshDataArray;
        public NativeArray<VertexAttributeDescriptor> VertexAttributes;

        public void Execute()
        {
            var bounds = new Bounds(new Vector3(0.5f, 0.5f), new Vector3(1f, 1f));
            
            MeshDataArray = Mesh.AllocateWritableMeshData(1);
            VertexAttributes = new NativeArray<VertexAttributeDescriptor>(4, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            VertexAttributes[0] = new VertexAttributeDescriptor(VertexAttribute.Position, dimension: 3, stream: 0);
            VertexAttributes[1] = new VertexAttributeDescriptor(VertexAttribute.Normal, dimension: 3, stream: 1);
            VertexAttributes[2] = new VertexAttributeDescriptor(VertexAttribute.Tangent, dimension: 4, stream: 2);
            VertexAttributes[3] = new VertexAttributeDescriptor(VertexAttribute.TexCoord0, dimension: 2, stream: 3);
            var meshData = MeshDataArray[0];
            meshData.SetVertexBufferParams(FacesCount * 4, VertexAttributes);
            meshData.SetIndexBufferParams(FacesCount * 6, IndexFormat.UInt32);
            meshData.subMeshCount = 1;
            meshData.SetSubMesh(0, new SubMeshDescriptor(0, FacesCount * 6)
            {
                bounds = bounds,
                vertexCount = FacesCount * 4,
            });
            VertexAttributes.Dispose();

            var blockSize = VoxelTerrain.BlockSize;
            var chunkSize = VoxelTerrain.ChunkSize;
            var verticesNoise = VoxelTerrain.VerticesNoise;
            var atlas = VoxelTerrain.TextureAtlas;
            
            var trianglesCount = 0;
            var normalsCount = 0;
            var tangentsCount = 0;
            var uvsCount = 0;
            var positionsCount = 0;
            
            var positions = meshData.GetVertexData<float3>(0);
            var normals = meshData.GetVertexData<float3>(1);
            var tangents = meshData.GetVertexData<float4>(2);
            var texCoords = meshData.GetVertexData<float2>(3);
            var triangles = meshData.GetIndexData<uint>();
            
            var chunkOffset = ChunkPosition * chunkSize.xz;
            var firstChunkBlockPosition = new int3(chunkOffset.x, 0, chunkOffset.y);
            
            for (var x = 0; x < chunkSize.x; x++)
            for (var y = 0; y < chunkSize.y; y++)
            for (var z = 0; z < chunkSize.z; z++)
            {
                ref var blockVoxel = ref VoxelTerrain.GetChunk(ChunkPosition).GetBlock(new int3(x, y, z)).VoxelIndex;
            
                if (blockVoxel == 0)
                    continue;

                var vertexOffset = GetVertexOffset(verticesNoise, firstChunkBlockPosition + new int3(x, y, z), blockSize);
                var position = new int3(x, y, z);
                
                vertexOffset.V110 *= GetNoiseWeight(VoxelTerrain, position, new int3(1, 1, -1)); 
                vertexOffset.V111 *= GetNoiseWeight(VoxelTerrain, position, new int3(1, 1, 1));
                vertexOffset.V100 *= GetNoiseWeight(VoxelTerrain, position, new int3(1, -1, -1));
                vertexOffset.V101 *= GetNoiseWeight(VoxelTerrain, position, new int3(1, -1, 1));
                vertexOffset.V010 *= GetNoiseWeight(VoxelTerrain, position, new int3(-1, 1, -1));
                vertexOffset.V011 *= GetNoiseWeight(VoxelTerrain, position, new int3(-1, 1, 1));
                vertexOffset.V000 *= GetNoiseWeight(VoxelTerrain, position, new int3(-1, -1, -1));
                vertexOffset.V001 *= GetNoiseWeight(VoxelTerrain, position, new int3(-1, -1, 1));

                vertexOffset.V000 *= VoxelTerrain.VerticesNoiseScale;
                vertexOffset.V001 *= VoxelTerrain.VerticesNoiseScale;
                vertexOffset.V011 *= VoxelTerrain.VerticesNoiseScale;
                vertexOffset.V010 *= VoxelTerrain.VerticesNoiseScale;
                vertexOffset.V100 *= VoxelTerrain.VerticesNoiseScale;
                vertexOffset.V101 *= VoxelTerrain.VerticesNoiseScale;
                vertexOffset.V111 *= VoxelTerrain.VerticesNoiseScale;
                vertexOffset.V110 *= VoxelTerrain.VerticesNoiseScale;
                
                var v000 = firstChunkBlockPosition + new float3(x, y, z) + vertexOffset.V000;
                var v001 = firstChunkBlockPosition + new float3(x, y, z + 1) + vertexOffset.V001;
                var v011 = firstChunkBlockPosition + new float3(x, y + 1, z + 1) + vertexOffset.V011;
                var v010 = firstChunkBlockPosition + new float3(x, y + 1, z) + vertexOffset.V010;
                var v100 = firstChunkBlockPosition + new float3(x + 1, y, z) + vertexOffset.V100;
                var v101 = firstChunkBlockPosition + new float3(x + 1, y, z + 1) + vertexOffset.V101;
                var v111 = firstChunkBlockPosition + new float3(x + 1, y + 1, z + 1) + vertexOffset.V111;
                var v110 = firstChunkBlockPosition + new float3(x + 1, y + 1, z) + vertexOffset.V110;  
                
                var textureData = atlas.GetVoxelTexturesUV(blockVoxel - 1, x + y + z);
                var uvPositions = new UVPositions
                {
                    Top = new float2(textureData.Top.x, -textureData.Top.y),
                    Bottom = new float2(textureData.Bottom.x, -textureData.Bottom.y),
                    Side = new float2(textureData.Side.x, -textureData.Side.y),
                };
                var uvSize = atlas.TextureSizeInAtlas;
            
                var left = firstChunkBlockPosition + new int3(x - 1, y, z);
                if (VoxelTerrain.IsBlockExistsInChunks(left) && !VoxelTerrain.IsSolidBlock(left))
                {
                    positions[positionsCount] = v000 * blockSize;
                    positionsCount++;
                    positions[positionsCount] = v001 * blockSize;
                    positionsCount++;
                    positions[positionsCount] = v011 * blockSize;
                    positionsCount++;
                    positions[positionsCount] = v010 * blockSize;
                    positionsCount++;

                    normals[normalsCount] = math.left();
                    normalsCount++;
                    normals[normalsCount] = math.left();
                    normalsCount++;
                    normals[normalsCount] = math.left();
                    normalsCount++;
                    normals[normalsCount] = math.left();
                    normalsCount++;
                    
                    AddTriangles(triangles, ref trianglesCount);
                    AddUVs(texCoords, ref uvsCount, uvPositions, uvSize, FaceDirection.Left);
                    AddTangents(tangents, ref tangentsCount);
                }
            
                var right = firstChunkBlockPosition + new int3(x + 1, y, z);
                if (VoxelTerrain.IsBlockExistsInChunks(right) && !VoxelTerrain.IsSolidBlock(right))
                {
                    positions[positionsCount] = v101 * blockSize;
                    positionsCount++;
                    positions[positionsCount] = v100 * blockSize;
                    positionsCount++;
                    positions[positionsCount] = v110 * blockSize;
                    positionsCount++;
                    positions[positionsCount] = v111 * blockSize;
                    positionsCount++;

                    normals[normalsCount] = math.right();
                    normalsCount++;
                    normals[normalsCount] = math.right();
                    normalsCount++;
                    normals[normalsCount] = math.right();
                    normalsCount++;
                    normals[normalsCount] = math.right();
                    normalsCount++;
                    
                    AddTriangles(triangles, ref trianglesCount);
                    AddUVs(texCoords, ref uvsCount, uvPositions, uvSize, FaceDirection.Right);
                    AddTangents(tangents, ref tangentsCount);
                }
            
                var top = firstChunkBlockPosition + new int3(x, y + 1, z);
                if (VoxelTerrain.IsBlockExistsInChunks(top) && !VoxelTerrain.IsSolidBlock(top) || !VoxelTerrain.IsBlockExistsInChunks(top))
                {
                    positions[positionsCount] = v010 * blockSize;
                    positionsCount++;
                    positions[positionsCount] = v011 * blockSize;
                    positionsCount++;
                    positions[positionsCount] = v111 * blockSize;
                    positionsCount++;
                    positions[positionsCount] = v110 * blockSize;
                    positionsCount++;

                    normals[normalsCount] = math.up();
                    normalsCount++;
                    normals[normalsCount] = math.up();
                    normalsCount++;
                    normals[normalsCount] = math.up();
                    normalsCount++;
                    normals[normalsCount] = math.up();
                    normalsCount++;
                    
                    AddTriangles(triangles, ref trianglesCount);
                    AddUVs(texCoords, ref uvsCount, uvPositions, uvSize, FaceDirection.Top);
                    AddTangents(tangents, ref tangentsCount);
                }
            
                var bottom = firstChunkBlockPosition + new int3(x, y - 1, z);
                if (VoxelTerrain.IsBlockExistsInChunks(bottom) && !VoxelTerrain.IsSolidBlock(bottom))
                {
                    positions[positionsCount] = v001 * blockSize;
                    positionsCount++;
                    positions[positionsCount] = v000 * blockSize;
                    positionsCount++;
                    positions[positionsCount] = v100 * blockSize;
                    positionsCount++;
                    positions[positionsCount] = v101 * blockSize;
                    positionsCount++;

                    normals[normalsCount] = math.down();
                    normalsCount++;
                    normals[normalsCount] = math.down();
                    normalsCount++;
                    normals[normalsCount] = math.down();
                    normalsCount++;
                    normals[normalsCount] = math.down();
                    normalsCount++;
                    
                    AddTriangles(triangles, ref trianglesCount);
                    AddUVs(texCoords, ref uvsCount, uvPositions, uvSize, FaceDirection.Bottom);
                    AddTangents(tangents, ref tangentsCount);
                }
            
                var back = firstChunkBlockPosition + new int3(x, y, z - 1);
                if (VoxelTerrain.IsBlockExistsInChunks(back) && !VoxelTerrain.IsSolidBlock(back))
                {
                    positions[positionsCount] = v100 * blockSize;
                    positionsCount++;
                    positions[positionsCount] = v000 * blockSize;
                    positionsCount++;
                    positions[positionsCount] = v010 * blockSize;
                    positionsCount++;
                    positions[positionsCount] = v110 * blockSize;
                    positionsCount++;

                    normals[normalsCount] = math.back();
                    normalsCount++;
                    normals[normalsCount] = math.back();
                    normalsCount++;
                    normals[normalsCount] = math.back();
                    normalsCount++;
                    normals[normalsCount] = math.back();
                    normalsCount++;
                    
                    AddTriangles(triangles, ref trianglesCount);
                    AddUVs(texCoords, ref uvsCount, uvPositions, uvSize, FaceDirection.Back);
                    AddTangents(tangents, ref tangentsCount);
                }
            
                var front = firstChunkBlockPosition + new int3(x, y, z + 1);
                if (VoxelTerrain.IsBlockExistsInChunks(front) && !VoxelTerrain.IsSolidBlock(front))
                {
                    positions[positionsCount] = v001 * blockSize;
                    positionsCount++;
                    positions[positionsCount] = v101 * blockSize;
                    positionsCount++;
                    positions[positionsCount] = v111 * blockSize;
                    positionsCount++;
                    positions[positionsCount] = v011 * blockSize;
                    positionsCount++;

                    normals[normalsCount] = math.forward();
                    normalsCount++;
                    normals[normalsCount] = math.forward();
                    normalsCount++;
                    normals[normalsCount] = math.forward();
                    normalsCount++;
                    normals[normalsCount] = math.forward();
                    normalsCount++;

                    AddTriangles(triangles, ref trianglesCount);
                    AddUVs(texCoords, ref uvsCount, uvPositions, uvSize, FaceDirection.Front);
                    AddTangents(tangents, ref tangentsCount);
                }
            }
            
            Mesh.ApplyAndDisposeWritableMeshData(MeshDataArray, Mesh);
        }
        
        private static float GetNoiseWeight(VoxelTerrain voxelTerrain, int3 blockGlobalPosition, int3 delta)
        {
            var noiseWeight = GetNoiseWeightForBlock(voxelTerrain, blockGlobalPosition);
            noiseWeight = math.min(noiseWeight, GetNoiseWeightForBlock(voxelTerrain, blockGlobalPosition + new int3(delta.x, delta.y, delta.z)));
            noiseWeight = math.min(noiseWeight, GetNoiseWeightForBlock(voxelTerrain, blockGlobalPosition + new int3(0, delta.y, 0)));
            noiseWeight = math.min(noiseWeight, GetNoiseWeightForBlock(voxelTerrain, blockGlobalPosition + new int3(0, 0, delta.z)));
            noiseWeight = math.min(noiseWeight, GetNoiseWeightForBlock(voxelTerrain, blockGlobalPosition + new int3(delta.x, delta.y, 0)));
            noiseWeight = math.min(noiseWeight, GetNoiseWeightForBlock(voxelTerrain, blockGlobalPosition + new int3(delta.x, 0, delta.z)));
            noiseWeight = math.min(noiseWeight, GetNoiseWeightForBlock(voxelTerrain, blockGlobalPosition + new int3(delta.x, 0, 0)));
            return noiseWeight;
        }
        
        private static float GetNoiseWeightForBlock(VoxelTerrain voxelTerrain, int3 blockGlobalPosition)
        {
            return 0;
            return voxelTerrain.IsBlockExistsInChunks(blockGlobalPosition) ? voxelTerrain.GetBlockNoiseWeight(blockGlobalPosition) / 255f : 1f;
        }
        
        private static VertexOffset GetVertexOffset(NoiseProvider noiseGenerator, int3 blockPosition, float3 blockSize)
        {
            var v111 = GetNoisedVertex(blockPosition + new int3(1, 1, 1));
            var v110 = GetNoisedVertex(blockPosition + new int3(1, 1, 0));
            var v101 = GetNoisedVertex(blockPosition + new int3(1, 0, 1));
            var v100 = GetNoisedVertex(blockPosition + new int3(1, 0, 0));
            var v011 = GetNoisedVertex(blockPosition + new int3(0, 1, 1));
            var v001 = GetNoisedVertex(blockPosition + new int3(0, 0, 1));
            var v010 = GetNoisedVertex(blockPosition + new int3(0, 1, 0));
            var v000 = GetNoisedVertex(blockPosition);
            return new VertexOffset
            {
                V111 = v111,
                V110 = v110,
                V100 = v100,
                V101 = v101,
                V011 = v011,
                V010 = v010,
                V000 = v000,
                V001 = v001,
            };
            
            float3 GetNoisedVertex(int3 vertexPosition)
            {
                return new float3
                {
                    x = 2f * noiseGenerator.GetNoise(vertexPosition * blockSize) - 1,
                    y = 2f * noiseGenerator.GetNoise(vertexPosition * blockSize + 111f * new float3(1, 1, 1) ) - 1,
                    z = 2f * noiseGenerator.GetNoise(vertexPosition * blockSize - 111f * new float3(1, 1, 1)) - 1,
                };
            }
        }
        
        private static void AddTangents(NativeArray<float4> tangents, ref int tangentsCount)
        {
            for (var i = 0; i < 4; i++)
            {
                tangents[tangentsCount] = new float4(1, 0, 0, -1);
                tangentsCount++;
            }
        }
        
        private static void AddTriangles(NativeArray<uint> triangles, ref int trianglesCount)
        {
            var index = (uint) trianglesCount;
            triangles[trianglesCount] = index * 4;
            trianglesCount++;
            triangles[trianglesCount] = index * 4 + 1;
            trianglesCount++;
            triangles[trianglesCount] = index * 4 + 2;
            trianglesCount++;
            triangles[trianglesCount] = index * 4 + 2;
            trianglesCount++;
            triangles[trianglesCount] = index * 4 + 3;
            trianglesCount++;
            triangles[trianglesCount] = index * 4;
            trianglesCount++;
        }
        
        private static void AddUVs(NativeArray<float2> uvs, ref int uvCount, UVPositions uvPositions, Vector2 uvSize, FaceDirection faceDirection)
        {
            switch (faceDirection)
            {
                case FaceDirection.Top:
                    uvs[uvCount] = uvPositions.Top + new float2(0, 1f - uvSize.y);
                    uvCount++;
                    uvs[uvCount] = uvPositions.Top + new float2(0, 1f);
                    uvCount++;
                    uvs[uvCount] = uvPositions.Top + new float2(uvSize.x, 1f);
                    uvCount++;
                    uvs[uvCount] = uvPositions.Top + new float2(uvSize.x, 1f - uvSize.y);
                    uvCount++;
                    break;
                case FaceDirection.Bottom:
                    uvs[uvCount] = uvPositions.Bottom + new float2(0, 1f - uvSize.y);
                    uvCount++;
                    uvs[uvCount] = uvPositions.Bottom + new float2(0, 1f);
                    uvCount++;
                    uvs[uvCount] = uvPositions.Bottom + new float2(uvSize.x, 1f);
                    uvCount++;
                    uvs[uvCount] = uvPositions.Bottom + new float2(uvSize.x, 1f - uvSize.y);
                    uvCount++;
                    break;
                default:
                    uvs[uvCount] = uvPositions.Side + new float2(uvSize.x, 1f - uvSize.y);
                    uvCount++;
                    uvs[uvCount] = uvPositions.Side + new float2(0, 1f - uvSize.y);
                    uvCount++;
                    uvs[uvCount] = uvPositions.Side + new float2(0, 1f);
                    uvCount++;
                    uvs[uvCount] = uvPositions.Side + new float2(uvSize.x, 1f);
                    uvCount++;
                    break;
            }
        }
        
        private struct UVPositions
        {
            public float2 Side;
            public float2 Top;
            public float2 Bottom;
        }
        
        private struct VertexOffset
        {
            public float3 V111;
            public float3 V110;
            public float3 V101;
            public float3 V100;
            
            public float3 V011;
            public float3 V010;
            public float3 V001;
            public float3 V000;
        }
    }
}