using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace AntColony.Units
{
    // Native playback follows the existing simulation's resolved action, including tools.
    // Unmapped actions retain the established modular visual until a suitable source is verified.
    [DefaultExecutionOrder(310)]
    public sealed class MeshyNativeMotionPlayer : MonoBehaviour
    {
        sealed class Rig
        {
            public GameObject pivot, model;
            public GameObject animationRoot;
            public Transform hips;
            public Vector3 bindHip;
            public PlayableGraph graph;
            public AnimationMixerPlayable mixer;
            public AnimationClipPlayable[] players;
            public float[] weights, times;
            public float scale;
            public MeshyNativeEquipment equipment;
            public MeshyEquipmentWearables wearables;
            public MeshyNativeAppearance appearance;
            public float? sleepLift;
        }
        [SerializeField] MeshyNativeLibrary library;
        Rig[] rigs;
        AntVisual visual;
        CommanderAnt commander;
        Transform original;
        readonly Dictionary<string,int> actions=new Dictionary<string,int>();
        Renderer[] oldRenderers;
        Material material;
        int selected=-1,revision=-1;
        Vector3 rootOrigin;
        public string PreviewMotion;
        public float PreviewPhase=-1;
        int RigIndex(int motion)=>library.motions[motion].rig*2+(library.motions[motion].armatureRoot?1:0);
        public string PlayingMotion=>selected<0?null:library.motions[selected].id;
        public Transform NativeModel=>selected<0||rigs==null||!library?null:rigs[RigIndex(selected)].model.transform;
        public void Configure(MeshyNativeLibrary value)
        {
            if(original&&library==value)return;
            Release();
            library=value;visual=GetComponent<AntVisual>();original=transform.Find("Kemono");
            commander=GetComponent<CommanderAnt>();
            if(!visual||!original)throw new InvalidOperationException("Game visual required");
            foreach(Transform child in transform)if(child.name.StartsWith("Meshy native rig ")){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            oldRenderers=original.GetComponentsInChildren<Renderer>(true);
            foreach(var binding in library.actions)
            {
                int index=Array.FindIndex(library.motions,m=>m.id==binding.motion);
                if(index>=0)actions[binding.action]=index;
            }
            material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.SetTexture("_BaseMap",library.texture);material.SetFloat("_Smoothness",.3f);
            rigs=new Rig[library.characters.Length*2];
        }
        Rig CreateRig(int index)
        {
            var r=new Rig();r.pivot=new GameObject("Meshy native rig "+index);r.pivot.transform.SetParent(transform,false);
            r.model=Instantiate(library.characters[index/2],r.pivot.transform,false);
            foreach(var a in r.model.GetComponentsInChildren<Animator>())a.enabled=false;
            r.animationRoot=index%2==1?r.model.transform.Find("SmartRigArmature").gameObject:r.model;
            var animator=r.animationRoot.GetComponent<Animator>();if(!animator)animator=r.animationRoot.AddComponent<Animator>();
            animator.enabled=true;
            animator.runtimeAnimatorController=null;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var skins=r.model.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach(var skin in skins){skin.updateWhenOffscreen=true;skin.sharedMaterials=Enumerable.Repeat(material,skin.sharedMaterials.Length).ToArray();}
            var bounds=MeshyNativeBounds.Rest(skins);
            r.scale=1.6f/Mathf.Max(bounds.size.y,.01f);r.pivot.transform.localScale=Vector3.one*r.scale;
            r.hips=r.model.GetComponentsInChildren<Transform>().First(t=>t.name=="mixamorig:Hips");
            var hipSkin=skins.First(s=>Array.IndexOf(s.bones,r.hips)>=0);
            int hipIndex=Array.IndexOf(hipSkin.bones,r.hips);
            r.bindHip=r.hips.parent.InverseTransformPoint((hipSkin.transform.localToWorldMatrix*hipSkin.sharedMesh.bindposes[hipIndex].inverse).MultiplyPoint3x4(Vector3.zero));
            r.equipment=r.pivot.AddComponent<MeshyNativeEquipment>();r.equipment.Bind(r.model.transform);
            r.wearables=r.pivot.AddComponent<MeshyEquipmentWearables>();
            r.graph=PlayableGraph.Create("Native Meshy actions "+index);r.graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            r.mixer=AnimationMixerPlayable.Create(r.graph,library.motions.Length);
            r.players=new AnimationClipPlayable[library.motions.Length];r.weights=new float[library.motions.Length];r.times=new float[library.motions.Length];
            for(int i=0;i<library.motions.Length;i++)if(RigIndex(i)==index)
            {
                r.players[i]=AnimationClipPlayable.Create(r.graph,library.motions[i].clip);r.graph.Connect(r.players[i],0,r.mixer,i);
            }
            AnimationPlayableOutput.Create(r.graph,"Original bones",animator).SetSourcePlayable(r.mixer);r.graph.Play();
            r.appearance=MeshyNativeAppearance.Attach(r.model.transform,original,index/2);
            rigs[index]=r;return r;
        }
        void LateUpdate()
        {
            if(!original)
            {
                var config=library?library:Resources.Load<MeshyNativeLibrary>("MeshyNative/Runtime");
                if(config)Configure(config);
                return;
            }
            if(!library)return;
            int next;
            if(!string.IsNullOrEmpty(PreviewMotion))next=Array.FindIndex(library.motions,m=>m.id==PreviewMotion);
            else if(!actions.TryGetValue(visual.MotionName??"",out next))next=-1;
            if(revision!=visual.MotionRevision)oldRenderers=original.GetComponentsInChildren<Renderer>(true);
            foreach(var renderer in oldRenderers)if(renderer)renderer.forceRenderingOff=next>=0;
            for(int i=0;i<rigs.Length;i++)if(rigs[i]!=null)rigs[i].pivot.SetActive(next>=0&&RigIndex(next)==i);
            if(next<0){selected=-1;return;}
            var entry=library.motions[next];var rig=rigs[RigIndex(next)]??CreateRig(RigIndex(next));
            rig.pivot.transform.localPosition=original.localPosition;rig.pivot.transform.localRotation=original.localRotation;
            if(selected!=next||string.IsNullOrEmpty(PreviewMotion)&&revision!=visual.MotionRevision)
            {
                bool newRig=selected<0||RigIndex(selected)!=RigIndex(next);
                selected=next;revision=visual.MotionRevision;rig.times[next]=0;
                if(newRig){Array.Clear(rig.weights,0,rig.weights.Length);rig.weights[next]=1;}
                // Record source origin without modifying its joint rotations or proportions.
                entry.clip.SampleAnimation(rig.animationRoot,0);rootOrigin=rig.bindHip;
                if(entry.id=="Sleep_Normally"&&!rig.sleepLift.HasValue)
                {
                    // Measure the original resting mesh once; retain all authored joint rotations.
                    entry.clip.SampleAnimation(rig.animationRoot,entry.clip.length*.5f);
                    float bottom=float.PositiveInfinity;var baked=new Mesh();
                    foreach(var skin in rig.model.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.enabled&&s.gameObject.activeInHierarchy))
                    {
                        skin.BakeMesh(baked);
                        foreach(var vertex in baked.vertices)bottom=Mathf.Min(bottom,skin.transform.TransformPoint(vertex).y);
                    }
                    Destroy(baked);
                    rig.sleepLift=(rig.pivot.transform.position.y-bottom)/Mathf.Max(.001f,transform.lossyScale.y);
                    entry.clip.SampleAnimation(rig.animationRoot,0);
                }
                rig.equipment.SetHandSpan(entry.maxHandSpan*rig.pivot.transform.lossyScale.x);
            }
            if(entry.id=="Sleep_Normally"&&rig.sleepLift.HasValue)
            {
                float floor=transform.position.y;
                if(Physics.Raycast(transform.position+Vector3.up*3,Vector3.down,out var ground,10,1<<8,QueryTriggerInteraction.Ignore))floor=ground.point.y;
                var resting=rig.pivot.transform.localPosition;
                resting.y=(floor-transform.position.y)/Mathf.Max(.001f,transform.lossyScale.y)+rig.sleepLift.Value;
                rig.pivot.transform.localPosition=resting;
            }
            float delta=string.IsNullOrEmpty(PreviewMotion)?Time.deltaTime:Time.unscaledDeltaTime;
            for(int i=0;i<rig.players.Length;i++)if(rig.players[i].IsValid())
            {
                var motion=library.motions[i];
                rig.times[i]+=delta*(string.IsNullOrEmpty(PreviewMotion)?visual.MotionSpeedFor(motion.clip.length):1f);
                if(i==selected&&!string.IsNullOrEmpty(PreviewMotion)&&PreviewPhase>=0)rig.times[i]=Mathf.Clamp01(PreviewPhase)*motion.clip.length;
                rig.players[i].SetTime(motion.loop?Mathf.Repeat(rig.times[i],motion.clip.length):Mathf.Min(rig.times[i],motion.clip.length));
                rig.weights[i]=Mathf.MoveTowards(rig.weights[i],i==selected?1:0,delta/.12f);
                rig.mixer.SetInputWeight(i,rig.weights[i]);
            }
            rig.graph.Evaluate(0);
            if(rig.appearance)rig.appearance.Sync();
            // Game navigation owns travel; preserve authored vertical motion and joint animation.
            var pos=rig.hips.localPosition;pos.x=rootOrigin.x;pos.z=rootOrigin.z;rig.hips.localPosition=pos;
            rig.equipment.Pose(visual.HeldTool,visual.MotionName,Mathf.Repeat(rig.times[next]/entry.clip.length,1));
            if(commander)rig.wearables.Apply(commander.EquippedArmor,commander.PersonalState.equipment.Find(e=>e.slot==EquipmentSlot.Trinket),commander.Weapon,visual.HeldTool,commander.PersonalHealth<=0);
        }
        public GameObject CreateDeath(bool persistent)
        {
            if(!library||selected<0)return null;
            var motion=library.motions.FirstOrDefault(m=>m.id=="Dead");if(motion==null)return null;
            var rig=rigs[RigIndex(selected)];
            return MeshyNativeCorpse.Create(library,motion,rig.pivot.transform.position,rig.pivot.transform.rotation,persistent,original);
        }
        void OnDisable()=>Release();
        void OnDestroy()=>Release();
        void Release()
        {
            if(rigs!=null)foreach(var rig in rigs)if(rig!=null){if(rig.graph.IsValid())rig.graph.Destroy();if(rig.pivot)Destroy(rig.pivot);}
            if(oldRenderers!=null)foreach(var renderer in oldRenderers)if(renderer)renderer.forceRenderingOff=false;
            if(material)Destroy(material);
            rigs=null;oldRenderers=null;original=null;material=null;selected=-1;revision=-1;actions.Clear();
        }
    }
}
