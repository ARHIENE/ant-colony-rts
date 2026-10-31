using System;
using System.Linq;
using AntColony.Core;
using UnityEngine;

namespace AntColony.Buildings
{
    public enum RocketPhase { Construction, Preparing, Boarding, Launched, Destroyed }

    public sealed partial class AirshipYard
    {
        public const int BaseSeats = 10, SeatsPerModule = 50, MaximumSeats = 1010;
        public const float PreparationSeconds = 15 * GameCalendar.SecondsPerDay;
        // shortcut: G52 연료 규격 미정. 확정 시 이 잠정 규격만 교체한다.
        public const AntColony.Data.ResourceType FuelResource = AntColony.Data.ResourceType.Petroleum;
        public const int FuelUnitsPerDay = 1;
        public RocketPhase Phase => state.phase;
        public bool Operating => Phase == RocketPhase.Preparing || Phase == RocketPhase.Boarding;
        public bool ModificationsLocked => Operating || Phase == RocketPhase.Launched;
        public int Capacity => BaseSeats + SeatsPerModule * Cocoons;
        public int CivilianPassengers => state.civilianPassengers;
        public int OccupiedSeats => CivilianPassengers + passengers.Where(c => c != null && !c.IsDead).Sum(c => 1 + c.TroopCount);
        public bool HasLaunchCommander => passengers.Any(c => CampaignOutcome.Survivor(c) && c.IsEmbarked);
        public ReserveRequirement[] ActivationReserves => new[] { new ReserveRequirement(FuelResource, FuelUnitsPerDay * FinalBattleEconomy.DefenseDays) };
        public int LoadedFuel(AntColony.Data.ResourceType type) => state.fuelType == (int)type ? state.fuelUnits : 0;
        public float PreparationRemaining => state.preparationRemaining;
        public bool FuelLoaded => state.fuelUnits > 0;
        public RocketSnapshot FixedSnapshot => !RocketSnapshot.Present(state.snapshot) ? null : JsonUtility.FromJson<RocketSnapshot>(JsonUtility.ToJson(state.snapshot));
        public RocketSnapshot ActivationPreview
        {
            get
            {
                ColonyPopulation.Instance?.ObserveRocketEconomy();
                var economy = RocketCampaign.Instance?.Economy;
                if (economy == null || economy.samples.Count == 0) return null;
                var current = economy.samples[economy.samples.Count - 1];
                return economy.Preview(GameCalendar.GameSeconds, current.wealth, current.population);
            }
        }

        // 연료 종류·일 소비량은 확정된 연료 규격을 가진 호출자가 전달한다. 15일분을 원자적으로 소비한다.
        public bool TryLoadFuel(AntColony.Data.ResourceType type, int unitsPerDay)
        {
            if (!isActiveAndEnabled || IsDead || Phase != RocketPhase.Construction || ModificationsLocked || FuelLoaded || unitsPerDay <= 0
                || unitsPerDay > int.MaxValue / 15 || !Enum.IsDefined(typeof(AntColony.Data.ResourceType), type)
                || ResourceManager.Instance == null) return false;
            int amount = unitsPerDay * 15;
            if (!ResourceManager.Instance.TrySpend(type, amount, ResourceReason.Construction)) return false;
            state.fuelType = (int)type; state.fuelUnits = amount;
            return true;
        }
        public bool TryActivate(float currentWealth, float currentPopulation)
        {
            var campaign = RocketCampaign.Instance;
            if (!isActiveAndEnabled || IsDead || !Ready || Phase != RocketPhase.Construction || !FuelLoaded
                || Demolition.Pending(this) || campaign == null || CampaignResearch.Instance == null
                || CampaignResearch.Instance.Departed || RocketCampaign.Operating || !RocketCampaign.AssaultAvailable) return false;
            var snapshot = campaign.Economy.Preview(GameCalendar.GameSeconds, currentWealth, currentPopulation);
            if (snapshot == null || !CampaignResearch.Instance.BossProgress.TryConsumeRocketCore()) return false;
            state.snapshot = snapshot;
            state.preparationRemaining = PreparationSeconds;
            state.phase = RocketPhase.Preparing;
            return true;
        }
        private void TickRocket(float seconds)
        {
            if (!Operating || IsDead) return;
            // 내장 전원이므로 외부 전력·부분 손상·수리와 독립적이다.
            state.preparationRemaining = Mathf.Max(0, state.preparationRemaining - seconds);
            if (state.preparationRemaining == 0) state.phase = RocketPhase.Boarding;
            RocketCampaign.TickAssault(this, FixedSnapshot, seconds);
        }
        internal void ResolveLaunch()
        {
            passengers.RemoveAll(c => c == null || c.IsDead || c.IsCaptive || c.IsDeparting || c.IsHostile || !c.IsEmbarked);
            if (!state.launchRequested) return;
            state.launchRequested = false;
            var research = CampaignResearch.Instance;
            if (!isActiveAndEnabled || IsDead || Phase != RocketPhase.Boarding || !HasLaunchCommander
                || research == null || research.Departed) return;
            RocketCampaign.Instance?.RecordEnding(this);
            ClearReservations();
            state.phase = RocketPhase.Launched;
            research.CompleteDeparture(passengers);
        }
        protected override void Die()
        {
            ClearReservations();
            if (Operating) RocketCampaign.Instance?.RocketDestroyed();
            RecordDestruction();
            state.phase = RocketPhase.Destroyed;
            state.launchRequested = false;
            DestroyPassengers();
            if (HasFoundation) return;
            base.Die();
        }
    }
}
