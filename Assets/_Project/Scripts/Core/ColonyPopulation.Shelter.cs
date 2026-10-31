using System.Linq;
using AntColony.Buildings;
using UnityEngine;

namespace AntColony.Core
{
    public sealed partial class ColonyPopulation
    {
        // 파괴 순간 주소를 끊는다. 같은 좌표에 새 집을 지어도 기존 주민/입주 예약을 승계하지 않는다.
        public void InvalidateHousing(Vector3 position)
        {
            InvalidateHousing(S, position);
            lastSync = (-1, 0, 0, 0, 0);
        }
        public static void InvalidateHousing(State state, Vector3 position)
        {
            foreach (var group in state.civilianLocations)
            {
                if (group.housing && (group.position - position).sqrMagnitude < .01f)
                { group.housing = false; group.sheltered = false; }
                if (group.rehousing && (group.housingDestination - position).sqrMagnitude < .01f)
                { group.rehousing = false; group.sheltered = false; }
            }
        }
        public int TemporaryResidents => S.civilianLocations.Where(g => g.temporary && !g.moving && g.sheltered).Sum(g => g.count);
        public int ExposedResidents => S.civilianLocations.Where(g => !g.sheltered).Sum(g => g.count);
        public float DisplacementPenalty => LocatedCivilians == 0 ? 0 : -(10f * ExposedResidents + 5f * TemporaryResidents) / LocatedCivilians;
        public int HousingRoom(Housing home) => Mathf.Max(0, home.Capacity - S.civilianLocations
            .Where(g => g.housing && (g.position - home.Position).sqrMagnitude < .01f
                || g.rehousing && (g.housingDestination - home.Position).sqrMagnitude < .01f).Sum(g => g.count));
        public int BeginRehousing(int sourceId, Housing home)
        {
            if (home == null || home.IsDead || !home.isActiveAndEnabled || Buildings.AirshipYard.IsGroupReserved(sourceId)) return 0;
            int room = HousingRoom(home); if (room == 0) return 0;
            int id = BeginCivilianEvacuation(sourceId, home.Position, room);
            var group = S.civilianLocations.Find(g => g.id == id);
            if (group != null) { group.rehousing = true; group.housingDestination = home.Position; }
            return id;
        }
        public int BeginTemporaryShelter(int sourceId)
        {
            if (AirshipYard.IsGroupReserved(sourceId)) return 0;
            int id = BeginCivilianEvacuation(sourceId, transform.position);
            var group = S.civilianLocations.Find(g => g.id == id);
            if (group != null) group.temporary = true;
            return id;
        }
        private void SettleArrivals()
        {
            foreach (var group in S.civilianLocations.Where(g => g.arrived && !g.moving))
            {
                if (group.temporary) { group.sheltered = true; continue; }
                if (!group.rehousing) continue;
                var home = Housing.All.FirstOrDefault(h => h.isActiveAndEnabled && !h.IsDead && (h.Position - group.housingDestination).sqrMagnitude < .01f);
                group.rehousing = false;
                if (home == null) continue;
                group.position = home.Position; group.housing = true; group.sheltered = true; group.arrived = false;
            }
        }
    }
}
