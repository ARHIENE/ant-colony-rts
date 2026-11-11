using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace AntColony.Units
{
    public sealed class MeshyNativeCorpse : MonoBehaviour
    {
        [SerializeField] AnimationClip clip;
        [SerializeField] Animator animator;
        [SerializeField] Material material;
        [SerializeField] float elapsed;
        PlayableGraph graph;
        AnimationClipPlayable player;

        public static GameObject Create(MeshyNativeLibrary library, MeshyNativeLibrary.Motion motion,
            Vector3 position, Quaternion rotation, bool persistent)
        {
            var body=new GameObject("Native fallen character");body.transform.SetPositionAndRotation(position,rotation);
            var model=Instantiate(library.characters[motion.rig],body.transform,false);
            foreach(var a in model.GetComponentsInChildren<Animator>())a.enabled=false;
            var root=motion.armatureRoot?model.transform.Find("SmartRigArmature").gameObject:model;
            var animator=root.GetComponent<Animator>();if(!animator)animator=root.AddComponent<Animator>();
            animator.enabled=true;animator.runtimeAnimatorController=null;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.SetTexture("_BaseMap",library.texture);material.SetFloat("_Smoothness",.3f);
            var skins=model.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach(var skin in skins){skin.updateWhenOffscreen=true;skin.sharedMaterials=Enumerable.Repeat(material,skin.sharedMaterials.Length).ToArray();skin.forceRenderingOff=false;}
            motion.clip.SampleAnimation(root,0);
            var bounds=skins.Select(s=>s.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});
            body.transform.localScale=Vector3.one*(1.6f/Mathf.Max(.01f,bounds.size.y));
            bounds=skins.Select(s=>s.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});
            model.transform.position+=Vector3.up*(position.y-bounds.min.y);
            var playback=body.AddComponent<MeshyNativeCorpse>();playback.clip=motion.clip;playback.animator=animator;playback.material=material;playback.StartGraph();
            if(!persistent)Destroy(body,Mathf.Max(2.5f,motion.clip.length+.25f));
            return body;
        }
        void StartGraph()
        {
            if(graph.IsValid())graph.Destroy();
            graph=PlayableGraph.Create("Native death");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            player=AnimationClipPlayable.Create(graph,clip);
            AnimationPlayableOutput.Create(graph,"Native death",animator).SetSourcePlayable(player);graph.Play();
        }
        void OnEnable(){if(clip&&animator)StartGraph();}
        void Update(){if(!graph.IsValid())return;elapsed+=Time.deltaTime;player.SetTime(Mathf.Min(elapsed,clip.length));graph.Evaluate(0);}
        void OnDisable(){if(graph.IsValid())graph.Destroy();}
        void OnDestroy(){if(graph.IsValid())graph.Destroy();if(material)Destroy(material);}
    }
}
