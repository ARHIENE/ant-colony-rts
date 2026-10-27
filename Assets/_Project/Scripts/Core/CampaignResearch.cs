using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Buildings;
using AntColony.Units;
using AntColony.World;
using UnityEngine;

namespace AntColony.Core
{
    public enum ScienceTechnology
    {
        FungalFarming, HoneydewRanch, Resin, Traps, Herbs, Sanitation,
        Fermentation, AdvancedCrops, AcidRefining, Watchtowers, Infirmary, Recreation,
        Blades, ArmorPlates, Vehicle, Drainage, Firebreaks,
        EfficientTransport, Mines, Regeneration, Trinkets, AdvancedWeapons, Gliding,
        Aircraft, HeavyTransport, Insulation, MigrationTheory, Hull, Cocoons, Engine, Fishing,
        ConscriptionLaw, ReserveForces, TotalMobilization, // Phase 4 병역 제도
        Pharmacy, PrecisionPharmacy, // 2026-10-25 D01 장수용 약품
        FormationLine, FormationColumn, FormationWedge, FormationDispersed, FormationCover, FormationFocus,
        UndergroundSurvey
    }

    public sealed class ScienceDefinition
    {
        public readonly ScienceTechnology Technology;
        public readonly string Name;
        public readonly int Tier;
        public readonly float Work;
        public readonly int Food, Soil, Special;
        public readonly ScienceTechnology[] Prerequisites;
        public ScienceDefinition(ScienceTechnology technology, string name, int tier, params ScienceTechnology[] prerequisites)
        {
            Technology = technology; Name = name; Tier = tier; Prerequisites = prerequisites;
            Work = GameBalance.ScienceWork[tier - 1]; Food = GameBalance.ScienceFood[tier - 1];
            Soil = GameBalance.ScienceSoil[tier - 1]; Special = GameBalance.ScienceSpecial[tier - 1];
        }
    }

