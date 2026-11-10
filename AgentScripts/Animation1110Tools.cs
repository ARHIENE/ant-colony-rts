using UnityEngine;
using UnityEditor;
using System.Linq;
public static class Animation1110Tools {public static string Main(){return string.Join("\n",new[]{"Tool_Pickaxe_Stone","Tool_Axe_Stone","Tool_Shovel_Stone","Tool_Hammer_Stone","Weapon_ShortBow"}.Select(n=>{var g=Object.Instantiate(Resources.Load<GameObject>("Kemono/Handheld/"+n));var bounds=g.GetComponentsInChildren<Renderer>().Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});var s=n+" rot="+g.transform.localEulerAngles+" scale="+g.transform.localScale+" bounds="+bounds;Object.DestroyImmediate(g);return s;}));}}

