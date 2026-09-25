using AntColony.Core;
using AntColony.Units;
using AntColony.World;
using UnityEngine;

namespace AntColony.Buildings
{
    // 함정 구덩이: 밟은 지상 적을 속박하고 파손된다. 장수 우클릭 수리(Soil 10, 4초) 또는 연구소 함정 3단계 자동 복구.
    public sealed class TrapPit : BuildingBase
    {
        public bool Armed { get; private set; } = true;
        public float BrokenSeconds { get; private set; }
        public float RepairProgress { get; private set; }
        public bool RepairPaid { get; private set; }
        public CommanderAnt Repairer { get; private set; }

        private void Update() => Tick(Time.deltaTime);

        public void Tick(float seconds)
        {
            if (!isActiveAndEnabled || IsDead || !(seconds > 0f) || float.IsInfinity(seconds)) return;
            if (Armed)
            {
                foreach (var monster in WildMonster.All)
                {
                    if (monster == null || monster.IsDead || monster.IsFlying
                        || (monster.Position - Position).sqrMagnitude > GameBalance.TrapTriggerRadius * GameBalance.TrapTriggerRadius) continue;
                    monster.Root(monster.GetComponent<AntColony.Boss.BossHealth>() != null ? GameBalance.TrapBossRootSeconds : GameBalance.TrapRootSeconds);
                    Spring();
                    return;
                }
                foreach (var boss in FindObjectsByType<AntColony.Boss.BossHealth>())
                    if (CombatTargeting.IsAlive(boss) && !CombatTargeting.IsAirborne(boss)
                        && (boss.Position - Position).sqrMagnitude <= GameBalance.TrapTriggerRadius * GameBalance.TrapTriggerRadius)
                    { boss.Root(GameBalance.TrapBossRootSeconds); Spring(); return; }
                foreach (var unit in AntUnitBase.Active)
                    if (unit is CommanderAnt c && c.IsHostile && !c.IsFlying && c.HasTroops && (c.Position - Position).sqrMagnitude <= GameBalance.TrapTriggerRadius * GameBalance.TrapTriggerRadius)
                    { c.Root(GameBalance.TrapRootSeconds); Spring(); return; }
                return;
            }
            BrokenSeconds += seconds;
            if (DefenseUpgrades.TrapAutoRepair && BrokenSeconds >= GameBalance.TrapAutoRepairSeconds) { Rearm(); return; }
            if (Repairer == null) return;
            if (!Repairer.CanChangeAllocation || Repairer.IsWorking || Repairer.IsInCombat || Repairer.IsAwayFromHome) { Repairer = null; return; }
            if (Vector3.Distance(Repairer.Position, Position) > 3f) return;
            RepairProgress += seconds;
            if (RepairProgress >= GameBalance.TrapRepairSeconds) Rearm();
        }

        // 장수 우클릭. 비용은 한 번만 내고, 수리 장수가 바뀌어도 진행도는 유지한다.
        public bool TryRepair(CommanderAnt commander)
        {
            if (!isActiveAndEnabled || Armed || IsDead || commander == null || !commander.CanChangeAllocation || commander.IsWorking || commander.IsAwayFromHome
                || !commander.CanReach(Position)) return false;
            if (!RepairPaid)
            {
                if (ResourceManager.Instance == null || !ResourceManager.Instance.TrySpend(0, GameBalance.TrapRepairSoil, reason: ResourceReason.Construction)) return false;
                RepairPaid = true;
            }
            Repairer = commander;
            commander.CommandMove(Position);
            return true;
        }

        private void Rearm() { Armed = true; BrokenSeconds = 0; RepairProgress = 0; RepairPaid = false; Repairer = null; }
        private void Spring() { Armed = false; BrokenSeconds = 0; RepairProgress = 0; RepairPaid = false; }

        internal void RestoreState(bool armed, float broken, float progress, bool paid)
        { Armed = armed; BrokenSeconds = Mathf.Max(0, broken); RepairProgress = Mathf.Max(0, progress); RepairPaid = paid && !armed; Repairer = null; }
    }
}
