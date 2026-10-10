using AntColony.Core;
using AntColony.Units;
using AntColony.World;
using UnityEngine;

namespace AntColony.Buildings
{
    // 끈끈이 함정(TrapPit): 밟은 지상 적을 속박하고 파손된다. 가시 함정(SpikeTrap): 반경 안 지상 적에 반복 피해, 정해진 횟수 뒤 파손.
    // 둘 다 장수 우클릭 수리(Soil 10, 4초) 또는 연구소 함정 3단계 자동 복구.
    public sealed class TrapPit : BuildingBase
    {
        public bool Armed { get; private set; } = true;
        public bool IsSpike => Data != null && Data.kind == AntColony.Data.BuildingKind.SpikeTrap;
        public int SpikeHits { get; private set; } // 가시 함정이 이번 장전에서 피해를 준 횟수
        private float spikeCooldown;
        public float BrokenSeconds { get; private set; }
        public float RepairProgress { get; private set; }
        public bool RepairPaid { get; private set; }
        public CommanderAnt Repairer { get; private set; }

        private void Update() => Tick(Time.deltaTime);

        public void Tick(float seconds)
        {
            if (!isActiveAndEnabled || IsDead || !(seconds > 0f) || float.IsInfinity(seconds)) return;
            if (Armed && IsSpike) { TickSpikes(seconds); return; }
            if (Armed)
            {
                foreach (var monster in WildMonster.All)
                {
                    if (monster == null || monster.IsDead || monster.IsFlying || monster.Docile
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
            if (!Repairer.CivilianWorkReady || Repairer.ServiceTarget != this || !Repairer.CanChangeAllocation || Repairer.IsWorking || Repairer.IsInCombat || Repairer.IsAwayFromHome) { Repairer = null; return; }
            if (Vector3.Distance(Repairer.Position, Position) > 7f) return;
            RepairProgress += seconds * Repairer.WorkRate(CommanderActivity.Building);
            Repairer.GainExperience(CommanderActivity.Building, seconds);
            if (RepairProgress >= GameBalance.TrapRepairSeconds) Rearm();
        }

        // 가시: 간격마다 반경 안 지상 적 전부(야생 몬스터·보스·적 장수)에 피해. 한 명이라도 맞으면 1회로 센다.
        private void TickSpikes(float seconds)
        {
            spikeCooldown -= seconds;
            if (spikeCooldown > 0) return;
            var r = GameBalance.TrapTriggerRadius * GameBalance.TrapTriggerRadius; var hit = false;
            foreach (var monster in new System.Collections.Generic.List<WildMonster>(WildMonster.All))
                if (monster != null && !monster.IsDead && !monster.IsFlying && !monster.Docile && (monster.Position - Position).sqrMagnitude <= r) { monster.TakeDamage(GameBalance.SpikeTrapDamage); hit = true; }
            foreach (var boss in FindObjectsByType<AntColony.Boss.BossHealth>())
                if (CombatTargeting.IsAlive(boss) && !CombatTargeting.IsAirborne(boss) && boss.GetComponent<WildMonster>() == null
                    && (boss.Position - Position).sqrMagnitude <= r) { boss.TakeDamage(GameBalance.SpikeTrapDamage); hit = true; }
            foreach (var unit in new System.Collections.Generic.List<AntUnitBase>(AntUnitBase.Active))
                if (unit is CommanderAnt c && c.IsHostile && !c.IsDead && !c.IsFlying && (c.Position - Position).sqrMagnitude <= r) { c.TakeDamage(GameBalance.SpikeTrapDamage); hit = true; }
            if (!hit) return;
            spikeCooldown = GameBalance.SpikeTrapInterval;
            if (++SpikeHits >= GameBalance.SpikeTrapHits) Spring();
        }

        // 장수 우클릭. 비용은 한 번만 내고, 수리 장수가 바뀌어도 진행도는 유지한다.
        public bool TryRepair(CommanderAnt commander)
        {
            if (!isActiveAndEnabled || Armed || IsDead || commander == null || !commander.CanChangeAllocation || commander.IsWorking || commander.IsAwayFromHome
                || !commander.CivilianWorkReady || !commander.CanDoJob(CommanderJobs.Repair) || !commander.CanReach(Position)) return false;
            if (!RepairPaid)
            {
                if (ResourceManager.Instance == null || !ResourceManager.Instance.TrySpend(0, GameBalance.TrapRepairSoil, reason: ResourceReason.Construction)) return false;
                RepairPaid = true;
            }
            if (commander.ServiceTarget != this && !commander.StartService(this, CommanderJobs.Repair)) return false;
            Repairer = commander;
            return true;
        }

        private void Rearm() { Armed = true; BrokenSeconds = 0; RepairProgress = 0; RepairPaid = false; Repairer = null; SpikeHits = 0; spikeCooldown = 0; }
        private void Spring() { Armed = false; BrokenSeconds = 0; RepairProgress = 0; RepairPaid = false; }

        internal void RestoreState(bool armed, float broken, float progress, bool paid, int spikeHits)
        {
            Armed = armed; BrokenSeconds = Mathf.Max(0, broken); RepairProgress = Mathf.Max(0, progress); RepairPaid = paid && !armed; Repairer = null;
            SpikeHits = armed ? Mathf.Clamp(spikeHits, 0, GameBalance.SpikeTrapHits - 1) : 0;
        }
    }
}
