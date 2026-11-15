using System.Linq;
using UnityEngine;
using AntColony.Core;
public static class CampaignView1114
{
 public static string Resume(){AntColony.UI.GameMenuController.Instance.Resume();AntColony.UI.SimulationPause.UserSpeed(0);UnityEditor.EditorApplication.Step();return "Resumed menu, simulation paused";}
 public static string Diagnostic(){var deck=Object.FindObjectsByType<MeshRenderer>().First(x=>x.name=="본선 갑판");deck.sharedMaterial.shader=Shader.Find("Universal Render Pipeline/Unlit");deck.sharedMaterial.color=Color.white;UnityEditor.EditorApplication.Step();return "Temporary unlit deck to isolate rendering";}
 public static string Boss(){var boss=RocketCampaign.Instance.MothershipBoss;return $"boss={boss.Position} scale={boss.transform.lossyScale} components="+string.Join(",",boss.GetComponents<Component>().Select(x=>x.GetType().Name))+" renderers="+string.Join(";",boss.GetComponentsInChildren<Renderer>(true).Select(r=>$"{r.name} enabled={r.enabled} active={r.gameObject.activeInHierarchy} bounds={r.bounds}"));}
 public static string Main(){var c=Camera.main;var r=Object.FindObjectsByType<MeshRenderer>().First(x=>x.name=="본선 갑판");return $"camera enabled={c.enabled} rot={c.transform.eulerAngles} viewport={c.WorldToViewportPoint(r.bounds.center)} target={c.targetTexture} ortho={c.orthographicSize} deck bounds={r.bounds} forceOff={r.forceRenderingOff} visible={r.isVisible} shader={r.sharedMaterial.shader.name} supported={r.sharedMaterial.shader.isSupported} cameras="+string.Join(";",Object.FindObjectsByType<Camera>().Select(x=>$"{x.name} enabled={x.enabled} depth={x.depth} clear={x.clearFlags} target={x.targetTexture}"));}
}

