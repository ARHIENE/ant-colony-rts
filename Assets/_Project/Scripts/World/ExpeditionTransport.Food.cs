using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Core;
using AntColony.Data;

namespace AntColony.World
{
    public partial class ExpeditionTransport
    {
        [Serializable] public sealed class FoodCargo { public ResourceType type; public FoodLots lots = new FoodLots(); }
        internal List<FoodCargo> CargoFood = new List<FoodCargo>();
        internal List<FoodCargo> CaptureFoodCargo() { MatchCargoFood(); return CargoFood; }
        internal void RestoreFoodCargo(List<FoodCargo> values)
        {
            CargoFood = values == null ? new List<FoodCargo>() : UnityEngine.JsonUtility.FromJson<AntColony.Save.FoodCargoWrapper>(
                UnityEngine.JsonUtility.ToJson(new AntColony.Save.FoodCargoWrapper { values = values })).values;
            MatchCargoFood();
        }
        internal FoodLots IncomingFood;
        private int FoodTotal(ResourceType type) => GetCargo(type) + withdraw.loads.Where(l => l.type == type && string.IsNullOrEmpty(l.workerId)).Sum(l => l.amount);
        private FoodLots CargoLots(ResourceType type)
        {
            var entry = CargoFood.Find(e => e.type == type);
            if (entry == null) { entry = new FoodCargo { type = type }; CargoFood.Add(entry); }
            return entry.lots;
        }
        internal void MatchCargoFood()
        {
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
                if (FoodLots.Perishable(type)) CargoLots(type).Match(FoodTotal(type), type);
        }
        internal void AgeCargoFood(float seconds)
        {
            if (!(seconds > 0) || float.IsInfinity(seconds)) return;
            MatchCargoFood();
            foreach (var e in CargoFood)
            {
                var spoiled = e.lots.Age(seconds); if (spoiled == 0) continue;
                var fromCargo = Math.Min(spoiled, GetCargo(e.type)); cargo[e.type] = GetCargo(e.type) - fromCargo;
                var pending = spoiled - fromCargo;
                foreach (var load in withdraw.loads.Where(l => l.type == e.type && string.IsNullOrEmpty(l.workerId)).ToArray())
                { var n = Math.Min(pending, load.amount); load.amount -= n; pending -= n; if (load.amount == 0) withdraw.loads.Remove(load); }
                cargo[ResourceType.RottenFood] = GetCargo(ResourceType.RottenFood) + spoiled;
                SpoilNotice.Add(e.type, spoiled);
            }
        }
        private void DropPendingFood()
        {
            MatchCargoFood();
            foreach (var load in withdraw.loads.Where(l => string.IsNullOrEmpty(l.workerId) && FoodLots.Perishable(l.type)).ToArray())
            {
                DiplomacyManager.DropFloor(load.type, load.amount, Position, TakeCargoFood(load.type, load.amount));
                withdraw.loads.Remove(load);
            }
        }
        internal FoodLots TakeCargoFood(ResourceType type, int amount) => FoodLots.Perishable(type) ? CargoLots(type).Take(amount) : null;
        internal void CreditCargoFood(ResourceType type, int amount)
        {
            if (!FoodLots.Perishable(type) || amount <= 0) return;
            // 대기 화물은 이미 묶음에 들어 있다. 실제 운반 장수의 물량만 신규 적재한다.
            var lots = CargoLots(type); var missing = FoodTotal(type) - lots.Total;
            if (missing > 0 && IncomingFood != null) lots.Add(IncomingFood.Take(Math.Min(missing, amount)));
            lots.Match(FoodTotal(type), type);
        }
        internal static bool ValidFoodCargo(List<FoodCargo> foods) => foods == null || foods.Count <= Enum.GetValues(typeof(ResourceType)).Length
            && foods.All(e => e != null && FoodLots.Perishable(e.type) && e.lots?.Valid == true)
            && foods.Select(e => e.type).Distinct().Count() == foods.Count;
        internal static bool ValidFoodAmounts(AntColony.Save.TransportDto dto)
        {
            // 구저장만 기본 보존 시간/숙련5로 생성한다. JsonUtility는 없는 목록을 빈 목록으로 읽으므로 빈 목록도 구저장으로 본다.
            if (dto.cargoFreshness == null || dto.cargoFreshness.Count == 0) return true;
            if (!ValidFoodCargo(dto.cargoFreshness)) return false;
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                if (!FoodLots.Perishable(type)) continue;
                long expected = dto.cargoAmounts != null && (int)type < dto.cargoAmounts.Length ? dto.cargoAmounts[(int)type] : type == ResourceType.Food ? dto.cargoFood : 0;
                expected += dto.withdrawal?.loads?.Where(l => l != null && l.type == type && string.IsNullOrEmpty(l.workerId)).Sum(l => (long)l.amount) ?? 0;
                if ((dto.cargoFreshness.FirstOrDefault(e => e.type == type)?.lots.lots.Sum(l => (long)l.amount) ?? 0) != expected) return false;
            }
            return true;
        }
    }
}
