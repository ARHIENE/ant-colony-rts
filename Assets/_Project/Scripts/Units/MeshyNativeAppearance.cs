using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace AntColony.Units
{
    // Skin and cosmetics bind to the original Meshy joints. No animation curves are retargeted.
    public sealed class MeshyNativeAppearance : MonoBehaviour
    {
        readonly List<(SkinnedMeshRenderer source,SkinnedMeshRenderer target)> pairs=new();
        MaterialPropertyBlock block;
        public static MeshyNativeAppearance Attach(Transform model, Transform original, int rig)
        {
            var kit=Resources.Load<MeshyNativeAppearanceSet>("MeshyNative/SourceParts/Kit"+rig);
            if(!kit||kit.parts==null||kit.parts.Length==0)return null;
            var bones=model.GetComponentsInChildren<Transform>().GroupBy(t=>t.name).ToDictionary(g=>g.Key,g=>g.First());
            var sources=original.GetComponentsInChildren<SkinnedMeshRenderer>(true).GroupBy(s=>s.name).ToDictionary(g=>g.Key,g=>g.First());
            foreach(var p in kit.parts)if(!sources.ContainsKey(p.name)||p.bones.Any(b=>!bones.ContainsKey(b)))return null;
            var existing=model.GetComponentsInChildren<SkinnedMeshRenderer>();
            var sync=model.gameObject.AddComponent<MeshyNativeAppearance>();
            foreach(var p in kit.parts)
            {
                var go=new GameObject(p.name);go.transform.SetParent(model,false);
                var skin=go.AddComponent<SkinnedMeshRenderer>();skin.sharedMesh=p.mesh;skin.sharedMaterials=p.materials;
                skin.bones=p.bones.Select(b=>bones[b]).ToArray();skin.rootBone=bones["mixamorig:Hips"];
                skin.updateWhenOffscreen=true;sync.pairs.Add((sources[p.name],skin));
            }
            foreach(var skin in existing)skin.enabled=false;
            sync.Sync();return sync;
        }
        public void Sync()
        {
            if(block==null)block=new MaterialPropertyBlock();
            foreach(var pair in pairs)
            {
                if(!pair.source||!pair.target)continue;
                pair.target.enabled=pair.source.enabled&&pair.source.gameObject.activeInHierarchy;
                for(int i=0;i<pair.target.sharedMaterials.Length;i++)
                {
                    pair.source.GetPropertyBlock(block,i);pair.target.SetPropertyBlock(block,i);
                }
                for(int i=0;i<pair.target.sharedMesh.blendShapeCount;i++) {
                    int sourceIndex=pair.source.sharedMesh.GetBlendShapeIndex(pair.target.sharedMesh.GetBlendShapeName(i));
                    pair.target.SetBlendShapeWeight(i,sourceIndex>=0?pair.source.GetBlendShapeWeight(sourceIndex):0);
                }
            }
        }
    }
}
