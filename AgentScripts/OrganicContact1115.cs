using System.Linq;
using UnityEngine;
using AntColony.World;
public static class OrganicContact1115
{
 public static string Main(){var v=Object.FindObjectsByType<OrganicEnemyVisual>().First(x=>x.name=="MotionCheck_WallRam");var s=v.Model.GetComponentInChildren<SkinnedMeshRenderer>();var m=new Mesh();s.BakeMesh(m,true);float low=m.vertices.Min(p=>s.transform.TransformPoint(p).y);s.BakeMesh(m,false);float noScale=m.vertices.Min(p=>s.transform.TransformPoint(p).y);using var contact=new OrganicGroundContact(v.Model);contact.Apply(v.Model,v.transform.position);float boneLow=v.transform.position.y-contact.LastCorrection;Object.Destroy(m);return $"anchor={v.transform.position} low={low} noScale={noScale} boneLow={boneLow} correction={v.GroundCorrection} newCorrection={contact.LastCorrection} localScale={s.transform.localScale} lossyScale={s.transform.lossyScale}";}
}

