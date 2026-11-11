using System.Linq;
using AntColony.Core;
using UnityEngine;

namespace AntColony.Units
{
    // Original Meshy skeleton playback; the legacy first-commander option remains for old configs.
    public sealed class MeshyNativeTrial:ScriptableObject
    {
        public bool enabledForFirstCommander;
        public bool enabledForAllCommanders;
        public GameObject character;
        public AnimationClip ready,walk,run,mining;
        public Texture2D texture;
        public MeshyNativeLibrary library;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var config=Resources.Load<MeshyNativeTrial>("MeshyNativeTrial");
            if(!config||(!config.enabledForFirstCommander&&!config.enabledForAllCommanders))return;
            var go=new GameObject("Meshy native trial");DontDestroyOnLoad(go);
            go.AddComponent<MeshyNativeTrialDriver>().Config=config;
        }
    }

    sealed class MeshyNativeTrialDriver:MonoBehaviour
    {
        public MeshyNativeTrial Config;
        float nextCheck;
        void Update()
        {
            if(Time.unscaledTime<nextCheck)return;nextCheck=Time.unscaledTime+.5f;
            if(!Config||(!Config.enabledForFirstCommander&&!Config.enabledForAllCommanders))return;
            var units=Config.enabledForAllCommanders
                ?UnityEngine.Object.FindObjectsByType<CommanderAnt>(FindObjectsSortMode.None)
                :CommanderRoster.Instance?CommanderRoster.Instance.Commanders.Where(c=>c&&c.IsColonyMember).Take(1).ToArray():System.Array.Empty<CommanderAnt>();
            foreach(var unit in units)
            {
                if(!unit||unit.GetComponent<MeshyNativePilot>()||unit.GetComponent<MeshyNativeMotionPlayer>()||!unit.transform.Find("Kemono"))continue;
                if(Config.library)unit.gameObject.AddComponent<MeshyNativeMotionPlayer>().Configure(Config.library);
                else unit.gameObject.AddComponent<MeshyNativePilot>().Configure(Config.character,Config.ready,Config.walk,Config.run,Config.mining,Config.texture);
            }
        }
    }
}
