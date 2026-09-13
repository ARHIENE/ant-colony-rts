using AntColony.Data;
using AntColony.Buildings;
using AntColony.Units;
using UnityEngine;

namespace AntColony.Core
{
    // 역할 변경에 따라 현재 공중 상태를 조회한다.
    public interface IAirborne
    {
        bool IsAirborne { get; }
    }

    // 표준 RTS형 비행 규칙: 공중 대상 여부와 역할별 공격 가능 여부를 여기 한 곳에서만 판정한다.
    public static class CombatTargeting
    {
        public static IDamageable FindNearestEnemy(Vector3 from, float radius, UnitRole role)
        {
            var monster = World.WildMonster.FindNearest(from, radius, role);
            var building = World.EnemyColony.FindNearestBuilding(from, radius, role);
            if (monster == null) return building;
            if (building == null) return monster;
            return (monster.Position - from).sqrMagnitude <= (building.Position - from).sqrMagnitude
                ? (IDamageable)monster : building;
        }

        public static bool IsAlive(IDamageable target)
        {
            if (target is CommanderAnt commander && !commander.HasTroops) return false;
            return target is Behaviour behaviour && behaviour != null && behaviour.isActiveAndEnabled && !target.IsDead;
        }

        public static bool IsAirborne(IDamageable target)
        {
            return target is IAirborne airborne && airborne.IsAirborne
                || target is AntColony.World.WildMonster monster && monster.IsFlying;
        }

        // 지상 대상은 모든 전투 역할이 공격할 수 있고, 공중 대상은 Ranged와 Flying만 공격할 수 있다.
        public static bool CanAttack(UnitRole role, IDamageable target)
        {
            if (!IsAlive(target)) return false;
            if (target is AntUnitBase) return false;
            if (target is BuildingBase building && building.CountsTowardPlayerDefeat) return false;
            if (role == UnitRole.Worker) return false;
            if (!IsAirborne(target)) return true;
            return role == UnitRole.Ranged || role == UnitRole.Flying;
        }
    }
}
