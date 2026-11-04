using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Core;
using AntColony.Units;
using UnityEngine;

namespace AntColony.Buildings
{
    public enum AirshipPart { Hull, Engine, Cocoon }

    // 로켓 발사대(2026-10-05, 구 비행선 조선소). 저장·프리팹 호환을 위해 코드 이름은 AirshipYard 유지.
    public sealed partial class AirshipYard : BuildingBase
    {
        [Serializable] public sealed class State
        {
            public bool hull, engine;
            public int cocoons;
            public int building = -1;
            public float remaining;
            public RocketPhase phase;
            public float preparationRemaining;
            public int fuelType = -1, fuelUnits;
            public RocketSnapshot snapshot;
            public bool launchRequested;
            public int civilianPassengers, civilianYoung, civilianOld;
            public List<int> passengers = new List<int>();
            public bool babelEligible, foundationUsed;
            public float constructionWork;
            public List<RocketBuilderWork> builders = new List<RocketBuilderWork>();
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
                || ModificationsLocked || research == null || research.Departed || !research.Has(Technology(part))
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
            if (!isActiveAndEnabled || IsDead || !(seconds > 0) || float.IsInfinity(seconds) || AntColony.Save.SaveSystem.Busy) return;
            TickRocket(seconds);
            if (state.building < 0) return;
            state.remaining = Mathf.Max(0, state.remaining - seconds);
            if (state.remaining > 0) return;
            if (state.building == (int)AirshipPart.Hull) state.hull = true;
            else if (state.building == (int)AirshipPart.Engine) state.engine = true;
            else state.cocoons++;
            AntColony.UI.ToastManager.Show("Rocket " + (AirshipPart)state.building + " complete.");
            state.building = -1;
        }

        public bool TryBoard(CommanderAnt commander)
        {
            if (!isActiveAndEnabled || IsDead || Phase != RocketPhase.Boarding || !Ready || CampaignResearch.Instance == null || CampaignResearch.Instance.Departed
                || commander == null || !commander.isActiveAndEnabled || commander.IsDead || commander.IsAwayFromHome
                || commander.IsEmbarked || commander.LabUpgradeBusy || !commander.CanChangeAllocation
                || commander.IsCaptive || commander.IsDeparting || commander.IsHostile
                || OccupiedSeats + 1 + commander.TroopCount > Capacity || passengers.Contains(commander) || Vector3.Distance(commander.Position, Position) > 8) return false;
            passengers.Add(commander);
            commander.SetEmbarked(true, Position);
            return true;
        }

        public void Unload()
        {
            state.launchRequested = false;
            UnloadCivilians();
            for (var i = 0; i < passengers.Count; i++)
                if (passengers[i] != null) passengers[i].SetEmbarked(false, Position + Vector3.right * (3 + i));
            passengers.Clear();
        }

        public bool TryDisembark(CommanderAnt commander)
        {
            if (!isActiveAndEnabled || IsDead || Phase != RocketPhase.Boarding || commander == null || !passengers.Remove(commander)) return false;
            state.launchRequested = false;
            commander.SetEmbarked(false, Position + Vector3.right * 3);
            return true;
        }

        public bool TryDepart()
        {
            var research = CampaignResearch.Instance;
            if (!isActiveAndEnabled || IsDead || Phase != RocketPhase.Boarding || !Ready || state.launchRequested || !HasLaunchCommander || research == null || research.Departed) return false;
            state.launchRequested = true;
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
            if (!Validate(value, commanders.Count, out var error)
                || value != null && value.passengers.Sum(id => 1L + commanders[id].TroopCount) + value.civilianPassengers > BaseSeats + SeatsPerModule * value.cocoons)
                throw new FormatException(string.IsNullOrEmpty(error) ? "Rocket seats exceeded." : error);
            // 시민 풀은 population 저장에서 복원한다. 상태 교체 때 승객을 다시 지급하지 않는다.
            state.civilianPassengers = state.civilianYoung = state.civilianOld = 0;
            Unload();
            state = value == null ? new State() : JsonUtility.FromJson<State>(JsonUtility.ToJson(value));
            // 한도 도입 전 저장은 초과분만 잘라낸다(이미 탑승한 장수의 자리는 유지).
            state.cocoons = Mathf.Min(state.cocoons, Mathf.Max(GameBalance.MaxCocoons, state.passengers.Count));
            foreach (var id in state.passengers) { passengers.Add(commanders[id]); commanders[id].SetEmbarked(true, Position); }
        }
        public static bool Validate(State value, int commanderCount, out string error)
        {
            error = "Invalid rocket launch pad state.";
            if (value == null) { error = ""; return true; }
            if (!ValidFoundation(value)) return false;
            if (value.cocoons < 0 || value.building < -1 || value.building > 2 || value.remaining < 0
                || value.civilianPassengers < 0 || value.civilianYoung < 0 || value.civilianOld < 0
                || (long)value.civilianYoung + value.civilianOld > value.civilianPassengers
                || float.IsInfinity(value.remaining) || float.IsNaN(value.remaining) || value.passengers == null
                || value.cocoons > GameBalance.MaxCocoons || (long)value.passengers.Count + value.civilianPassengers > BaseSeats + SeatsPerModule * value.cocoons
                || (value.building == -1 && value.remaining != 0)
                || !Enum.IsDefined(typeof(RocketPhase), value.phase) || !RocketSnapshot.Finite(value.preparationRemaining)
                || value.preparationRemaining > PreparationSeconds || value.fuelUnits < 0 || value.fuelUnits % 15 != 0
                || value.fuelUnits == 0 && value.fuelType != -1
                || value.fuelUnits > 0 && !Enum.IsDefined(typeof(AntColony.Data.ResourceType), value.fuelType)
                || RocketSnapshot.Present(value.snapshot) && !value.snapshot.Valid
                || (value.phase == RocketPhase.Preparing || value.phase == RocketPhase.Boarding || value.phase == RocketPhase.Launched)
                    && (!value.hull || !value.engine || value.building >= 0 || value.fuelUnits <= 0 || !RocketSnapshot.Present(value.snapshot))
                || (value.phase == RocketPhase.Boarding || value.phase == RocketPhase.Launched) && value.preparationRemaining != 0
                || value.phase == RocketPhase.Preparing && value.preparationRemaining <= 0
                || value.phase == RocketPhase.Destroyed && (value.passengers.Count > 0 || value.civilianPassengers > 0 || value.launchRequested)
                || value.civilianPassengers > 0 && value.phase != RocketPhase.Boarding && value.phase != RocketPhase.Launched
                || value.phase == RocketPhase.Construction && (value.preparationRemaining != 0 || RocketSnapshot.Present(value.snapshot))
                || value.launchRequested && value.phase != RocketPhase.Boarding) return false;
            var unique = new HashSet<int>();
            foreach (var id in value.passengers) if (id < 0 || id >= commanderCount || !unique.Add(id)) return false;
            error = ""; return true;
        }
        protected override void OnDisable() { if (Application.isPlaying) Unload(); base.OnDisable(); }
    }
}
