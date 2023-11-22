using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace AleVerDes.Voxels
{
    [CreateAssetMenu(menuName = "Voxels/Texture Atlas", fileName = "Texture Atlas", order = 20)]
    public class TextureAtlas : ScriptableObject
    {
        private static int[] _atlasSizes = { 64, 128, 256, 512, 1024, 2048, 4096 };
        private static int[] _textureSizes = { 8, 16, 32, 64, 128, 256 };
        
        [SerializeField] private VoxelDatabase _voxelDatabase;

        [Header("Atlas Settings")] 
        [ValueDropdown("_atlasSizes")] [SerializeField] private int _atlasSize = 1024;
        [ValueDropdown("_textureSizes")] [SerializeField] private int _textureSize = 64;
        
        [Header("Advanced")]
        [SerializeField] private Texture _atlasTexture;
        [SerializeField] private VoxelData[] _voxelData;
        [SerializeField] private Vector2[] _texturesPositions;
        [SerializeField] private Vector2 _textureSizeInAtlas;
        [SerializeField] private float _textureRectScale = 0.99f;

        public VoxelDatabase VoxelDatabase => _voxelDatabase;
        public Vector2 TextureSizeInAtlas => _textureSizeInAtlas;
        public int Count => _voxelData.Length;

#if UNITY_EDITOR
        [Button("Generate Texture Atlas")]
        public void GenerateTextureAtlas()
        {
            var atlasTextureLength = _atlasSize / _textureSize;
            
            var uvSize = (float) _textureSize / _atlasSize;
            _textureSizeInAtlas = new Vector2(uvSize, uvSize);

            var atlas = new Texture2D(_atlasSize, _atlasSize, TextureFormat.RGBA32, 0, true);
            var voxelDataList = new List<VoxelData>();
            var texturesIndices = new Dictionary<Texture2D, int>();
            var texturesUvPositions = new Dictionary<int, Vector2>();

            var atlasTextureIndex = 0;
            for (var voxelIndex = 0; voxelIndex < _voxelDatabase.Count; voxelIndex++)
            {
                var voxel = _voxelDatabase[voxelIndex];
                var voxelData = new VoxelData
                {
                    VoxelIndex = voxelIndex,
                    VariantsTextures = new VoxelVariantTextures[voxel.Variants.Length]
                };

                for (var voxelVariantIndex = 0; voxelVariantIndex < voxel.Variants.Length; voxelVariantIndex++)
                {
                    var voxelVariant = voxel.Variants[voxelVariantIndex];
                    voxelData.VariantsTextures[voxelVariantIndex].TopTextureIndex = GetVoxelVariantTextureAtlasIndex(voxelVariant.Top);
                    voxelData.VariantsTextures[voxelVariantIndex].BottomTextureIndex = GetVoxelVariantTextureAtlasIndex(voxelVariant.Bottom);
                    voxelData.VariantsTextures[voxelVariantIndex].SideTextureIndex = GetVoxelVariantTextureAtlasIndex(voxelVariant.Side);

                    int GetVoxelVariantTextureAtlasIndex(Texture2D texture)
                    {
                        if (texturesIndices.TryGetValue(texture, out var atlasIndex))
                            return atlasIndex;

                        var topTextureColumnAndRow = GetTextureColumnAndRow(atlasTextureIndex, atlasTextureLength);
                        var uvPosition = WriteTextureToAtlas(atlas, topTextureColumnAndRow, texture);
                        texturesUvPositions[atlasTextureIndex] = uvPosition;

                        texturesIndices[texture] = atlasTextureIndex;
                        atlasIndex = atlasTextureIndex;
                        atlasTextureIndex++;

                        return atlasIndex;
                    }
                }

                voxelDataList.Add(voxelData);
            }

            _voxelData = voxelDataList.ToArray();
            _texturesPositions = texturesUvPositions.Values.ToArray();
            
            var pathToAtlas = AssetDatabase.GetAssetPath(this);
            var pathToPng = Path.Combine(Path.GetDirectoryName(pathToAtlas), Path.GetFileNameWithoutExtension(pathToAtlas) + ".png");
            var pngBytes = atlas.EncodeToPNG();
            File.WriteAllBytes(pathToPng, pngBytes);
            AssetDatabase.ImportAsset(pathToPng);
            
            _atlasTexture = AssetDatabase.LoadAssetAtPath<Texture>(pathToPng);
            _atlasTexture.filterMode = FilterMode.Point;
        }

        private Vector2Int GetTextureColumnAndRow(int index, int length)
        {
            var x = index % length;
            var y = Mathf.FloorToInt((float)index / length);
            return new Vector2Int(x, y);
        }
        
        private Vector2 WriteTextureToAtlas(Texture2D atlas, Vector2Int position, Texture2D texture)
        {
            var scaledTexture = TextureScaler.Scale(texture, _textureSize, _textureSize);
            var pixels = scaledTexture.GetPixels(0, 0, scaledTexture.width, scaledTexture.height);
            atlas.SetPixels(_textureSize * position.x, _atlasSize - _textureSize * (position.y + 1), _textureSize, _textureSize, pixels);
            return position * _textureSizeInAtlas;
        }
#endif

        public VoxelUV GetVoxelTexturesUV(int voxelIndex, int variantSeed = 0)
        {
            var voxelTexturesUV = new VoxelUV();
            var voxelData = _voxelData[voxelIndex];
            var variantIndex = variantSeed % voxelData.VariantsTextures.Length;
            var variantTextures = voxelData.VariantsTextures[variantIndex];
            voxelTexturesUV.Top = _texturesPositions[variantTextures.TopTextureIndex];
            voxelTexturesUV.Bottom = _texturesPositions[variantTextures.BottomTextureIndex];
            voxelTexturesUV.Side = _texturesPositions[variantTextures.SideTextureIndex];
            return voxelTexturesUV;
        }
        
        [Serializable]
        private struct VoxelData
        {
            public int VoxelIndex;
            public VoxelVariantTextures[] VariantsTextures;
        }

        [Serializable]
        private struct VoxelVariantTextures
        {
            public int TopTextureIndex;
            public int BottomTextureIndex;
            public int SideTextureIndex;
        }
        
        private struct MyStruct
        {
            
        }
    }
}