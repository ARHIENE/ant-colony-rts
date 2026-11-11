using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace AntColony.Units
{
    // Opt-in, one-unit trial. Game decisions still come from AntVisual/WorkerAnt.
    // The imported Meshy skeleton is animated directly; no common-rig retarget or arm IK.
    [DefaultExecutionOrder(300)]
    public sealed class MeshyNativePilot:MonoBehaviour
    {
        GameObject pivot,model,tool;Transform original;
        AntVisual visual;WorkerAnt worker;
        PlayableGraph graph;AnimationMixerPlayable mixer;
        AnimationClipPlayable[] players;AnimationClip[] clips;
        float[] times,weights;int selected=-1;
        Vector3 groundOffset;
        public bool PreviewMining;
        public string PlayingMotion=>clips==null||selected<0?"":clips[selected].name;
        public Transform NativeModel=>model?model.transform:null;

        public void Configure(GameObject character,AnimationClip idle,AnimationClip walk,AnimationClip run,AnimationClip mining,Texture2D texture)
        {
            visual=GetComponent<AntVisual>();worker=GetComponent<WorkerAnt>();
            original=transform.Find("Kemono");
            if(!visual||!original)throw new InvalidOperationException("Existing game visual required");
            pivot=new GameObject("Meshy native pilot");pivot.transform.SetParent(transform,false);
            model=Instantiate(character,pivot.transform,false);
            var animator=model.GetComponent<Animator>();if(!animator)animator=model.AddComponent<Animator>();
            animator.runtimeAnimatorController=null;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.enabled=true;
            clips=new[]{idle,walk,run,mining};times=new float[4];weights=new float[4];weights[0]=1;
            graph=PlayableGraph.Create("Meshy native game pilot");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            mixer=AnimationMixerPlayable.Create(graph,4);
            players=clips.Select(c=>AnimationClipPlayable.Create(graph,c)).ToArray();
            for(int i=0;i<4;i++){graph.Connect(players[i],0,mixer,i);mixer.SetInputWeight(i,weights[i]);}
            AnimationPlayableOutput.Create(graph,"Original Meshy bones",animator).SetSourcePlayable(mixer);graph.Play();graph.Evaluate(0);
            foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                skin.updateWhenOffscreen=true;
                var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.name="Meshy pilot material";mat.SetTexture("_BaseMap",texture);mat.SetFloat("_Smoothness",.3f);
                skin.sharedMaterials=Enumerable.Repeat(mat,skin.sharedMaterials.Length).ToArray();
            }
            var bounds=Bounds();pivot.transform.localScale=Vector3.one*(1.6f/Mathf.Max(.01f,bounds.size.y));
            bounds=Bounds();groundOffset=Vector3.up*(transform.position.y-bounds.min.y);
            AttachTool();
            foreach(var renderer in original.GetComponentsInChildren<Renderer>(true))renderer.forceRenderingOff=true;
        }
        Bounds Bounds()=>model.GetComponentsInChildren<SkinnedMeshRenderer>().Select(s=>s.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});
        void AttachTool()
        {
            // Calibrate a rigid tool mount against the original two-hand pose. Never rotate bones.
            clips[3].SampleAnimation(model,clips[3].length*.2f);
            var bones=model.GetComponentsInChildren<Transform>();
            var right=bones.First(t=>t.name=="mixamorig:RightHand");var left=bones.First(t=>t.name=="mixamorig:LeftHand");
            var finger=bones.First(t=>t.name=="mixamorig:RightHandMiddle1");var otherFinger=bones.First(t=>t.name=="mixamorig:LeftHandMiddle1");
            var palm=(right.position+finger.position)*.5f;var support=(left.position+otherFinger.position)*.5f;
            tool=Instantiate(Resources.Load<GameObject>("Kemono/Handheld/Tool_Pickaxe_Stone"));
            tool.transform.position=palm;tool.transform.rotation=Quaternion.FromToRotation(Vector3.up,(support-palm).normalized);tool.transform.SetParent(right,true);
            tool.SetActive(false);graph.Evaluate(0);
        }
        void LateUpdate()
        {
            if(!model||!graph.IsValid())return;
            bool mining=PreviewMining||worker&&worker.IsGatheringAnimation&&worker.CurrentResourceNode&&KemonoModel.Gather(worker.CurrentResourceNode.ResourceType).clip=="Tool_Mine";
            bool supported=mining||visual.StateName=="Idle_A"||visual.StateName=="Run"||visual.StateName=="Walk";
            pivot.SetActive(supported);
            // Hide the old renderer only; keep the established simulation/state selection intact.
            foreach(var renderer in original.GetComponentsInChildren<Renderer>(true))renderer.forceRenderingOff=supported;
            if(!supported)return;
            pivot.transform.localPosition=original.localPosition+groundOffset;pivot.transform.localRotation=original.localRotation;
            int next=mining?3:visual.StateName=="Run"?2:visual.StateName=="Walk"||visual.StateName=="Carry"?1:0;
            if(selected!=next){selected=next;times[next]=0;}
            tool.SetActive(mining);
            float delta=PreviewMining?Time.unscaledDeltaTime:Time.deltaTime;
            for(int i=0;i<4;i++)
            {
                times[i]+=delta;players[i].SetTime(Mathf.Repeat(times[i],clips[i].length));
                weights[i]=Mathf.MoveTowards(weights[i],i==selected?1:0,delta/.15f);mixer.SetInputWeight(i,weights[i]);
            }
            graph.Evaluate(0);
        }
        void OnDestroy()
        {
            if(graph.IsValid())graph.Destroy();
            if(original)foreach(var renderer in original.GetComponentsInChildren<Renderer>(true))renderer.forceRenderingOff=false;
            if(model)foreach(var mat in model.GetComponentsInChildren<SkinnedMeshRenderer>().SelectMany(s=>s.sharedMaterials).Distinct())Destroy(mat);
            if(pivot)Destroy(pivot);
        }
    }
}
