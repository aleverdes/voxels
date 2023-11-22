using System;
using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AleVerDes.Voxels
{
    [SelectionBase]
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    public class VoxelTerrain : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] private TextureAtlas _textureAtlas;
        [SerializeField] private Material _terrainMaterial;

        [Header("Sizes")]
        [DisableIf("_isInitialized")] [SerializeField] private int2 _chunksCount = new int2(16, 16);
        [DisableIf("_isInitialized")] [SerializeField] private int3 _chunkSize = new int3(16, 16, 16);
        [DisableIf("_isInitialized")] [SerializeField] private float3 _blockSize = new float3(1f, 1f, 1f);
        
        [Header("Noise")]
        [SerializeField] private NoiseProvider _verticesNoise;
        [DisableIf("_verticesNoise == null")] [SerializeField] private float3 _verticesNoiseScale = new float3(0.1f, 0.1f, 0.1f);
        
        [HideInInspector] [SerializeField] private Chunk[] _chunks;        
        
        private bool _isInitialized;
        
        private VoxelTerrainEditorTool _selectedEditorTool;
        private float _paintingBrushRadius = 1f;
        private float _noiseWeightBrushRadius = 1f;
        [Range(0, 10f)] private float _noiseWeightBrushStrength = 1f;
        [Range(0, 1f)] private float _noiseWeightBrushTargetValue = 0f;
        private Voxel _selectedPaintingVoxel;

        #region Settings properties

        public TextureAtlas TextureAtlas => _textureAtlas;
        public Material TerrainMaterial => _terrainMaterial;
        public int2 ChunksCount => _chunksCount;
        public int3 ChunkSize => _chunkSize;
        public float3 BlockSize => _blockSize;
        public NoiseProvider VerticesNoise => _verticesNoise;
        public float3 VerticesNoiseScale => _verticesNoiseScale;

        #endregion
        
        #region Editor properties

        public VoxelTerrainEditorTool SelectedEditorTool => _selectedEditorTool;
        public float PaintingBrushRadius => _paintingBrushRadius;
        public float NoiseWeightBrushRadius => _noiseWeightBrushRadius;
        public float NoiseWeightBrushStrength => _noiseWeightBrushStrength;
        public float NoiseWeightBrushTargetValue => _noiseWeightBrushTargetValue;
        public Voxel SelectedPaintingVoxel => _selectedPaintingVoxel;
        
        #endregion

        #region Chunks management

        [ContextMenu("Initialize")]
        public void Initialize()
        {
            if (_textureAtlas == null)
                throw new Exception("Texture atlas not set");
            
            if (_terrainMaterial == null)
                throw new Exception("Terrain material not set");
            
            CleanUp();
            
            _chunks = new Chunk[ChunksCount.x * ChunksCount.y];
            
            for (int y = 0; y < _chunksCount.y; y++)
            for (int x = 0; x < _chunksCount.x; x++)
            {
                CreateChunk(new int2(x, y));
            }
            
            UpdateChunks();
        }

        private void CleanUp()
        {
            foreach (var chunk in _chunks)
            {
#if UNITY_EDITOR
                if (chunk.GameObject != null)
                    DestroyImmediate(chunk.GameObject);
                if (chunk.Data != null)
                {
                    chunk.Data.CleanUp();
                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(chunk.Data));
                }
#endif
            }

            _chunks = default;
        }

        public void CreateChunk(int2 chunkPosition)
        {
            var chunkIndex = GetChunkIndex(chunkPosition);
            
            var chunkGameObject = new GameObject($"Chunk {chunkPosition}");
            chunkGameObject.transform.SetParent(transform);
            chunkGameObject.hideFlags = HideFlags.HideInHierarchy;
            
            var chunkAsset = ScriptableObject.CreateInstance<VoxelTerrainChunkData>();
#if UNITY_EDITOR
            var chunkAssetName = $"Chunk {chunkPosition}";
            var chunkAssetPath = $"{Path.GetDirectoryName(SceneManager.GetActiveScene().path)}/Chunks";
            
            if (!Directory.Exists(chunkAssetPath)) 
                Directory.CreateDirectory(chunkAssetPath);

            AssetDatabase.CreateAsset(chunkAsset, Path.Combine(chunkAssetPath, $"{chunkAssetName}.asset"));
#endif
            chunkAsset.Initialize(chunkPosition, ChunkSize);
            
            var chunk = new Chunk
            {
                Data = chunkAsset,
                GameObject = chunkGameObject,
                MeshFilter = chunkGameObject.AddComponent<MeshFilter>(),
                MeshRenderer = chunkGameObject.AddComponent<MeshRenderer>(),
                MeshCollider = chunkGameObject.AddComponent<MeshCollider>()
            };
            
            chunk.MeshRenderer.sharedMaterial = _terrainMaterial;
            
            chunk.MeshFilter.sharedMesh = chunkAsset.Mesh;
            chunk.MeshCollider.sharedMesh = chunkAsset.Mesh;
            
            for (var x = 0; x < ChunkSize.x; x++)
            for (var y = 0; y < ChunkSize.y; y++)
            for (var z = 0; z < ChunkSize.z; z++)
                if (y <= ChunkSize.y / 2)
                    chunk.Data.GetBlock(new int3(x, y, z)).VoxelIndex = (byte)Random.Range(1, TextureAtlas.Count);
            
            _chunks[chunkIndex] = chunk;
        }
        
        #endregion

        #region Chunks update methods

        [Button("Update chunks")]
        [ContextMenu("Update chunks")]
        public void UpdateChunks()
        {
            for (var i = 0; i < _chunks.Length; i++) 
                UpdateChunk(i);
            SaveAssets();
        }

        public void UpdateChunk(int2 chunkPosition, bool includeNeighbours = false)
        {
            UpdateChunks(includeNeighbours ? ChunkUtils.GetHorizontalNeighbours(chunkPosition) : new[] { chunkPosition });
        }

        public void UpdateChunks(IEnumerable<int2> chunks)
        {
            foreach (var chunkPosition in chunks)
                UpdateChunk(chunkPosition.x + chunkPosition.y * _chunksCount.x);
            SaveAssets();
        }

        public void UpdateChunks(IEnumerable<int> chunks)
        {
            foreach (var chunkIndex in chunks) 
                UpdateChunk(chunkIndex);
            SaveAssets();
        }

        private void UpdateChunk(int chunkIndex)
        {
            var chunk = _chunks[chunkIndex];
            chunk.Data.GenerateMesh(this, GetChunkPosition(chunkIndex));
            chunk.MeshFilter.sharedMesh = chunk.Data.Mesh;
            chunk.MeshCollider.sharedMesh = chunk.Data.Mesh;
            SetChunkAsDirty(chunk);
        }

        private void SetChunkAsDirty(Chunk chunk)
        {
#if UNITY_EDITOR
            EditorUtility.SetDirty(chunk.Data);
            EditorUtility.SetDirty(chunk.Data.Mesh);
#endif
        }

        private void SaveAssets()
        {
#if UNITY_EDITOR
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif
        }
        
        #endregion

        #region Chunk methods

        public int GetChunkIndex(int2 chunkPosition)
        {
            return chunkPosition.x + chunkPosition.y * _chunksCount.x;
        }

        public int2 GetChunkPosition(int chunkIndex)
        {
            return _chunks[chunkIndex].Data.ChunkPosition;
        }
        
        public VoxelTerrainChunkData GetChunk(int2 chunkPosition)
        {
            return _chunks[GetChunkIndex(chunkPosition)].Data;
        }

        public bool IsChunkExists(int chunkIndex)
        {
            return chunkIndex >= 0 && chunkIndex <= _chunks.Length - 1;
        }

        public bool IsChunkExists(int2 chunkPosition)
        {
            var chunkIndex = GetChunkIndex(chunkPosition);
            return chunkIndex >= 0 && chunkIndex <= _chunks.Length - 1;
        }
        
        public int3 GetFirstBlockGlobalPosition(int2 chunkPosition)
        {
            return new int3(chunkPosition.x * _chunkSize.x, 0, chunkPosition.y * _chunkSize.z);
        }
        
        public float3 GetChunkWorldSize()
        {
            return new float3(_chunkSize.x * _blockSize.x, _chunkSize.y * _blockSize.y, _chunkSize.z * _blockSize.z);
        }

        #endregion

        #region Blocks methods

        public bool IsBlockExistsInChunks(int3 blockGlobalPosition)
        {
            if (blockGlobalPosition.y < 0 || blockGlobalPosition.y > _chunkSize.y - 1) return false;
            var chunkPosition = GetChunkPositionByBlockGlobalPosition(blockGlobalPosition);
            return IsChunkExists(chunkPosition);
        }

        public bool IsSolidBlock(int3 blockGlobalPosition)
        {
            if (blockGlobalPosition.y < 0 || blockGlobalPosition.y > _chunkSize.y - 1) return false;
            var chunkPosition = GetChunkPositionByBlockGlobalPosition(blockGlobalPosition);
            if (!IsChunkExists(chunkPosition))
                return false;
            var chunkIndex = GetChunkIndex(chunkPosition);
            var blockPositionInChunk = GetBlockPositionInChunk(blockGlobalPosition);
            return _chunks[chunkIndex].Data.GetBlock(blockPositionInChunk).VoxelIndex != 0;
        }
        
        public int3 GetBlockPositionInChunk(int3 blockGlobalPosition)
        {
            return new int3(blockGlobalPosition.x % _chunkSize.x, blockGlobalPosition.y, blockGlobalPosition.z % _chunkSize.z);
        }
        
        public int2 GetChunkPositionByBlockGlobalPosition(int3 blockGlobalPosition)
        {
            return new int2((int) math.floor(blockGlobalPosition.x * (1f / _chunkSize.x)), (int) math.floor(blockGlobalPosition.z * (1f / _chunkSize.z)));
        }

        public int GetChunkIndexByBlockGlobalPosition(int3 blockGlobalPosition)
        {
            return GetChunkIndex(GetChunkPositionByBlockGlobalPosition(blockGlobalPosition));
        }
        
        public ref Block GetBlock(int3 blockGlobalPosition)
        {
            return ref GetChunk(GetChunkIndexByBlockGlobalPosition(blockGlobalPosition)).GetBlock(GetBlockPositionInChunk(blockGlobalPosition));
        }
        
        public ref byte GetBlockVoxelIndex(int3 blockGlobalPosition)
        {
            return ref GetBlock(blockGlobalPosition).VoxelIndex;
        }
        
        public ref byte GetBlockNoiseWeight(int3 blockGlobalPosition)
        {
            return ref GetBlock(blockGlobalPosition).NoiseWeight;
        }

        #endregion

        #region Generation methods

        public NativeHashMap<int3, byte> GetNativeHashMapOfBlockVoxelIndicesForChunkWithNeighbours(int2 chunkPosition)
        {
            var chunksCount = 0;
            if (IsChunkExists(chunkPosition + new int2(-1, 1))) chunksCount++;
            if (IsChunkExists(chunkPosition + new int2(-1, 0))) chunksCount++;
            if (IsChunkExists(chunkPosition + new int2(-1, -1))) chunksCount++;
            if (IsChunkExists(chunkPosition + new int2(1, 0))) chunksCount++;
            if (IsChunkExists(chunkPosition + new int2(0, 1))) chunksCount++;
            if (IsChunkExists(chunkPosition + new int2(0, 0))) chunksCount++;
            if (IsChunkExists(chunkPosition + new int2(0, -1))) chunksCount++;
            if (IsChunkExists(chunkPosition + new int2(1, 1))) chunksCount++;
            if (IsChunkExists(chunkPosition + new int2(1, 0))) chunksCount++;
            if (IsChunkExists(chunkPosition + new int2(1, -1))) chunksCount++;
            if (chunksCount == 0)
                throw new Exception("No chunks found");
            
            var blockVoxels = new NativeHashMap<int3, byte>(chunksCount * _chunkSize.x * _chunkSize.y * _chunkSize.z, Allocator.TempJob);
            for (var chunkX = -1; chunkX <= 1; chunkX++)
            for (var chunkY = -1; chunkY <= 1; chunkY++)
            {
                var currentChunkPosition = chunkPosition + new int2(chunkX, chunkY);
                if (!IsChunkExists(currentChunkPosition)) 
                    continue;

                var currentChunk = GetChunk(currentChunkPosition);
                for (var x = 0; x < _chunkSize.x; x++)
                for (var y = 0; y < _chunkSize.x; y++)
                for (var z = 0; z < _chunkSize.x; z++)
                {
                    var blockGlobalPosition = GetFirstBlockGlobalPosition(currentChunkPosition) + new int3(x, y, z);
                    if (!blockVoxels.TryAdd(blockGlobalPosition, currentChunk.GetBlock(new int3(x, y, z)).VoxelIndex))
                        throw new Exception("Block already exists");
                }
            }

            return blockVoxels;
        }

        public NativeHashMap<int3, byte> GetNativeHashMapOfBlockNoiseWeightsForChunkWithNeighbours(int2 chunkPosition)
        {
            var chunksCount = 0;
            if (IsChunkExists(chunkPosition + new int2(-1, 1))) chunksCount++;
            if (IsChunkExists(chunkPosition + new int2(-1, 0))) chunksCount++;
            if (IsChunkExists(chunkPosition + new int2(-1, -1))) chunksCount++;
            if (IsChunkExists(chunkPosition + new int2(1, 0))) chunksCount++;
            if (IsChunkExists(chunkPosition + new int2(0, 1))) chunksCount++;
            if (IsChunkExists(chunkPosition + new int2(0, 0))) chunksCount++;
            if (IsChunkExists(chunkPosition + new int2(0, -1))) chunksCount++;
            if (IsChunkExists(chunkPosition + new int2(1, 1))) chunksCount++;
            if (IsChunkExists(chunkPosition + new int2(1, 0))) chunksCount++;
            if (IsChunkExists(chunkPosition + new int2(1, -1))) chunksCount++;
            if (chunksCount == 0)
                throw new Exception("No chunks found");
            
            var noiseWeights = new NativeHashMap<int3, byte>(chunksCount * _chunkSize.x * _chunkSize.y * _chunkSize.z, Allocator.TempJob);
            for (var chunkX = -1; chunkX <= 1; chunkX++)
            for (var chunkY = -1; chunkY <= 1; chunkY++)
            {
                var currentChunkPosition = chunkPosition + new int2(chunkX, chunkY);
                if (!IsChunkExists(currentChunkPosition)) 
                    continue;

                var currentChunk = GetChunk(currentChunkPosition);
                for (var x = 0; x < _chunkSize.x; x++)
                for (var y = 0; y < _chunkSize.x; y++)
                for (var z = 0; z < _chunkSize.x; z++)
                {
                    var blockGlobalPosition = GetFirstBlockGlobalPosition(currentChunkPosition) + new int3(x, y, z);
                    if (!noiseWeights.TryAdd(blockGlobalPosition, currentChunk.GetBlock(new int3(x, y, z)).NoiseWeight))
                        throw new Exception("Block already exists");
                }
            }

            return noiseWeights;
        }

        #endregion
        
        [Serializable]
        public struct Chunk
        {
            public VoxelTerrainChunkData Data;
            public GameObject GameObject;
            public MeshFilter MeshFilter;
            public MeshRenderer MeshRenderer;
            public MeshCollider MeshCollider;
        }
    }
}