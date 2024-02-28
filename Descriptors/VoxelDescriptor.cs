using System;
using UnityEngine;

namespace TravkinGames.Voxels
{
    [CreateAssetMenu(menuName = "Voxels/Descriptors/Voxel", fileName = "Voxel Descriptor")]
    public class VoxelDescriptor : ScriptableObject
    {
        [SerializeField] private Variant[] _variants;
        
        public Variant[] Variants => _variants;
        
        [Serializable]
        public class Variant
        {
            [SerializeField] private Texture2D _top;
            [SerializeField] private Texture2D _side;
            [SerializeField] private Texture2D _bottom;
            [SerializeField] private Material _material;
            
            public Texture2D Top => _top;
            public Texture2D Side => _side;
            public Texture2D Bottom => _bottom;
            public Material Material => _material;
            
            public Texture2D this[FaceDirection direction]
            {
                get
                {
                    return direction switch
                    {
                        FaceDirection.Top => Top,
                        FaceDirection.Bottom => Bottom,
                        _ => Side
                    };
                }
            }
            
            public Texture2D this[int index]
            {
                get
                {
                    return index switch
                    {
                        0 => Top,
                        1 => Bottom,
                        2 => Side,
                        _ => throw new ArgumentException("Invalid index value for VoxelPrototype.Variant")
                    };
                }
            }
        }
    }
}