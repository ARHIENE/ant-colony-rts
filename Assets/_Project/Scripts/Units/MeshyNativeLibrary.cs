using System;
using UnityEngine;

namespace AntColony.Units
{
    // Motion assets retain their Meshy skeleton. Each entry explicitly identifies its skin rig.
    public sealed class MeshyNativeLibrary : ScriptableObject
    {
        [Serializable] public sealed class Motion
        {
            public string id;
            public AnimationClip clip;
            public int rig;
            public bool armatureRoot;
            public float maxHandSpan;
            public bool loop = true;
        }
        [Serializable] public sealed class ActionBinding
        {
            public string action, motion;
        }
        public GameObject[] characters;
        public Texture2D texture;
        public Motion[] motions;
        public ActionBinding[] actions;
    }
}
