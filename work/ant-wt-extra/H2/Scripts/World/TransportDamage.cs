using System;
using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;

namespace AntColony.World
{
    // D07: 실제 ScienceLab 건조비 중 식량을 제외한 재료만 수리·잔해에 쓴다.
    public static class TransportDamage
    {
        public const float VehicleHealth = 400, AircraftHealth = 600;
        public const float DisabledBelow = .25f, SlowBelow = .5f, SlowMultiplier = .75f;
        public const float SalvageShare = .25f, SalvageSeconds = 10;
        public static int FoodCost(bool air) => air ? 80 : 50;
        public static int MaterialCost(bool air) => air ? 100 : 60;
        public static float Speed(float health, float max) => health < max * DisabledBelow ? 0 : health < max * SlowBelow ? SlowMultiplier : 1;
        // 부동소수점 경계 오차로 정확한 정수 비용을 한 개 더 청구하지 않는다.
        public static int RepairCost(bool air, double restoredFraction) => (int)Math.Ceiling(MaterialCost(air) * restoredFraction * GameBalance.RepairCostShare - .00001);
        internal static ExpeditionTransport FindTarget(Vector3 from, float radius) => WorldMapManager.Instance?.Transports
            .Where(t => t != null && !t.Walking && !t.IsDead && t.isActiveAndEnabled
                && (t.State == ExpeditionState.Home || t.State == ExpeditionState.Deployed) && (t.Position - from).sqrMagnitude <= radius * radius)
            .OrderBy(t => (t.Position - from).sqrMagnitude).FirstOrDefault();
    }

    [Serializable]
    public sealed class TransportDamageState
    {
        public bool present; // JsonUtility의 빈 inline 객체와 구저장을 구분한다.
        public float health, salvageProgress;
        public double repairedFraction;
        public bool salvaged;
        public string repairWorker, salvageWorker;
    }

    public partial class ExpeditionTransport
    {
        private double repairedFraction;
        private float salvageProgress;
        private bool salvaged;
        public override float MaxHealth => Walking ? base.MaxHealth : Aircraft ? TransportDamage.AircraftHealth : TransportDamage.VehicleHealth;
        public float TravelSpeedMultiplier => Walking ? 1 : TransportDamage.Speed(CurrentHealth, MaxHealth);
        public bool CanOperate => !IsDead && TravelSpeedMultiplier > 0 && !salvaged;
        public bool HasPilot => Walking || crew.Any(c => c != null && !c.IsDead && c.PersonalHealth > 0 && !c.Health.Downed);
        public bool IsWreck => !Walking && IsDead;
        public bool Salvaged => salvaged;
        public float SalvageRemaining => Mathf.Max(0, TransportDamage.SalvageSeconds - salvageProgress);

        internal TransportDamageState CaptureDamage() => new TransportDamageState { present = true, health = CurrentHealth,
            repairedFraction = repairedFraction, salvageProgress = salvageProgress, salvaged = salvaged,
            repairWorker = repairWorker != null && FieldWorker(repairWorker) ? repairWorker.PersonalState.id : null,
            salvageWorker = salvageWorker != null && FieldWorker(salvageWorker) ? salvageWorker.PersonalState.id : null };
        internal void RestoreDamage(TransportDamageState s)
        {
            bool present = s != null && s.present;
            RestoreHealth(present ? s.health : MaxHealth);
            repairedFraction = present ? s.repairedFraction : 0;
            salvageProgress = present ? s.salvageProgress : 0;
            salvaged = present && s.salvaged;
            if (IsWreck) name = "수송수단 잔해";
        }
        internal void RestoreDamageWorkers(TransportDamageState s)
        {
            if (s == null || !s.present) return;
            repairWorker = CommanderRoster.Instance.Commanders.FirstOrDefault(c => c.PersonalState.id == s.repairWorker);
            salvageWorker = CommanderRoster.Instance.Commanders.FirstOrDefault(c => c.PersonalState.id == s.salvageWorker);
        }

        private bool FieldWorker(CommanderAnt c) => c != null && c.IsColonyMember && !c.IsHostile && c.CanReceiveOrders && !c.IsCarrying && !c.IsConstructing
            && !withdraw.bodies.Any(j => j.carrierId == c.PersonalState.id)
            && (State == ExpeditionState.Home || State == ExpeditionState.Deployed) && (c.Position - Position).sqrMagnitude <= 9;

