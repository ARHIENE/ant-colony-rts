using AntColony.Core;
using AntColony.Data;
using AntColony.World;
using UnityEngine;

namespace AntColony.Buildings
{
    // 범위형 분사탑: 지상 대상 주변 반경 3에 피해. 개미산 정제·방어시설 연구소 보정을 받는다.
    public sealed class AreaAcidTower : BuildingBase
    {
        private float cooldown, searchTimer;
        public float Range => GameBalance.AreaTowerRange + DefenseUpgrades.RangeBonus;
        public float Damage => GameBalance.AreaTowerDamage * ScienceEffects.AcidTowerDamageMultiplier;
        public float Cooldown => cooldown;
        protected override bool UsesDefenseDurability => true;

        private void Update() => Tick(Time.deltaTime);

        public void Tick(float seconds)
        {
            if (!isActiveAndEnabled || IsDead || !(seconds > 0f) || float.IsInfinity(seconds)) return;
            cooldown = Mathf.Max(0, cooldown - seconds);
            searchTimer -= seconds;
            if (cooldown > 0 || searchTimer > 0) return;
            searchTimer = .25f;
            // 근접 역할 기준 탐색은 공중 대상을 제외한다(지상 전용).
            var target = CombatTargeting.FindNearestEnemy(Position, Range, UnitRole.Melee);
            foreach (var boss in FindObjectsByType<AntColony.Boss.BossHealth>())
                if (CombatTargeting.CanAttack(UnitRole.Melee, boss) && (boss.Position - Position).sqrMagnitude <= Range * Range
                    && (target == null || (boss.Position - Position).sqrMagnitude < (target.Position - Position).sqrMagnitude)) target = boss;
            if (target == null) return;
            cooldown = GameBalance.AreaTowerInterval;
            var center = target.Position;
            var radius = GameBalance.AreaTowerRadius * GameBalance.AreaTowerRadius;
            foreach (var m in new System.Collections.Generic.List<WildMonster>(WildMonster.All))
                if (m != null && !m.IsDead && !m.IsFlying && (m.Position - center).sqrMagnitude <= radius) m.TakeDamage(Damage);
            foreach (var boss in FindObjectsByType<AntColony.Boss.BossHealth>())
                if (CombatTargeting.CanAttack(UnitRole.Melee, boss) && (boss.Position - center).sqrMagnitude <= radius) boss.TakeDamage(Damage);
            foreach (var unit in new System.Collections.Generic.List<AntColony.Units.AntUnitBase>(AntColony.Units.AntUnitBase.Active))
                if (unit is AntColony.Units.CommanderAnt c && CombatTargeting.CanAttack(UnitRole.Melee, c) && (c.Position - center).sqrMagnitude <= radius) c.TakeDamage(Damage);
            if (!(target is WildMonster) && !(target is AntColony.Boss.BossHealth) && !(target is AntColony.Units.CommanderAnt)) target.TakeDamage(Damage);
        }

        internal void RestoreCooldown(float value) => cooldown = Mathf.Clamp(value, 0, GameBalance.AreaTowerInterval);
    }
}
