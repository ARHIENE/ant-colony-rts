using System;
using System.Collections.Generic;
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
        Aircraft, HeavyTransport, Insulation, MigrationTheory, Hull, Cocoons, Engine
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

    public sealed class CampaignResearch : MonoBehaviour
    {
        public static CampaignResearch Instance { get; private set; }
        public static readonly ScienceDefinition[] Technologies = {
            new ScienceDefinition(ScienceTechnology.FungalFarming, "균류 재배", 1),
            new ScienceDefinition(ScienceTechnology.HoneydewRanch, "감로 목장", 1, ScienceTechnology.FungalFarming),
            new ScienceDefinition(ScienceTechnology.Resin, "흙벽 공법", 1),
            new ScienceDefinition(ScienceTechnology.Traps, "함정 공학", 1, ScienceTechnology.Resin),
            new ScienceDefinition(ScienceTechnology.Herbs, "약초 처방", 1),
            new ScienceDefinition(ScienceTechnology.Sanitation, "방역", 1, ScienceTechnology.Herbs),
            new ScienceDefinition(ScienceTechnology.Fermentation, "압축 저장", 2, ScienceTechnology.FungalFarming),
            new ScienceDefinition(ScienceTechnology.AdvancedCrops, "고급 작물", 2, ScienceTechnology.HoneydewRanch),
            new ScienceDefinition(ScienceTechnology.AcidRefining, "개미산 정제", 2, ScienceTechnology.Resin),
            new ScienceDefinition(ScienceTechnology.Watchtowers, "감시탑", 2, ScienceTechnology.Traps),
            new ScienceDefinition(ScienceTechnology.Infirmary, "의무실", 2, ScienceTechnology.Herbs),
            new ScienceDefinition(ScienceTechnology.Recreation, "휴게실", 2, ScienceTechnology.Herbs),
            new ScienceDefinition(ScienceTechnology.Blades, "큰턱 날", 2, ScienceTechnology.Resin),
            new ScienceDefinition(ScienceTechnology.ArmorPlates, "외골격 코팅", 2, ScienceTechnology.Resin),
            new ScienceDefinition(ScienceTechnology.Vehicle, "바퀴 차량", 2),
            new ScienceDefinition(ScienceTechnology.Drainage, "치수 공사", 2, ScienceTechnology.Sanitation),
            new ScienceDefinition(ScienceTechnology.Firebreaks, "방화대", 2, ScienceTechnology.Resin),
            new ScienceDefinition(ScienceTechnology.EfficientTransport, "수송 효율", 3, ScienceTechnology.Vehicle),
            new ScienceDefinition(ScienceTechnology.Mines, "자폭 매설", 3, ScienceTechnology.Traps),
            new ScienceDefinition(ScienceTechnology.Regeneration, "부위 재생", 3, ScienceTechnology.Infirmary),
            new ScienceDefinition(ScienceTechnology.Trinkets, "장신구", 3, ScienceTechnology.ArmorPlates),
            new ScienceDefinition(ScienceTechnology.AdvancedWeapons, "고급 무기", 3, ScienceTechnology.Blades),
            new ScienceDefinition(ScienceTechnology.Gliding, "활공 날개", 3, ScienceTechnology.Vehicle),
            new ScienceDefinition(ScienceTechnology.Aircraft, "비행기", 3, ScienceTechnology.Gliding),
            new ScienceDefinition(ScienceTechnology.HeavyTransport, "대형 수송", 3, ScienceTechnology.Aircraft),
            new ScienceDefinition(ScienceTechnology.Insulation, "보온 설비", 3, ScienceTechnology.Drainage),
            new ScienceDefinition(ScienceTechnology.MigrationTheory, "대이주 이론", 3, ScienceTechnology.Aircraft),
            new ScienceDefinition(ScienceTechnology.Hull, "선체", 4, ScienceTechnology.MigrationTheory),
            new ScienceDefinition(ScienceTechnology.Cocoons, "동면 고치", 4, ScienceTechnology.MigrationTheory),
            new ScienceDefinition(ScienceTechnology.Engine, "추진기관", 4, ScienceTechnology.MigrationTheory)
        };

        [Serializable] public sealed class State
        {
            public List<int> completed = new List<int>();
            public int active = -1;
            public float progress;
            public bool blueprint;
            public bool departed;
            public List<string> passengers = new List<string>();
            public List<string> leftBehind = new List<string>();
            public float endingGameSeconds;
            // 방어시설 연구소 4라인(화력/사거리/내구/함정) 단계. 이전 저장에는 없으므로 비어 있으면 전부 0이다.
            public List<int> defense = new List<int>();
        }
        private State state = new State();
        public ScienceDefinition Active => state.active >= 0 ? Technologies[state.active] : null;
        public float Progress => state.progress;
        public bool HasBlueprint => state.blueprint;
        public bool Departed => state.departed;
        public IReadOnlyList<string> Passengers => state.passengers;
        public IReadOnlyList<string> LeftBehind => state.leftBehind;
        public float EndingGameSeconds => state.endingGameSeconds;
        public event Action OnDeparted;
        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }
        public bool Has(ScienceTechnology technology) => state.completed.Contains((int)technology);
        public int DefenseLevel(DefenseLine line) => (int)line < state.defense.Count ? state.defense[(int)line] : 0;
        internal void SetDefenseLevel(DefenseLine line, int level)
        {
            while (state.defense.Count < 4) state.defense.Add(0);
            state.defense[(int)line] = level;
        }
        public void AcquireBlueprint() { state.blueprint = true; AntColony.UI.ToastManager.Show("Airship engine blueprint acquired."); }

        public string BlockReason(ScienceTechnology technology)
        {
            var i = (int)technology;
            if (i < 0 || i >= Technologies.Length) return "Unknown technology.";
            if (Departed) return "The colony has departed.";
            if (Has(technology)) return "Already researched.";
            if (Active != null) return "One shared research project at a time.";
            var definition = Technologies[i];
            foreach (var prerequisite in definition.Prerequisites)
                if (!Has(prerequisite)) return "Requires " + Technologies[(int)prerequisite].Name + ".";
            if (technology == ScienceTechnology.Engine && !HasBlueprint) return "Bring an engine blueprint home from a boss nest or trading post.";
            foreach (var lab in FindObjectsByType<ScienceLab>(FindObjectsSortMode.None))
                if (lab.isActiveAndEnabled && lab.Tier >= definition.Tier) return "";
            return "Requires science lab tier " + definition.Tier + ".";
        }

        public bool TryStart(ScienceTechnology technology)
        {
            if (BlockReason(technology) != "") return false;
            var definition = Technologies[(int)technology];
            if (ResourceManager.Instance == null || !ResourceManager.Instance.TrySpend(definition.Food, definition.Soil, definition.Special, reason: ResourceReason.Research)) return false;
            state.active = (int)technology;
            state.progress = 0;
            return true;
        }

        private void Update() => Tick(Time.deltaTime);
        public void Tick(float seconds)
        {
            if (!(seconds > 0) || float.IsInfinity(seconds) || Active == null || Departed) return;
            foreach (var lab in FindObjectsByType<ScienceLab>(FindObjectsSortMode.None))
            {
                var c = lab.Target;
                if (!lab.isActiveAndEnabled || lab.Busy || lab.Tier < Active.Tier || c == null
                    || !c.CanReceiveOrders || c.IsAwayFromHome || c.ScienceAssignment != lab) continue;
                state.progress += seconds * c.Talents.Multiplier(CommanderActivity.Research) * c.Traits.WorkMultiplier * (1f + .25f * (lab.Tier - 1));
                c.GainExperience(CommanderActivity.Research, seconds);
            }
            if (state.progress < Active.Work) return;
            var completed = Active;
            state.completed.Add(state.active);
            state.active = -1;
            state.progress = 0;
            var world = WorldMapManager.Instance;
            if (completed.Technology == ScienceTechnology.Fermentation)
                foreach (var storage in FindObjectsByType<Storage>()) storage.RefreshCapacity();
            if (world != null)
            {
                if (completed.Technology == ScienceTechnology.Vehicle) world.VehicleResearched = true;
                if (completed.Technology == ScienceTechnology.Aircraft) world.AircraftResearched = true;
            }
            AntColony.UI.ToastManager.Show(completed.Name + " research complete.");
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
                else state.leftBehind.Add(c.CommanderName + (c.IsDead ? " (deceased)" : c.IsCaptive ? " (captive)" : ""));
            }
            Time.timeScale = 0;
            OnDeparted?.Invoke();
            AntColony.UI.GameMenuController.Instance?.ShowDeparture();
        }

        public State CaptureState() => JsonUtility.FromJson<State>(JsonUtility.ToJson(state));
        public void RestoreState(State value)
        {
            state = value == null ? new State() : JsonUtility.FromJson<State>(JsonUtility.ToJson(value));
            if (state.defense == null) state.defense = new List<int>();
            foreach (var storage in FindObjectsByType<Storage>()) storage.RefreshCapacity();
        }
        public static bool Validate(State value, out string error)
        {
            error = "Invalid campaign research state.";
            if (value == null) { error = ""; return true; }
            if (value.completed == null || value.passengers == null || value.leftBehind == null
                || value.active < -1 || value.active >= Technologies.Length || float.IsNaN(value.progress)
                || float.IsInfinity(value.progress) || value.progress < 0 || value.endingGameSeconds < 0
                || float.IsNaN(value.endingGameSeconds) || float.IsInfinity(value.endingGameSeconds)) return false;
            if (value.defense != null && (value.defense.Count > 4 || value.defense.Exists(l => l < 0 || l > 3))) return false;
            var unique = new HashSet<int>();
            foreach (var i in value.completed) if (i < 0 || i >= Technologies.Length || !unique.Add(i)) return false;
            if (value.active >= 0 && (unique.Contains(value.active) || value.progress >= Technologies[value.active].Work)) return false;
            error = ""; return true;
        }
    }
}
