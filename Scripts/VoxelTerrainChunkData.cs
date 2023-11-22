using System;
using System.IO;
using Unity.Jobs;
using Unity.Mathematics;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace AleVerDes.Voxels
{
    public class VoxelTerrainChunkData : ScriptableObject
    {
        [HideInInspector] [SerializeField] private Block[] _blocks;
        [HideInInspector] [SerializeField] private Mesh _mesh;

        [HideInInspector] [SerializeField] private bool _isInitialized;
        [HideInInspector] [SerializeField] private int2 _chunkPosition;
        [HideInInspector] [SerializeField] private int3 _chunkSize;
        
        public int2 ChunkPosition => _chunkPosition;
        public Mesh Mesh => _mesh;

        public void Initialize(int2 chunkPosition, int3 chunkSize)
        {
#if UNITY_EDITOR
            if (_isInitialized)
                throw new Exception("Chunk data already initialized");
            
            _chunkPosition = chunkPosition;
            _chunkSize = chunkSize;
            
            _blocks = new Block[chunkSize.x * chunkSize.y * chunkSize.z];
            for (var i = 0; i < _blocks.Length; i++)
            {
                _blocks[i].VoxelIndex = 0;
                _blocks[i].NoiseWeight = 255;
            }
            
            if (_mesh != null)
            {
                var oldMeshAssetPath = AssetDatabase.GetAssetPath(_mesh);
                AssetDatabase.DeleteAsset(oldMeshAssetPath);
            }
            
            _mesh = new Mesh { name = $"Chunk {chunkPosition}" };
            var newMeshAssetName = $"Chunk {chunkPosition} Mesh";

            var thisChunkAssetPath = AssetDatabase.GetAssetPath(this);
            if (string.IsNullOrEmpty(thisChunkAssetPath))
                throw new Exception("Can't find path to chunk asset " + chunkPosition);
            
            var newMeshAssetFolderPath = Path.Combine(Path.GetDirectoryName(thisChunkAssetPath), "Meshes");
            var newMeshAssetPath = Path.Combine(newMeshAssetFolderPath, newMeshAssetName + ".asset");
            if (!Directory.Exists(newMeshAssetFolderPath))
                Directory.CreateDirectory(newMeshAssetFolderPath);
            
            if (!File.Exists(newMeshAssetPath))
            {
                AssetDatabase.CreateAsset(_mesh, newMeshAssetPath);
                AssetDatabase.SaveAssets();
            }

            _isInitialized = true;
#endif
        }

        public void CleanUp()
        {
#if UNITY_EDITOR
            if (!_isInitialized)
                throw new Exception("Chunk data isn't initialized");
            
            _chunkPosition = default;
            _chunkSize = default;
            _blocks = default;
            
            if (_mesh != null)
            {
                var oldMeshAssetPath = AssetDatabase.GetAssetPath(_mesh);
                AssetDatabase.DeleteAsset(oldMeshAssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            _mesh = null;

            _isInitialized = false;
#endif
        }

        public void GenerateMesh(VoxelTerrain voxelTerrain, int2 chunkPosition)
        {
            var calculateFacesCountJob = new CalculateFacesCountJob
            {
                BlockVoxelIndices = voxelTerrain.GetNativeHashMapOfBlockVoxelIndicesForChunkWithNeighbours(chunkPosition),
                ChunkPosition = chunkPosition,
                ChunkSize = voxelTerrain.ChunkSize,
            };
            var calculateFacesCountHandle = calculateFacesCountJob.Schedule();
            calculateFacesCountHandle.Complete();

            var generateChunkMeshJob = new GenerateChunkMeshJob
            {
                VoxelTerrain = voxelTerrain,
                ChunkPosition = chunkPosition,
                Mesh = _mesh,
                FacesCount = calculateFacesCountJob.FacesCount,
            };
            // var generateChunkMeshHandle = generateChunkMeshJob.Schedule();
            // generateChunkMeshHandle.Complete();
            generateChunkMeshJob.Execute();
        }
        
        public ref Block GetBlock(int3 blockPositionInChunk)
        {
            if (_blocks == null || _blocks.Length == 0)
                throw new Exception("Chunk data not initialized");
            
            return ref _blocks[blockPositionInChunk.x + blockPositionInChunk.y * _chunkSize.x + blockPositionInChunk.z * _chunkSize.x * _chunkSize.y];
        }

        // public int GetBlockIndex(int3 blockPosition, int3 chunkSize)
        // {
        //     return blockPosition.x + blockPosition.y * chunkSize.x + blockPosition.z * chunkSize.x * chunkSize.y;
        // }
        //
        // public ref byte GetBlockVoxelIndex(int3 blockPosition, int3 chunkSize)
        // {
        //     return ref BlockVoxels[GetBlockIndex(blockPosition, chunkSize)];
        // }
        //
        // public ref byte GetBlockNoiseWeightIndex(int3 blockPosition, int3 chunkSize)
        // {
        //     return ref BlockNoiseWeights[GetBlockIndex(blockPosition, chunkSize)];
        // }
        //
        // public void UpdateMesh(ref Mesh mesh, GenerationData generationData)
        // {
        //     // if (!mesh)
        //     //     mesh = new Mesh();
        //     //
        //     // var voxelTerrain = generationData.VoxelTerrain;
        //     // var voxelTerrainSettings = voxelTerrain.Settings;
        //     // var blockSize = voxelTerrainSettings.BlockSize;
        //     // var chunkSize = voxelTerrainSettings.ChunkSize;
        //     // var verticesNoise = voxelTerrain.VerticesNoise;
        //     // var atlas = voxelTerrainSettings.TextureAtlas;
        //     //
        //     // var trianglesCount = 0;
        //     //
        //     // var chunkOffset = generationData.ChunkPosition * chunkSize;
        //     //
        //     // for (var x = 0; x < chunkSize.x; x++)
        //     // for (var y = 0; y < chunkSize.y; y++)
        //     // for (var z = 0; z < chunkSize.z; z++)
        //     // {
        //     //     ref var blockVoxel = ref GetBlockVoxelIndex(new int3(x, y, z), chunkSize);
        //     //
        //     //     if (blockVoxel == 0)
        //     //         continue;
        //     //
        //     //     float GetNoiseWeight(int3 blockPosition, int3 delta)
        //     //     {
        //     //         var noiseWeight = GetNoiseWeightForBlock(blockPosition);
        //     //         noiseWeight = Mathf.Min(noiseWeight, GetNoiseWeightForBlock(blockPosition + new int3(delta.x, delta.y, delta.z)));
        //     //         noiseWeight = Mathf.Min(noiseWeight, GetNoiseWeightForBlock(blockPosition + new int3(0, delta.y, 0)));
        //     //         noiseWeight = Mathf.Min(noiseWeight, GetNoiseWeightForBlock(blockPosition + new int3(0, 0, delta.z)));
        //     //         noiseWeight = Mathf.Min(noiseWeight, GetNoiseWeightForBlock(blockPosition + new int3(delta.x, delta.y, 0)));
        //     //         noiseWeight = Mathf.Min(noiseWeight, GetNoiseWeightForBlock(blockPosition + new int3(delta.x, 0, delta.z)));
        //     //         noiseWeight = Mathf.Min(noiseWeight, GetNoiseWeightForBlock(blockPosition + new int3(delta.x, 0, 0)));
        //     //             
        //     //         return noiseWeight;
        //     //
        //     //         float GetNoiseWeightForBlock(int3 bp)
        //     //         {
        //     //             return generationData.VoxelTerrain.IsBlockExistsInChunks(chunkOffset + bp)
        //     //                 ? generationData.VoxelTerrain.GetBlockNoiseWeight(chunkOffset + bp) / 255f
        //     //                 : 1f;
        //     //         }
        //     //     }
        //     //     
        //     //     var vertexOffset = GetVertexOffset(verticesNoise, chunkOffset + new int3(x, y, z), blockSize);
        //     //     var position = new int3(x, y, z);
        //     //     
        //     //     vertexOffset.RTB *= GetNoiseWeight(position, new int3(1, 1, -1)); 
        //     //     vertexOffset.RTF *= GetNoiseWeight(position, new int3(1, 1, 1));
        //     //     vertexOffset.RBB *= GetNoiseWeight(position, new int3(1, -1, -1));
        //     //     vertexOffset.RBF *= GetNoiseWeight(position, new int3(1, -1, 1));
        //     //     vertexOffset.LTB *= GetNoiseWeight(position, new int3(-1, 1, -1));
        //     //     vertexOffset.LTF *= GetNoiseWeight(position, new int3(-1, 1, 1));
        //     //     vertexOffset.LBB *= GetNoiseWeight(position, new int3(-1, -1, -1));
        //     //     vertexOffset.LBF *= GetNoiseWeight(position, new int3(-1, -1, 1));
        //     //
        //     //     vertexOffset.RTB = new(vertexOffset.RTB.x * voxelTerrainSettings.VerticesNoiseScale.x, vertexOffset.RTB.y * voxelTerrainSettings.VerticesNoiseScale.y, vertexOffset.RTB.z * voxelTerrainSettings.VerticesNoiseScale.z);
        //     //     vertexOffset.RTF = new(vertexOffset.RTF.x * voxelTerrainSettings.VerticesNoiseScale.x, vertexOffset.RTF.y * voxelTerrainSettings.VerticesNoiseScale.y, vertexOffset.RTF.z * voxelTerrainSettings.VerticesNoiseScale.z);
        //     //     vertexOffset.RBB = new(vertexOffset.RBB.x * voxelTerrainSettings.VerticesNoiseScale.x, vertexOffset.RBB.y * voxelTerrainSettings.VerticesNoiseScale.y, vertexOffset.RBB.z * voxelTerrainSettings.VerticesNoiseScale.z);
        //     //     vertexOffset.RBF = new(vertexOffset.RBF.x * voxelTerrainSettings.VerticesNoiseScale.x, vertexOffset.RBF.y * voxelTerrainSettings.VerticesNoiseScale.y, vertexOffset.RBF.z * voxelTerrainSettings.VerticesNoiseScale.z);
        //     //     vertexOffset.LTB = new(vertexOffset.LTB.x * voxelTerrainSettings.VerticesNoiseScale.x, vertexOffset.LTB.y * voxelTerrainSettings.VerticesNoiseScale.y, vertexOffset.LTB.z * voxelTerrainSettings.VerticesNoiseScale.z);
        //     //     vertexOffset.LTF = new(vertexOffset.LTF.x * voxelTerrainSettings.VerticesNoiseScale.x, vertexOffset.LTF.y * voxelTerrainSettings.VerticesNoiseScale.y, vertexOffset.LTF.z * voxelTerrainSettings.VerticesNoiseScale.z);
        //     //     vertexOffset.LBB = new(vertexOffset.LBB.x * voxelTerrainSettings.VerticesNoiseScale.x, vertexOffset.LBB.y * voxelTerrainSettings.VerticesNoiseScale.y, vertexOffset.LBB.z * voxelTerrainSettings.VerticesNoiseScale.z);
        //     //     vertexOffset.LBF = new(vertexOffset.LBF.x * voxelTerrainSettings.VerticesNoiseScale.x, vertexOffset.LBF.y * voxelTerrainSettings.VerticesNoiseScale.y, vertexOffset.LBF.z * voxelTerrainSettings.VerticesNoiseScale.z);
        //     //
        //     //     var v000 = chunkOffset + new float3(x, y, z) + vertexOffset.LBB;
        //     //     var v001 = chunkOffset + new float3(x, y, z + 1) + vertexOffset.LBF;
        //     //     var v011 = chunkOffset + new float3(x, y + 1, z + 1) + vertexOffset.LTF;
        //     //     var v010 = chunkOffset + new float3(x, y + 1, z) + vertexOffset.LTB;
        //     //     var v100 = chunkOffset + new float3(x + 1, y, z) + vertexOffset.RBB;
        //     //     var v101 = chunkOffset + new float3(x + 1, y, z + 1) + vertexOffset.RBF;
        //     //     var v111 = chunkOffset + new float3(x + 1, y + 1, z + 1) + vertexOffset.RTF;
        //     //     var v110 = chunkOffset + new float3(x + 1, y + 1, z) + vertexOffset.RTB;  
        //     //     
        //     //     var textureData = atlas.GetVoxelTexturesUV(blockVoxel - 1, x + y + z);
        //     //     var uvPositions = new UVPositions
        //     //     {
        //     //         Top = textureData.Top.WithY(-textureData.Top.y),
        //     //         Bottom = textureData.Bottom.WithY(-textureData.Bottom.y),
        //     //         Side = textureData.Side.WithY(-textureData.Side.y),
        //     //     };
        //     //     var uvSize = atlas.TextureSizeInAtlas;
        //     //
        //     //     var left = chunkOffset + new int3(x - 1, y, z);
        //     //     if (!voxelTerrain.IsSolidBlock(left) && voxelTerrain.IsBlockExistsInChunks(left))
        //     //     {
        //     //         _vertices.AddRange(new[]
        //     //         {
        //     //             Mul(v000, blockSize),
        //     //             Mul(v001, blockSize),
        //     //             Mul(v011, blockSize),
        //     //             Mul(v010, blockSize),
        //     //         });
        //     //
        //     //         AddTriangles(_triangles, ref trianglesCount);
        //     //         AddUVs(_uvs, uvPositions, uvSize, FaceDirection.Left);
        //     //         AddTangents(_tangents)
        //     //     }
        //     //
        //     //     var right = chunkOffset + new int3(x + 1, y, z);
        //     //     if (!voxelTerrain.IsSolidBlock(right) && voxelTerrain.IsBlockExistsInChunks(right))
        //     //     {
        //     //         _vertices.AddRange(new[]
        //     //         {
        //     //             Mul(v101, blockSize),
        //     //             Mul(v100, blockSize),
        //     //             Mul(v110, blockSize),
        //     //             Mul(v111, blockSize),
        //     //         });
        //     //
        //     //         AddTriangles(_triangles, ref trianglesCount);
        //     //         AddUVs(_uvs, uvPositions, uvSize, FaceDirection.Right);
        //     //         AddTangents(_tangents);
        //     //     }
        //     //
        //     //     var top = chunkOffset + new int3(x, y + 1, z);
        //     //     if (!voxelTerrain.IsSolidBlock(top))
        //     //     {
        //     //         _vertices.AddRange(new[]
        //     //         {
        //     //             Mul(v010, blockSize),
        //     //             Mul(v011, blockSize),
        //     //             Mul(v111, blockSize),
        //     //             Mul(v110, blockSize),
        //     //         });
        //     //
        //     //         AddTriangles(_triangles, ref trianglesCount);
        //     //         AddUVs(_uvs, uvPositions, uvSize, FaceDirection.Top);
        //     //         AddTangents(_tangents);
        //     //     }
        //     //
        //     //     var bottom = chunkOffset + new int3(x, y - 1, z);
        //     //     if (!voxelTerrain.IsSolidBlock(bottom) && voxelTerrain.IsBlockExistsInChunks(bottom))
        //     //     {
        //     //         _vertices.AddRange(new[]
        //     //         {
        //     //             Mul(v001, blockSize),
        //     //             Mul(v000, blockSize),
        //     //             Mul(v100, blockSize),
        //     //             Mul(v101, blockSize),
        //     //         });
        //     //
        //     //         AddTriangles(_triangles, ref trianglesCount);
        //     //         AddUVs(_uvs, uvPositions, uvSize, FaceDirection.Bottom);
        //     //         AddTangents(_tangents);
        //     //     }
        //     //
        //     //     var back = chunkOffset + new int3(x, y, z - 1);
        //     //     if (!voxelTerrain.IsSolidBlock(back) && voxelTerrain.IsBlockExistsInChunks(back))
        //     //     {
        //     //         _vertices.AddRange(new[]
        //     //         {
        //     //             Mul(v100, blockSize),
        //     //             Mul(v000, blockSize),
        //     //             Mul(v010, blockSize),
        //     //             Mul(v110, blockSize),
        //     //         });
        //     //
        //     //         AddTriangles(_triangles, ref trianglesCount);
        //     //         AddUVs(_uvs, uvPositions, uvSize, FaceDirection.Back);
        //     //         AddTangents(_tangents);
        //     //     }
        //     //
        //     //     var front = chunkOffset + new int3(x, y, z + 1);
        //     //     if (!voxelTerrain.IsSolidBlock(front) && voxelTerrain.IsBlockExistsInChunks(front))
        //     //     {
        //     //         _vertices.AddRange(new[]
        //     //         {
        //     //             Mul(v001, blockSize),
        //     //             Mul(v101, blockSize),
        //     //             Mul(v111, blockSize),
        //     //             Mul(v011, blockSize),
        //     //         });
        //     //
        //     //         AddTriangles(_triangles, ref trianglesCount);
        //     //         AddUVs(_uvs, uvPositions, uvSize, FaceDirection.Front);
        //     //         AddTangents(_tangents);
        //     //     }
        //     // }
        //
        // }
        //
        // private static VertexOffset GetVertexOffset(NoiseProvider noiseGenerator, int3 blockPosition, float3 blockSize)
        // {
        //     return default;
        //     // var rtf = GetNoisedVertex(blockPosition + int3.right + int3.forward + int3.up);
        //     // var rtb = GetNoisedVertex(blockPosition + int3.right + int3.up);
        //     // var rbf = GetNoisedVertex(blockPosition + int3.right + int3.forward);
        //     // var rbb = GetNoisedVertex(blockPosition + int3.right);
        //     // var ltf = GetNoisedVertex(blockPosition + int3.forward + int3.up);
        //     // var lbf = GetNoisedVertex(blockPosition + int3.forward);
        //     // var ltb = GetNoisedVertex(blockPosition + int3.up);
        //     // var lbb = GetNoisedVertex(blockPosition);
        //     // return new VertexOffset
        //     // {
        //     //     RTF = rtf,
        //     //     RTB = rtb,
        //     //     RBB = rbb,
        //     //     RBF = rbf,
        //     //     LTF = ltf,
        //     //     LTB = ltb,
        //     //     LBB = lbb,
        //     //     LBF = lbf,
        //     // };
        //     //
        //     // float3 GetNoisedVertex(int3 vertexPosition)
        //     // {
        //     //     return new float3
        //     //     {
        //     //         x = 2f * noiseGenerator.GetNoise(Mul(vertexPosition, blockSize)) - 1,
        //     //         y = 2f * noiseGenerator.GetNoise(Mul(vertexPosition, blockSize) + 111f * float3.one) - 1,
        //     //         z = 2f * noiseGenerator.GetNoise(Mul(vertexPosition, blockSize) - 111f * float3.one) - 1,
        //     //     };
        //     // }
        // }
        //
        // private static void AddTangents(List<Vector4> tangents)
        // {
        //     var upTangent = new Vector4(1f, 0f, 0f, -1f);
        //     tangents.AddRange(new []
        //     {
        //         upTangent,
        //         upTangent,
        //         upTangent,
        //         upTangent
        //     });
        // }
        //
        // private static void AddTriangles(List<int> triangles, ref int trianglesCount)
        // {
        //     triangles.AddRange(new []
        //     {
        //         trianglesCount * 4,
        //         trianglesCount * 4 + 1,
        //         trianglesCount * 4 + 2,
        //         trianglesCount * 4 + 2,
        //         trianglesCount * 4 + 3,
        //         trianglesCount * 4,
        //     });
        //     trianglesCount++;
        // }
        //
        // private static void AddUVs(List<Vector2> uvs, UVPositions uvPositions, Vector2 uvSize, FaceDirection faceDirection)
        // {
        //     switch (faceDirection)
        //     {
        //         case FaceDirection.Top:
        //             uvs.AddRange(new []
        //             {
        //                 uvPositions.Top + new Vector2(0, 1f - uvSize.y),
        //                 uvPositions.Top + new Vector2(0, 1),
        //                 uvPositions.Top + new Vector2(uvSize.x, 1),
        //                 uvPositions.Top + new Vector2(uvSize.x, 1f - uvSize.y),
        //             });
        //             break;
        //         case FaceDirection.Bottom:
        //             uvs.AddRange(new []
        //             {
        //                 uvPositions.Bottom + new Vector2(0, 1f - uvSize.y),
        //                 uvPositions.Bottom + new Vector2(0, 1),
        //                 uvPositions.Bottom + new Vector2(uvSize.x, 1),
        //                 uvPositions.Bottom + new Vector2(uvSize.x, 1f - uvSize.y),
        //             });
        //             break;
        //         default:
        //             uvs.AddRange(new []
        //             {
        //                 uvPositions.Side + new Vector2(uvSize.x, 1f - uvSize.y),
        //                 uvPositions.Side + new Vector2(0, 1f - uvSize.y),
        //                 uvPositions.Side + new Vector2(0, 1),
        //                 uvPositions.Side + new Vector2(uvSize.x, 1),
        //             });
        //             break;
        //     }
        // }
        //
        // private static float3 Mul(float3 a, float3 b)
        // {
        //     return new float3(a.x * b.x, a.y * b.y, a.z * b.z);
        // }
        //
        // public struct GenerationData
        // {
        //     public int3 ChunkPosition;
        //     public VoxelTerrain VoxelTerrain;
        // }
        //
        // private struct UVPositions
        // {
        //     public Vector2 Side;
        //     public Vector2 Top;
        //     public Vector2 Bottom;
        // }
        //
        // private struct VertexOffset
        // {
        //     public float3 RTF;
        //     public float3 RTB;
        //     public float3 RBF;
        //     public float3 RBB;
        //     
        //     public float3 LTF;
        //     public float3 LTB;
        //     public float3 LBF;
        //     public float3 LBB;
        // }
    }
}