    public sealed partial class CampaignResearch : MonoBehaviour
    {
        // 연구 주제별 보조 능력(2026-10-07 기획 표: 생물·의료 → 의료, 농업 → 농사, 기계 → 제작, 군사 전술 → 지휘, 무기 → 근접/원거리).
        // 항목별 분류는 잠정(Claude 초안). 표에 맞는 분야가 없는 항목(저장·치수·방화·보온·휴게·낚시)은 연구만 성장한다.
        public static CommanderActivity? Topic(ScienceTechnology t) => t switch
        {
            ScienceTechnology.FungalFarming or ScienceTechnology.HoneydewRanch or ScienceTechnology.AdvancedCrops => CommanderActivity.Farming,
            ScienceTechnology.Herbs or ScienceTechnology.Sanitation or ScienceTechnology.Infirmary or ScienceTechnology.Regeneration
                or ScienceTechnology.Pharmacy or ScienceTechnology.PrecisionPharmacy => CommanderActivity.Medicine,
            ScienceTechnology.Blades or ScienceTechnology.AdvancedWeapons => CommanderActivity.Melee,
            ScienceTechnology.AcidRefining => CommanderActivity.Ranged,
            ScienceTechnology.ArmorPlates or ScienceTechnology.Trinkets or ScienceTechnology.Gliding or ScienceTechnology.Traps or ScienceTechnology.Mines
                or ScienceTechnology.Vehicle or ScienceTechnology.Aircraft or ScienceTechnology.HeavyTransport or ScienceTechnology.EfficientTransport
                or ScienceTechnology.MigrationTheory or ScienceTechnology.Hull or ScienceTechnology.Cocoons or ScienceTechnology.Engine or ScienceTechnology.UndergroundSurvey => CommanderActivity.Crafting,
            ScienceTechnology.FormationLine or ScienceTechnology.FormationColumn or ScienceTechnology.FormationWedge
                or ScienceTechnology.FormationDispersed or ScienceTechnology.FormationCover or ScienceTechnology.FormationFocus
                or ScienceTechnology.Watchtowers or ScienceTechnology.ConscriptionLaw or ScienceTechnology.ReserveForces or ScienceTechnology.TotalMobilization => CommanderActivity.Command,
            _ => null
        };
        public static CampaignResearch Instance { get; private set; }
        // 6시대(2026-10-05). 기존 4시대 매핑: 소굴=소굴, 공학=증기, 항공=전기, 대이주=미래.
        // TODO 석유(3)·원자(5) 시대 연구 항목은 아직 없음(빈 시대).
        public static readonly string[] EraNames = { "정착", "증기", "석유", "전기", "원자", "미래" };
        public const int EraCount = 6;
        public static readonly ScienceDefinition[] Technologies = new[] {
            new ScienceDefinition(ScienceTechnology.FungalFarming, "균류 재배", 1),
            new ScienceDefinition(ScienceTechnology.HoneydewRanch, "감로 목장", 1, ScienceTechnology.FungalFarming),
            new ScienceDefinition(ScienceTechnology.Resin, "흙벽 공법", 1),
            new ScienceDefinition(ScienceTechnology.Traps, "함정 공학", 1, ScienceTechnology.Resin),
            new ScienceDefinition(ScienceTechnology.Herbs, "약초 처방", 1),
            new ScienceDefinition(ScienceTechnology.Sanitation, "방역", 1, ScienceTechnology.Herbs),
            new ScienceDefinition(ScienceTechnology.Fermentation, "압축 저장", 2, ScienceTechnology.FungalFarming),
            new ScienceDefinition(ScienceTechnology.AdvancedCrops, "고급 작물", 2, ScienceTechnology.HoneydewRanch),
            new ScienceDefinition(ScienceTechnology.AcidRefining, "산성액 정제", 2, ScienceTechnology.Resin),
            new ScienceDefinition(ScienceTechnology.Watchtowers, "감시탑", 2, ScienceTechnology.Traps),
            new ScienceDefinition(ScienceTechnology.Infirmary, "의무실", 2, ScienceTechnology.Herbs),
            new ScienceDefinition(ScienceTechnology.Recreation, "휴게실", 2, ScienceTechnology.Herbs),
            new ScienceDefinition(ScienceTechnology.Blades, "근접 무기", 2, ScienceTechnology.Resin),
            new ScienceDefinition(ScienceTechnology.ArmorPlates, "보호 갑옷", 2, ScienceTechnology.Resin),
            new ScienceDefinition(ScienceTechnology.Vehicle, "바퀴 차량", 2),
            new ScienceDefinition(ScienceTechnology.Drainage, "치수 공사", 2, ScienceTechnology.Sanitation),
            new ScienceDefinition(ScienceTechnology.Firebreaks, "방화대", 2, ScienceTechnology.Resin),
            new ScienceDefinition(ScienceTechnology.EfficientTransport, "수송 효율", 4, ScienceTechnology.Vehicle),
            new ScienceDefinition(ScienceTechnology.Mines, "폭약 매설", 4, ScienceTechnology.Traps),
            new ScienceDefinition(ScienceTechnology.Regeneration, "부위 재생", 4, ScienceTechnology.Infirmary),
            new ScienceDefinition(ScienceTechnology.Trinkets, "장신구", 4, ScienceTechnology.ArmorPlates),
            new ScienceDefinition(ScienceTechnology.AdvancedWeapons, "고급 무기", 4, ScienceTechnology.Blades),
            new ScienceDefinition(ScienceTechnology.Gliding, "비행 장비", 4, ScienceTechnology.Vehicle),
            new ScienceDefinition(ScienceTechnology.Aircraft, "비행기", 4, ScienceTechnology.Gliding),
            new ScienceDefinition(ScienceTechnology.HeavyTransport, "비행선(대형 수송)", 4, ScienceTechnology.Aircraft),
            new ScienceDefinition(ScienceTechnology.Insulation, "보온 설비", 4, ScienceTechnology.Drainage),
            new ScienceDefinition(ScienceTechnology.MigrationTheory, "로켓 이론", 6, ScienceTechnology.Aircraft),
            new ScienceDefinition(ScienceTechnology.Hull, "선체", 6, ScienceTechnology.MigrationTheory),
            new ScienceDefinition(ScienceTechnology.Cocoons, "동면 캡슐", 6, ScienceTechnology.MigrationTheory),
            new ScienceDefinition(ScienceTechnology.Engine, "추진기관", 6, ScienceTechnology.MigrationTheory),
            new ScienceDefinition(ScienceTechnology.Fishing, "낚시", 1),
            new ScienceDefinition(ScienceTechnology.ConscriptionLaw, "징병제", 1),
            new ScienceDefinition(ScienceTechnology.ReserveForces, "예비군제", 2, ScienceTechnology.ConscriptionLaw),
            new ScienceDefinition(ScienceTechnology.TotalMobilization, "국민개병", 4, ScienceTechnology.ReserveForces)
        }.Concat(MedicineResearch.Definitions(ScienceTechnology.Pharmacy, ScienceTechnology.PrecisionPharmacy)).Concat(FormationResearch.Definitions()).Concat(UndergroundResearch.Definitions()).ToArray();

