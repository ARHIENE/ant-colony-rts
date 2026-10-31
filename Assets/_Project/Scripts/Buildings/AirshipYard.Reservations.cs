using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Core;
using AntColony.Units;

namespace AntColony.Buildings
{
    public sealed partial class AirshipYard
    {
        public IReadOnlyList<string> ReservedCommanders => state.reservedCommanders;
        public IReadOnlyList<int> ReservedGroups => state.reservedGroups;
        private IEnumerable<CommanderAnt> ReservationCrew => CommanderRoster.Instance?.Commanders ?? Enumerable.Empty<CommanderAnt>();
        public int ReservedSeats => state.reservedGroups.Sum(id => ColonyPopulation.Instance?.S.civilianLocations.Find(g => g.id == id)?.count ?? 0)
            + state.reservedCommanders.Sum(id => ReservationCrew.FirstOrDefault(c => c != null && c.PersonalState.id == id) is CommanderAnt c ? 1 + c.TroopCount : 0);
        public int UnreservedSeats => Math.Max(0, Capacity - OccupiedSeats - ReservedSeats);
        public static int SeatOverflow(int capacity, int occupied, int reserved) => Math.Max(0, occupied + reserved - capacity);
        public int ReservationOverflow => SeatOverflow(Capacity, OccupiedSeats, ReservedSeats);
        public string ReservationWarning => ReservationOverflow > 0
            ? $"좌석 초과 경고: {ReservationOverflow}석 초과 (탑승 {OccupiedSeats} + 예약 {ReservedSeats} / 정원 {Capacity}). 예약 취소·병력·좌석을 직접 조정하세요." : "";
        public static bool IsReserved(string commander) => SceneObjects.All<AirshipYard>().Any(y => y.state.reservedCommanders.Contains(commander));
        public static bool IsGroupReserved(int group) => SceneObjects.All<AirshipYard>().Any(y => y.state.reservedGroups.Contains(group));
        public bool Reserve(CommanderAnt c)
        {
            if (!ReservationOpen || c == null || !c.CanChangeAllocation || c.IsAwayFromHome || c.IsEmbarked
                || IsReserved(c.PersonalState.id) || 1 + c.TroopCount > UnreservedSeats) return false;
            state.reservedCommanders.Add(c.PersonalState.id); return true;
        }
        public bool CancelReservation(string commander) => state.reservedCommanders.Remove(commander);
        public bool CancelGroupReservation(int group) => state.reservedGroups.Remove(group);
        private bool ReservationOpen => isActiveAndEnabled && !IsDead && Phase != RocketPhase.Destroyed && Phase != RocketPhase.Launched;
        public int EvacuateAndReserve(int sourceId)
        {
            var pop = ColonyPopulation.Instance;
            if (!ReservationOpen || pop == null || IsGroupReserved(sourceId)) return 0;
            int seats = UnreservedSeats - (HasLaunchCommander || state.reservedCommanders.Count > 0 ? 0 : 1);
            if (seats <= 0) return 0;
            int id = pop.BeginCivilianEvacuation(sourceId, Position, seats);
            if (id > 0) state.reservedGroups.Add(id);
            return id;
        }
        private void ClearReservations() { state.reservedCommanders.Clear(); state.reservedGroups.Clear(); }
        private void PruneReservations()
        {
            state.reservedCommanders.RemoveAll(id => !ReservationCrew.Any(c => c != null && c.PersonalState.id == id
                && !c.IsDead && !c.IsCaptive && !c.IsDeparting && !c.IsHostile && !c.IsEmbarked && !c.IsAwayFromHome));
            state.reservedGroups.RemoveAll(id => ColonyPopulation.Instance?.S.civilianLocations.Any(g => g.id == id && g.count > 0) != true);
        }
        private int OtherReservedSeats(CommanderAnt c) => ReservedSeats - (state.reservedCommanders.Contains(c.PersonalState.id) ? 1 + c.TroopCount : 0);
    }
}
