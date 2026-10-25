using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using UnityEngine;
using Object = UnityEngine.Object;

// 2026-10-18 건물 모델 연결: 매핑된 모든 BuildingKind의 시설 모델이 Resources에 있고,
// 실제 건물로 활성화되면 상자는 그리기만 멈추고 모델이 상자 바닥 면적 안에 들어간다.
public static class FacilityVisualChecks
{
    static int checks;
    static void Check(bool value, string message) { if (!value) { File.WriteAllText("Temp/facility-visual-checks.txt", "FAIL " + message); throw new Exception("FAIL " + message); } checks++; }
    static async Task Ready() { for (int i = 0; i < 1800 && SaveSystem.Busy; i++) await Task.Delay(50); Check(!SaveSystem.Busy, "scene ready"); }

    public static Task<string> Main() => Run(true);
    public static Task<string> Existing() => Run(false);
    private static async Task<string> Run(bool fresh)
    {
        checks = 0; Check(Application.isPlaying, "play mode");
        string original = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "FacVis-" + Guid.NewGuid().ToString("N"));
        try
        {
            await Ready();
            if (fresh) { SaveSystem.NewGame(new NewGameOptions { seed = 261018, mapSize = MapSize.Small }); await Ready(); }
            File.WriteAllText("Temp/facility-visual-checks.txt", "RUNNING");
            var getTemplate = typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic);
            int mapped = 0, shown = 0;
            foreach (BuildingKind kind in Enum.GetValues(typeof(BuildingKind)))
            {
                var id = FacilityVisual.ModelFor(kind);
                if (id == null) continue;
                mapped++;
                Check(Resources.Load<GameObject>("Facilities/" + id) != null, "model resource " + id);
                var template = getTemplate.Invoke(null, new object[] { kind, UnitRole.Worker }) as GameObject;
                if (template == null || template.GetComponent<MeshFilter>() == null) continue; // 씬 프리팹 건물은 상자가 아니라 그대로 둔다
                var go = Object.Instantiate(template, new Vector3(300 + shown * 8, 0, 300), Quaternion.Euler(0, 90, 0));
                go.SetActive(true);
                await Task.Yield(); await Task.Delay(30);
                var visual = go.GetComponent<FacilityVisual>();
                if (kind == BuildingKind.ElectricLamp) Check(go.GetComponent<Light>() != null, "electric lamp has runtime light");
                Check(visual != null && visual.Model != null, "model attached " + kind);
                var prefab = Resources.Load<GameObject>("Facilities/" + id);
                Check(Quaternion.Angle(visual.Model.rotation, go.transform.rotation * prefab.transform.localRotation) < .01f, "imported axis preserved " + kind);
                Check(go.GetComponent<Renderer>().forceRenderingOff && go.GetComponent<Renderer>().enabled, "box hidden but kept " + kind);
                var box = go.GetComponent<Renderer>().bounds;
                var model = visual.Model.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
                Check(model.size.x <= box.size.x + .05f && model.size.z <= box.size.z + .05f, $"fits footprint {kind} {model.size} in {box.size}");
                Check(Mathf.Abs(model.min.y - box.min.y) < .05f, "sits on ground " + kind);
                Object.Destroy(go); shown++;
            }
            Check(mapped == 120, "120 kinds mapped (" + mapped + ")"); // 2026-10-24 새 시설 16종 연결
            var wreck = Object.FindFirstObjectByType<CrashWreck>();
            Check(wreck != null && wreck.GetComponent<FacilityVisual>()?.Model != null, "crash landing wreck shows model (old stockpile removed by new start)");
            var result = $"PASS {checks} facility visual checks ({shown} built with models)";
            File.WriteAllText("Temp/facility-visual-checks.txt", result);
            return result;
        }
        finally { SaveStorage.RootOverride = original; }
    }
}
