using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.UI;
using UnityEngine;

namespace AntColony.Units
{
    // D21~D24 전투 다이아. 수치는 기획 확정값, 세부 처리 선택은 docs/DECISIONS_1031_claude.md.
    public partial class CommanderAnt
    {
        public const float WrathSeconds = 20, WrathMaxSeconds = 120, WrathExhaustSeconds = 60, WrathRadius = 10, ArmorBreakRatio = .3f, PierceFalloff = .9f, PierceWidth = .75f;
        // 역린: 발동 중 공격·방어·공격 속도·이동 +200%, 끝난 뒤 탈진 동안 -50%.
        private float WrathFactor => PersonalState.wrathRemaining > 0 ? 3f : PersonalState.wrathExhaust > 0 ? .5f : 1f;
        private float PierceRangeMultiplier => Role == UnitRole.Ranged && traits.Has(CommanderTrait.DiamondPierce) ? 2f : 1f;
        private bool NormalMelee => Role != UnitRole.Ranged && (Weapon?.AttackRadius ?? 0) <= 0;

        // 참경: 일반 근접 공격은 대상 방어의 30%를 무시한다(방어를 빼는 대상만: 유닛·건물).
        private float ArmorBreakBonus(IDamageable target)
        {
            if (!NormalMelee || !traits.Has(CommanderTrait.DiamondArmorBreak)) return 0;
            var armor = target is AntUnitBase unit ? unit.Armor : target is BuildingBase building ? building.Armor : 0;
            return Mathf.Max(0, armor) * ArmorBreakRatio;
        }

        // 무간: 일반 근접은 같은 대상에 한 번 더(100%, 첫 타에 죽으면 생략). 엑스텐: 일반 원거리는 선 위 다른 적을 ×0.9씩 관통.
        private void DiamondFollowUp(IDamageable target, float damage)
        {
            if (NormalMelee && traits.Has(CommanderTrait.DiamondDoubleStrike) && CombatTargeting.IsAlive(target))
                StrikeWithEquipment(target, damage);
            if (Role != UnitRole.Ranged || (Weapon?.AttackRadius ?? 0) > 0 || !traits.Has(CommanderTrait.DiamondPierce)) return;
            var direction = target.Position - Position; direction.y = 0;
            if (direction.sqrMagnitude < .01f) return;
            direction.Normalize();
            var range = AttackRange;
            // ponytail: 장면 전체 대상 순회(기존 DamageArea와 같은 방식), 대상이 많아져 느려지면 공간 분할로 교체.
            var line = SceneObjects.All<MonoBehaviour>().OfType<IDamageable>()
                .Where(t => t != target && CombatTargeting.CanAttack(Role, t))
                .Select(t => (t, along: Vector3.Dot(t.Position - Position, direction)))
                .Where(x => x.along > 0 && x.along <= range && Vector3.Cross(direction, x.t.Position - Position).magnitude <= PierceWidth)
                .OrderBy(x => x.along).ToArray();
            foreach (var (t, _) in line)
            {
                damage *= PierceFalloff;
                var wasAlive = CombatTargeting.IsAlive(t);
                FormationDamage.Apply(t, damage, Position, true);
                if (wasAlive && !CombatTargeting.IsAlive(t)) GainExperience(CombatActivity, CommanderProgression.KillXp(t));
            }
        }

        // 역린(근접 다이아): 10m 안에서 아군 장수가 실제로 죽으면 20초 발동(탈진 중에도 재발동). 살해자 정보가 없어 사망 지점에 가장 가까운 적을 노린다.
        internal void TryWrath(Vector3 place)
        {
            if (!traits.Has(CommanderTrait.DiamondWrath) || !isActiveAndEnabled || IsDead || IsCaptive || IsEmbarked || IsDeparting
                || (place - Position).sqrMagnitude > WrathRadius * WrathRadius) return;
            // 발동 중 추가 사망은 +20초, 남은 시간 상한 120초(기획 결정).
            PersonalState.wrathRemaining = PersonalState.wrathRemaining > 0 ? Mathf.Min(WrathMaxSeconds, PersonalState.wrathRemaining + WrathSeconds) : WrathSeconds;
            PersonalState.wrathExhaust = 0;
            if (Agent != null) Agent.speed = MovementSpeed;
            ToastManager.Show(commanderName + ": 역린 발동");
            var killer = CombatTargeting.FindNearestEnemy(place, WrathRadius, IsFlying ? UnitRole.Flying : Role);
            if (killer != null && CanAttackTarget(killer)) CommandAttack(killer);
        }
    }
}
