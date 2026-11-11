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
public static class Pickaxe1111Contact
{
    const int RockLayer = 30;
    static string Dir { get { var d = Path.Combine(Directory.GetCurrentDirectory(), "Temp", "pickaxe-1111"); Directory.CreateDirectory(d); return d; } }

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
        foreach (var f in s.item.GetComponentsInChildren<MeshFilter>()) if (f.sharedMesh) foreach (var v in f.sharedMesh.vertices) points.Add(s.mount.InverseTransformPoint(f.transform.TransformPoint(v)));
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

    // 바위 윗면 기준 여유 높이: 점 바로 아래(위)에 보이는 바위 표면이 있으면 point.y - 표면 y, 바위 밖이면 NaN. 음수 = 바위 속.
    static float Clearance(Vector3 point)
        => Physics.Raycast(new Vector3(point.x, point.y + 6, point.z), Vector3.down, out var hit, 20, 1 << RockLayer, QueryTriggerInteraction.Ignore) ? point.y - hit.point.y : float.NaN;

    // 손 궤적은 그대로 두고, 도구 굴림 기준(시상면)과 서는 거리만 바꿨을 때 날 끝이 어떻게 닿는지 계산한다(수정 전 탐색용).
    public static async Task<string> Explore(string label = "Stone")
    {
        var s = Build();
        try
        {
            s.player.PreviewMotion = s.player.PlayingMotion; s.player.PreviewPhase = 0; SimulationPause.UserSpeed(0); EditorApplication.isPaused = false; await Task.Delay(200);
            const int steps = 96; var pivot = s.mount.parent; Vector3 f = pivot.forward, n = pivot.right, o = s.unit.Position;
            float ground = Physics.Raycast(o + Vector3.up * 3, Vector3.down, out var g, 12, 1 << 8, QueryTriggerInteraction.Ignore) ? g.point.y : o.y;
            var L = new Vector3[steps + 1]; var R = new Vector3[steps + 1];
            for (int i = 0; i <= steps; i++) { s.player.PreviewPhase = i / (float)steps * .999f; await Task.Delay(45); L[i] = s.Palm("Left"); R[i] = s.Palm("Right"); }
            float headDist = Mathf.Abs(Vector3.Dot(s.item.TransformPoint(s.headCentre) - L[steps], s.mount.forward)), half = Vector3.Distance(s.item.TransformPoint(s.tipA), s.item.TransformPoint(s.tipB)) * .5f;
            var sb = new StringBuilder();
            sb.AppendLine($"# {label} unitY={o.y:0.000} groundY={ground:0.000} headDist={headDist:0.000} tipHalf={half:0.000} rock centre={s.rock.center:F3} size={s.rock.size:F3}");
            sb.AppendLine("# rock profile along facing (forward distance : rock top height above ground, '-' = no rock)");
            var prof = new StringBuilder(); for (float d = 0; d <= 2.01f; d += .1f) { var p = o + f * d; p.y = ground; var c = Clearance(p); prof.Append($"{d:0.0}:{(float.IsNaN(c) ? "-" : (-c).ToString("0.00"))} "); }
            sb.AppendLine("# " + prof);
            sb.AppendLine("phase\tL.fwd\tL.h\tL.side\tR.fwd\tR.h\tR.side\tstrike.fwd\tstrike.h\ttrail.fwd\ttrail.h\thead.fwd\thead.h\tstrikeAim·vel");
            var strike = new Vector3[steps + 1]; var head = new Vector3[steps + 1];
            for (int i = 0; i <= steps; i++)
            {
                var shaft = (R[i] - L[i]).normalized; var x = Vector3.Cross(n, shaft).normalized; // 시상면 안에서 자루에 수직
                head[i] = L[i] - shaft * headDist; strike[i] = head[i] - x * half; var trail = head[i] + x * half;
                var vel = i > 0 ? strike[i] - strike[i - 1] : Vector3.zero;
                Func<Vector3, string> fh = p => $"{Vector3.Dot(p - o, f):0.000}\t{p.y - ground:0.000}";
                sb.AppendLine($"{i / (float)steps:0.000}\t{fh(L[i])}\t{Vector3.Dot(L[i] - o, n):0.000}\t{fh(R[i])}\t{Vector3.Dot(R[i] - o, n):0.000}\t{fh(strike[i])}\t{fh(trail)}\t{fh(head[i])}\t{(vel.sqrMagnitude > 1e-8f ? Vector3.Dot(-x, vel.normalized) : 0):0.00}");
            }
            // 자루를 잡는 위치(앞손에서 머리 중앙까지 거리 h)와 서는 거리(d만큼 뒤로)를 바꿔 본다.
            // depth = 표면 안쪽으로 들어간 깊이(수직·수평 중 작은 값). 접촉은 내려치는 구간(위상 0.55 이후)의 첫 진입.
            sb.AppendLine("# h=lead palm to head centre, d=unit moved back. contact phase/height/handle angle below horizontal, deepest tip depth after contact, deepest head/shaft/palm depth, closest approach if no contact");
            var summary = new StringBuilder();
            Func<Vector3, float> depth = p =>
            {
                float c = Clearance(p); if (float.IsNaN(c) || c >= 0) return 0;
                float horizontal = Physics.Raycast(p - f * 3, f, out var hh, 3, 1 << RockLayer, QueryTriggerInteraction.Ignore) ? 3 - hh.distance : -c;
                return Mathf.Min(-c, Mathf.Max(0, horizontal));
            };
            foreach (float h in new[] { headDist, .25f, .32f, .38f, .45f })
                for (float d = -.1f; d <= .51f; d += .1f)
                {
                    var shift = -f * d; float first = -1, firstH = 0, firstAngle = 0, deepTip = 0, deepBody = 0, closest = float.MaxValue;
                    for (int i = steps * 55 / 100; i <= steps * 90 / 100; i++)
                    {
                        var shaft = (R[i] - L[i]).normalized; var x = Vector3.Cross(n, shaft).normalized;
                        var hc = L[i] - shaft * h; var tip = hc - x * half + shift;
                        float dt = depth(tip); float c = Clearance(tip);
                        if (!float.IsNaN(c)) closest = Mathf.Min(closest, c);
                        if (dt > 0 && first < 0) { first = i / (float)steps; firstH = tip.y - ground; firstAngle = Mathf.Asin(Mathf.Clamp(-Vector3.Dot(-shaft, Vector3.up), -1, 1)) * Mathf.Rad2Deg; }
                        deepTip = Mathf.Max(deepTip, dt);
                        for (float t = 0; t <= 1.001f; t += .25f) deepBody = Mathf.Max(deepBody, depth(Vector3.Lerp(hc, R[i], t) + shift)); // 머리 중앙~뒷손 사이 자루
                        deepBody = Mathf.Max(deepBody, depth(hc + x * half + shift)); // 반대쪽 날
                    }
                    var line = $"h={h:0.00} d={d:+0.0;-0.0}: contact={(first < 0 ? "none" : first.ToString("0.00"))} height={firstH:0.00} handleBelowHorizontal={-firstAngle:0}deg tipDeepest={deepTip:0.000} shaft/otherTip/handDeepest={deepBody:0.000} closest={(closest == float.MaxValue ? "n/a" : closest.ToString("0.000"))}";
                    sb.AppendLine("# " + line); summary.AppendLine(line);
                }
            File.WriteAllText(Path.Combine(Dir, "explore-" + label + ".txt"), sb.ToString(), new UTF8Encoding(false));
            return summary.ToString();
        }
        finally { Clean(s); }
    }

