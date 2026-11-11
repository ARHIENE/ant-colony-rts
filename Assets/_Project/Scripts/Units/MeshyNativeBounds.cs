using System;
using UnityEngine;
namespace AntColony.Units
{
    public static class MeshyNativeBounds
    {
        // Renderer bounds include the entire imported animation's travel and are not body size.
        public static Bounds Rest( SkinnedMeshRenderer[] skins)
        {
            bool first=true;var result=new Bounds();
            foreach(var skin in skins)
            {
                var b=skin.sharedMesh.bounds;
                for(int i=0;i<8;i++)
                {
                    var v=b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                    var p=skin.transform.TransformPoint(v);
                    if(first){result=new Bounds(p,Vector3.zero);first=false;}else result.Encapsulate(p);
                }
            }
            if(first||result.size.y<.001f)throw new InvalidOperationException("Native rest geometry missing");
            return result;
        }
    }
}
