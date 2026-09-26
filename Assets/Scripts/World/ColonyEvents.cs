using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Map;
using AntColony.Save;
using AntColony.Units;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace AntColony.World
{
    public sealed class ColonyEvents : MonoBehaviour
    {
        [Serializable] public class State
        {
            public float checkRemaining = EventRules.CheckSeconds, sinceLast = EventRules.MinimumGap;
            public float cold, flood, drought;
            public float[] cooldowns = new float[11];
            public List<EventActor.State> actors = new List<EventActor.State>();
        }
        public static ColonyEvents Instance { get; private set; }
        private State state = new State();
        private readonly Dictionary<ResourceNode, bool> floodNodes = new Dictionary<ResourceNode, bool>();
        public float ColdRemaining => state.cold;
        public float FloodRemaining => state.flood;
        public float DroughtRemaining => state.drought;
        public static bool ProductionBlocked => FindObjectsByType<EventActor>(FindObjectsSortMode.None).Any(a => a.Kind == EventActorKind.Wasp && a.Alive);
        public static float MoveMultiplier(CommanderAnt c) => Instance != null && Instance.state.cold > 0 && !c.IsAwayFromHome
            ? 1 - EventRules.ColdMovePenalty * ScienceEffects.ColdEffectMultiplier : 1;
        public static float GatherMultiplier(CommanderAnt c) => Instance != null && Instance.state.cold > 0 && !c.IsAwayFromHome
            ? 1 - EventRules.ColdGatherPenalty * ScienceEffects.ColdEffectMultiplier : 1;
        public static bool Flooded(ResourceNode node) => Instance != null && Instance.state.flood > 0 && !ScienceEffects.FloodImmune && Instance.NearWater(node);
        public static float GrowthMultiplier(ResourceNode node)
        {
            if (Flooded(node)) return 0;
            if (Instance == null || Instance.state.drought <= 0 || !Home(node.transform.position) || node.GetComponent<BuildingBase>() == null) return 1;
            return 1 - (node.GetComponent<FarmPlot>()?.DroughtPenalty ?? ScienceEffects.DroughtGrowthPenalty);
        }
        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }
        private void Update()
        {
            if (!GameSession.Exists || !GameSession.Instance.GameStarted || SaveSystem.Busy) return;
            Tick(Time.deltaTime);
        }
        public void Tick(float seconds)
        {
            if (!(seconds > 0) || float.IsInfinity(seconds)) return;
            // 일정한 작은 스텝으로 이벤트 간격과 만료를 보존한다(검사의 큰 시간 점프도 동일).
            while (seconds > 0)
            {
                var dt = Mathf.Min(seconds, 1); seconds -= dt;
                state.cold = Mathf.Max(0, state.cold - dt); state.flood = Mathf.Max(0, state.flood - dt); state.drought = Mathf.Max(0, state.drought - dt);
                state.sinceLast = Mathf.Min(EventRules.Cooldown, state.sinceLast + dt);
                for (int i = 0; i < state.cooldowns.Length; i++) state.cooldowns[i] = Mathf.Max(0, state.cooldowns[i] - dt);
                foreach (var actor in FindObjectsByType<EventActor>(FindObjectsSortMode.None)) actor.Tick(dt);
                state.checkRemaining -= dt;
                if (state.checkRemaining > 0) continue;
                state.checkRemaining += EventRules.CheckSeconds;
                if (state.sinceLast < EventRules.MinimumGap || Random.value >= EventRules.Chance(DifficultyRuntime.Level)) continue;
                var crisis = Random.value < EventRules.CrisisChance(DifficultyRuntime.Level);
                var choices = Enum.GetValues(typeof(ColonyEvent)).Cast<ColonyEvent>().Where(e => ((int)e <= 5) == crisis && Eligible(e)).ToArray();
                if (choices.Length > 0) TryTrigger(choices[Random.Range(0, choices.Length)]);
            }
        }
        private static bool Home(Vector3 p) => HomeMapBuilder.CurrentWorldBounds.Contains(new Vector3(p.x, 0, p.z));
        private static ResourceNode[] Farms() => FindObjectsByType<ResourceNode>(FindObjectsSortMode.None)
            .Where(n => n.RegrowSeconds > 0 && n.GetComponent<BuildingBase>() != null && Home(n.transform.position)).ToArray();
        public bool Eligible(ColonyEvent e) => Enum.IsDefined(typeof(ColonyEvent), e) && state.cooldowns[(int)e] == 0
            && EventRules.InSeason(e, GameCalendar.CurrentSeason) && (e switch
            {
                ColonyEvent.Wildfire or ColonyEvent.Harvest => Farms().Length > 0,
                ColonyEvent.Mold => HomeCommanders().Any(c => !c.PersonalState.infected),
                ColonyEvent.Wasps => !ProductionBlocked && FindFirstObjectByType<QueenChamber>() != null,
                ColonyEvent.Wanderer => CommanderRoster.Instance != null && CommanderRoster.Instance.Count < ScoutPost.DefaultMaxCommanders,
                _ => true
            });
        public bool TryTrigger(ColonyEvent e)
        {
            if (state.sinceLast < EventRules.MinimumGap || !Eligible(e)) return false;
            string result;
            switch (e)
            {
                case ColonyEvent.Cold: state.cold = EventRules.ColdSeconds; result = "90초간 이동 -25%·채집 -20% (보온 설비로 감소)"; break;
                case ColonyEvent.Flood: state.flood = EventRules.FloodSeconds; CacheFloodNodes(); result = "물가 20m 내 자원·밭·낚시터 60초 중단 (치수 공사로 면제)"; break;
                case ColonyEvent.Drought: state.drought = EventRules.DroughtSeconds; result = "120초간 밭 성장 감소 (감로·치수 공사는 -20%)"; break;
                case ColonyEvent.Wildfire:
                    var farms = Farms(); Burn(farms[Random.Range(0, farms.Length)]); result = "밭에 산불 발생. 흙벽·방화대로 확산을 막을 수 있습니다."; break;
                case ColonyEvent.Mold:
                    var healthy = HomeCommanders().Where(c => !c.PersonalState.infected).ToArray();
                    var patient = healthy[Random.Range(0, healthy.Length)]; Infect(patient); result = patient.CommanderName + " 감염. 의무실에서 치료하세요."; break;
                case ColonyEvent.Wasps:
                    if (!TryEdge(out var waspPoint)) return false;
                    for (int i = 0; i < EventRules.Wasps; i++) EventActor.Spawn(new EventActor.State { kind = EventActorKind.Wasp,
                        position = new Vec3Dto(waspPoint + Vector3.up * 2 + Vector3.right * i), amount = EventRules.WaspHealth });
                    result = "말벌 3마리 습격. 모두 처치할 때까지 일반개미 생산이 멈춥니다."; break;
                case ColonyEvent.Wanderer:
                    if (!TryEdge(out var wandererPoint)) return false;
                    EventActor.Spawn(new EventActor.State { kind = EventActorKind.Wanderer, position = new Vec3Dto(wandererPoint), remaining = EventRules.WandererSeconds });
                    result = $"맵 가장자리 ({wandererPoint.x:0}, {wandererPoint.z:0})에 후보 발견. 60초 안에 장수를 3m 이내로 보내세요."; break;
                case ColonyEvent.Driftwood:
                    if (!TryEdge(out var driftPoint)) return false;
                    EventActor.Spawn(new EventActor.State { kind = EventActorKind.Food, position = new Vec3Dto(driftPoint), remaining = EventRules.DriftSeconds, amount = EventRules.DriftFood });
                    EventActor.Spawn(new EventActor.State { kind = EventActorKind.Soil, position = new Vec3Dto(driftPoint + Vector3.right * 2), remaining = EventRules.DriftSeconds, amount = EventRules.DriftSoil });
                    result = $"({driftPoint.x:0}, {driftPoint.z:0})에 Food 80·Soil 40. 5분 안에 운반하세요."; break;
                case ColonyEvent.Harvest:
                    foreach (var farm in Farms()) farm.GrantBountifulHarvest(); result = "각 밭의 다음 수확 1회 +50%"; break;
                case ColonyEvent.Migration: AntPool.Instance?.Breed(EventRules.Migrants); result = "일반개미 10마리 합류"; break;
                case ColonyEvent.Caravan:
                    DiplomacyManager.Instance.Data.caravanUntil = DiplomacyManager.Instance.Data.elapsed + DiplomacyRules.CaravanSeconds;
                    result = "교역 캐러밴 도착 — J 외교에서 거래 (90초)"; break;
                default: return false;
            }
            state.cooldowns[(int)e] = EventRules.Cooldown; state.sinceLast = 0;
            if (CampaignHistory.Recording) CampaignHistory.Instance.Data.events[(int)e]++;
            CampaignHistory.Record("이벤트", EventRules.Names[(int)e], result, true);
            return true;
        }
        public static IEnumerable<CommanderAnt> HomeCommanders() => CommanderRoster.Instance == null ? Enumerable.Empty<CommanderAnt>()
            : CommanderRoster.Instance.Commanders.Where(c => c.isActiveAndEnabled && !c.IsDead && !c.IsAwayFromHome && !c.IsEmbarked);
        public static void Infect(CommanderAnt c)
        {
            if (c == null || c.IsDead || c.PersonalState.infected) return;
            c.PersonalState.infected = true; c.PersonalState.moldLoss = c.PersonalState.moldSpread = c.PersonalState.moldTreatment = 0;
        }
        private void CacheFloodNodes()
        {
            floodNodes.Clear();
        }
        private bool NearWater(ResourceNode node)
        {
            if (node == null || !Home(node.transform.position)) return false;
            if (floodNodes.TryGetValue(node, out var result)) return result;
            var terrain = FindFirstObjectByType<MapGenerator>();
            result = node.RequiresFishing || FindObjectsByType<ResourceNode>(FindObjectsSortMode.None).Any(f => f.RequiresFishing
                && Vector2.Distance(new Vector2(f.transform.position.x, f.transform.position.z), new Vector2(node.transform.position.x, node.transform.position.z)) <= EventRules.WaterRadius)
                || terrain != null && terrain.NearWater(node.transform.position, EventRules.WaterRadius);
            floodNodes[node] = result; return result;
        }
        public static void Burn(ResourceNode source)
        {
            var burned = new HashSet<ResourceNode>(); var queue = new Queue<ResourceNode>(); queue.Enqueue(source);
            var farms = Farms(); var nodes = FindObjectsByType<ResourceNode>(FindObjectsSortMode.None);
            while (queue.Count > 0)
            {
                var farm = queue.Dequeue(); if (!burned.Add(farm)) continue;
                farm.BurnCrop(ScienceEffects.WildfireDamageMultiplier);
                foreach (var node in nodes.Where(n => n != farm && n.GetComponent<BuildingBase>() == null && n.ResourceType == ResourceType.Food
                    && Vector3.Distance(n.transform.position, farm.transform.position) <= EventRules.FireRadius))
                    if (burned.Add(node)) node.TryConsumeStock(node.AmountRemaining * .5f * ScienceEffects.WildfireDamageMultiplier);
                if (ScienceEffects.FirebreaksBlockSpread) continue;
                foreach (var next in farms.Where(n => !burned.Contains(n) && Vector3.Distance(n.transform.position, farm.transform.position) <= EventRules.FireRadius))
                {
                    var from = farm.transform.position + Vector3.up * .25f; var to = next.transform.position + Vector3.up * .25f;
                    if (!Physics.RaycastAll(from, to - from, Vector3.Distance(from, to)).Any(h => h.collider.GetComponentInParent<SoilWall>() != null)) queue.Enqueue(next);
                }
            }
        }
        private static bool TryEdge(out Vector3 point)
        {
            var bounds = HomeMapBuilder.CurrentWorldBounds; var queen = FindFirstObjectByType<QueenChamber>(); point = bounds.center;
            if (queen == null) return false;
            // 가장자리 후보만 사용한다. 본거지 근처로 순간 이동시키지 않는다.
            for (int i = 0; i < 64; i++)
            {
                float t = Random.Range(-.9f, .9f); int side = i % 4;
                var p = bounds.center + new Vector3(side < 2 ? (side == 0 ? -.9f : .9f) * bounds.extents.x : t * bounds.extents.x, queen.Position.y,
                    side >= 2 ? (side == 2 ? -.9f : .9f) * bounds.extents.z : t * bounds.extents.z);
                if (!NavMesh.SamplePosition(p, out var hit, 10, NavMesh.AllAreas)) continue;
                var path = new NavMeshPath();
                if (!NavMesh.CalculatePath(hit.position, queen.Position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete) continue;
                point = hit.position; return true;
            }
            return false;
        }
        public State Capture()
        {
            var copy = JsonUtility.FromJson<State>(JsonUtility.ToJson(state));
            copy.actors = FindObjectsByType<EventActor>(FindObjectsSortMode.None).Where(a => a.Alive).Select(a => a.Capture()).ToList(); return copy;
        }
        public void Restore(State value)
        {
            foreach (var a in FindObjectsByType<EventActor>(FindObjectsSortMode.None)) { a.gameObject.SetActive(false); Destroy(a.gameObject); }
            state = JsonUtility.FromJson<State>(JsonUtility.ToJson(value));
            foreach (var a in state.actors) EventActor.Spawn(a);
            if (state.flood > 0) CacheFloodNodes();
        }
        public static bool Validate(State s)
        {
            bool N(float v, float max) => !float.IsNaN(v) && !float.IsInfinity(v) && v >= 0 && v <= max;
            return s != null && N(s.checkRemaining, EventRules.CheckSeconds) && s.checkRemaining > 0 && N(s.sinceLast, EventRules.Cooldown)
                && N(s.cold, EventRules.ColdSeconds) && N(s.flood, EventRules.FloodSeconds) && N(s.drought, EventRules.DroughtSeconds)
                && s.cooldowns != null && s.cooldowns.Length == 11 && s.cooldowns.All(v => N(v, EventRules.Cooldown))
                && s.actors != null && s.actors.Count <= 6 && s.actors.All(a => a != null && Enum.IsDefined(typeof(EventActorKind), a.kind)
                    && a.position != null && a.position.IsFinite() && Mathf.Abs(a.position.x) < 100000 && Mathf.Abs(a.position.y) < 100000 && Mathf.Abs(a.position.z) < 100000
                    && N(a.attackCooldown, EventRules.WaspInterval) && N(a.remaining, a.kind == EventActorKind.Wanderer ? EventRules.WandererSeconds : EventRules.DriftSeconds)
                    && N(a.amount, a.kind == EventActorKind.Wasp ? EventRules.WaspHealth : a.kind == EventActorKind.Food ? EventRules.DriftFood : EventRules.DriftSoil))
                && s.actors.Count(a => a.kind == EventActorKind.Wasp) <= EventRules.Wasps
                && s.actors.Where(a => a.kind != EventActorKind.Wasp).Select(a => a.kind).Distinct().Count() == s.actors.Count(a => a.kind != EventActorKind.Wasp);
        }
    }
}
