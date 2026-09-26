using System;
using System.Collections.Generic;
using AntColony.Core;
using AntColony.Units;
using UnityEngine;

namespace AntColony.Buildings
{
    public enum AirshipPart { Hull, Engine, Cocoon }

    public sealed class AirshipYard : BuildingBase
    {
        [Serializable] public sealed class State
        {
            public bool hull, engine;
            public int cocoons;
            public int building = -1;
            public float remaining;
            public List<int> passengers = new List<int>();
        }
        private State state = new State();
        private readonly List<CommanderAnt> passengers = new List<CommanderAnt>();
        public bool Hull => state.hull;
        public bool Engine => state.engine;
        public int Cocoons => state.cocoons;
        public float Remaining => state.remaining;
        public IReadOnlyList<CommanderAnt> Passengers => passengers;
        public bool Ready => Hull && Engine && state.building < 0;
        public static ScienceTechnology Technology(AirshipPart part) => part == AirshipPart.Hull
            ? ScienceTechnology.Hull : part == AirshipPart.Engine ? ScienceTechnology.Engine : ScienceTechnology.Cocoons;

        public bool TryBuild(AirshipPart part)
        {
            var research = CampaignResearch.Instance;
            if (!Enum.IsDefined(typeof(AirshipPart), part) || !isActiveAndEnabled || IsDead || state.building >= 0
                || research == null || research.Departed || !research.Has(Technology(part))
                || (part == AirshipPart.Hull && Hull) || (part == AirshipPart.Engine && Engine)
                || (part == AirshipPart.Cocoon && Cocoons >= GameBalance.MaxCocoons)) return false;
            var cocoon = part == AirshipPart.Cocoon;
            if (ResourceManager.Instance == null || !ResourceManager.Instance.TrySpend(cocoon ? GameBalance.CocoonFood : 100,
                cocoon ? GameBalance.CocoonSoil : 150, cocoon ? GameBalance.CocoonSpecial : 150, reason: ResourceReason.Construction)) return false;
            state.building = (int)part;
            state.remaining = cocoon ? GameBalance.CocoonSeconds : 300;
            AntColony.World.DiplomacyManager.Instance?.AirshipConstructionStarted();
            return true;
        }

        private void Update() => Tick(Time.deltaTime);
        public void Tick(float seconds)
        {
            if (!isActiveAndEnabled || state.building < 0 || !(seconds > 0) || float.IsInfinity(seconds)) return;
            state.remaining = Mathf.Max(0, state.remaining - seconds);
            if (state.remaining > 0) return;
            if (state.building == (int)AirshipPart.Hull) state.hull = true;
            else if (state.building == (int)AirshipPart.Engine) state.engine = true;
            else state.cocoons++;
            AntColony.UI.ToastManager.Show("Airship " + (AirshipPart)state.building + " complete.");
            state.building = -1;
        }

        public bool TryBoard(CommanderAnt commander)
        {
            if (!isActiveAndEnabled || !Ready || CampaignResearch.Instance == null || CampaignResearch.Instance.Departed
                || commander == null || !commander.isActiveAndEnabled || commander.IsDead || commander.IsAwayFromHome
                || commander.IsEmbarked || commander.LabUpgradeBusy || !commander.CanChangeAllocation
                || passengers.Count >= Cocoons || passengers.Contains(commander) || Vector3.Distance(commander.Position, Position) > 8) return false;
            passengers.Add(commander);
            commander.SetEmbarked(true, Position);
            return true;
        }

        public void Unload()
        {
            for (var i = 0; i < passengers.Count; i++)
                if (passengers[i] != null) passengers[i].SetEmbarked(false, Position + Vector3.right * (3 + i));
            passengers.Clear();
        }

        public bool TryDepart()
        {
            var research = CampaignResearch.Instance;
            if (!isActiveAndEnabled || !Ready || research == null || research.Departed) return false;
            research.CompleteDeparture(passengers);
            return true;
        }

        public State CaptureState(IReadOnlyList<CommanderAnt> commanders)
        {
            var copy = JsonUtility.FromJson<State>(JsonUtility.ToJson(state));
            copy.passengers.Clear();
            foreach (var c in passengers)
                for (var i = 0; i < commanders.Count; i++) if (commanders[i] == c) { copy.passengers.Add(i); break; }
            return copy;
        }
        public void RestoreState(State value, IReadOnlyList<CommanderAnt> commanders)
        {
            Unload();
            state = value == null ? new State() : JsonUtility.FromJson<State>(JsonUtility.ToJson(value));
            // 한도 도입 전 저장은 초과분만 잘라낸다(이미 탑승한 장수의 자리는 유지).
            state.cocoons = Mathf.Min(state.cocoons, Mathf.Max(GameBalance.MaxCocoons, state.passengers.Count));
            foreach (var id in state.passengers) { passengers.Add(commanders[id]); commanders[id].SetEmbarked(true, Position); }
        }
        public static bool Validate(State value, int commanderCount, out string error)
        {
            error = "Invalid airship yard state.";
            if (value == null) { error = ""; return true; }
            if (value.cocoons < 0 || value.building < -1 || value.building > 2 || value.remaining < 0
                || float.IsInfinity(value.remaining) || float.IsNaN(value.remaining) || value.passengers == null
                || value.passengers.Count > value.cocoons || (value.building == -1 && value.remaining != 0)) return false;
            var unique = new HashSet<int>();
            foreach (var id in value.passengers) if (id < 0 || id >= commanderCount || !unique.Add(id)) return false;
            error = ""; return true;
        }
        protected override void OnDisable() { if (Application.isPlaying) Unload(); base.OnDisable(); }
    }
}
