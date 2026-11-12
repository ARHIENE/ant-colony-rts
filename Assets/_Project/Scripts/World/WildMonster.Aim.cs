using AntColony.Buildings;
using AntColony.Core;
using UnityEngine;
using UnityEngine.AI;

namespace AntColony.World
{
    // 2026-11-12 사용자 확정: 적 유형 이름은 1순위 표적만 뜻한다. 공격 가능한 1순위가 없으면 2·3순위로 내려가 계속 공격한다.
    // 일반 공격형(기본)은 기존 탐색 그대로 장수 → 벽·건물. 나머지 유형의 차순위 배열은 조정 가능한 초안이다.
    public enum EnemyAim { General, Building, Defense, Wall }

    public partial class WildMonster
    {
        public EnemyAim Aim { get; private set; }
        internal void ConfigureAim(EnemyAim aim) => Aim = aim;
        public static string AimLabel(EnemyAim aim) => aim == EnemyAim.Building ? "건물 공격" : aim == EnemyAim.Defense ? "방어시설 파괴"
            : aim == EnemyAim.Wall ? "벽 파괴" : "침입";

        public static bool IsDefenseFacility(IDamageable target) => target is AcidTower || target is AreaAcidTower || target is TrapPit;

        // 낮을수록 먼저 노린다. 유닛(장수·아군)은 건물이 아닌 대상이다.
        public static int AimRank(EnemyAim aim, IDamageable target)
        {
            var unit = !(target is BuildingBase);
            var barrier = IsBarrier(target);
            switch (aim)
            {
                case EnemyAim.Building: return unit ? 2 : barrier ? 1 : 0;
                case EnemyAim.Defense: return unit ? 3 : IsDefenseFacility(target) ? 0 : barrier ? 2 : 1;
                case EnemyAim.Wall: return unit ? 2 : barrier ? 0 : 1;
                default: return unit ? 0 : 1;
            }
        }

        // 구조물 우선형의 표적 탐색. 지금 표적이 새 후보보다 순위가 낮지 않으면 유지해 표적이 흔들리지 않게 한다.
        private IDamageable FindAimTarget()
        {
            var keep = TargetAlive(currentTarget) ? currentTarget : null;
            var keepRank = keep == null ? int.MaxValue : AimRank(Aim, keep);
            if (keepRank == 0) return keep;

            // 벽 파괴형은 아무 벽이나 두드리지 않는다. 본거지로 가는 길을 실제로 막는 벽만 1순위다.
            if (Aim == EnemyAim.Wall && BlockingBarrier() is BuildingBase gap) return gap;

            var aim = Aim;
            var bestRank = int.MaxValue;
            var manager = GameManager.Instance;
            var best = manager == null ? null : manager.FindPlayerBuildingByRank(Position,
                b => aim == EnemyAim.Wall && IsBarrier(b) ? int.MaxValue : AimRank(aim, b), out bestRank);
            if (best != null) return keepRank <= bestRank ? keep : best;
            // 건물이 하나도 없으면 마지막 순위인 장수를 노린다.
            return keep ?? FindNearestAnt();
        }

        // 일반 공격형이 벽을 치는 중에 실제로 닿을 수 있는 장수가 들어오면 장수로 돌아간다. 벽 너머 장수 때문에 표적이 오락가락하지 않는다.
        private IDamageable BarrierOrCommander()
            => FindNearestAnt() is IDamageable ant && Engageable(ant) ? ant : currentTarget;

        private bool Engageable(IDamageable target) => (target.Position - Position).sqrMagnitude <= attackRange * attackRange
            || agent != null && agent.enabled && agent.isOnNavMesh && AntColony.Map.NavRoute.Reachable(agent.nextPosition, target.Position);

        // 본거지까지 길이 벽에 막혀 있으면 길이 끊긴 지점의 벽·문을 돌려준다. 길이 열려 있으면 null(유효한 벽 없음 → 차순위).
        private BuildingBase BlockingBarrier()
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh) return null;
            siegePath ??= new NavMeshPath();
            if (!NavMesh.SamplePosition(HomeReturnPoint.Anchor, out var goal, 10f, NavMesh.AllAreas)
                || !NavMesh.CalculatePath(agent.nextPosition, goal.position, NavMesh.AllAreas, siegePath)
                || siegePath.status != NavMeshPathStatus.PathPartial || siegePath.corners.Length == 0
                || AntColony.Map.NavRoute.Reachable(agent.nextPosition, goal.position)) return null;
            return FindBarrier(siegePath.corners[siegePath.corners.Length - 1]);
        }
    }
}
