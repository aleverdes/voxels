#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace AleVerDes.Voxels
{
    public class TextureMapToVoxelTerrainTextureEditorWindow : EditorWindow
    {
        // private VoxelTerrain _voxelTerrain;
        // private Texture2D _terrainTextureMap;
        //
        // [MenuItem("Tools/Voxels/Texture map to voxel terrain")]
        // private static void ShowWindow()
        // {
        //     var window = GetWindow<TextureMapToVoxelTerrainTextureEditorWindow>();
        //     window.minSize = new Vector2(256, 128);
        //     window.Show();
        // }
        //
        // private void OnGUI()
        // {
        //     _voxelTerrain = (VoxelTerrain) EditorGUILayout.ObjectField("Voxel Terrain", _voxelTerrain, typeof(VoxelTerrain), true);
        //     _terrainTextureMap = (Texture2D) EditorGUILayout.ObjectField("Terrain Texture Map", _terrainTextureMap, typeof(Texture2D), true);
        //     
        //     if (!_voxelTerrain) return;
        //     if (!_terrainTextureMap) return;
        //
        //     if (GUILayout.Button("Paint"))
        //         Paint();
        // }
        //
        // private void Paint()
        // {
        //     var voxelsColors = CalculateVoxelColors();
        //         
        //     for (var j = 0; j < _terrainTextureMap.width; j++)
        //     for (int i = 0; i < _terrainTextureMap.height; i++)
        //     {
        //         var pixelColor = _terrainTextureMap.GetPixel(j, i);
        //         var voxel = GetVoxelIndexByColor(pixelColor, voxelsColors);
        //         
        //         for (int k = 0; k < _voxelTerrain.Settings.ChunkSize.y; k++)
        //         {
        //             ref var block = ref _voxelTerrain.GetBlockVoxelIndex(new int3(j, k, i));
        //             if (block > 0)
        //                 block = (byte) (voxel + 1);
        //         }
        //     }
        //     
        //     _voxelTerrain.UpdateChunks();
        // }
        //
        // private Dictionary<int, Color> CalculateVoxelColors()
        // {
        //     var dictionary = new Dictionary<int, Color>();
        //     foreach (var voxel in _voxelTerrain.Settings.TextureAtlas.VoxelDatabase.Voxels)
        //     {
        //         var variant = voxel.Variants.First();
        //         var pixel = TextureScaler.Scale(variant.Top, 1, 1);
        //         var voxelIndex = _voxelTerrain.Settings.TextureAtlas.VoxelDatabase.IndexOf(voxel);
        //         dictionary.Add(voxelIndex, pixel.GetPixel(0, 0));
        //     }
        //
        //     return dictionary;
        // }

        private int GetVoxelIndexByColor(Color inputColor, Dictionary<int, Color> voxelColors)
        {
            var minDistance = float.MaxValue;
            var voxelCandidate = voxelColors.First().Key;
            var inputVector = new Vector3(inputColor.r, inputColor.g, inputColor.b);
            foreach (var (voxel, voxelColor) in voxelColors)
            {
                var vector = new Vector3(voxelColor.r, voxelColor.g, voxelColor.b);
                var distance = Vector3.Distance(vector, inputVector);
                if (distance < minDistance)
                {
                    voxelCandidate = voxel;
                    minDistance = distance;
                }
            }

            return voxelCandidate;
        }
    }
}
#endif