        private CommanderAnt repairWorker, salvageWorker;
        public bool FieldWorkActive => repairWorker != null || salvageWorker != null;
        public void CancelFieldWork() { repairWorker = null; salvageWorker = null; }
        public bool TryRepair(CommanderAnt c)
        {
            if (!FieldWorker(c) || !c.CanDoJob(CommanderJobs.Repair) || Walking || IsDead || CurrentHealth >= MaxHealth || IsWithdrawing || c.ServiceTarget == this || repairWorker != null && repairWorker != c) return false;
            c.CommandStop(); repairWorker = c; return true;
        }
        public bool TrySalvage(CommanderAnt c)
        {
            if (!FieldWorker(c) || !c.CanDoJob(CommanderJobs.Repair) || !IsWreck || salvaged || salvageWorker != null && salvageWorker != c) return false;
            c.CommandStop(); salvageWorker = c; return true;
        }
        public bool WorkRepair(CommanderAnt c, float seconds)
        {
            if (!FieldWorker(c) || !c.CanDoJob(CommanderJobs.Repair) || Walking || IsDead || CurrentHealth >= MaxHealth || !(seconds > 0) || float.IsInfinity(seconds)) return false;
            var rate = MaxHealth * GameBalance.RepairPerSecond * c.WorkRate(CommanderActivity.Building);
            if (!(rate > 0)) return false;
            var restored = Mathf.Min(MaxHealth, CurrentHealth + rate * seconds) - CurrentHealth;
            var fraction = repairedFraction + (double)restored / MaxHealth;
            var cost = TransportDamage.RepairCost(Aircraft, fraction) - TransportDamage.RepairCost(Aircraft, repairedFraction);
            if (cost > 0)
            {
                if (State == ExpeditionState.Deployed)
                {
                    if (GetCargo(ResourceType.Soil) < cost) return true; // 현장 보급 대기. 연료·식량 소비 없음.
                    cargo[ResourceType.Soil] = GetCargo(ResourceType.Soil) - cost;
                }
                else if (ResourceManager.Instance == null || !ResourceManager.Instance.TrySpend(ResourceType.Soil, cost, ResourceReason.Construction)) return true;
            }
            repairedFraction = fraction;
            RestoreHealth(CurrentHealth + restored);
            c.GainExperience(CommanderActivity.Building, restored / rate);
            return CurrentHealth < MaxHealth;
        }
        private void TickDamage(float seconds)
        {
            if (repairWorker != null && (!FieldWorker(repairWorker) || !WorkRepair(repairWorker, seconds))) repairWorker = null;
            if (salvageWorker == null) return;
            if (!FieldWorker(salvageWorker) || !IsWreck || salvaged) { salvageWorker = null; return; }
            salvageProgress = Mathf.Min(TransportDamage.SalvageSeconds, salvageProgress + seconds * salvageWorker.WorkRate(CommanderActivity.Building));
            if (salvageProgress < TransportDamage.SalvageSeconds) return;
            salvaged = true; salvageWorker = null;
            DiplomacyManager.DropFloor(ResourceType.Soil, Mathf.FloorToInt(TransportDamage.MaterialCost(Aircraft) * TransportDamage.SalvageShare), Position);
        }
        private void BecomeWreck()
        {
            CancelWithdrawal(); DropPendingLoads(); ClearCrewOrders(); Route?.Stop("수송수단 파괴"); repairWorker = null;
            // 생존자는 현장 소속을 유지해 다른 수송수단의 기존 탑승 명령으로 구조할 수 있다.
            foreach (var c in crew) if (c != null && c.IsCarrying) c.DropCargo();
            foreach (var c in crew) if (c != null && c.IsEmbarked) c.SetEmbarked(false, Position + Vector3.right * 3);
            foreach (var pair in cargo) if (pair.Value > 0) DiplomacyManager.DropFloor(pair.Key, pair.Value, Position);
            cargo.Clear(); UnloadBodies(Position);
            if (EquipmentCargo.Count > 0) EquipmentLoot.Drop(Position, EquipmentCargo);
            EquipmentCargo.Clear();
            if (Site != null && Site.Visitor == this) Site.Visitor = null;
            State = Site == null ? ExpeditionState.Home : ExpeditionState.Deployed; Remaining = 0;
            name = "수송수단 잔해";
        }
    }
}