        [Serializable] public sealed class State
        {
            public AntColony.Boss.BossProgress.State bosses = new AntColony.Boss.BossProgress.State();
            public List<int> samples = new List<int>();
            public List<int> resourceResearch = new List<int>();
            public int resourceActive = -1; public float resourceProgress; // G06 진행 중인 자원 표본 연구(구저장 -1)
            public List<CampaignOutcome.Entry> leftCommanders = new List<CampaignOutcome.Entry>(); // G43: 명부에서 지워진 이탈자(포획된 이탈자·반란 합류)
            public List<string> consumptionOverrides = new List<string>();
            public List<int> completed = new List<int>();
            public int active = -1;
            public float progress;
            public bool blueprint;
            public bool betaScenario;
            public bool departed;
            public List<string> passengers = new List<string>();
            public List<string> leftBehind = new List<string>();
            public float endingGameSeconds;
            // 방어시설 연구소 4라인(화력/사거리/내구/함정) 단계. 이전 저장에는 없으므로 비어 있으면 전부 0이다.
            public List<int> defense = new List<int>();
            // 2026-10-15 V5 연구 화면: 정지와 변경. 변경 시 이전 연구의 진행도·지불 여부를 보존해 다시 고르면 비용 없이 이어간다.
            public bool paused;
            public List<int> partialIds = new List<int>();
            public List<float> partialProgress = new List<float>();
        }
        private State state = new State();
        public ScienceDefinition Active => state.active >= 0 ? Technologies[state.active] : null;
        public float Progress => state.progress;
        public bool Paused => state.paused;
        public void SetPaused(bool value) { if (Active != null) state.paused = value; }
        // 이전에 시작했던(비용 지불한) 연구의 보존 진행도. 없으면 -1.
        public float SavedProgress(ScienceTechnology t)
        {
            var i = state.partialIds.IndexOf((int)t);
            return i < 0 ? -1 : state.partialProgress[i];
        }
        public bool HasBlueprint => state.blueprint;
        public bool BetaScenario { get => state.betaScenario; set => state.betaScenario = value; }
        public bool Departed => state.departed;
        public IReadOnlyList<string> Passengers => state.passengers;
        public IReadOnlyList<string> LeftBehind => state.leftBehind;
        public float EndingGameSeconds => state.endingGameSeconds;
        public event Action OnDeparted;
        private void Awake()
        {
            Instance = this;
            if (GetComponent<AntColony.World.BossRewardVisual>() == null) gameObject.AddComponent<AntColony.World.BossRewardVisual>();
        }
        private void OnDestroy() { if (Instance == this) Instance = null; }
        public bool Has(ScienceTechnology technology) => state.completed.Contains((int)technology)
            || technology == ScienceTechnology.Fishing && GameManager.Instance != null && GameManager.Instance.FishingUnlocked;
        public int DefenseLevel(DefenseLine line) => (int)line < state.defense.Count ? state.defense[(int)line] : 0;
        internal void SetDefenseLevel(DefenseLine line, int level)
        {
            while (state.defense.Count < 4) state.defense.Add(0);
            state.defense[(int)line] = level;
        }
        public void AcquireBlueprint()
        {
            if (!BossProgress.Delivered(AntColony.Boss.BossId.D)) return;
            state.blueprint = true;
        }

