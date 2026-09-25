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
        public readonly int Food, Soil;
        public readonly ScienceTechnology[] Prerequisites;
        // ponytail: branch names are provisional; research times/costs remain tuning constants until playtesting.
        public ScienceDefinition(ScienceTechnology technology, string name, int tier, params ScienceTechnology[] prerequisites)
        { Technology = technology; Name = name; Tier = tier; Work = 300f * tier * tier; Food = 10 * tier; Soil = 15 * tier; Prerequisites = prerequisites; }
    }

    public sealed class CampaignResearch : MonoBehaviour
    {
        public static CampaignResearch Instance { get; private set; }
        public static readonly ScienceDefinition[] Technologies = {
            new ScienceDefinition(ScienceTechnology.FungalFarming, "Fungal farming", 1),
            new ScienceDefinition(ScienceTechnology.HoneydewRanch, "Honeydew ranch", 1, ScienceTechnology.FungalFarming),
            new ScienceDefinition(ScienceTechnology.Resin, "Resin processing", 1),
            new ScienceDefinition(ScienceTechnology.Traps, "Trap engineering", 1, ScienceTechnology.Resin),
            new ScienceDefinition(ScienceTechnology.Herbs, "Herbal medicine", 1),
            new ScienceDefinition(ScienceTechnology.Sanitation, "Sanitation", 1, ScienceTechnology.Herbs),
            new ScienceDefinition(ScienceTechnology.Fermentation, "Fermentation storage", 2, ScienceTechnology.FungalFarming),
            new ScienceDefinition(ScienceTechnology.AdvancedCrops, "Advanced crops", 2, ScienceTechnology.HoneydewRanch),
            new ScienceDefinition(ScienceTechnology.AcidRefining, "Acid refining", 2, ScienceTechnology.Resin),
            new ScienceDefinition(ScienceTechnology.Watchtowers, "Watchtowers", 2, ScienceTechnology.Traps),
            new ScienceDefinition(ScienceTechnology.Infirmary, "Infirmary", 2, ScienceTechnology.Herbs),
            new ScienceDefinition(ScienceTechnology.Recreation, "Recreation", 2, ScienceTechnology.Herbs),
            new ScienceDefinition(ScienceTechnology.Blades, "Mandible blades", 2, ScienceTechnology.Resin),
            new ScienceDefinition(ScienceTechnology.ArmorPlates, "Carapace armor", 2, ScienceTechnology.Resin),
            new ScienceDefinition(ScienceTechnology.Vehicle, "Wheeled vehicle", 2),
            new ScienceDefinition(ScienceTechnology.Drainage, "Flood control", 2, ScienceTechnology.Sanitation),
            new ScienceDefinition(ScienceTechnology.Firebreaks, "Firebreaks", 2, ScienceTechnology.Resin),
            new ScienceDefinition(ScienceTechnology.EfficientTransport, "Efficient transport", 3, ScienceTechnology.Vehicle),
            new ScienceDefinition(ScienceTechnology.Mines, "Buried explosives", 3, ScienceTechnology.Traps),
            new ScienceDefinition(ScienceTechnology.Regeneration, "Limb regeneration", 3, ScienceTechnology.Infirmary),
            new ScienceDefinition(ScienceTechnology.Trinkets, "Trinkets", 3, ScienceTechnology.ArmorPlates),
            new ScienceDefinition(ScienceTechnology.AdvancedWeapons, "Advanced weapons", 3, ScienceTechnology.Blades),
            new ScienceDefinition(ScienceTechnology.Gliding, "Gliding wings", 3, ScienceTechnology.Vehicle),
            new ScienceDefinition(ScienceTechnology.Aircraft, "Aircraft", 3, ScienceTechnology.Gliding),
            new ScienceDefinition(ScienceTechnology.HeavyTransport, "Heavy transport", 3, ScienceTechnology.Aircraft),
            new ScienceDefinition(ScienceTechnology.Insulation, "Insulation", 3, ScienceTechnology.Drainage),
            new ScienceDefinition(ScienceTechnology.MigrationTheory, "Great migration theory", 3, ScienceTechnology.Aircraft),
            new ScienceDefinition(ScienceTechnology.Hull, "Airship hull", 4, ScienceTechnology.MigrationTheory),
            new ScienceDefinition(ScienceTechnology.Cocoons, "Hibernation cocoons", 4, ScienceTechnology.MigrationTheory),
            new ScienceDefinition(ScienceTechnology.Engine, "Airship engine (blueprint)", 4, ScienceTechnology.MigrationTheory)
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
            if (ResourceManager.Instance == null || !ResourceManager.Instance.TrySpend(definition.Food, definition.Soil)) return false;
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
            var unique = new HashSet<int>();
            foreach (var i in value.completed) if (i < 0 || i >= Technologies.Length || !unique.Add(i)) return false;
            if (value.active >= 0 && (unique.Contains(value.active) || value.progress >= Technologies[value.active].Work)) return false;
            error = ""; return true;
        }
    }
}
