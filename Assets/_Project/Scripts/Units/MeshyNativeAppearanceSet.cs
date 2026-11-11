using System;
using UnityEngine;
namespace AntColony.Units
{
    public sealed class MeshyNativeAppearanceSet : ScriptableObject
    {
        [Serializable] public sealed class Part
        {
            public string name;
            public Mesh mesh;
            public Material[] materials;
            public string[] bones;
        }
        public Part[] parts;
    }
}
