using System;
using System.Linq;
using AntColony.Buildings;
using AntColony.World;
using UnityEngine;

namespace AntColony.Core
{
    [Serializable] public sealed class CivilianLocation
    {
        public int id, count;
        public Vector3 position, destination, housingDestination;
        public bool housing, sheltered, moving, arrived;
        public bool temporary, rehousing;
        public float damage;
    }

    public sealed partial class ColonyPopulation
    {
        // 동원·건설 예약자는 기존 풀 소유권을 유지한다. 대피 중인 시민도 도착 전에는 시민 수에 포함한다.
        public int LocatedCivilians => 0;
        public System.Collections.Generic.IReadOnlyList<CivilianLocation> CivilianLocations => System.Array.Empty<CivilianLocation>();

        // G26: 적 개체마다 매 프레임 부르면 주거 300동에서 10ms. 같은 프레임에 집·인원·위치 묶음이 그대로면 다시 맞추지 않는다.
        private (int frame, int homes, int groups, int counted, int located) lastSync = (-1, 0, 0, 0, 0);
        public void SyncCivilianLocations()
        {  }

        private CivilianLocation AddCivilianLocation(Vector3 position, bool housing, bool sheltered)
        {
            var location = new CivilianLocation { id = S.nextCivilianLocation++, position = position, housing = housing, sheltered = sheltered };
            S.civilianLocations.Add(location); return location;
        }

        public bool SetCivilianProtection(int id, bool sheltered)
        { return false; }

        public int DamageCivilians(int id, float amount)
        { return 0; }

        private int RemoveLocatedCivilians(int count, bool died)
        {
            var young = Mathf.Min(count, S.young); S.young -= young; count -= young;
            var free = AntPool.Instance?.RemoveFree(count, died) ?? 0; count -= free;
            var old = Mathf.Min(count, Mathf.Max(0, S.old - S.agingDue)); S.old -= old;
            if (died && young + old > 0) CampaignHistory.Ants(young + old, true);
            EnsureAges(); return young + free + old;
        }

        private static bool FinitePosition(Vector3 p) => !float.IsNaN(p.x) && !float.IsInfinity(p.x)
            && !float.IsNaN(p.y) && !float.IsInfinity(p.y) && !float.IsNaN(p.z) && !float.IsInfinity(p.z);

        private static bool ValidCivilianState(State s)
        {
            if (s.civilianLocations == null) return true; // v19 저장의 추가 필드 기본값.
            var ids = new System.Collections.Generic.HashSet<int>();
            foreach (var l in s.civilianLocations)
                if (l == null || l.id <= 0 || !ids.Add(l.id) || l.count < 0 || !FiniteRange(l.damage, 9.999999f)
                    || l.temporary && (l.housing || l.rehousing) || l.rehousing && (l.housing || !l.moving && !l.arrived)
                    || l.rehousing && !FinitePosition(l.housingDestination)
                    || !FinitePosition(l.position) || !FinitePosition(l.destination) || l.moving && (l.count > 50 || l.housing || l.sheltered || l.arrived)
                    || l.arrived && (l.count > 50 || l.housing) || s.nextCivilianLocation <= l.id) return false;
            return true;
        }
    }
}
