using AntColony.Core;
using AntColony.Units;
using UnityEngine;

namespace AntColony.Buildings
{
    // 지불한 수리량은 건물에 남아 장수 교대·저장으로 중복 결제되지 않는다.
    public sealed class BuildingRepair : MonoBehaviour
    {
        public float Credit { get; internal set; }
        public static BuildingRepair For(BuildingBase b) => b.GetComponent<BuildingRepair>() ?? b.gameObject.AddComponent<BuildingRepair>();
        public static bool Needed(BuildingBase b) => b != null && b.isActiveAndEnabled && !b.IsDead && b.CountsTowardPlayerDefeat
            && b.Data != null && (b.CurrentHealth < b.MaxHealth || b is TrapPit trap && !trap.Armed);
        public bool Work(CommanderAnt c, float seconds)
        {
            var b = GetComponent<BuildingBase>();
            if (!Needed(b) || !(seconds > 0) || float.IsInfinity(seconds)) return false;
            if (b is TrapPit trap && !trap.Armed) return trap.Repairer == c || trap.TryRepair(c);
            if (Credit <= 0)
            {
                var missing = b.MaxHealth - b.CurrentHealth;
                var fraction = missing / b.MaxHealth * GameBalance.RepairCostShare;
                if (ResourceManager.Instance == null || !ResourceManager.Instance.TrySpend(Mathf.CeilToInt(b.Data.foodCost * fraction),
                    Mathf.CeilToInt(b.Data.soilCost * fraction), Mathf.CeilToInt(b.Data.specialCost * fraction), reason: ResourceReason.Construction)) return false;
                Credit = missing;
            }
            var rate = b.MaxHealth * GameBalance.RepairPerSecond * c.WorkRate(CommanderActivity.Building);
            var restored = Mathf.Min(Credit, b.MaxHealth - b.CurrentHealth, rate * seconds);
            b.RestoreHealth(b.CurrentHealth + restored); Credit = Mathf.Max(0, Credit - restored);
            c.GainExperience(CommanderActivity.Building, restored / rate);
            return b.CurrentHealth < b.MaxHealth;
        }
    }
}