    // 채굴 대상들의 보이는 메시가 실행 중 읽을 수 있는지(빌드에서 표면 계산 가능 여부)와 크기를 본다.
    public static string Inspect()
    {
        var sb = new StringBuilder();
        foreach (var node in UnityEngine.Object.FindObjectsByType<ResourceNode>(FindObjectsInactive.Exclude).Where(n => n.RequiresMiningTool).OrderBy(n => n.ResourceType.ToString()).Take(14))
        {
            sb.Append(node.ResourceType + " canGather=" + node.CanGather + " colliders=" + string.Join(",", node.GetComponentsInChildren<Collider>().Select(c => c.GetType().Name + (c.isTrigger ? "(trigger)" : "") + ":L" + c.gameObject.layer)));
            foreach (var r in node.GetComponentsInChildren<MeshRenderer>())
            {
                var f = r.GetComponent<MeshFilter>(); if (!f || !f.sharedMesh) continue;
                sb.Append($" | {f.sharedMesh.name} readable={f.sharedMesh.isReadable} verts={f.sharedMesh.vertexCount} tris={f.sharedMesh.triangles.Length / 3} size={r.bounds.size:F2} lossy={r.transform.lossyScale:F2} asset={AssetDatabase.GetAssetPath(f.sharedMesh)}");
            }
            sb.AppendLine();
        }
        File.WriteAllText(Path.Combine(Dir, "inspect.txt"), sb.ToString(), new UTF8Encoding(false));
        return sb.ToString();
    }

