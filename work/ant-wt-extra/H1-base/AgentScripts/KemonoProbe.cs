using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Save;
using AntColony.Units;
using UnityEngine;
using Object = UnityEngine.Object;

// 2026-10-24 kemono 캐릭터 적용 진단: 새 게임에서 장수 외형·애니메이터 상태를 보고하고 장수 가까이 카메라를 옮겨 찍는다.
public static class KemonoProbe
{
    public static async Task<string> Main()
    {
        var root = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "KemonoProbe-" + Guid.NewGuid().ToString("N"));
        try
        {
            SaveSystem.NewGame(new NewGameOptions { seed = 241024, mapSize = MapSize.Small });
            var until = DateTime.UtcNow.AddSeconds(90);
            await Task.Delay(500); while (SaveSystem.Busy && DateTime.UtcNow < until) await Task.Delay(50);
            await Task.Delay(2500);
            var lines = new System.Collections.Generic.List<string> { "available " + KemonoModel.Available };
            foreach (var c in CommanderRoster.Instance.Commanders.Take(3))
            {
                var v = c.GetComponent<AntVisual>();
                var anim = c.GetComponentsInChildren<Animator>().FirstOrDefault();
                var rs = c.GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray();
                var b = rs.Length > 0 ? rs.Select(r => r.bounds).Aggregate((x, y) => { x.Encapsulate(y); return x; }) : new Bounds();
                lines.Add($"{c.CommanderName}: scale {c.transform.lossyScale} ortho {UnityEngine.Camera.main.orthographic} visual {(v != null)} state {v?.StateName} anim {(anim != null ? anim.runtimeAnimatorController?.name + " avatar " + (anim.avatar != null && anim.avatar.isValid) : "none")} renderers {rs.Length} [{string.Join(",", rs.Take(6).Select(r => r.name))}] size {b.size}");
            }
            var target = CommanderRoster.Instance.Commanders.First();
            var tm = target.GetComponentsInChildren<Transform>().Where(t => t.name == "Kemono" || t.name.StartsWith("Kemono_Character") || t.name == "KemonoRig" || t.name == "Root" || t.name == "pevis" || t.name == "head").Select(t => $"{t.name} rot {t.rotation.eulerAngles} up {t.up}");
            lines.Add(string.Join(" | ", tm));
            foreach (var c2 in CommanderRoster.Instance.Commanders.Skip(1)) foreach (var a2 in c2.GetComponentsInChildren<Animator>()) a2.enabled = false;
            var cam = UnityEngine.Camera.main; cam.transform.position = target.transform.position + new Vector3(0, 6, -6); cam.transform.LookAt(target.transform.position + Vector3.up);
            var ctl = cam.GetComponent<AntColony.Camera.IsometricCameraController>(); var was = ctl != null && ctl.enabled; if (ctl != null) ctl.enabled = false;
            var ortho = cam.orthographicSize; var fov = cam.fieldOfView; if (cam.orthographic) cam.orthographicSize = 3; else { cam.fieldOfView = 20; cam.transform.position = target.transform.position + new Vector3(0, 5, -5); cam.transform.LookAt(target.transform.position + Vector3.up * .6f); }
            await Task.Delay(300);
            Directory.CreateDirectory("Temp/ui-shots"); ScreenCapture.CaptureScreenshot(Path.GetFullPath("Temp/ui-shots/kemono_close.png"));
            await Task.Delay(500); cam.orthographicSize = ortho; cam.fieldOfView = fov; if (ctl != null) ctl.enabled = was;
            return string.Join("\n", lines);
        }
        finally { SaveStorage.RootOverride = root; }
    }
}
