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
            var name=Type.ToString();
            if(Type==ResourceType.Water||Type==ResourceType.CrudeOil||Type==ResourceType.Petroleum||Type==ResourceType.Resin)
            {int tint=Type==ResourceType.Water?0x90b4cf:Type==ResourceType.Resin?0xcf9b57:0x777363;Poly(tint,V(16,2),V(27,19),V(26,25),V(20,29),V(12,29),V(6,25),V(5,19));Poly(0xd2d7d2,V(10,17),V(12,16),V(12,24),V(9,22));}
            else if(name.Contains("Medicine")||Type==ResourceType.SurgeryKit)
            {Poly(0xd7d4c7,V(5,9),V(27,9),V(27,28),V(5,28));Poly(0x999b94,V(10,4),V(22,4),V(22,9),V(10,9));Poly(0xaf6656,V(13,12),V(19,12),V(19,25),V(13,25));Poly(0xaf6656,V(9,16),V(23,16),V(23,21),V(9,21));}
            else if(Type==ResourceType.Leaf||Type==ResourceType.Herb||Type==ResourceType.PlantingMaterial)
            {Poly(0x7e9461,V(4,26),V(5,13),V(15,5),V(29,3),V(26,17),V(17,26));Poly(0xc1bb89,V(3,29),V(6,29),V(24,9),V(21,10));}
            else if(Type==ResourceType.Component||Type==ResourceType.ElectronicComponent)
            {Poly(Type==ResourceType.Component?0x9b9d94:0x65715a,V(5,7),V(27,7),V(27,26),V(5,26));Poly(0xd1b772,V(2,11),V(30,11),V(30,14),V(2,14));Poly(0xd1b772,V(2,20),V(30,20),V(30,23),V(2,23));Poly(0x48494a,V(11,10),V(22,10),V(22,23),V(11,23));}
            else if(name.Contains("Drum")||name.Contains("Cylinder")||Type==ResourceType.NaturalGas)
            {Poly(name.Contains("Gas")?0xb3a86e:0x8c8e8b,V(9,6),V(23,6),V(27,11),V(27,28),V(5,28),V(5,11));Poly(0xd5cab0,V(6,12),V(26,12),V(26,15),V(6,15));Poly(0xd5cab0,V(6,22),V(26,22),V(26,25),V(6,25));}
            else if(name.Contains("Ore")||Type==ResourceType.Coal||Type==ResourceType.Charcoal||Type==ResourceType.Coke||Type==ResourceType.Sulfur)
            {int tint=Type==ResourceType.Sulfur?0xc4b35e:name.Contains("Copper")?0xb77952:name.Contains("Coal")||Type==ResourceType.Charcoal||Type==ResourceType.Coke?0x555658:0x9a9e9b;Poly(tint,V(3,24),V(8,9),V(18,4),V(29,18),V(25,28),V(8,28));Poly(0xc2b8a0,V(10,13),V(15,10),V(19,17),V(14,20));}
            else if(Type==ResourceType.Plank||Type==ResourceType.Brick||Type==ResourceType.StoneBlock)
            {int tint=Type==ResourceType.Plank?0xb18b5a:Type==ResourceType.Brick?0xab735a:0xa3a69b;Poly(tint,V(3,10),V(26,5),V(29,13),V(6,18));Poly(tint,V(3,22),V(26,17),V(29,25),V(6,30));}
            else if(Type==ResourceType.Cloth||Type==ResourceType.Fiber||Type==ResourceType.Thread||Type==ResourceType.Cobweb)
            {Poly(0xc9bca0,V(5,5),V(27,5),V(25,28),V(3,26));Poly(0x8b8170,V(10,5),V(13,5),V(11,27),V(8,27));Poly(0xe2d7bb,V(4,12),V(26,12),V(26,15),V(4,15));}
            else if(Type==ResourceType.Wood){Poly(0x92623e,V(4,21),V(18,6),V(28,13),V(14,28));Poly(0xd1aa72,V(2,22),V(7,19),V(14,21),V(16,25),V(10,29),V(3,27));Poly(0x976c43,V(6,23),V(11,22),V(12,25),V(8,27));}
            else if(Type==ResourceType.Stone){Poly(0x92988f,V(3,22),V(8,10),V(20,4),V(29,19),V(24,28),V(9,28));Poly(0xbcc0b4,V(8,10),V(20,4),V(29,19),V(16,17));Poly(0x747c75,V(16,17),V(29,19),V(24,28),V(9,28));}
            else if(Type==ResourceType.Food){Poly(0xcf9b57,V(5,14),V(8,7),V(16,4),V(24,7),V(27,14),V(27,27),V(5,27));Poly(0xf3d89f,V(10,10),V(12,9),V(16,18),V(14,19));Poly(0xf3d89f,V(18,10),V(20,9),V(24,18),V(22,19));}
            else if(Type.IsFood()){Poly(0xbcad8b,V(3,15),V(29,15),V(26,25),V(20,29),V(12,29),V(6,25));Poly(0xd4a55e,V(3,15),V(8,10),V(24,10),V(29,15),V(24,19),V(8,19));Poly(0x789759,V(9,13),V(13,10),V(18,15),V(22,12),V(24,15),V(18,18));}
            else{Poly(0xaaa88e,V(3,21),V(10,10),V(25,10),V(30,21),V(23,28),V(8,28));Poly(0xded5b6,V(10,10),V(25,10),V(30,21),V(16,21));}
        }
    }
}