    // 채굴 자원 모델의 메시를 실행 중 읽을 수 있게 한다(표면 접촉 계산용). 모양·재질은 바꾸지 않는다. Play 밖에서 실행.
    public static string EnableReadable()
    {
        if (Application.isPlaying) throw new Exception("Run outside Play");
        var paths = new[] { "Assets/Resources/Environment1110/Models/Rock/Rock.fbx", "Assets/Resources/Environment1110/Models/CopperDeposit/CopperDeposit.fbx",
            "Assets/Resources/Environment1110/Models/IronDeposit/IronDeposit.fbx", "Assets/Resources/ResourceModels/CC_OreOutcrop.fbx" };
        var sb = new StringBuilder();
        foreach (var path in paths)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) { sb.AppendLine(path + ": importer not found"); continue; }
            if (importer.isReadable) { sb.AppendLine(path + ": already readable"); continue; }
            importer.isReadable = true; importer.SaveAndReimport(); sb.AppendLine(path + ": Read/Write enabled");
        }
        return sb.ToString();
    }

    // 실제 재생(시뮬레이션 진행) 중 여러 주기에서 날 끝 최소 거리를 잰다. 모의 재생만 통과해서는 완료가 아니다.
    public static async Task<string> Live(string label = "Stone", float seconds = 8)
    {
        var s = Build();
        try
        {
            s.player.PreviewMotion = null; s.player.PreviewPhase = -1; SimulationPause.UserSpeed(1); EditorApplication.isPaused = false; await Task.Delay(300);
            float minA = float.MaxValue, minB = float.MaxValue, minHead = float.MaxValue, minHips = float.MaxValue, maxLead = 0, minSpare = 10, minClearTip = 10, minClearHead = 10, minClearShaft = 10, contact = -2; bool exact = false; int samples = 0, gathering = 0, touches = 0; bool inside = false;
            var end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end)
            {
                await Task.Delay(16); samples++;
                if (!s.unit.IsGatheringAnimation || s.unit.CurrentResourceNode != s.node || !s.mount) continue; gathering++;
                Vector3 a = s.item.TransformPoint(s.tipA), b = s.item.TransformPoint(s.tipB), hc = s.item.TransformPoint(s.headCentre);
                float ga = ToCentre(s, a), gb = ToCentre(s, b), gh = ToCentre(s, hc), hips = ToCentre(s, s.bones["mixamorig:Hips"].position);
                if (!float.IsNaN(ga)) minA = Mathf.Min(minA, ga); if (!float.IsNaN(gb)) minB = Mathf.Min(minB, gb);
                if (!float.IsNaN(gh)) minHead = Mathf.Min(minHead, gh); if (!float.IsNaN(hips)) minHips = Mathf.Min(minHips, hips);
                float best = Mathf.Min(float.IsNaN(ga) ? 9 : ga, float.IsNaN(gb) ? 9 : gb);
                if (best <= .02f && !inside) { touches++; inside = true; } else if (best > .08f) inside = false;
                maxLead = Mathf.Max(maxLead, Mathf.Max(Vector3.Cross(s.Palm("Left") - hc, s.mount.forward).magnitude, Vector3.Cross(s.Palm("Right") - hc, s.mount.forward).magnitude)); minSpare = Mathf.Min(minSpare, Vector3.Dot(s.item.TransformPoint(s.end) - s.Palm("Right"), s.mount.forward)); float ca = Clearance(a), cb = Clearance(b), ch = Clearance(hc), cm = Clearance(Vector3.Lerp(hc, s.Palm("Left"), .5f)); if (!float.IsNaN(ca)) minClearTip = Mathf.Min(minClearTip, ca); if (!float.IsNaN(cb)) minClearTip = Mathf.Min(minClearTip, cb); if (!float.IsNaN(ch)) minClearHead = Mathf.Min(minClearHead, ch); if (!float.IsNaN(cm)) minClearShaft = Mathf.Min(minClearShaft, cm); contact = s.player.StrikeContactPhase; exact = s.player.StrikeSurfaceExact;
            }
            var result = $"{label} live {seconds:0}s: samples={samples} gatheringSamples={gathering} touches(tip within 2cm)={touches} tipA min={minA:0.000} tipB min={minB:0.000} headCentre min={minHead:0.000} hips min={minHips:0.000} palmOffShaft max={maxLead:0.000} spareHandle min={minSpare:0.000} | vertical clearance above rock top: tip min={minClearTip:0.000} headCentre min={minClearHead:0.000} shaftMid min={minClearShaft:0.000} | contactPhase={contact:0.000} exactSurface={exact} state={s.unit.GetComponent<AntVisual>().StateName} stillGathering={s.unit.IsGatheringAnimation}";
            File.AppendAllText(Path.Combine(Dir, "live.txt"), result + "\n");
            return result;
        }
        finally { Clean(s); }
    }

    // 실제 재생 중 날 끝이 표면에 닿는 순간 시뮬레이션을 멈추고(포즈 고정) 넓은 화면·근접 화면을 찍은 뒤, 한 주기를 연속으로 찍는다.
    public static async Task<string> CaptureContact(string label = "Stone")
    {
        var s = Build();
        try
        {
            s.player.PreviewMotion = null; s.player.PreviewPhase = -1; SimulationPause.UserSpeed(1); EditorApplication.isPaused = false;
            var controller = UnityEngine.Object.FindAnyObjectByType<AntColony.Camera.IsometricCameraController>(); if (controller) controller.enabled = false;
            var original = s.unit.transform.Find("Kemono"); var cam = Camera.main; cam.orthographic = true; cam.nearClipPlane = .02f;
            EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
            Action<Vector3, float, Vector3> look = (focus, size, offset) => { cam.orthographicSize = size; cam.transform.position = focus + offset; cam.transform.LookAt(focus); };
            var mid = Vector3.Lerp(s.unit.Position, s.node.transform.position, .45f); mid.y = s.rock.min.y + .75f;
            var sideOffset = original.right * 2.6f + Vector3.up * .3f;
            // 한 주기 연속(넓은 옆모습)
            look(mid, 1.3f, sideOffset); await Task.Delay(200);
            var log = new StringBuilder(); float plane = 0;
            for (int i = 0; i < 12; i++)
            {
                var a0 = s.item.TransformPoint(s.tipA); var b0 = s.item.TransformPoint(s.tipB); float ca = Clearance(a0), cb = Clearance(b0);
                plane = Mathf.Max(plane, Mathf.Abs(Vector3.Dot(s.mount.right, s.mount.parent.right)));
                log.AppendLine($"cycle-{i:00}: tipA h={a0.y - s.rock.min.y:0.00} clear={ca:0.000} tipB h={b0.y - s.rock.min.y:0.00} clear={cb:0.000} gathering={s.unit.IsGatheringAnimation}");
                ScreenCapture.CaptureScreenshot(Path.Combine(Dir, $"cycle-{label}-{i:00}.png")); await Task.Delay(170);
            }
            // 접촉 순간 고정
            var until = Time.realtimeSinceStartup + 12; bool touched = false; Vector3 tip = Vector3.zero; float clear = 9;
            while (Time.realtimeSinceStartup < until)
            {
                await Task.Delay(8);
                if (!s.unit.IsGatheringAnimation) continue;
                Vector3 a = s.item.TransformPoint(s.tipA), b = s.item.TransformPoint(s.tipB); float ca = Clearance(a), cb = Clearance(b);
                float best = Mathf.Min(float.IsNaN(ca) ? 9 : ca, float.IsNaN(cb) ? 9 : cb);
                if (best <= .012f) { touched = true; tip = (float.IsNaN(cb) || ca <= cb) && !float.IsNaN(ca) ? a : b; clear = best; SimulationPause.UserSpeed(0); break; }
            }
            if (!touched) throw new Exception("No contact within 12s (gathering=" + s.unit.IsGatheringAnimation + ")\n" + log);
            await Task.Delay(200);
            look(mid, 1.3f, sideOffset); await Task.Delay(150); ScreenCapture.CaptureScreenshot(Path.Combine(Dir, $"contact-{label}-side.png")); await Task.Delay(450);
            look(tip, .42f, sideOffset); await Task.Delay(150); ScreenCapture.CaptureScreenshot(Path.Combine(Dir, $"contact-{label}-close.png")); await Task.Delay(450);
            look(mid, 1.3f, original.right * 1.6f - original.forward * 2.0f + Vector3.up * 1.2f); await Task.Delay(150); ScreenCapture.CaptureScreenshot(Path.Combine(Dir, $"contact-{label}-quarter.png")); await Task.Delay(450);
            look(tip, .42f, -original.right * 2.6f + Vector3.up * .3f); await Task.Delay(150); ScreenCapture.CaptureScreenshot(Path.Combine(Dir, $"contact-{label}-close-other.png")); await Task.Delay(450);
            float off = Mathf.Max(Vector3.Cross(s.Palm("Left") - s.item.TransformPoint(s.headCentre), s.mount.forward).magnitude, Vector3.Cross(s.Palm("Right") - s.item.TransformPoint(s.headCentre), s.mount.forward).magnitude);
            var result = $"{label} contact frozen: tip clearance={clear:0.000}m tipHeightAboveGround={tip.y - s.rock.min.y:0.00} headCentre clearance={Clearance(s.item.TransformPoint(s.headCentre)):0.000} palmsOffShaft={off:0.000} headOutOfSwingPlane(max |cos|)={plane:0.000} contactPhase={s.player.StrikeContactPhase:0.000} exact={s.player.StrikeSurfaceExact}";
            File.WriteAllText(Path.Combine(Dir, $"capture-{label}.txt"), result + "\n" + log, new UTF8Encoding(false));
            SimulationPause.UserSpeed(1);
            return result;
        }
        finally { Clean(s); }
    }
    // 옆에서 본 화면을 고정 위상으로 캡처한다(Game 뷰 실제 렌더).
    public static async Task<string> Capture(string label = "Stone", float phase = .5f)
    {
        var c = Unit(); var player = c.GetComponent<MeshyNativeMotionPlayer>(); var node = c.CurrentResourceNode;
        if (!player || !node) throw new Exception("Mine first");
        if (phase >= 0) { player.PreviewMotion = player.PlayingMotion; player.PreviewPhase = phase; SimulationPause.UserSpeed(0); }
        else { player.PreviewMotion = null; player.PreviewPhase = -1; SimulationPause.UserSpeed(1); }
        EditorApplication.isPaused = false;
        var controller = UnityEngine.Object.FindAnyObjectByType<AntColony.Camera.IsometricCameraController>(); if (controller) controller.enabled = false;
        var original = c.transform.Find("Kemono");
        var focus = Vector3.Lerp(c.Position, node.transform.position, .5f) + Vector3.up * .7f;
        var cam = Camera.main; cam.orthographic = true; cam.orthographicSize = 1.25f; cam.nearClipPlane = .02f;
        cam.transform.position = focus + original.right * 2.4f + Vector3.up * .35f; cam.transform.LookAt(focus);
        EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
        await Task.Delay(250);
        var file = Path.Combine(Dir, $"contact-{label}-{(phase < 0 ? "live" : phase.ToString("0.00"))}.png");
        ScreenCapture.CaptureScreenshot(file); await Task.Delay(500);
        return file;
    }
}
