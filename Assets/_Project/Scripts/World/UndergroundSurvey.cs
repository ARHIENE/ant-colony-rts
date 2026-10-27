using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using UnityEngine;

namespace AntColony.World
{
    public sealed class UndergroundSurvey : MonoBehaviour
    {
        [Serializable] public sealed class Layer
        {
            public int id, map, type, total;
            public Vector3 position;
            public float work, remaining;
            public bool discovered, discoveryPending, exhaustionNotified;
        }
        [Serializable] public sealed class State
        {
            public bool present;
            public List<Layer> layers = new List<Layer>();
            public float noticeSeconds;
        }
        public static UndergroundSurvey Instance { get; private set; }
        private State state = new State();
        private readonly Dictionary<int, ResourceNode> nodes = new Dictionary<int, ResourceNode>();
        public IReadOnlyList<Layer> Layers => state.layers;
        public Layer Find(int id) => state.layers.FirstOrDefault(l => l.id == id);
        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }
        public void Initialize()
        {
            state = new State { present = true };
            EnsureMaps();
        }
        private void EnsureMaps()
        {
            var world = WorldMapManager.Instance;
            if (world == null) return;
            var options = GameSession.Instance.Options;
            AddMap(0, world.HomePosition, 40 * MapSizes.Scale(options.mapSize), options.biome, options.seed);
            for (int i = 0; i < world.Sites.Count; i++)
                AddMap(i + 1, world.Sites[i].transform.position, 30, world.Sites[i].Biome, options.seed);
        }
        private void AddMap(int map, Vector3 center, float radius, MapBiome biome, int seed)
        {
            if (!state.layers.Any(l => l.map == map)) state.layers.AddRange(Generate(seed, map, center, radius, biome));
        }
        public static int Amount(ResourceType type) => type switch {
            ResourceType.IronOre or ResourceType.CopperOre or ResourceType.Coal => 400,
            ResourceType.CrudeOil or ResourceType.NaturalGas => 600,
            ResourceType.TitaniumOre => 160, ResourceType.StellarOre => 80, _ => 0 };
        public static List<Layer> Generate(int seed, int map, Vector3 center, float radius, MapBiome biome)
        {
            var rng = new System.Random(unchecked(seed ^ 0xD0800 ^ map * 73856093));
            var types = new[] { ResourceType.IronOre, ResourceType.CopperOre, ResourceType.Coal, ResourceType.CrudeOil,
                ResourceType.NaturalGas, ResourceType.TitaniumOre, ResourceType.StellarOre };
            var result = new List<Layer>();
            // 각 후보의 구역을 분리해 3×3층이 중첩되지 않게 한다. 자원 종류는 기존 바이옴 배율로 가중 추첨한다.
            var slots = Enumerable.Range(0, 12).ToArray();
            for (int i = 11; i > 0; i--) { int j = rng.Next(i + 1); (slots[i], slots[j]) = (slots[j], slots[i]); }
            var weights = types.Select(t => AntColony.Map.MaterialDeposits.Multiplier(biome, t)).ToArray();
            for (int i = 0; i < 12; i++)
            {
                int slot = slots[i];
                var pos = center + new Vector3((slot % 4 - 1.5f) * radius / 2 + (float)rng.NextDouble() * 2 - 1,
                    0, (slot / 4 - 1) * radius / 2 + (float)rng.NextDouble() * 2 - 1);
                pos.x = Mathf.Floor(pos.x) + .5f; pos.z = Mathf.Floor(pos.z) + .5f;
                int type = -1;
                if (i >= 4)
                {
                    var roll = (float)rng.NextDouble() * weights.Sum(); int choice = 0;
                    while (choice < weights.Length - 1 && roll >= weights[choice]) roll -= weights[choice++];
                    type = (int)types[choice];
                }
                int total = type < 0 ? 0 : Amount((ResourceType)type);
                result.Add(new Layer { id = map * 12 + i, map = map, type = type, position = pos, total = total, remaining = total });
            }
            return result;
        }
        public int MapAt(Vector3 position)
        {
            var world = WorldMapManager.Instance;
            for (int i = 0; i < world.Sites.Count; i++)
                if (Mathf.Abs(position.x - world.Sites[i].transform.position.x) <= 35
                    && Mathf.Abs(position.z - world.Sites[i].transform.position.z) <= 35) return i + 1;
            return 0;
        }
        public bool InRange(int id, Vector3 at)
        {
            var layer = Find(id);
            if (layer == null || layer.discovered || layer.map != MapAt(at)) return false;
            var delta = layer.position - at; delta.y = 0;
            return delta.sqrMagnitude <= UndergroundScanner.Radius * UndergroundScanner.Radius;
        }
        internal int Reserve(UndergroundScanner scanner)
        {
            EnsureMaps();
            var taken = AntColony.Core.SceneObjects.All<UndergroundScanner>()
                .Where(s => s != scanner && s.isActiveAndEnabled && !s.IsDead && s.Target != null && InRange(s.CandidateId, s.Position))
                .Select(s => s.CandidateId).ToHashSet();
            return state.layers.Where(l => InRange(l.id, scanner.Position) && !taken.Contains(l.id))
                .OrderBy(l => new Vector2(l.position.x - scanner.Position.x, l.position.z - scanner.Position.z).sqrMagnitude)
                .ThenBy(l => l.id).Select(l => l.id).DefaultIfEmpty(-1).First();
        }
        internal void Work(int id, float amount)
        {
            var l = Find(id);
            if (l == null || l.discovered || !(amount > 0) || float.IsInfinity(amount)) return;
            l.work = Mathf.Min(UndergroundScanner.CandidateSeconds, l.work + amount);
            if (l.work < UndergroundScanner.CandidateSeconds) return;
            l.discovered = true; l.discoveryPending = true; CreateNode(l);
        }
        private void CreateNode(Layer l)
        {
            if (!l.discovered || l.type < 0 || nodes.ContainsKey(l.id)) return;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.SetActive(false); go.name = "Underground Layer " + l.id;
            go.transform.SetParent(transform); go.transform.position = l.position + Vector3.up * .1f;
            go.transform.localScale = new Vector3(3, .2f, 3); go.GetComponent<Collider>().isTrigger = true;
            var marker = go.AddComponent<UndergroundLayer>(); marker.Id = l.id;
            var node = go.AddComponent<ResourceNode>(); node.ConfigureNatural((ResourceType)l.type, l.remaining, 0);
            node.IsUndergroundLayer = true; node.GatheringForbidden = true; nodes[l.id] = node; go.SetActive(l.remaining > 0);
        }
        public static string RequiredFacility(ResourceType type) => type == ResourceType.CrudeOil ? "원유 시추기"
            : type == ResourceType.NaturalGas ? "가스 추출기" : type == ResourceType.StellarOre ? "합금 심층 채굴기" : "심층 채굴기";
        public string Description(Layer l) => l.type < 0 ? $"매장 없음 · ({l.position.x:0.0}, {l.position.z:0.0})"
            : $"{((ResourceType)l.type).DisplayName()} · ({l.position.x:0.0}, {l.position.z:0.0}) · 남은 총량 {l.remaining:0} · {RequiredFacility((ResourceType)l.type)} 필요"
                + (CampaignResearch.Instance?.ResourceResearched((ResourceType)l.type) == true ? "" : " · 표본 연구 필요");
        // 별도 생산 시설용 연결점. 발견·범위·표본 연구를 통과한 같은 층 노드에서만 차감한다.
        internal float Extract(int id, Vector3 facilityPosition, ResourceType type, float amount)
        {
            var l = Find(id);
            if (l == null || !l.discovered || l.type != (int)type || MapAt(facilityPosition) != l.map
                || Mathf.Abs(facilityPosition.x - l.position.x) > 1.5f || Mathf.Abs(facilityPosition.z - l.position.z) > 1.5f
                || CampaignResearch.Instance?.ResourceResearched(type) != true || !(amount > 0) || float.IsInfinity(amount)
                || !nodes.TryGetValue(id, out var node)) return 0;
            return Consume(l, node, amount);
        }
        internal float ExtractSample(int id, Vector3 at, ResourceType type)
        {
            var l = Find(id);
            if (l == null || !l.discovered || l.type != (int)type || MapAt(at) != l.map
                || Mathf.Abs(at.x - l.position.x) > 1.5f || Mathf.Abs(at.z - l.position.z) > 1.5f
                || CampaignResearch.Instance?.HasSample(type) != false || l.remaining < 1
                || !nodes.TryGetValue(id, out var node)) return 0;
            return Consume(l, node, 1);
        }
        private float Consume(Layer l, ResourceNode node, float amount)
        {
            var type = (ResourceType)l.type;
            var take = Mathf.Min(amount, node.AmountRemaining);
            if (!node.TryConsumeStock(take)) return 0;
            l.remaining = node.AmountRemaining;
            if (l.remaining <= 0)
            {
                node.gameObject.SetActive(false);
                if (!l.exhaustionNotified) { l.exhaustionNotified = true; AntColony.UI.ToastManager.ShowAt(type.DisplayName() + " 매장층 고갈", () => Highlight(new[] { l.id })); }
            }
            return take;
        }
        private void Update() => TickNotices(Time.deltaTime);
        public void TickNotices(float seconds)
        {
            if (!(seconds > 0) || float.IsInfinity(seconds) || Time.timeScale <= 0 || AntColony.Save.SaveSystem.Busy
                || !GameSession.Exists || !GameSession.Instance.GameStarted || !state.layers.Any(l => l.discoveryPending)) return;
            state.noticeSeconds += seconds;
            if (state.noticeSeconds < 1) return;
            var pending = state.layers.Where(l => l.discoveryPending).ToArray();
            var ids = pending.Select(l => l.id).ToArray();
            AntColony.UI.ToastManager.ShowAt(string.Join("\n", pending.Select(Description)), () => Highlight(ids), "매장층 범위");
            foreach (var l in pending) l.discoveryPending = false;
            state.noticeSeconds = 0;
        }
        public void Highlight(int[] ids)
        {
            foreach (var id in ids)
            {
                var l = Find(id); if (l == null || !l.discovered) continue;
                var go = new GameObject("Survey highlight"); var line = go.AddComponent<LineRenderer>();
                line.useWorldSpace = true; line.loop = true; line.positionCount = 4; line.startWidth = line.endWidth = .12f;
                line.material = new Material(Shader.Find("Sprites/Default")); line.startColor = line.endColor = Color.yellow;
                var at = l.position + Vector3.up * .3f;
                line.SetPositions(new[] { at + new Vector3(-1.5f,0,-1.5f), at + new Vector3(1.5f,0,-1.5f), at + new Vector3(1.5f,0,1.5f), at + new Vector3(-1.5f,0,1.5f) });
                Destroy(line.material, 4); Destroy(go, 4);
            }
            var first = ids.Select(Find).FirstOrDefault(l => l != null);
            if (first == null) return;
            if (first.map > 0) WorldMapManager.Instance.ViewSite(WorldMapManager.Instance.Sites[first.map - 1]);
            else WorldMapManager.Instance.ViewSite(null);
            AntColony.Core.SceneObjects.Any<AntColony.Camera.IsometricCameraController>()?.FocusOn(first.position);
        }
        public State Capture()
        {
            EnsureMaps();
            foreach (var pair in nodes) if (pair.Value != null) Find(pair.Key).remaining = pair.Value.AmountRemaining;
            return JsonUtility.FromJson<State>(JsonUtility.ToJson(state));
        }
        public void Restore(State saved)
        {
            foreach (var n in nodes.Values) if (n != null) { n.gameObject.SetActive(false); Destroy(n.gameObject); }
            nodes.Clear();
            if (saved?.present != true) Initialize();
            else { state = JsonUtility.FromJson<State>(JsonUtility.ToJson(saved)); EnsureMaps(); }
            foreach (var l in state.layers) CreateNode(l);
        }
        public static bool Valid(State s, int maps)
        {
            if (s?.present != true) return true;
            if (s.layers == null || s.layers.Count != maps * 12 || !(s.noticeSeconds >= 0 && s.noticeSeconds < 1)) return false;
            var ids = new HashSet<int>();
            foreach (var l in s.layers)
                if (l == null || l.map < 0 || l.map >= maps || l.id < l.map * 12 || l.id >= (l.map + 1) * 12 || !ids.Add(l.id)
                    || !Finite(l.position.x) || !Finite(l.position.y) || !Finite(l.position.z)
                    || l.type != -1 && Amount((ResourceType)l.type) == 0 || l.total != (l.type == -1 ? 0 : Amount((ResourceType)l.type))
                    || !(l.work >= 0 && l.work <= UndergroundScanner.CandidateSeconds) || !(l.remaining >= 0 && l.remaining <= l.total)
                    || l.discovered != (l.work == UndergroundScanner.CandidateSeconds) || l.discoveryPending && !l.discovered
                    || !l.discovered && l.remaining != l.total
                    || l.discovered && l.type >= 0 && l.remaining == 0 && !l.exhaustionNotified || l.exhaustionNotified && (!l.discovered || l.type < 0 || l.remaining != 0)) return false;
            return s.layers.GroupBy(l => l.map).All(g => g.Count() == 12 && g.Count(l => l.type == -1) == 4);
        }
        private static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v) && Mathf.Abs(v) < 100000;
    }
}
