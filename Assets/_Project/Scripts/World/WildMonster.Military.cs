using AntColony.Units;
using UnityEngine;
using UnityEngine.AI;

namespace AntColony.World
{
    public partial class WildMonster
    {
        public int MilitaryEra { get; internal set; }
        public int MilitaryMember { get; internal set; }
        public EquipmentItem MilitaryEquipment { get; internal set; }
        // 근접 호위와 원거리의 역할 분리. 좁은 길에서는 기존 사격을 계속한다.
        public static bool WantsMilitarySpacing(int era, float range, float distance) => era >= 2 && range > 2 && distance < range * .45f;
        private float nextSpacingCheck;
        private bool spacingPath;
        private bool TryMilitarySpacing(Vector3 target, float distance)
        {
            if (Allied || GuardBoss >= 0 || IsFlying || !CanMove() || !WantsMilitarySpacing(MilitaryEra, attackRange, distance)) { spacingPath = false; return false; }
            // 재계산을 기다리는 동안 기존 후퇴 경로와 공격 주기를 유지한다.
            if (Time.time < nextSpacingCheck)
            {
                if (!spacingPath || !(agent.hasPath || agent.pathPending)) return false;
                agent.stoppingDistance = 0;
                return true;
            }
            nextSpacingCheck = Time.time + .2f + perfPhase * .05f;
            spacingPath = false;
            var direction = Position - target; direction.y = 0;
            if (direction.sqrMagnitude < .01f) return false;
            var destination = Position + direction.normalized * 2;
            if (!NavMesh.SamplePosition(destination, out var hit, 1, NavMesh.AllAreas)) return false;
            siegePath ??= new NavMeshPath();
            if (!NavMesh.CalculatePath(Position, hit.position, NavMesh.AllAreas, siegePath) || siegePath.status != NavMeshPathStatus.PathComplete) return false;
            agent.stoppingDistance = 0;
            agent.SetPath(siegePath);
            spacingPath = true;
            pathRequested = false;
            return true;
        }
        private void DropMilitaryEquipment()
        {
            if (MilitaryEquipment == null) return;
            var item = MilitaryEquipment; MilitaryEquipment = null;
            EquipmentLoot.Drop(Position, new[] { item });
        }
    }
}
