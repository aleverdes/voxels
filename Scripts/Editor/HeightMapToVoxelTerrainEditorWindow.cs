#if UNITY_EDITOR
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace AleVerDes.Voxels
{
    public class HeightMapToVoxelTerrainEditorWindow : EditorWindow
    {
        // private VoxelTerrain _voxelTerrain;
        // private Texture2D _terrainHeightMap;
        //
        // [MenuItem("Tools/Voxels/Height map to voxel terrain")]
        // private static void ShowWindow()
        // {
        //     var window = GetWindow<HeightMapToVoxelTerrainEditorWindow>();
        //     window.minSize = new Vector2(256, 128);
        //     window.Show();
        // }
        //
        // private void OnGUI()
        // {
        //     _voxelTerrain = (VoxelTerrain) EditorGUILayout.ObjectField("Voxel Terrain", _voxelTerrain, typeof(VoxelTerrain), true);
        //     _terrainHeightMap = (Texture2D) EditorGUILayout.ObjectField("Terrain Height Map", _terrainHeightMap, typeof(Texture2D), true);
        //     
        //     if (!_voxelTerrain) return;
        //     if (!_terrainHeightMap) return;
        //
        //     if (GUILayout.Button("Generate"))
        //         Generate();
        // }
        //
        // private void Generate()
        // {
        //     _voxelTerrain.CleanUp();
        //     
        //     var chunksLength = new Vector2Int(_terrainHeightMap.width / _voxelTerrain.Settings.ChunkSize.x, _terrainHeightMap.height / _voxelTerrain.Settings.ChunkSize.z);
        //     for (var j = 0; j < chunksLength.x; j++)
        //     for (var i = 0; i < chunksLength.y; i++)
        //     {
        //         _voxelTerrain.CreateChunk(new int3(j, 0, i));
        //     }
        //
        //     for (var j = 0; j < _terrainHeightMap.width; j++)
        //     for (int i = 0; i < _terrainHeightMap.height; i++)
        //     {
        //         var height = _terrainHeightMap.GetPixel(j, i);
        //         for (int k = 0; k < _voxelTerrain.Settings.ChunkSize.y; k++)
        //         {
        //             ref var block = ref _voxelTerrain.GetBlockVoxelIndex(new int3(j, k, i));
        //             if (k <= height.r * _voxelTerrain.Settings.ChunkSize.y)
        //                 block = 1;
        //             else
        //                 block = 0;
        //         }
        //     }
        //     
        //     _voxelTerrain.UpdateChunks();
        // }
    }
}
#endif