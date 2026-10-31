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
            public List<ArtworkRetry> artworkRetries = new List<ArtworkRetry>();
            public List<Civilization> civilizations = new List<Civilization>();
            public List<Civilization> markets = new List<Civilization>();
            public List<string> owners = new List<string>();
            public float elapsed, nextMonth = DiplomacyRules.Month, caravanUntil;
            public bool airshipStarted;
            public int blueprintSite = -1;
            public int extraSites;
            public List<RebelMember> rebelMembers = new List<RebelMember>();
            public List<Negotiation> talks = new List<Negotiation>();
            public List<Homecoming> homecomings = new List<Homecoming>();
            public bool homecomingEntrySet; // 구저장 false: 기존 본거지 경계 진입
            public Vector3 homecomingEntry;
            public List<Seizure> seizures = new List<Seizure>();
            public List<string> reputation = new List<string>();
            public int trust, threat, nextTalk;
            public List<TravellingRaid> travellingRaids = new List<TravellingRaid>();
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
        private void Update() { if (!Save.SaveSystem.Busy) Tick(Time.deltaTime); if (Time.frameCount % 15 == 0) RefreshPins(); }
        // 보류 협상은 상대 세력별로 묶어 오른쪽 알림에 고정한다(요약·남은 기한, 누르면 재개).
        private void RefreshPins()
        {
            var live = new HashSet<string>();
            foreach (var group in Data.talks.GroupBy(n => n.civ))
            {
                var c = CivOf(group.First()); if (c == null) continue;
                var key = "talks:" + c.id; live.Add(key);
                var parts = group.Select(n => { var left = Remaining(n); return KindName(n.kind) + (n.invalid ? "(무효)" : "") + (n.give.Empty ? "" : " " + Summary(n.give)) + (left >= 0 ? $" · 남은 {(int)left / 60}:{(int)left % 60:00}" : ""); });
                var first = group.First();
                ToastManager.SetPinned(key, c.name + " — " + string.Join(" / ", parts), () => AntColony.UI.GameMenuController.Instance?.OpenTalk(first), "협상 열기");
            }
            foreach (var key in ToastManager.PinnedKeys.Where(k => k.StartsWith("talks:") && !live.Contains(k)).ToArray()) ToastManager.SetPinned(key, null);
        }
        public void Contact(ExpeditionSite site)
        {
            if (!Available) return;
            var c = Faction(site);
            if (c == null || c.contacted) return;
            MarkContact(c);
        }
        public bool DeclareWar(Civilization c, bool surprise = false)
        {
            if (!Available || c == null || !Data.civilizations.Contains(c) || c.extinct || c.war || !c.contacted && !surprise
                || c.HasTreaty(TreatyKind.NonAggression, Data.elapsed)) return false;
            var broken = c.HasTreaty(TreatyKind.Trade, Data.elapsed) || c.HasTreaty(TreatyKind.Alliance, Data.elapsed);
            MarkContact(c);
            c.war = true; c.warStarted = Data.elapsed; c.nextRaid = Data.elapsed + DiplomacyRules.Season;
            c.playerScore = c.enemyScore = 0; Array.Clear(c.treaties, 0, c.treaties.Length); c.offerExpires = 0;
            c.ChangeAffinity(-10, "전쟁");
            if (surprise) foreach (var other in Data.civilizations.Where(x => !x.extinct)) other.ChangeAffinity(DiplomacyRules.SurprisePenalty, "기습 선전포고");
            if (surprise) { Data.trust = Mathf.Clamp(Data.trust - 20, -100, 100); Reputation("기습 선전포고 (" + c.name + ")"); }
            else if (broken) TrustEvent(-10, "협정 파기", null);
            Data.talks.RemoveAll(n => n.civ == c.id && n.deadline <= 0);
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
                || compensation < 0 && c.resources[0] < -compensation) return false;
            if (compensation > 0) { resources.TrySpend(compensation, 0, reason: ResourceReason.Trade); c.resources[0] += compensation; }
            if (compensation < 0) { c.resources[0] += compensation; StoreResource(AntColony.Data.ResourceType.Food, -compensation, ResourceReason.Trade); }
            c.war = false; c.nextOffer = Data.elapsed + DiplomacyRules.Month * 3;
            foreach (var raider in AntColony.Core.SceneObjects.All<WildMonster>())
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
            if (c == null) return false;
            var offer = Data.talks.Find(n => n.civ == c.id && n.kind == TalkKind.TreatyOffer);
            if (offer == null || Respond(offer, true) != "수락") return false;
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
            var killer = AntColony.Core.SceneObjects.All<WildMonster>()
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
            foreach (var c in Data.civilizations.Where(c => c.contacted && !c.extinct && (c.war || c.affinity <= -40 || c.agenda == LeaderAgenda.Scientist)))
            {
                if (c.agenda == LeaderAgenda.Conqueror && Data.elapsed < c.contactedAt + DiplomacyRules.Season) continue;
                if (!c.war && !DeclareWar(c)) continue;
                if (c.agenda == LeaderAgenda.Conqueror) continue;
                Raid(c, true);
            }
        }
        public void Tick(float seconds)
        {
            if (!(seconds > 0) || float.IsInfinity(seconds)) return;
            Data.elapsed += seconds;
            TickHomecomings();
            if (!Available) return;
            foreach (var c in Data.civilizations.Where(c => !c.extinct))
                if (AIEraProgress.Tick(c, seconds, world.Sites.Where(s => Faction(s) == c && !s.Cleared).Sum(s => s.Colony?.RemainingBuildings ?? 0)))
                {
                    CampaignHistory.Record("세력 시대", c.name, CampaignResearch.EraNames[c.era - 1]);
                    foreach (var site in world.Sites.Where(s => Faction(s) == c)) AIEraMilitary.UpgradeGuards(site, c);
                }
            TickTalks();
            TickRaidTravel();
            TickConquerors();
            while (Data.nextMonth <= Data.elapsed)
            {
                Data.nextMonth += DiplomacyRules.Month;
                foreach (var c in Data.civilizations.Where(c => !c.extinct).ToArray()) { Monthly(c); MonthlyTalks(c); }
                Data.threat -= Math.Sign(Data.threat) * Math.Min(2, Math.Abs(Data.threat));
            }
        }
        private void Monthly(Civilization c)
        {
            if (!c.contacted) return;
            if (c.contacted && !c.war)
            {
                if (PeriodicTradeEnabled(c, Data.elapsed))
                {
                    var r = ResourceManager.Instance; var amount = DiplomacyRules.MonthlyTradeAmount;
                    if (r != null && c.resources[1] >= amount && r.GetCapacity(AntColony.Data.ResourceType.Soil) - r.GetAmount(AntColony.Data.ResourceType.Soil) >= amount
                        && c.resources[0] <= int.MaxValue - amount && r.TrySpend(amount, 0, reason: ResourceReason.Trade))
                    { r.Add(AntColony.Data.ResourceType.Soil, amount, ResourceReason.Trade); c.resources[0] += amount; c.resources[1] -= amount; c.ChangeAffinity(1, "월별 교역"); }
                }
                if (Data.elapsed >= c.nextOffer)
                {
                    c.nextOffer = Data.elapsed + 3 * DiplomacyRules.Month;
                    if (UnityEngine.Random.value < (c.agenda == LeaderAgenda.Trader ? DiplomacyRules.TraderOfferChance : DiplomacyRules.OfferChance))
                        OfferTreaty(c, (TreatyKind)UnityEngine.Random.Range(0, 3));
                }
                var chance = c.affinity <= -40 ? DiplomacyRules.HostileWarChance : 0;
                if (c.agenda == LeaderAgenda.Conqueror) chance = 0; // 정복자는 접촉 후 계절별 실제 공격대 전력으로 판단한다.
                if (UnityEngine.Random.value < chance) DeclareWar(c);
            }
            if (c.agenda != LeaderAgenda.Conqueror && c.war && Data.elapsed >= c.nextRaid)
            { if (Raid(c, c.rebel && Data.elapsed - c.founded < DiplomacyRules.Year) || RocketCampaign.SuppressNewCrises) c.nextRaid = Data.elapsed + DiplomacyRules.Season; }
            if (Data.airshipStarted && c.agenda != LeaderAgenda.Conqueror && c.war && !AntColony.Core.SceneObjects.All<AirshipYard>().Any(y => y.Ready)
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
            if (!c.contacted || !c.war || c.agenda == LeaderAgenda.Conqueror && Data.elapsed < c.contactedAt + DiplomacyRules.Season) return false;
            if (RocketCampaign.SuppressNewCrises) return false; // G39: 신규 출발만 막는다. 이미 이동 중인 원정대는 도착한다.
            var source = RaidSource(c);
            var food = source?.Colony?.GetStock(AntColony.Data.ResourceType.Food) ?? c.resources[0];
            var count = Mathf.Clamp((int)food / 20, 1, 20);
            if (food < count * 10) return false;
            if (c.agenda == LeaderAgenda.Conqueror && !IsWeakAgainst(count * 20, AvailableHomePower)) return false;
            var destination = home ? null : world.Sites.FirstOrDefault(s => s.Disposition == ConquestDisposition.Annexed && s.Defense != null && !s.Defense.UnderAttack);
            var spawned = destination != null ? destination.Defense.TryStartRaid(count, c.id) : DispatchRaid(source, c, count);
            if (!spawned) return false;
            if (source?.Colony != null) source.Colony.TrySpendResources(count * 10, 0); else c.resources[0] -= count * 10;
            CampaignHistory.Record("침공", c.name, destination?.Title ?? "본거지", true); return true;
        }
        public float ScheduledRaidSeconds(ExpeditionSite site)
        {
            if (world.Sites.FirstOrDefault(s => s.Disposition == ConquestDisposition.Annexed && s.Defense != null && !s.Defense.UnderAttack) != site) return 3 * DiplomacyRules.Month;
            return Data.civilizations.Where(c => c.war && !c.extinct && !(c.rebel && Data.elapsed - c.founded < DiplomacyRules.Year))
                .Select(c => Mathf.Max(0, c.nextRaid - Data.elapsed)).DefaultIfEmpty(3 * DiplomacyRules.Month).Min();
        }
        public State Capture() { CaptureRebels(); return JsonUtility.FromJson<State>(JsonUtility.ToJson(Data)); }
        public void Restore(State state)
        {
            if (state == null) return;
            Data = JsonUtility.FromJson<State>(JsonUtility.ToJson(state));
            Data.travellingRaids ??= new List<TravellingRaid>();
            foreach (var c in Data.civilizations.Where(c => c.contacted && c.nextConquestCheck == 0))
            { c.contactedAt = Data.elapsed; c.nextConquestCheck = Data.elapsed + DiplomacyRules.Season; }
            foreach (var n in Data.talks.Where(n => n.deadline <= 0 && n.expires <= 0)) n.expires = Data.elapsed + DiplomacyRules.OfferSeconds;
            // 옛 즉시 수락 버튼도 담당 지정이 있는 협상 경로로 이관한다.
            foreach (var c in Data.civilizations.Where(c => c.offerExpires > Data.elapsed))
            {
                var n = StartTalk(c, TalkKind.TreatyOffer); n.expires = c.offerExpires;
                if (n.take.treaties.Count == 0) n.take.treaties.Add(c.offeredTreaty);
                c.offerExpires = 0;
            }
        }
        public static bool Validate(State s, int sites)
        {
            if (s != null && !ValidArtwork(s)) return false;
            bool Finite(float f) => f >= 0 && !float.IsNaN(f) && !float.IsInfinity(f);
            if (s == null || !ValidHomecomingEntry(s) || s.civilizations == null || s.civilizations.Count < 4 || s.civilizations.Count > 100
                || s.owners == null || s.owners.Count != sites || !Finite(s.elapsed) || !Finite(s.nextMonth) || !Finite(s.caravanUntil)
                || s.blueprintSite < -1 || s.blueprintSite >= sites || s.extraSites < 0 || s.extraSites > 100
                || s.markets == null || s.markets.Count > 4 || s.rebelMembers == null || s.rebelMembers.Count > 1000) return false;
            var ids = new HashSet<string>();
            foreach (var c in s.civilizations.Concat(s.markets))
                if (c == null || !AIEraProgress.Valid(c) || string.IsNullOrEmpty(c.id) || !ids.Add(c.id) || !Enum.IsDefined(typeof(LeaderAgenda), c.agenda)
                    || !Enum.IsDefined(typeof(LeaderAgenda), c.hiddenAgenda) || !s.markets.Contains(c) && c.agenda == c.hiddenAgenda || c.affinity < -100 || c.affinity > 100
                    || c.treaties == null || c.treaties.Length != 3 || c.treaties.Any(t => !Finite(t)) || !Finite(c.warStarted) || !Finite(c.founded)
                    || !Finite(c.nextRaid) || !Finite(c.nextOffer) || !Finite(c.offerExpires) || !Finite(c.contactedAt) || !Finite(c.nextConquestCheck) || c.resources == null || c.resources.Length != 3
                    || c.resources.Any(r => r < 0) || c.reasons == null || c.equipment == null || c.equipment.Any(e => e == null || !e.IsValid)
                    || c.prisoners == null || c.rebels == null || c.playerScore < 0 || c.enemyScore < 0
                    || !Enum.IsDefined(typeof(TreatyKind), c.offeredTreaty)) return false;
            if (s.markets.Any(m => !m.id.StartsWith("market:") || !int.TryParse(m.id.Substring(7), out var i) || i < -1 || i >= sites)) return false;
            if (!ValidRaidTravel(s, sites)) return false;
            var members = new HashSet<string>();
            foreach (var m in s.rebelMembers)
                if (m?.commander?.PersonalState == null || !members.Add(m.commander.PersonalState.id) || m.site < 0 || m.site >= sites
                    || m.troops < 0 || m.troops > 100000 || !Finite(m.health) || float.IsNaN(m.position.sqrMagnitude) || float.IsInfinity(m.position.sqrMagnitude)
                    || !s.civilizations.Any(c => c.id == m.faction && c.rebel && !c.extinct && c.rebels.Any(p => p?.PersonalState?.id == m.commander.PersonalState.id))) return false;
            // 2026-10-09 협상·포로 귀환·압수 장비·평판(이전 저장은 빈 목록).
            s.talks ??= new List<Negotiation>(); s.homecomings ??= new List<Homecoming>(); s.seizures ??= new List<Seizure>(); s.reputation ??= new List<string>();
            if (s.talks.Count > 100 || s.homecomings.Count > 1000 || s.seizures.Count > 5000 || s.trust < -100 || s.trust > 100 || s.threat < -100 || s.threat > 100) return false;
            foreach (var n in s.talks)
                if (n == null || !ids.Contains(n.civ) || !Enum.IsDefined(typeof(TalkKind), n.kind) || n.give == null || n.take == null
                    || n.give.resources?.Length != 3 || n.take.resources?.Length != 3 || n.give.resources.Concat(n.take.resources).Any(r => r < 0)
                    || n.patience < -100 || n.patience > 100 || !Finite(n.expires) || !Finite(n.deadline) || n.log == null) return false;
            foreach (var h in s.homecomings)
                if (h?.commander?.PersonalState == null || h.commander.Talents == null || h.gear == null || h.gear.Any(e => e == null || !e.IsValid) || !Finite(h.arrive)) return false;
            if (s.civilizations.Any(c => c.threatCred < -100 || c.threatCred > 100 || !Finite(c.talksCooldown)) || s.seizures.Any(x => x == null || x.owner == null)) return false;
            return s.owners.All(o => o == "" || ids.Contains(o));
        }
    }
}
