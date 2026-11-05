using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AntColony.Units
{
    public sealed class MeshyEquipmentWearables : MonoBehaviour
    {
        readonly List<GameObject> armorParts=new List<GameObject>(),trinketParts=new List<GameObject>();
        string armorKey,trinketKey,weaponKey;GameObject holster;
        Transform Bone(string name)=>GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==name);
        public void Apply(EquipmentItem armor,EquipmentItem trinket,EquipmentItem weapon,string held,bool downed)
        {
            string key=armor==null?"":armor.id+":"+armor.armor+":"+armor.bossEquipment;
            if(armorKey!=key){armorKey=key;Clear(armorParts);
                if(armor!=null){
                    string model=armor.bossEquipment==BossEquipmentKind.Durable?"Wear_BossA":armor.bossEquipment==BossEquipmentKind.HeatShield?"Wear_BossB":armor.armor==ArmorKind.Wings?"Wear_FlightPack":"Wear_PlateArmor";
                    bool wings=armor.armor==ArmorKind.Wings;
                    var part=Wear(model,"spine_02",wings?new Vector3(0,-.06f,-.18f):new Vector3(0,-.08f,0),wings?new Vector3(.48f,.52f,.15f):new Vector3(.36f,.37f,.27f));
                    if(part!=null)armorParts.Add(part);
                }
            }
            key=trinket==null?"":trinket.id+":"+trinket.effect;
            if(trinketKey!=key){trinketKey=key;Clear(trinketParts);
                if(trinket!=null){GameObject part=trinket.effect switch{
                    TrinketEffect.Move=>Wear("Wear_Anklet","foot.L",new Vector3(0,.06f,0),new Vector3(.12f,.05f,.13f)),
                    TrinketEffect.Gather=>Wear("Wear_Goggles","head",new Vector3(0,.32f,.06f),new Vector3(.37f,.13f,.28f)),
                    TrinketEffect.Mood=>Wear("Wear_ScentPouch","spine",new Vector3(-.16f,-.06f,.08f),new Vector3(.12f,.15f,.07f)),
                    _=>Wear("Wear_Insignia","spine_02",new Vector3(.06f,-.04f,.14f),new Vector3(.065f,.075f,.025f))};
                    if(part!=null)trinketParts.Add(part);
                }
            }
            key=weapon==null?"":weapon.id+":"+MeshyEquipmentRules.HeldModel(weapon);
            if(weaponKey!=key){weaponKey=key;if(holster!=null){holster.SetActive(false);Destroy(holster);}holster=null;
                if(weapon!=null){var src=Resources.Load<GameObject>("Kemono/Handheld/"+MeshyEquipmentRules.HeldModel(weapon));var bone=Bone("spine_02");
                    if(src!=null&&bone!=null){holster=Instantiate(src,transform,false);holster.name="Holstered_"+src.name;
                        holster.transform.localRotation=Quaternion.Euler(MeshyEquipmentRules.IsFirearm(weapon.weapon)?-70:0,0,30);
                        holster.transform.position=bone.position-transform.forward*.18f-transform.up*.16f;
                        holster.transform.SetParent(bone,true);
                    }
                }
            }
            if(holster!=null)holster.SetActive(!downed&&held!=MeshyEquipmentRules.HeldModel(weapon));
        }
        GameObject Wear(string name,string boneName,Vector3 offset,Vector3 size)
        {
            var src=Resources.Load<GameObject>("Kemono/Handheld/"+name);var bone=Bone(boneName);
            if(src==null||bone==null)return null;
            var item=Instantiate(src,transform,false);item.name=name;
            var mesh=item.GetComponentInChildren<MeshFilter>().sharedMesh;var b=mesh.bounds;
            item.transform.localRotation=Quaternion.Euler(-90,0,0);
            item.transform.localScale=new Vector3(size.x/Mathf.Max(.001f,b.size.x),size.z/Mathf.Max(.001f,b.size.y),size.y/Mathf.Max(.001f,b.size.z));
            var center=bone.position+transform.TransformDirection(offset);
            item.transform.position+=center-item.transform.TransformPoint(b.center);
            item.transform.SetParent(bone,true);return item;
        }
        static void Clear(List<GameObject> items){foreach(var item in items)if(item!=null){item.SetActive(false);Destroy(item);}items.Clear();}
    }
}
