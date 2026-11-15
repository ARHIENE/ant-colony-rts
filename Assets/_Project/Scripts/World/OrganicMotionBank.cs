using System;
using UnityEngine;

namespace AntColony.World
{
    // All bodies sample the same authored clips; only pose time differs per instance.
    [PreferBinarySerialization]
    public sealed class OrganicMotionBank : ScriptableObject
    {
        [Serializable] public struct Clip { public string name; public int first, frames; public float seconds; public bool loop; }
        public Mesh mesh;
        public Material material;
        public Texture2D positions, normals;
        public Clip[] clips;
        public float width, strideSpeed;
        public Bounds animatedBounds;
        public float Frame(int action, float normalized, float phase = 0)
        {
            var clip = clips[Mathf.Clamp(action, 0, clips.Length - 1)];
            float t = clip.loop ? Mathf.Repeat(normalized + phase, 1) : Mathf.Clamp01(normalized);
            return clip.first + t * (clip.frames - 1);
        }
    }
}