        public string BlockReason(ScienceTechnology technology)
        {
            var i = (int)technology;
            if (i < 0 || i >= Technologies.Length) return "알 수 없는 기술입니다.";
            if (Departed) return "로켓이 이미 발사되었습니다.";
            if (state.resourceActive >= 0) return "자원 표본 연구 중입니다.";
            if (Has(technology)) return "이미 연구했습니다.";
            if (Active != null && Active.Technology == technology) return "이미 진행 중인 연구입니다.";
            var definition = Technologies[i];
            foreach (var prerequisite in definition.Prerequisites)
                if (!Has(prerequisite)) return "선행 연구 필요: " + Technologies[(int)prerequisite].Name + ".";
            if (technology == ScienceTechnology.Engine && !HasBlueprint) return "D 보스의 로켓 핵심물을 본거지에 하역하세요.";
            foreach (var lab in AntColony.Core.SceneObjects.All<ScienceLab>())
                if (lab.isActiveAndEnabled && lab.Tier >= definition.Tier) return "";
            return "과학 연구소 " + definition.Tier + "등급(" + EraNames[definition.Tier - 1] + " 시대)이 필요합니다.";
        }

        public bool TryStart(ScienceTechnology technology)
        {
            if (BlockReason(technology) != "") return false;
            var definition = Technologies[(int)technology];
            var saved = state.partialIds.IndexOf((int)technology);
            // 처음 시작하는 연구만 비용을 낸다. 이전에 시작했던 연구는 보존 진행도에서 이어간다(중복 차감 없음).
            if (saved < 0 && (ResourceManager.Instance == null || !ResourceManager.Instance.TrySpend(definition.Food, definition.Soil, definition.Special, reason: ResourceReason.Research))) return false;
            if (Active != null) { state.partialIds.Add(state.active); state.partialProgress.Add(state.progress); }
            state.active = (int)technology;
            state.progress = 0;
            if (saved >= 0) { state.progress = state.partialProgress[saved]; state.partialIds.RemoveAt(saved); state.partialProgress.RemoveAt(saved); }
            state.paused = false;
            return true;
        }

        // 연구소 한 곳이 지금 공동 연구에 기여하지 못하는 이유. 빈 문자열이면 기여 중.
        public string LabWaitReason(ScienceLab lab)
        {
            if (Active == null && state.resourceActive < 0) return "선택한 연구 없음";
            if (state.paused) return "연구 정지됨";
            if (!lab.isActiveAndEnabled) return "시설 비활성";
            if (lab.Busy) return "연구소가 차량 작업 중";
            if (Active != null && lab.Tier < Active.Tier) return $"연구소 {lab.Tier}등급 — {EraNames[Active.Tier - 1]} 시대 연구 불가({Active.Tier}등급 필요)";
            var c = lab.Target;
            if (c == null) return "담당 장수 없음";
            if (c.IsAwayFromHome) return c.CommanderName + " 본거지 밖";
            if (!c.ResearchWorkReady || !c.CanReceiveOrders) return c.CommanderName + " 작업 불가 상태(이동·수면·부상·식사 등)";
            if ((c.Position - lab.Position).sqrMagnitude > 64) return c.CommanderName + " 연구소로 복귀 중";
            return "";
        }

        private void Update() => Tick(Time.deltaTime);
        public void Tick(float seconds)
        {
            if (seconds > 0 && !float.IsInfinity(seconds)) BossProgress.TickCraft(seconds);
            if (Active == null && state.resourceActive >= 0 && seconds > 0 && !float.IsInfinity(seconds) && !Departed) { TickResourceResearch(seconds); return; }
            if (!(seconds > 0) || float.IsInfinity(seconds) || Active == null || Departed || state.paused) return;
            foreach (var lab in AntColony.Core.SceneObjects.All<ScienceLab>())
            {
                var c = lab.Target;
                if (!lab.isActiveAndEnabled || lab.Busy || lab.Tier < Active.Tier || c == null
                    || !c.ResearchWorkReady || !c.CanReceiveOrders || c.IsAwayFromHome || c.ScienceAssignment != lab || (c.Position - lab.Position).sqrMagnitude > 64) continue;
                state.progress += seconds * c.WorkRate(CommanderActivity.Research) * (1f + .25f * (lab.Tier - 1));
                c.GainExperience(CommanderActivity.Research, seconds, Topic(Active.Technology));
            }
            if (state.progress < Active.Work) return;
            var completed = Active;
            state.completed.Add(state.active);
            state.active = -1;
            state.progress = 0;
            state.paused = false;
            var world = WorldMapManager.Instance;
            if (completed.Technology == ScienceTechnology.Fishing && GameManager.Instance != null)
                GameManager.Instance.FishingUnlocked = true;
            if (completed.Technology == ScienceTechnology.Fermentation)
                foreach (var storage in AntColony.Core.SceneObjects.All<Storage>()) storage.RefreshCapacity();
            if (world != null)
            {
                if (completed.Technology == ScienceTechnology.Vehicle) world.VehicleResearched = true;
                if (completed.Technology == ScienceTechnology.Aircraft) world.AircraftResearched = true;
            }
            AntColony.UI.ToastManager.Show(completed.Name + " 연구 완료.");
        }

