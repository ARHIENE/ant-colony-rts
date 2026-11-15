using UnityEngine;

namespace AntColony.Core
{
    public sealed partial class RocketCampaign
    {
        void BuildShipDetails(Material deckMaterial, Material beaconMaterial)
        {
            var dark = new Material(deckMaterial) { color = new Color(.075f,.11f,.14f) };
            var panel = new Material(deckMaterial) { color = new Color(.26f,.32f,.34f) };
            var amber = new Material(beaconMaterial) { color = new Color(.72f,.35f,.12f) };
            shipMaterials.Add(dark); shipMaterials.Add(panel); shipMaterials.Add(amber);
            void Part(string name, PrimitiveType shape, Vector3 position, Vector3 size, Material material)
            {
                var go = GameObject.CreatePrimitive(shape); go.name = name;
                go.transform.SetParent(shipSurface.transform,false); go.transform.localPosition = position; go.transform.localScale = size;
                go.GetComponent<Renderer>().sharedMaterial = material;
                // Decorations stay outside the walkable deck, or below ankle height.
                var collider = go.GetComponent<Collider>(); collider.enabled = false; Destroy(collider);
            }
            for(int z=-28;z<=28;z+=8)
            {
                Part("갑판 이음",PrimitiveType.Cube,new Vector3(0,.012f,z),new Vector3(48,.018f,.12f),dark);
                foreach(int side in new[]{-1,1})
                {
                    Part("외곽 격벽",PrimitiveType.Cube,new Vector3(side*24.8f,1.2f,z),new Vector3(1.2f,2.4f,6.8f),panel);
                    Part("외곽 유도등",PrimitiveType.Cube,new Vector3(side*23.5f,.05f,z),new Vector3(.3f,.06f,4),beaconMaterial);
                }
            }
            for(int x=-16;x<=16;x+=8)
                Part("갑판 세로 이음",PrimitiveType.Cube,new Vector3(x,.014f,0),new Vector3(.1f,.02f,64),dark);
            for(int z=-17;z<=4;z+=3)
                foreach(int side in new[]{-1,1}) Part("전투 구역 유도등",PrimitiveType.Cube,new Vector3(side*6,.05f,z),new Vector3(.35f,.06f,1.5f),beaconMaterial);
            Part("지휘체 구역 경계",PrimitiveType.Cube,new Vector3(0,.025f,7),new Vector3(32,.03f,.25f),amber);
            foreach(int side in new[]{-1,1})
            {
                Part("지휘체 동력탑",PrimitiveType.Cylinder,new Vector3(side*22,2.5f,23),new Vector3(2.5f,2.5f,2.5f),dark);
                Part("동력탑 핵",PrimitiveType.Sphere,new Vector3(side*22,5.2f,23),Vector3.one*1.8f,amber);
            }
            // The boarding point stays unobstructed in front of the invulnerable shuttle.
            Part("착륙선 선체",PrimitiveType.Capsule,new Vector3(0,1.8f,-29),new Vector3(3.5f,1.8f,3.5f),panel);
            Part("착륙선 창",PrimitiveType.Sphere,new Vector3(0,2.6f,-27.7f),new Vector3(2.5f,1.1f,.8f),beaconMaterial);
            Part("착륙선 발판",PrimitiveType.Cube,new Vector3(0,.15f,-26),new Vector3(3,.3f,3),panel);
        }
    }
}
