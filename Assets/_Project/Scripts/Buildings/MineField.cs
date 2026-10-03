using System.Linq;
using AntColony.Core;
using AntColony.World;
using UnityEngine;

namespace AntColony.Buildings
{
    // 자폭개미 매설지: 적이 밟으면 반경 4 안의 지상 적에게 피해를 주고 사라진다. 동시 최대 8개.
    public sealed class MineField : BuildingBase
    {
        public static int Count => FindObjectsByType<MineField>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Count(m => m.gameObject.scene.IsValid() && !m.name.EndsWith("Template"));

        private void Update() => Tick();

        public void Tick()
        {
            if (!isActiveAndEnabled || IsDead) return;
            var trigger = GameBalance.MineTriggerRadius * GameBalance.MineTriggerRadius;
            var bosses = FindObjectsByType<AntColony.Boss.BossHealth>().Where(b => CombatTargeting.IsAlive(b) && !CombatTargeting.IsAirborne(b)).ToArray();
            var rebels = AntColony.Units.AntUnitBase.Active.OfType<AntColony.Units.CommanderAnt>().Where(c => c.IsHostile && CombatTargeting.IsAlive(c) && !c.IsFlying).ToArray();
            if (!WildMonster.All.Any(m => m != null && !m.IsDead && !m.IsFlying && (m.Position - Position).sqrMagnitude <= trigger)
                && !bosses.Any(b => (b.Position - Position).sqrMagnitude <= trigger) && !rebels.Any(c => (c.Position - Position).sqrMagnitude <= trigger)) return;
            var radius = GameBalance.MineRadius * GameBalance.MineRadius;
            var damage = GameBalance.MineDamage * DefenseUpgrades.FirepowerMultiplier;
            foreach (var m in WildMonster.All.ToArray())
                if (m != null && !m.IsDead && !m.IsFlying && (m.Position - Position).sqrMagnitude <= radius) m.TakeDamage(damage);
            foreach (var boss in bosses) if ((boss.Position - Position).sqrMagnitude <= radius) boss.TakeDamage(damage);
            foreach (var c in rebels) if ((c.Position - Position).sqrMagnitude <= radius) c.TakeDamage(damage);
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
