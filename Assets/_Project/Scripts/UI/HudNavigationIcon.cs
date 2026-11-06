using UnityEngine;
using UnityEngine.UI;

namespace AntColony.UI
{
    // Small, consistent ink pictograms; no external asset dependency.
    public sealed class HudNavigationIcon : MaskableGraphic
    {
        public int Kind;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            void Stroke(float x1,float y1,float x2,float y2)
            {
                var a=new Vector2(x1,y1);var b=new Vector2(x2,y2);
                var d=(b-a).normalized;var n=new Vector2(-d.y,d.x)*.85f;
                var r=rectTransform.rect;var scale=new Vector2(r.width/24,r.height/24);
                int i=vh.currentVertCount;
                foreach(var p in new[]{a-n,a+n,b+n,b-n})
                    vh.AddVert(new Vector3(r.x+p.x*scale.x,r.y+p.y*scale.y),color,Vector2.zero);
                vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
            }
            void Box(float x,float y,float w,float h){Stroke(x,y,x+w,y);Stroke(x+w,y,x+w,y+h);Stroke(x+w,y+h,x,y+h);Stroke(x,y+h,x,y);}
            switch(Kind)
            {
                case 0: Stroke(2,12,12,21);Stroke(12,21,22,12);Box(5,3,14,10);Box(10,3,4,7);break;
                case 1: Box(5,3,14,18);for(int y=7;y<20;y+=5){Stroke(8,y,10,y);Stroke(13,y,16,y);}break;
                case 2: Box(9,13,6,7);Stroke(4,3,5,9);Stroke(5,9,12,12);Stroke(12,12,19,9);Stroke(19,9,20,3);Stroke(4,3,20,3);break;
                case 3: Stroke(5,3,17,19);Stroke(10,20,20,13);Stroke(8,18,10,20);Stroke(18,11,20,13);break;
                case 4: Stroke(9,21,15,21);Stroke(10,21,10,14);Stroke(14,21,14,14);Stroke(10,14,4,4);Stroke(4,4,20,4);Stroke(20,4,14,14);Stroke(7,8,17,8);break;
                case 5: Box(3,3,7,7);Box(14,3,7,7);Box(9,15,7,6);Stroke(12,15,12,12);Stroke(6,12,18,12);Stroke(6,12,6,10);Stroke(18,12,18,10);break;
                case 6: Stroke(3,3,3,19);Stroke(3,19,9,21);Stroke(9,21,15,18);Stroke(15,18,21,21);Stroke(21,21,21,5);Stroke(21,5,15,3);Stroke(15,3,9,6);Stroke(9,6,3,3);Stroke(9,6,9,21);Stroke(15,3,15,18);break;
                case 7: Stroke(3,3,3,17);Stroke(21,3,21,12);Box(3,6,18,6);Box(5,12,5,3);break;
                default: Box(4,4,16,16);Stroke(4,15,20,15);Stroke(12,4,12,20);Stroke(4,4,8,8);Stroke(20,4,16,8);break;
            }
        }
    }
}