        public void CompleteDeparture(IReadOnlyList<CommanderAnt> boarded)
        {
            if (Departed) return;
            state.departed = true;
            state.endingGameSeconds = GameCalendar.GameSeconds;
            foreach (var c in CommanderRoster.Instance.Commanders)
            {
                var aboard = false;
                foreach (var passenger in boarded) if (passenger == c) { aboard = true; break; }
                if (aboard) state.passengers.Add(c.CommanderName);
                else state.leftBehind.Add(c.CommanderName + (c.IsDead ? " (사망)" : c.IsCaptive ? " (포로)" : ""));
            }
            Time.timeScale = 0;
            OnDeparted?.Invoke();
            AntColony.UI.GameMenuController.Instance?.ShowDeparture();
        }

        public State CaptureState() => JsonUtility.FromJson<State>(JsonUtility.ToJson(state));
        public void RestoreState(State value)
        {
            state = value == null ? new State() : JsonUtility.FromJson<State>(JsonUtility.ToJson(value));
            state.bosses ??= new AntColony.Boss.BossProgress.State();
            state.samples ??= new List<int>();
            state.resourceResearch ??= new List<int>();
            state.consumptionOverrides ??= new List<string>();
            if (state.defense == null) state.defense = new List<int>();
            if (state.partialIds == null || state.partialProgress == null || state.partialIds.Count != state.partialProgress.Count)
            { state.partialIds = new List<int>(); state.partialProgress = new List<float>(); }
            foreach (var storage in AntColony.Core.SceneObjects.All<Storage>()) storage.RefreshCapacity();
        }
        public static bool Validate(State value, out string error)
        {
            error = "Invalid campaign research state.";
            if (value == null) { error = ""; return true; }
            if (!AntColony.Boss.BossProgress.Validate(value.bosses)) return false;
            if (!ValidResourceRecords(value.samples) || !ValidResourceRecords(value.resourceResearch)) return false;
            if (value.resourceResearch != null && value.resourceResearch.Exists(i => value.samples == null || !value.samples.Contains(i))) return false;
            if (!ValidResourceActive(value)) return false;
            if (value.leftCommanders != null && (value.leftCommanders.Exists(e => e == null || string.IsNullOrEmpty(e.id) || e.name == null)
                || value.leftCommanders.Select(e => e.id).Distinct().Count() != value.leftCommanders.Count)) return false;
            if (value.consumptionOverrides != null && (value.consumptionOverrides.Exists(string.IsNullOrWhiteSpace)
                || new HashSet<string>(value.consumptionOverrides).Count != value.consumptionOverrides.Count)) return false;
            if (value.completed == null || value.passengers == null || value.leftBehind == null
                || value.active < -1 || value.active >= Technologies.Length || float.IsNaN(value.progress)
                || float.IsInfinity(value.progress) || value.progress < 0 || value.endingGameSeconds < 0
                || float.IsNaN(value.endingGameSeconds) || float.IsInfinity(value.endingGameSeconds)) return false;
            if (value.defense != null && (value.defense.Count > 4 || value.defense.Exists(l => l < 0 || l > 3))) return false;
            var unique = new HashSet<int>();
            foreach (var i in value.completed) if (i < 0 || i >= Technologies.Length || !unique.Add(i)) return false;
            if (value.partialIds != null && (value.partialProgress == null || value.partialIds.Count != value.partialProgress.Count
                || value.partialIds.Exists(i => i < 0 || i >= Technologies.Length || unique.Contains(i) || i == value.active)
                || value.partialProgress.Exists(p => float.IsNaN(p) || float.IsInfinity(p) || p < 0))) return false;
            if (value.active >= 0 && (unique.Contains(value.active) || value.progress >= Technologies[value.active].Work)) return false;
            error = ""; return true;
        }
    }
}
