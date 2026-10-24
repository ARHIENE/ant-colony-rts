using AntColony.Core;
using UnityEngine;

namespace AntColony.Buildings
{
    public sealed partial class AirshipYard
    {
        public int TryBoard(int groupId)
        {
            var pop = ColonyPopulation.Instance;
            if (!isActiveAndEnabled || IsDead || Phase != RocketPhase.Boarding || !Ready || pop == null
                || CampaignResearch.Instance == null || CampaignResearch.Instance.Departed) return 0;
            pop.SyncCivilianLocations();
            var group = pop.S.civilianLocations.Find(l => l.id == groupId);
            if (group == null || group.moving || !group.arrived || Vector3.Distance(group.position, Position) > 8) return 0;
            int young = pop.S.young, old = pop.S.old;
            int boarded = pop.TakeArrivedCivilians(groupId, Capacity - OccupiedSeats);
            state.civilianPassengers += boarded;
            state.civilianYoung += young - pop.S.young;
            state.civilianOld += old - pop.S.old;
            return boarded;
        }

        private void UnloadCivilians()
        {
            // 발사 뒤 씬 정리는 이미 엔딩에 기록한 승객을 다시 인구로 지급하지 않는다.
            if (Phase == RocketPhase.Launched || CivilianPassengers == 0) return;
            var pop = ColonyPopulation.Instance;
            if (pop == null) return;
            pop.ReturnRocketCivilians(CivilianPassengers, state.civilianYoung, state.civilianOld, Position);
            state.civilianPassengers = state.civilianYoung = state.civilianOld = 0;
        }
    }
}
