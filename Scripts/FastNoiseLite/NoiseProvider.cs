using Unity.Mathematics;
using UnityEngine;

namespace AleVerDes.Voxels
{
    public abstract class NoiseProvider : ScriptableObject, INoiseProvider
    {
        public abstract void Normalize();

        public float GetNoise(Vector2 position)
        {
            return GetNoise(position.x, 0, position.y);
        }

        public float GetNoise(float2 position)
        {
            return GetNoise(position.x, 0, position.y);
        }

        public float GetNoise(float x, float z)
        {
            return GetNoise(x, 0, z);
        }

        public float GetNoise(Vector3 position)
        {
            return GetNoise(position.x, position.y, position.z);
        }

        public float GetNoise(float3 position)
        {
            return GetNoise(position.x, position.y, position.z);
        }

        public abstract float GetNoise(float x, float y, float z);
    }
}