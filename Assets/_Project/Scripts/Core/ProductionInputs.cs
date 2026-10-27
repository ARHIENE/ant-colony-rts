using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Data;
using UnityEngine;

namespace AntColony.Core
{
    // 가공대·장비 제작대의 실제 도착 재료. 운반 중 예약은 WorkerOrder 복원 때 다시 연결한다.
    public sealed class ProductionInputs
    {
        [Serializable] public sealed class Stock { public ResourceType type; public int amount; public FoodLots lots; }
        public List<Stock> staged = new List<Stock>();
        private readonly Dictionary<object, (ResourceType type, int amount)> incoming = new Dictionary<object, (ResourceType, int)>();
        public int Amount(ResourceType type) => staged.Where(s => s.type == type).Sum(s => s.amount);
        private int Missing(ResourceType type, int need, object except = null) => Mathf.Max(0, need - Amount(type)
            - incoming.Where(p => p.Key != except && p.Value.type == type).Sum(p => p.Value.amount));
        public bool Ready(IEnumerable<(ResourceType type, int amount)> needs) => needs.All(n => Amount(n.type) >= n.amount);
        public bool Next(IEnumerable<(ResourceType type, int amount)> needs, out ResourceType type, out int amount)
        {
            foreach (var n in needs)
            {
                var missing = Missing(n.type, n.amount);
                if (missing <= 0 || (ResourceManager.Instance?.GetAmount(n.type) ?? 0) <= 0) continue;
                type = n.type; amount = missing; return true;
            }
            type = default; amount = 0; return false;
        }
        public bool Reserve(object courier, ResourceType type, int amount, IEnumerable<(ResourceType type, int amount)> needs)
        {
            var need = needs.Where(n => n.type == type).Sum(n => n.amount);
            if (courier == null || amount <= 0 || Missing(type, need, courier) < amount) return false;
            incoming[courier] = (type, amount); return true;
        }
        public void Abort(object courier) { if (courier != null) incoming.Remove(courier); }
        public int Deliver(object courier, ResourceType type, int amount, IEnumerable<(ResourceType type, int amount)> needs, FoodLots lots = null)
        {
            Abort(courier);
            var need = needs.Where(n => n.type == type).Sum(n => n.amount);
            var take = Mathf.Min(amount, Missing(type, need));
            if (take <= 0) return 0;
            var stock = staged.FirstOrDefault(s => s.type == type);
            if (stock == null) { stock = new Stock { type = type }; staged.Add(stock); }
            if (FoodLots.Perishable(type))
            {
                stock.lots ??= new FoodLots(); stock.lots.Match(stock.amount, type);
                var delivered = lots == null ? new FoodLots() : JsonUtility.FromJson<FoodLots>(JsonUtility.ToJson(lots));
                if (delivered.Total < take) delivered.Match(take, type); stock.lots.Add(delivered.Take(take));
            }
            stock.amount += take; return take;
        }
        public bool Consume(IEnumerable<(ResourceType type, int amount)> needs)
        {
            var bill = needs.Where(n => n.amount > 0).ToArray(); if (!Ready(bill)) return false;
            foreach (var n in bill) { var s = staged.First(s => s.type == n.type); s.lots?.Take(n.amount); s.amount -= n.amount; }
            staged.RemoveAll(s => s.amount == 0); return true;
        }
        public void Drop(Vector3 position)
        {
            foreach (var s in staged) if (s.amount > 0) AntColony.World.DiplomacyManager.DropFloor(s.type, s.amount, position + ResourceManager.Scatter(), s.lots);
            staged.Clear(); incoming.Clear();
        }
        public void Restore(List<Stock> stock) { staged = stock ?? new List<Stock>(); incoming.Clear(); }
        public void Age(float seconds, Vector3 position)
        {
            foreach (var s in staged.Where(s => FoodLots.Perishable(s.type)))
            {
                s.lots ??= new FoodLots(); s.lots.Match(s.amount, s.type);
                var spoiled = s.lots.Age(seconds); s.amount -= spoiled;
                if (spoiled > 0) { SpoilNotice.Add(s.type, spoiled); AntColony.World.DiplomacyManager.DropFloor(ResourceType.RottenFood, spoiled, position); }
            }
            staged.RemoveAll(s => s.amount == 0);
        }
    }
}
