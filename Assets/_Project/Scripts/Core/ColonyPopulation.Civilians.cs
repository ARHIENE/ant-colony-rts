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
        public Vector3 position, destination;
        public bool housing, sheltered, moving, arrived;
        public float damage;
    }

    public sealed partial class ColonyPopulation
    {
        // 동원·건설 예약자는 기존 풀 소유권을 유지한다. 대피 중인 시민도 도착 전에는 시민 수에 포함한다.
        public int LocatedCivilians => S.young + Mathf.Max(0, S.old - S.agingDue) + (AntPool.Instance?.Free ?? 0);
        public System.Collections.Generic.IReadOnlyList<CivilianLocation> CivilianLocations => S.civilianLocations;

        // G26: 적 개체마다 매 프레임 부르면 주거 300동에서 10ms. 같은 프레임에 집·인원·위치 묶음이 그대로면 다시 맞추지 않는다.
        private (int frame, int homes, int groups, int counted, int located) lastSync = (-1, 0, 0, 0, 0);
        public void SyncCivilianLocations()
        {
            var homes = Housing.All.Where(h => h.isActiveAndEnabled).ToArray();
            var key = (Time.frameCount, homes.Length, S.civilianLocations.Count, S.civilianLocations.Sum(l => l.count), LocatedCivilians);
            if (key == lastSync) return;
            foreach (var location in S.civilianLocations.Where(l => l.housing))
                location.sheltered = homes.Any(h => (h.Position - location.position).sqrMagnitude < .01f);
            var difference = LocatedCivilians - S.civilianLocations.Sum(l => l.count);
            if (difference > 0)
            {
                foreach (var home in homes)
                {
                    var location = S.civilianLocations.FirstOrDefault(l => l.housing && (l.position - home.Position).sqrMagnitude < .01f);
                    if (location == null) location = AddCivilianLocation(home.Position, true, true);
                    var added = Mathf.Min(difference, Mathf.Max(0, home.Capacity - location.count));
                    location.count += added; difference -= added;
                    if (difference == 0) break;
                }
                if (difference > 0)
                {
                    var open = S.civilianLocations.FirstOrDefault(l => !l.housing && !l.moving && !l.arrived && !l.sheltered);
                    if (open == null) open = AddCivilianLocation(transform.position, false, false);
                    open.count += difference;
                }
            }
            else if (difference < 0)
            {
                // 외부 동원/탈주/노화와 집계를 맞출 때 이동 중 그룹은 마지막에 줄인다.
                foreach (var location in S.civilianLocations.OrderBy(l => l.moving ? 1 : 0))
                {
                    var removed = Mathf.Min(location.count, -difference);
                    location.count -= removed; difference += removed;
                    if (difference == 0) break;
                }
            }
            lastSync = (Time.frameCount, homes.Length, S.civilianLocations.Count, S.civilianLocations.Sum(l => l.count), LocatedCivilians);
        }

        private CivilianLocation AddCivilianLocation(Vector3 position, bool housing, bool sheltered)
        {
            var location = new CivilianLocation { id = S.nextCivilianLocation++, position = position, housing = housing, sheltered = sheltered };
            S.civilianLocations.Add(location); return location;
        }

        public bool SetCivilianProtection(int id, bool sheltered)
        {
            var location = S.civilianLocations.Find(l => l.id == id);
            if (location == null || location.moving) return false;
            location.sheltered = sheltered; return true;
        }

        public int DamageCivilians(int id, float amount)
        {
            if (!(amount > 0) || float.IsInfinity(amount)) return 0;
            SyncCivilianLocations();
            var location = S.civilianLocations.Find(l => l.id == id);
            if (location == null || location.sheltered || location.count == 0) return 0;
            location.damage = Mathf.Min(location.count * 10f, location.damage + amount);
            var killed = RemoveLocatedCivilians(Mathf.Min(location.count, Mathf.FloorToInt(location.damage / 10f)), true);
            location.count -= killed; location.damage = location.count == 0 ? 0 : location.damage - killed * 10f;
            if (killed > 0)
            {
                var remains = Corpse.All.FirstOrDefault(c => c != null && c.Available && c.Data.kind == CorpseKind.Ant
                    && c.Data.name == "시민" && (c.Position - location.position).sqrMagnitude < 1f);
                if (remains != null) remains.Data.count += killed;
                else Corpse.Spawn(new Corpse.State { kind = CorpseKind.Ant, name = "시민", count = killed, position = location.position });
            }
            return killed;
        }

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
                    || !FinitePosition(l.position) || !FinitePosition(l.destination) || l.moving && (l.count > 50 || l.housing || l.sheltered || l.arrived)
                    || l.arrived && (l.count > 50 || l.housing) || s.nextCivilianLocation <= l.id) return false;
            return true;
        }
    }
}
