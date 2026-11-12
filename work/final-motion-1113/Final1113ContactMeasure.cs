using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;

// PICKAXE-CONTACT-1111 (Fable): 곡괭이의 뾰족한 날 끝과 실제 바위 표면 사이 거리를 한 주기 동안 잰다.
// 손잡이 접점이 아니라 날 끝이 기준이다. 격리 새 게임만 쓰고 사용자 저장 슬롯은 건드리지 않는다.
// run_script는 호출마다 static이 초기화되므로 각 진입점이 필요한 상태를 스스로 다시 찾는다.
public static class Final1113ContactMeasure
{
    const int RockLayer = 30;
    static string Dir { get { var d = Path.Combine(Directory.GetCurrentDirectory(), "work", "final-motion-1113"); Directory.CreateDirectory(d); return d; } }

    public static async Task<string> Prepare()
    {
        if (!Application.isPlaying) throw new Exception("Play required");
        EditorApplication.isPaused = false;
        for (int i = 0; i < 200 && (SaveSystem.Busy || GameMenuController.Instance == null || !GameSession.Exists); i++) await Task.Delay(100);
        SessionState.SetString("Pickaxe1111.settings", JsonUtility.ToJson(UserSettings.Current));
        SessionState.SetString("Pickaxe1111.root", SaveStorage.RootOverride ?? "");
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "PICKAXE1111-" + Guid.NewGuid().ToString("N"));
        var settings = UserSettings.Current.Clone(); settings.autoSaveEnabled = false; UserSettings.Apply(settings, false);
        SaveSystem.NewGame(new NewGameOptions { seed = 1110, mapSize = MapSize.Small, startingCount = 3, startingTeam = Enumerable.Range(0, 3).Select(StartingCandidate.Roll).ToArray() });
        for (int i = 0; i < 300 && (SaveSystem.Busy || GameMenuController.Instance == null || !GameSession.Exists || !GameSession.Instance.GameStarted); i++) await Task.Delay(100);
        if (!GameSession.Exists || !GameSession.Instance.GameStarted) throw new Exception("Isolated game did not start");
        GameMenuController.Instance.Resume(); SimulationPause.UserSpeed(1);
        return "Isolated live game, autosave off, user settings/save root captured";
    }

    public static string Restore()
    {
        var root = SessionState.GetString("Pickaxe1111.root", ""); SaveStorage.RootOverride = root == "" ? null : root;
        var json = SessionState.GetString("Pickaxe1111.settings", "");
        if (json != "") UserSettings.Apply(JsonUtility.FromJson<UserSettingsData>(json), false);
        var controller = UnityEngine.Object.FindAnyObjectByType<AntColony.Camera.IsometricCameraController>(); if (controller) controller.enabled = true;
        return "Restored user settings and save root without writing saves";
    }

    static CommanderAnt Unit()
    {
        var player = UnityEngine.Object.FindAnyObjectByType<MeshyNativeMotionPlayer>();
        if (player) return player.GetComponent<CommanderAnt>();
        return CommanderRoster.Instance.Commanders.First(x => x != null && x.IsColonyMember);
    }

    // 실제 채집 명령으로 채굴을 시작한다. 워프는 대상 2m 밖까지만 하고, 작업 지점까지는 게임의 접근 로직이 걷게 둔다.
    public static async Task<string> Mine(string resource = "Stone")
    {
        var type = (ResourceType)Enum.Parse(typeof(ResourceType), resource);
        var c = Unit();
        var player = c.GetComponent<MeshyNativeMotionPlayer>(); if (player) { player.PreviewMotion = null; player.PreviewPhase = -1; }
        EditorApplication.isPaused = false;
        foreach (var previous in UnityEngine.Object.FindObjectsByType<ResourceNode>(FindObjectsInactive.Exclude)) if (previous.WorkRequested) previous.CancelWork();
        c.CommandStop();
        typeof(WorkerAnt).GetMethod("DropCargo", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)?.Invoke(c, null);
        var nodes = UnityEngine.Object.FindObjectsByType<ResourceNode>(FindObjectsInactive.Exclude).Where(n => n.ResourceType == type && !n.IsLooseCargo).ToArray();
        if (nodes.Length == 0) throw new Exception("No node of type " + resource);
        var node = nodes.Where(n => n.CanGather).OrderBy(n => (n.transform.position - c.Position).sqrMagnitude).FirstOrDefault();
        if (node == null) throw new Exception(resource + " nodes exist but CanGather=false (" + nodes.Length + ")");
        // 검사 준비(격리 게임 한정): 돌 도구 등급과 작업 지정. 실제 규칙은 그대로 두고 조건만 채운다.
        if (c.PersonalState.toolTier < ResourceNode.MiningToolTier) c.PersonalState.toolTier = ResourceNode.MiningToolTier;
        node.RequestWork();
        var nav = c.GetComponent<NavMeshAgent>();
        for (int i = 0; i < 12; i++) { var p = node.transform.position + Quaternion.Euler(0, i * 30, 0) * Vector3.forward * 2.5f; if (NavMesh.SamplePosition(p, out var hit, 3, NavMesh.AllAreas)) { nav.Warp(hit.position); break; } }
        c.CommandGather(node); SimulationPause.UserSpeed(1);
        for (int i = 0; i < 200 && !c.IsGatheringAnimation; i++) await Task.Delay(100);
        var v = c.GetComponent<AntVisual>();
        if (!c.IsGatheringAnimation || c.CurrentResourceNode != node)
            throw new Exception("Gather did not start: " + resource + " gathering=" + c.IsGatheringAnimation + " node=" + c.CurrentResourceNode + " state=" + v.StateName + " reason=" + node.WorkWaitReason);
        return resource + " actualGather=" + c.IsGatheringAnimation + " motion=" + v.MotionName + " tool=" + v.HeldTool + " native=" + (player ? player.PlayingMotion : "(no native player)")
            + " unit=" + c.Position + " node=" + node.transform.position + " horizontal=" + Vector3.ProjectOnPlane(c.Position - node.transform.position, Vector3.up).magnitude.ToString("0.000");
    }

    sealed class Scene
    {
        public CommanderAnt unit; public MeshyNativeMotionPlayer player; public ResourceNode node;
        public Transform mount, item; public Dictionary<string, Transform> bones;
        public Vector3 tipA, tipB, headCentre, end; // item-local
        public string axisNote; public List<GameObject> temp = new List<GameObject>(); public Bounds rock;
        public Vector3 Palm(string side) => Vector3.Lerp(bones["mixamorig:" + side + "Hand"].position, bones["mixamorig:" + side + "HandMiddle1"].position, .65f);
    }

    static Scene Build()
    {
        var s = new Scene(); s.unit = Unit(); s.player = s.unit.GetComponent<MeshyNativeMotionPlayer>();
        if (!s.player || !s.player.NativeModel) throw new Exception("Native motion player is not showing a native rig");
        s.node = s.unit.CurrentResourceNode; if (!s.node) throw new Exception("Unit has no current resource node");
        s.bones = s.player.NativeModel.GetComponentsInChildren<Transform>().GroupBy(t => t.name).ToDictionary(g => g.Key, g => g.First());
        s.mount = s.player.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "Native equipment grip" && t.gameObject.activeInHierarchy);
        if (!s.mount || s.mount.childCount == 0) throw new Exception("No native equipment mount (tool not held?)");
        s.item = s.mount.GetChild(0);
        // 날 끝: 자루 축(mount Z)의 머리 쪽 1/4 구간에서, 자루에 수직인 두 방향 중 더 긴 쪽의 양 끝 꼭짓점.
        var points = new List<Vector3>();
        foreach (var f in s.item.GetComponentsInChildren<MeshFilter>()) if (f.sharedMesh) foreach (var v in Read(f.sharedMesh)) points.Add(s.mount.InverseTransformPoint(f.transform.TransformPoint(v)));
        if (points.Count == 0) throw new Exception("Tool mesh has no readable vertices");
        float minZ = points.Min(p => p.z), maxZ = points.Max(p => p.z);
        var head = points.Where(p => p.z < minZ + (maxZ - minZ) * .25f).ToList();
        float ex = head.Max(p => p.x) - head.Min(p => p.x), ey = head.Max(p => p.y) - head.Min(p => p.y);
        bool alongX = ex >= ey;
        Vector3 a = alongX ? head.OrderByDescending(p => p.x).First() : head.OrderByDescending(p => p.y).First();
        Vector3 b = alongX ? head.OrderBy(p => p.x).First() : head.OrderBy(p => p.y).First();
        var centre = new Vector3(alongX ? 0 : (a.x + b.x) * .5f, alongX ? (a.y + b.y) * .5f : 0, (a.z + b.z) * .5f); if (alongX) centre.x = (a.x + b.x) * .5f; else centre.y = (a.y + b.y) * .5f;
        s.tipA = s.item.InverseTransformPoint(s.mount.TransformPoint(a)); s.tipB = s.item.InverseTransformPoint(s.mount.TransformPoint(b));
        s.headCentre = s.item.InverseTransformPoint(s.mount.TransformPoint(centre)); s.end = s.item.InverseTransformPoint(s.mount.TransformPoint(new Vector3(centre.x, centre.y, maxZ)));
        s.axisNote = $"mountLocal z={minZ:0.000}..{maxZ:0.000} headExtentX={ex:0.000} headExtentY={ey:0.000} tipAxis={(alongX ? "X" : "Y")} tipA={a:F3} tipB={b:F3} itemLocalScale={s.item.localScale:F3} itemLocalRot={s.item.localEulerAngles:F1} mountLossy={s.mount.lossyScale:F3}";
        // 바위의 보이는 표면: 켜진 MeshRenderer의 실제 메시로 임시 충돌체를 만든다(전용 레이어, 게임 물리에 영향 없음).
        bool first = true;
        foreach (var r in s.node.GetComponentsInChildren<MeshRenderer>())
        {
            if (!r.enabled || r.forceRenderingOff) continue; var f = r.GetComponent<MeshFilter>(); if (!f || !f.sharedMesh) continue;
            var go = new GameObject("Pickaxe1111 rock surface") { layer = RockLayer, hideFlags = HideFlags.DontSave };
            go.transform.SetPositionAndRotation(r.transform.position, r.transform.rotation); go.transform.localScale = r.transform.lossyScale;
            go.AddComponent<MeshCollider>().sharedMesh = f.sharedMesh; s.temp.Add(go);
            if (first) { s.rock = r.bounds; first = false; } else s.rock.Encapsulate(r.bounds);
        }
        if (s.temp.Count == 0) throw new Exception("Resource node has no visible mesh");
        Physics.SyncTransforms();
        return s;
    }
    static void Clean(Scene s) { foreach (var g in s.temp) UnityEngine.Object.DestroyImmediate(g); }

    // direction으로 계속 가면 표면까지 남은 거리. 음수면 이미 표면 안쪽. 빗나가면 NaN.
    static float Gap(Vector3 point, Vector3 direction)
    {
        if (direction.sqrMagnitude < 1e-8f) return float.NaN; direction.Normalize();
        return Physics.Raycast(point - direction * 5, direction, out var hit, 20, 1 << RockLayer, QueryTriggerInteraction.Ignore) ? hit.distance - 5 : float.NaN;
    }
    static float ToCentre(Scene s, Vector3 point) => Gap(point, s.rock.center - point);

    // 한 주기 측정: 모의 재생(시뮬레이션 정지)에서 위상별 날 끝·머리 중앙·손·몸과 바위 표면의 관계.
    public static async Task<string> Measure(string label = "Stone")
    {
        var s = Build();
        try
        {
            var visual = s.unit.GetComponent<AntVisual>();
            s.player.PreviewMotion = s.player.PlayingMotion; s.player.PreviewPhase = 0;
            SimulationPause.UserSpeed(0); EditorApplication.isPaused = false; await Task.Delay(200);
            const int steps = 48;
            var rows = new StringBuilder();
            rows.AppendLine("# " + label + " motion=" + visual.MotionName + " native=" + s.player.PlayingMotion + " tool=" + visual.HeldTool);
            rows.AppendLine("# " + s.axisNote);
            var pivot = s.mount.parent;
            rows.AppendLine($"# unit={s.unit.Position:F3} pivotForward={pivot.forward:F3} node={s.node.transform.position:F3} rockBounds centre={s.rock.center:F3} size={s.rock.size:F3}");
            rows.AppendLine($"# unit-to-rock-centre horizontal={Vector3.ProjectOnPlane(s.rock.center - s.unit.Position, Vector3.up).magnitude:0.000} facingDot={Vector3.Dot(pivot.forward, Vector3.ProjectOnPlane(s.rock.center - s.unit.Position, Vector3.up).normalized):0.000}");
            rows.AppendLine("phase\ttipA.y\ttipA.fwd\tgapA.centre\tgapA.aim\ttipB.y\ttipB.fwd\tgapB.centre\tgapB.aim\theadCentreGap\tleadPalmGap\thipsGap\tleadErr\tsupportOnShaft\trollDelta\tspeedA\taimA·vel");
            Vector3 prevA = Vector3.zero, prevRight = Vector3.zero; bool has = false;
            float bestA = float.MaxValue, bestB = float.MaxValue, phaseA = -1, phaseB = -1, maxRoll = 0, maxLead = 0, minSupport = 10, maxSupport = -10, minHead = float.MaxValue, minHips = float.MaxValue, lowA = float.MaxValue, lowAPhase = -1, lowB = float.MaxValue, lowBPhase = -1;
            for (int i = 0; i <= steps; i++)
            {
                float phase = i / (float)steps * .999f; s.player.PreviewPhase = phase; await Task.Delay(60);
                Vector3 a = s.item.TransformPoint(s.tipA), b = s.item.TransformPoint(s.tipB), hc = s.item.TransformPoint(s.headCentre);
                Vector3 aimA = (a - hc).normalized, aimB = (b - hc).normalized;
                float ga = ToCentre(s, a), gb = ToCentre(s, b), gaAim = Gap(a, aimA), gbAim = Gap(b, aimB);
                float headGap = ToCentre(s, hc), palmGap = ToCentre(s, s.Palm("Left")), hipsGap = ToCentre(s, s.bones["mixamorig:Hips"].position);
                var axis = s.mount.forward; float scale = s.item.lossyScale.y;
                float leadErr = Mathf.Max(Vector3.Cross(s.Palm("Left") - hc, axis).magnitude, Vector3.Cross(s.Palm("Right") - hc, axis).magnitude); // 두 손바닥이 자루 축에서 벗어난 거리
                float support = Vector3.Dot(s.item.TransformPoint(s.end) - s.Palm("Right"), axis); // 뒷손 뒤로 남은 자루 길이(음수면 손이 자루 밖)
                float roll = has ? Vector3.Angle(prevRight, s.mount.right) : 0; var vel = has ? a - prevA : Vector3.zero;
                Func<Vector3, float> fwd = p => Vector3.Dot(p - s.unit.Position, pivot.forward);
                rows.AppendLine($"{phase:0.000}\t{a.y - s.unit.Position.y:0.000}\t{fwd(a):0.000}\t{ga:0.000}\t{gaAim:0.000}\t{b.y - s.unit.Position.y:0.000}\t{fwd(b):0.000}\t{gb:0.000}\t{gbAim:0.000}\t{headGap:0.000}\t{palmGap:0.000}\t{hipsGap:0.000}\t{leadErr:0.000}\t{support:0.000}\t{roll:0.0}\t{vel.magnitude:0.000}\t{(vel.sqrMagnitude > 1e-8f ? Vector3.Dot(aimA, vel.normalized) : 0):0.00}");
                if (!float.IsNaN(ga) && ga < bestA) { bestA = ga; phaseA = phase; }
                if (!float.IsNaN(gb) && gb < bestB) { bestB = gb; phaseB = phase; }
                if (a.y < lowA) { lowA = a.y; lowAPhase = phase; } if (b.y < lowB) { lowB = b.y; lowBPhase = phase; }
                if (i > 0) maxRoll = Mathf.Max(maxRoll, roll); maxLead = Mathf.Max(maxLead, leadErr); minSupport = Mathf.Min(minSupport, support); maxSupport = Mathf.Max(maxSupport, support);
                if (!float.IsNaN(headGap)) minHead = Mathf.Min(minHead, headGap); if (!float.IsNaN(hipsGap)) minHips = Mathf.Min(minHips, hipsGap);
                prevA = a; prevRight = s.mount.right; has = true;
            }
            var summary = $"{label}: tipA minGap={bestA:0.000}m @phase {phaseA:0.00}, tipB minGap={bestB:0.000}m @phase {phaseB:0.00}; lowest tipA y @phase {lowAPhase:0.00}, tipB @phase {lowBPhase:0.00}; headCentre minGap={minHead:0.000}; hips minGap={minHips:0.000}; maxRollPerStep={maxRoll:0.0}deg; leadPalmErr max={maxLead:0.000}; support={minSupport:0.000}..{maxSupport:0.000}";
            rows.AppendLine("# " + summary);
            File.WriteAllText(Path.Combine(Dir, "measure-" + label + ".txt"), rows.ToString(), new UTF8Encoding(false));
            return summary + "\n" + s.axisNote;
        }
        finally { Clean(s); }
    }

 static Vector3[] Read(Mesh mesh){using(var data=UnityEditor.MeshUtility.AcquireReadOnlyMeshData(mesh)){using(var vertices=new Unity.Collections.NativeArray<Vector3>(data[0].vertexCount,Unity.Collections.Allocator.Temp)){data[0].GetVertices(vertices);return vertices.ToArray();}}}
 public static Task<string> Wood()=>Mine("Wood");
 public static Task<string> Clay()=>Mine("Clay");
 public static Task<string> Copper()=>Mine("CopperOre");
 public static Task<string> MeasureWood()=>Measure("Wood");
 public static Task<string> MeasureClay()=>Measure("Clay");
 public static Task<string> MeasureCopper()=>Measure("CopperOre"); public static async Task<string> WoodGif(){await Mine("Wood");return await Record("chopping");}
 public static async Task<string> StoneGif(){await Mine("Stone");return await Record("mining");}
 public static async Task<string> ClayGif(){await Mine("Clay");return await Record("digging");}
 static async Task<string> Record(string name){var c=Unit();var player=c.GetComponent<MeshyNativeMotionPlayer>();SimulationPause.UserSpeed(0);player.PreviewMotion=null;player.PreviewPhase=-1;var node=c.CurrentResourceNode;var outward=Vector3.ProjectOnPlane(c.Position-node.transform.position,Vector3.up).normalized;var right=Vector3.Cross(Vector3.up,-outward);var focus=(c.Position+node.transform.position)*.5f+Vector3.up*.3f;var camera=Camera.main;UnityEngine.Object.FindAnyObjectByType<AntColony.Camera.IsometricCameraController>().enabled=false;camera.orthographicSize=1.65f;camera.transform.position=focus+outward*3+right*3+Vector3.up*2;camera.transform.LookAt(focus);EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();var dir="work/final-motion-1113/media/"+name;Directory.CreateDirectory(dir);await Task.Delay(150);SimulationPause.UserSpeed(1);Time.timeScale=.5f;int frames=0;try{for(int n=0;n<80;n++){await Task.Delay(100);if(!c.IsGatheringAnimation)break;ScreenCapture.CaptureScreenshot(dir+"/"+n.ToString("D3")+".png");frames++;}}finally{SimulationPause.UserSpeed(0);}if(frames<12)throw new Exception("Too few real frames "+frames);var report=name+" "+frames+" actual Game frames, no Preview, simulation .5x, frame wait100ms plus rendering; full cycle not guaranteed";File.WriteAllText(dir+"/capture.txt",report);return report;}}