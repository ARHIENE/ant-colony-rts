using AntColony.Buildings;
using AntColony.Core;
using AntColony.Map;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace AntColony.World
{
    public sealed partial class DiplomacyManager
    {
        public Vector3 HomecomingAnchor => Data.homecomingEntrySet ? Data.homecomingEntry : HomeReturnPoint.Anchor;

        internal static bool ValidHomecomingEntry(State s) => !s.homecomingEntrySet || ValidEntryCoordinates(s.homecomingEntry);
        internal static bool ValidEntryCoordinates(Vector3 p) => !float.IsNaN(p.x) && !float.IsNaN(p.y) && !float.IsNaN(p.z)
            && System.Math.Abs(p.x) <= 1000000 && System.Math.Abs(p.y) <= 1000000 && System.Math.Abs(p.z) <= 1000000;

        private bool TryHomecomingLanding(out Vector3 landing)
        {
            if (!Data.homecomingEntrySet) return HomeReturnPoint.TryGround(HomeReturnPoint.Anchor, out landing);
            return TryEntry(Data.homecomingEntry, out landing);
        }

        private static bool TryEntry(Vector3 candidate, out Vector3 landing)
        {
            landing = candidate;
            if (!ValidEntryCoordinates(candidate)) return false;
            var bounds = HomeMapBuilder.CurrentWorldBounds;
            if (bounds.size.x <= 0 || bounds.size.z <= 0 || !NavMesh.SamplePosition(candidate, out var hit, 1, NavMesh.AllAreas)) return false;
            if (hit.position.x < bounds.min.x || hit.position.x > bounds.max.x || hit.position.z < bounds.min.z || hit.position.z > bounds.max.z) return false;
            foreach (var collider in Physics.OverlapSphere(hit.position + Vector3.up, 1))
                if (collider.GetComponentInParent<BuildingBase>() != null) return false;
            landing = hit.position;
            return true;
        }

        public bool SetHomecomingEntry(Vector3 candidate, out string error)
        {
            if (!TryEntry(candidate, out var landing))
            { error = "본거지 지도 안의 건물 없는 보행 지점을 지정하세요(탐색 반경 1m)."; return false; }
            Data.homecomingEntry = landing; Data.homecomingEntrySet = true;
            error = null;
            return true;
        }

        public void ResetHomecomingEntry() { Data.homecomingEntrySet = false; Data.homecomingEntry = Vector3.zero; }

        private bool HomecomingThreatened() => ExpeditionTransport.HomeHasEnemies()
            || Data.homecomingEntrySet && WildMonster.All.Any(m => m != null && m.Threatening
                && (m.Position - Data.homecomingEntry).sqrMagnitude <= 25 * 25
                && CombatTargeting.CanAttack(AntColony.Data.UnitRole.Ranged, m));

        public string HomecomingWaitReason(Homecoming h)
        {
            if (!string.IsNullOrEmpty(h.to) || h.arrive > Data.elapsed) return "";
            if (HomecomingThreatened()) return "본거지·지정 진입점 주변에 적 잔류: 적을 물리친 뒤 귀환";
            if (!TryHomecomingLanding(out _)) return Data.homecomingEntrySet
                ? "지정 진입점 통행 불가·건물 점유: 다른 안전 지점 지정"
                : "본거지 경계의 보행 진입점 없음: 대체 안전 진입점 지정";
            return CommanderRoster.Instance == null ? "본거지 장수 명부 준비 대기" : "안전 진입 준비: 게임 재개 후 귀환";
        }
    }
}
