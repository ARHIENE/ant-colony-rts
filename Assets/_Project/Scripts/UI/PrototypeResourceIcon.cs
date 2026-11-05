using AntColony.Data;
using UnityEngine;
using UnityEngine.UI;
namespace AntColony.UI
{
    // Small vector stock icons, drawn at UI resolution rather than font glyphs.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PrototypeResourceIcon : MaskableGraphic
    {
        public ResourceType Type;
        public static void Create(Transform parent,ResourceType type,float x,float y)
        {var r=MenuLayout.Place(MenuTheme.Rect("ResourceIcon "+type,parent),x,y,22,22);var icon=r.gameObject.AddComponent<PrototypeResourceIcon>();icon.Type=type;icon.raycastTarget=false;}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            void Poly(int hex,params Vector2[] points){int offset=vh.currentVertCount;var rect=rectTransform.rect;foreach(var p in points)vh.AddVert(new Vector3(rect.xMin+p.x/32*rect.width,rect.yMax-p.y/32*rect.height),MenuTheme.Hex(hex),Vector2.zero);for(int i=1;i<points.Length-1;i++)vh.AddTriangle(offset,offset+i,offset+i+1);}
            Vector2 V(float x,float y)=>new Vector2(x,y);
            if(Type==ResourceType.Wood){Poly(0x92623e,V(4,21),V(18,6),V(28,13),V(14,28));Poly(0xd1aa72,V(2,22),V(7,19),V(14,21),V(16,25),V(10,29),V(3,27));Poly(0x976c43,V(6,23),V(11,22),V(12,25),V(8,27));}
            else if(Type==ResourceType.Stone){Poly(0x92988f,V(3,22),V(8,10),V(20,4),V(29,19),V(24,28),V(9,28));Poly(0xbcc0b4,V(8,10),V(20,4),V(29,19),V(16,17));Poly(0x747c75,V(16,17),V(29,19),V(24,28),V(9,28));}
            else if(Type==ResourceType.Food){Poly(0xcf9b57,V(5,14),V(8,7),V(16,4),V(24,7),V(27,14),V(27,27),V(5,27));Poly(0xf3d89f,V(10,10),V(12,9),V(16,18),V(14,19));Poly(0xf3d89f,V(18,10),V(20,9),V(24,18),V(22,19));}
            else if(Type.IsFood()){Poly(0xbcad8b,V(3,15),V(29,15),V(26,25),V(20,29),V(12,29),V(6,25));Poly(0xd4a55e,V(3,15),V(8,10),V(24,10),V(29,15),V(24,19),V(8,19));Poly(0x789759,V(9,13),V(13,10),V(18,15),V(22,12),V(24,15),V(18,18));}
            else{Poly(0xaaa88e,V(3,21),V(10,10),V(25,10),V(30,21),V(23,28),V(8,28));Poly(0xded5b6,V(10,10),V(25,10),V(30,21),V(16,21));}
        }
    }
}
