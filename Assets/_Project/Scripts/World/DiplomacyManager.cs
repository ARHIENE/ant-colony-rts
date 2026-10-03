using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.UI;
using UnityEngine;

namespace AntColony.World
{
    public sealed partial class DiplomacyManager : MonoBehaviour
    {
        [Serializable] public sealed class State
        {
            public List<Civilization> civilizations = new List<Civilization>();
            public List<Civilization> markets = new List<Civilization>();
            public List<string> owners = new List<string>();
            public float elapsed, nextMonth = DiplomacyRules.Month, caravanUntil;
            public bool airshipStarted;
            public int blueprintSite = -1;
            public int extraSites;
            public List<RebelMember> rebelMembers = new List<RebelMember>();
        }
        public static DiplomacyManager Instance { get; private set; }
        public State Data { get; private set; } = new State();
        private WorldMapManager world;
        public bool Available => world != null && world.Unlocked;
        public Civilization Faction(ExpeditionSite site)
        {
            if (site == null || world == null) return null;
            var index = world.Sites.ToList().IndexOf(site);
            return index < 0 || index >= Data.owners.Count ? null : Data.civilizations.Find(c => c.id == Data.owners[index] && !c.extinct);
        }
        public void Initialize(WorldMapManager map, int seed)
        {
            Instance = this; world = map;
            Data = InitialState(seed, map.LegacyLayout);
        }
        public static State InitialState(int seed, bool legacy)
        {
            var state = new State();
            var random = new System.Random(seed);
            var agendas = Enumerable.Range(0, 6).OrderBy(_ => random.Next()).ToArray();
            foreach (var name in new[] { "Amber", "Azure", "Jade", "Crimson" })
            {
                var index = state.civilizations.Count;
                var c = new Civilization { id = name, name = name, leader = name + " 여왕", agenda = (LeaderAgenda)agendas[index],
                    hiddenAgenda = (LeaderAgenda)((agendas[index] + 1 + random.Next(5)) % 6), color = Color.HSVToRGB(index * .23f + .08f, .7f, .9f), nextOffer = DiplomacyRules.Month * 3 };
                c.equipment.Add(Units.EquipmentRecipes.Create((Units.EquipmentRecipe)index, 1));
                state.civilizations.Add(c);
            }
            for (var i = 0; i < (legacy ? 30 : 33); i++)
            {
                var civ = legacy ? (i % 5 < 2 ? i % 5 : -1) : i >= 9 && i < 15 ? (i - 9) / 3 : i >= 23 && i < 29 ? 2 + (i - 23) / 3 : -1;
                state.owners.Add(civ < 0 ? "" : state.civilizations[civ].id);
            }
            if (!legacy) state.blueprintSite = new[] { 4, 18, 29 }[random.Next(3)];
            return state;
        }
        private void OnDestroy() { if (Instance == this) Instance = null; }
        private void Update() { if (!Save.SaveSystem.Busy) Tick(Time.deltaTime); }
        public void Contact(ExpeditionSite site)
        {
            if (!Available) return;
            var c = Faction(site);
            if (c == null || c.contacted) return;
            c.contacted = true;
            CampaignHistory.Record("접촉", c.name, c.leader, true);
        }
        public bool DeclareWar(Civilization c, bool surprise = false)
        {
            if (!Available || c == null || !Data.civilizations.Contains(c) || c.extinct || c.war
                || c.HasTreaty(TreatyKind.NonAggression, Data.elapsed)) return false;
            c.contacted = c.war = true; c.warStarted = Data.elapsed; c.nextRaid = Data.elapsed + 3 * DiplomacyRules.Month;
            c.playerScore = c.enemyScore = 0; Array.Clear(c.treaties, 0, c.treaties.Length); c.offerExpires = 0;
            c.ChangeAffinity(-10, "전쟁");
            if (surprise) foreach (var other in Data.civilizations.Where(x => !x.extinct)) other.ChangeAffinity(DiplomacyRules.SurprisePenalty, "기습 선전포고");
            CampaignHistory.Record("선전포고", c.name, surprise ? "기습 선전포고" : "전쟁 시작", true);
            return true;
        }
        public bool CanNegotiatePeace(Civilization c) => Available && c != null && c.war && Data.elapsed - c.warStarted >= DiplomacyRules.PeaceDelay;
        public int Reparations(Civilization c) => c.enemyScore - c.playerScore;
        public bool MakePeace(Civilization c)
        {
            if (!CanNegotiatePeace(c)) return false;
            var compensation = Reparations(c); var resources = ResourceManager.Instance;
            if (resources == null || compensation > 0 && (!resources.CanAfford(compensation, 0) || c.resources[0] > int.MaxValue - compensation)
                || compensation < 0 && (c.resources[0] < -compensation || resources.GetCapacity(ResourceType.Food) - resources.GetAmount(ResourceType.Food) < -compensation)) return false;
            if (compensation > 0) { resources.TrySpend(compensation, 0, reason: ResourceReason.Trade); c.resources[0] += compensation; }
            if (compensation < 0) { c.resources[0] += compensation; resources.Add(ResourceType.Food, -compensation, ResourceReason.Trade); }
            c.war = false; c.nextOffer = Data.elapsed + DiplomacyRules.Month * 3;
            foreach (var raider in FindObjectsByType<WildMonster>(FindObjectsSortMode.None))
                if (raider.DiplomaticFactionId == c.id && string.IsNullOrEmpty(raider.RebelId)) Destroy(raider.gameObject);
            CampaignHistory.Record("평화", c.name, "배상 Food " + compensation, true);
            return true;
        }
        public bool SignTreaty(Civilization c, TreatyKind kind)
        {
            if (!Available || c == null || !Data.civilizations.Contains(c) || c.extinct || !c.contacted || c.war || !Enum.IsDefined(typeof(TreatyKind), kind)) return false;
            c.treaties[(int)kind] = Data.elapsed + DiplomacyRules.Year;
            if (kind == TreatyKind.Trade) c.hiddenRevealed = true;
            c.ChangeAffinity(10, "협정 " + kind);
            CampaignHistory.Record("협정", c.name, kind.ToString(), true); return true;
        }
        public bool AcceptOffer(Civilization c)
        {
            if (c == null || c.offerExpires <= Data.elapsed || !SignTreaty(c, c.offeredTreaty)) return false;
            c.offerExpires = 0; return true;
        }
        public static bool Hostile(Component target)
        {
            if (target is WildMonster monster && !string.IsNullOrEmpty(monster.DiplomaticFactionId))
                return Instance?.Data.civilizations.Find(c => c.id == monster.DiplomaticFactionId)?.war == true;
            var site = target != null ? target.GetComponentInParent<ExpeditionSite>() : null;
            var c = Instance?.Faction(site);
            return c == null || c.war;
        }
        public static bool TryAttack(Component target)
        {
            if (target is WildMonster ally && ally.Allied) return false;
            if (target is WildMonster monster && !string.IsNullOrEmpty(monster.DiplomaticFactionId))
            {
                var source = Instance?.Data.civilizations.Find(c => c.id == monster.DiplomaticFactionId);
                return source == null || source.war || Instance.DeclareWar(source, true);
            }
            var c = Instance?.Faction(target != null ? target.GetComponentInParent<ExpeditionSite>() : null);
            return c == null || c.war || Instance.DeclareWar(c, true);
        }
        // ponytail: 피해 출처를 추적하지 않으므로 쓰러진 위치 15m 안의 교전 중 세력에 격파 점수를 준다.
        public void CommanderDowned(Vector3 position)
        {
            var killer = FindObjectsByType<WildMonster>(FindObjectsSortMode.None)
                .Where(m => !m.IsDead && !m.Allied && (m.Position - position).sqrMagnitude < 225)
                .Select(m => string.IsNullOrEmpty(m.DiplomaticFactionId) ? Faction(m.GetComponentInParent<ExpeditionSite>())
                    : Data.civilizations.Find(c => c.id == m.DiplomaticFactionId))
                .FirstOrDefault(c => c != null && c.war);
            if (killer != null) killer.enemyScore += 10;
        }
        public void AirshipConstructionStarted()
        {
            if (Data.airshipStarted) return;
            Data.airshipStarted = true;
            foreach (var c in Data.civilizations.Where(c => !c.extinct && (c.war || c.affinity <= -40 || c.agenda == LeaderAgenda.Scientist)))
            {
                if (!c.war && !DeclareWar(c)) continue;
                Raid(c, true);
            }
        }
        public void Tick(float seconds)
        {
            if (!(seconds > 0) || float.IsInfinity(seconds)) return;
            Data.elapsed += seconds;
            if (!Available) return;
            while (Data.nextMonth <= Data.elapsed)
            {
                Data.nextMonth += DiplomacyRules.Month;
                foreach (var c in Data.civilizations.Where(c => !c.extinct).ToArray()) Monthly(c);
            }
        }
        private void Monthly(Civilization c)
        {
            if (c.contacted && !c.war)
            {
                if (c.HasTreaty(TreatyKind.Trade, Data.elapsed))
                {
                    var r = ResourceManager.Instance; var amount = DiplomacyRules.MonthlyTradeAmount;
                    if (r != null && c.resources[1] >= amount && r.GetCapacity(ResourceType.Soil) - r.GetAmount(ResourceType.Soil) >= amount
                        && c.resources[0] <= int.MaxValue - amount && r.TrySpend(amount, 0, reason: ResourceReason.Trade))
                    { r.Add(ResourceType.Soil, amount, ResourceReason.Trade); c.resources[0] += amount; c.resources[1] -= amount; c.ChangeAffinity(1, "월별 교역"); }
                }
                if (Data.elapsed >= c.nextOffer)
                {
                    c.nextOffer = Data.elapsed + 3 * DiplomacyRules.Month;
                    if (UnityEngine.Random.value < (c.agenda == LeaderAgenda.Trader ? DiplomacyRules.TraderOfferChance : DiplomacyRules.OfferChance))
                    { c.offeredTreaty = (TreatyKind)UnityEngine.Random.Range(0, 3); c.offerExpires = Data.elapsed + DiplomacyRules.Month; ToastManager.Show(c.name + " 협정 제안 — J 외교에서 확인 (1개월)"); }
                }
                var chance = c.affinity <= -40 ? DiplomacyRules.HostileWarChance : 0;
                if (c.agenda == LeaderAgenda.Conqueror && world.Sites.Count(s => s.Disposition == ConquestDisposition.Annexed) < world.Sites.Count(s => Faction(s) == c && s.Disposition != ConquestDisposition.Annexed)) chance = Mathf.Max(chance, DiplomacyRules.ConquerorWarChance);
                if (UnityEngine.Random.value < chance) DeclareWar(c);
            }
            if (c.war && Data.elapsed >= c.nextRaid)
            { if (Raid(c, c.rebel && Data.elapsed - c.founded < DiplomacyRules.Year)) c.nextRaid = Data.elapsed + DiplomacyRules.Month * 3; }
            if (Data.airshipStarted && c.war && !FindObjectsByType<AirshipYard>(FindObjectsSortMode.None).Any(y => y.Ready)
                && UnityEngine.Random.value < DiplomacyRules.AirshipRaidChance) Raid(c, true);
            if (c.HasTreaty(TreatyKind.Alliance, Data.elapsed))
            {
                var target = world.Sites.FirstOrDefault(s => Faction(s)?.war == true && s.Colony != null && !s.Colony.IsDefeated);
                var building = target?.Colony.Buildings.FirstOrDefault(b => b != null && !b.IsDead);
                if (building != null) { building.TakeDamage(float.MaxValue); CampaignHistory.Record("동맹 지원", c.name, target.Title + " 건물 1동 파괴", true); }
            }
        }
        private bool Raid(Civilization c, bool home)
        {
            if (!c.war) return false;
            var source = world.Sites.FirstOrDefault(s => Faction(s) == c && s.Disposition != ConquestDisposition.Annexed && s.Colony != null && !s.Colony.IsDefeated);
            var food = source?.Colony.GetStock(ResourceType.Food) ?? c.resources[0];
            var count = Mathf.Clamp((int)food / 20, 1, 20);
            if (food < count * 10) return false;
            var destination = home ? null : world.Sites.FirstOrDefault(s => s.Disposition == ConquestDisposition.Annexed && s.Defense != null && !s.Defense.UnderAttack);
            var spawned = destination != null ? destination.Defense.TryStartRaid(count, c.id) : FindFirstObjectByType<LocalIncursions>()?.TrySpawnFaction(c, count) == true;
            if (!spawned) return false;
            if (source != null) source.Colony.TrySpendResources(count * 10, 0); else c.resources[0] -= count * 10;
            CampaignHistory.Record("침공", c.name, destination?.Title ?? "본거지", true); return true;
        }
        public float ScheduledRaidSeconds(ExpeditionSite site)
        {
            if (world.Sites.FirstOrDefault(s => s.Disposition == ConquestDisposition.Annexed && s.Defense != null && !s.Defense.UnderAttack) != site) return 3 * DiplomacyRules.Month;
            return Data.civilizations.Where(c => c.war && !c.extinct && !(c.rebel && Data.elapsed - c.founded < DiplomacyRules.Year))
                .Select(c => Mathf.Max(0, c.nextRaid - Data.elapsed)).DefaultIfEmpty(3 * DiplomacyRules.Month).Min();
        }
        public State Capture() { CaptureRebels(); return JsonUtility.FromJson<State>(JsonUtility.ToJson(Data)); }
        public void Restore(State state) { if (state != null) Data = JsonUtility.FromJson<State>(JsonUtility.ToJson(state)); }
        public static bool Validate(State s, int sites)
        {
            bool Finite(float f) => f >= 0 && !float.IsNaN(f) && !float.IsInfinity(f);
            if (s == null || s.civilizations == null || s.civilizations.Count < 4 || s.civilizations.Count > 100
                || s.owners == null || s.owners.Count != sites || !Finite(s.elapsed) || !Finite(s.nextMonth) || !Finite(s.caravanUntil)
                || s.blueprintSite < -1 || s.blueprintSite >= sites || s.extraSites < 0 || s.extraSites > 100
                || s.markets == null || s.markets.Count > 4 || s.rebelMembers == null || s.rebelMembers.Count > 1000) return false;
            var ids = new HashSet<string>();
            foreach (var c in s.civilizations.Concat(s.markets))
                if (c == null || string.IsNullOrEmpty(c.id) || !ids.Add(c.id) || !Enum.IsDefined(typeof(LeaderAgenda), c.agenda)
                    || !Enum.IsDefined(typeof(LeaderAgenda), c.hiddenAgenda) || !s.markets.Contains(c) && c.agenda == c.hiddenAgenda || c.affinity < -100 || c.affinity > 100
                    || c.treaties == null || c.treaties.Length != 3 || c.treaties.Any(t => !Finite(t)) || !Finite(c.warStarted) || !Finite(c.founded)
                    || !Finite(c.nextRaid) || !Finite(c.nextOffer) || !Finite(c.offerExpires) || c.resources == null || c.resources.Length != 3
                    || c.resources.Any(r => r < 0) || c.reasons == null || c.equipment == null || c.equipment.Any(e => e == null || !e.IsValid)
                    || c.prisoners == null || c.rebels == null || c.playerScore < 0 || c.enemyScore < 0
                    || !Enum.IsDefined(typeof(TreatyKind), c.offeredTreaty)) return false;
            if (s.markets.Any(m => !m.id.StartsWith("market:") || !int.TryParse(m.id.Substring(7), out var i) || i < -1 || i >= sites)) return false;
            var members = new HashSet<string>();
            foreach (var m in s.rebelMembers)
                if (m?.commander?.PersonalState == null || !members.Add(m.commander.PersonalState.id) || m.site < 0 || m.site >= sites
                    || m.troops < 0 || m.troops > 100000 || !Finite(m.health) || float.IsNaN(m.position.sqrMagnitude) || float.IsInfinity(m.position.sqrMagnitude)
                    || !s.civilizations.Any(c => c.id == m.faction && c.rebel && !c.extinct && c.rebels.Any(p => p?.PersonalState?.id == m.commander.PersonalState.id))) return false;
            return s.owners.All(o => o == "" || ids.Contains(o));
        }
    }
}
