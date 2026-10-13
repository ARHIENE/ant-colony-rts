using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Buildings;
using AntColony.Data;
using UnityEngine;

namespace AntColony.Core
{
    // 소굴 자원 창구. 2026-10-12부터 보유량은 창고 칸(StoreBin)에 실제로 담긴 물건의 합계다(산소미포함식).
    // 한도(capacities)는 기존대로 식량·재료(종류별 같은 한도)·특수 3칸이며, 창고 건물 몫을 뺀 나머지가 비축더미 칸의 한도다.
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance { get; private set; }

        [SerializeField] private int startingFood = 100;
        [SerializeField] private int startingSoil = 50;
        [SerializeField] private int baseFoodCapacity = 200;
        [SerializeField] private int baseSoilCapacity = 200;
        [SerializeField, Min(0)] private int baseSpecialCapacity = 100;

        private readonly Dictionary<ResourceType, int> capacities = new Dictionary<ResourceType, int>();
        private readonly List<StoreBin> bins = new List<StoreBin>();
        private readonly Dictionary<object, (StoreBin bin, ResourceType type, int amount)> reservations = new Dictionary<object, (StoreBin, ResourceType, int)>();
        public StoreBin BaseBin { get; private set; }
        public IReadOnlyList<StoreBin> Bins => bins;

        public event Action OnResourcesChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            capacities[ResourceType.Food] = baseFoodCapacity;
            capacities[ResourceType.Soil] = baseSoilCapacity;
            capacities[ResourceType.Special] = baseSpecialCapacity;
            BaseBin = new StoreBin { capacity = BaseCapacity };
            bins.Add(BaseBin);
            BaseBin.amounts[(int)ResourceType.Food] = Mathf.Min(startingFood, baseFoodCapacity);
            BaseBin.amounts[(int)ResourceType.Soil] = Mathf.Min(startingSoil, baseSoilCapacity);
        }

        internal static Vector3 HomePosition
        {
            get
            {
                var pile = FindObjectsByType<Stockpile>(FindObjectsSortMode.None).FirstOrDefault(s => s.isActiveAndEnabled && !s.IsDead);
                return pile != null ? pile.Position : AntColony.World.WorldMapManager.Instance?.HomePosition ?? Vector3.zero;
            }
        }
        private static ResourceType Key(ResourceType t) => t.IsFood() ? ResourceType.Food : t.IsMaterial() ? ResourceType.Soil : ResourceType.Special;
        // 비축더미 칸 한도 = 전체 한도 − 창고 건물이 늘린 한도.
        private int BaseCapacity(ResourceType t) => GetCapacity(t) - bins.Where(b => !b.IsBase && b.Host is Storage).Sum(b => ((Storage)b.Host).RegisteredCapacity(t));

        // 창고 건물이 생기면 칸을 등록한다. 칸이 사라질 때 물건은 Unregister가 바닥에 내놓을지 정한다.
        internal StoreBin Register(Storage host)
        {
            var bin = new StoreBin { Host = host, capacity = host.RegisteredCapacity };
            bins.Add(bin); OnResourcesChanged?.Invoke(); return bin;
        }
        internal void Unregister(StoreBin bin, bool dropContents)
        {
            if (bin == null || bin == BaseBin || !bins.Remove(bin)) return;
            foreach (var k in reservations.Where(r => r.Value.bin == bin).Select(r => r.Key).ToList()) reservations.Remove(k);
            if (dropContents) foreach (var t in bin.Stocked.ToList()) { AntColony.World.DiplomacyManager.DropFloor(t, bin.amounts[(int)t], bin.Position + Scatter()); bin.amounts[(int)t] = 0; }
            OnResourcesChanged?.Invoke();
        }
        internal static Vector3 Scatter() => new Vector3(UnityEngine.Random.Range(-2.5f, 2.5f), 0, UnityEngine.Random.Range(-2.5f, 2.5f));

        // 저장 복원 전용. 상한을 먼저 세우고 보유량을 칸들에 다시 나눠 담는다(칸별 저장값은 RestoreBins가 덮어쓴다).
        internal void RestoreState(int food, int soil, int special, int foodCapacity, int soilCapacity, int specialCapacity)
        {
            capacities[ResourceType.Food] = Mathf.Max(0, foodCapacity);
            capacities[ResourceType.Soil] = Mathf.Max(0, soilCapacity);
            capacities[ResourceType.Special] = Mathf.Max(0, specialCapacity);
            foreach (var b in bins) Array.Clear(b.amounts, 0, b.amounts.Length);
            reservations.Clear();
            Fill(ResourceType.Food, food); Fill(ResourceType.Soil, soil); Fill(ResourceType.Special, special);
            OnResourcesChanged?.Invoke();
        }
        // 흙 외 재료 보유량(저장 v16). 순서는 ResourceLabels.Materials에서 흙을 뺀 것.
        internal int[] CaptureMaterials() => OtherMaterials.Select(GetAmount).ToArray();
        internal void RestoreMaterials(int[] values)
        {
            for (int i = 0; i < OtherMaterials.Length; i++)
            {
                foreach (var b in bins) b.amounts[(int)OtherMaterials[i]] = 0;
                Fill(OtherMaterials[i], values != null && i < values.Length ? values[i] : 0);
            }
            OnResourcesChanged?.Invoke();
        }
        internal static readonly ResourceType[] OtherMaterials = Array.FindAll(ResourceLabels.Materials, t => t != ResourceType.Soil);
        // 칸별 저장값(v17)을 그대로 되돌린다. 이전 저장(null)은 RestoreState가 나눠 담은 그대로 둔다.
        internal void RestoreBins(StoreBin.Dto baseBin, IEnumerable<(Storage storage, StoreBin.Dto dto)> storages)
        {
            if (baseBin == null) { Redistribute(); return; }
            // 총량은 RestoreState·RestoreMaterials가 넣은 값(저장의 식량·재료·특수 칸)이 기준이다. 칸 값을 덮은 뒤 차이(파괴된 창고 등)를 맞춘다.
            var totals = Enumerable.Range(0, StoreBin.TypeCount).Select(i => bins.Sum(b => b.amounts[i])).ToArray();
            BaseBin.Apply(baseBin);
            foreach (var (storage, dto) in storages) if (storage.Bin != null && dto != null) storage.Bin.Apply(dto);
            for (int i = 0; i < totals.Length; i++)
            {
                var t = (ResourceType)i; if (t == ResourceType.RottenFood) continue; // 썩은 식품은 칸 값만 있다
                var diff = totals[i] - bins.Sum(b => b.amounts[i]);
                if (diff > 0) Fill(t, diff); else if (diff < 0) Take(t, -diff);
            }
            OnResourcesChanged?.Invoke();
        }
        // 이전 저장 이관: 창고가 모두 복원된 뒤 총량을 우선순위대로 다시 담는다. 한도를 넘친 몫은 예전처럼 버린다.
        private void Redistribute()
        {
            var totals = Enumerable.Range(0, StoreBin.TypeCount).Select(i => bins.Sum(b => b.amounts[i])).ToArray();
            foreach (var b in bins) Array.Clear(b.amounts, 0, b.amounts.Length);
            for (int i = 0; i < totals.Length; i++) Fill((ResourceType)i, totals[i]);
            OnResourcesChanged?.Invoke();
        }
        private int Fill(ResourceType t, int amount)
        {
            int placed = 0;
            foreach (var b in Destinations(t, HomePosition, null))
            {
                if (amount - placed <= 0) break;
                var n = Mathf.Min(amount - placed, b.Free(t)); b.amounts[(int)t] += n; placed += n;
            }
            return placed;
        }

        // 보유량은 사용할 수 있는 칸(개보수 중이거나 부서진 창고 제외)의 합계.
        public int GetAmount(ResourceType type) => bins.Where(b => b.Usable).Sum(b => b.amounts[(int)type]);
        // 재료는 종류마다 같은 한도(창고 '재료' 한도)를 쓴다.
        public int GetCapacity(ResourceType type) => capacities.TryGetValue(Key(type), out var value) ? value : 0;
        // 이 물건을 더 받을 수 있는 칸들의 남은 공간(허용 품목·예약 반영).
        public int Room(ResourceType type, object owner = null) => bins.Where(b => b.Usable && b.Allows(type)).Sum(b => b.Free(type, owner));

        public void AddCapacity(ResourceType type, int amount)
        {
            type = Key(type);
            capacities[type] = Mathf.Max(0, GetCapacity(type) + amount);
            OnResourcesChanged?.Invoke();
        }

        // 바닥 물자의 입고 목적지: 허용·빈 공간이 있는 칸 중 우선순위 높은 곳 → 가까운 곳.
        public IEnumerable<StoreBin> Destinations(ResourceType type, Vector3 from, object owner)
            => bins.Where(b => b.Accepts(type, owner)).OrderByDescending(b => b.priority).ThenBy(b => (b.Position - from).sqrMagnitude);
        public StoreBin BinOf(BuildingBase host) => host is Storage s ? s.Bin : host is Stockpile ? BaseBin : null;

        // 운반 목적지 공간 예약(중복 운반·초과 적재 방지). owner당 하나.
        public void Reserve(object owner, StoreBin bin, ResourceType type, int amount)
        {
            if (owner == null) return;
            if (bin == null || amount <= 0) reservations.Remove(owner); else reservations[owner] = (bin, type, amount);
        }
        internal int ReservedFor(StoreBin bin, ResourceType type, object except)
            => reservations.Where(r => r.Key != except && r.Value.bin == bin && r.Value.type == type).Sum(r => r.Value.amount);

        // 들어갈 칸에 담고 넘친 몫은 바닥(near 주변, 없으면 비축더미 주변)에 둔다. 담은 양을 돌려준다.
        public int Add(ResourceType type, int amount, ResourceReason reason = ResourceReason.Gathering, Vector3? near = null)
        {
            if (amount <= 0) return 0;
            var from = near ?? HomePosition; int placed = 0;
            foreach (var b in Destinations(type, from, null).ToList())
            {
                if (amount - placed <= 0) break;
                var n = Mathf.Min(amount - placed, b.Free(type)); b.amounts[(int)type] += n; placed += n;
            }
            CampaignHistory.Resource(type, placed, false, reason);
            if (amount > placed) AntColony.World.DiplomacyManager.DropFloor(type, amount - placed, from + Scatter());
            OnResourcesChanged?.Invoke();
            return placed;
        }
        // 운반 장수가 특정 칸에 넣는다. 들어간 양을 돌려준다(남은 짐은 장수가 다른 칸을 찾거나 내려놓는다).
        public int AddTo(StoreBin bin, ResourceType type, int amount, object owner = null, ResourceReason reason = ResourceReason.Gathering, FoodLots lots = null)
        {
            if (bin == null || amount <= 0 || !bin.Usable || !bin.Allows(type)) return 0;
            var n = Mathf.Min(amount, bin.Free(type, owner));
            if (n <= 0) return 0;
            var have = bin.Lots(type); // 넣기 전 신선도를 맞춰 둔다
            bin.amounts[(int)type] += n;
            if (have != null) { if (lots != null) have.Add(lots.Take(n)); have.Match(bin.amounts[(int)type], type); } // 들고 온 물량의 신선도를 그대로
            CampaignHistory.Resource(type, n, false, reason);
            OnResourcesChanged?.Invoke();
            return n;
        }
        // 칸에서 꺼낸다(쓰거나 바닥에 내놓을 때). 꺼낸 양을 돌려준다.
        public int TakeFrom(StoreBin bin, ResourceType type, int amount) => TakeFrom(bin, type, amount, out _);
        // 꺼낸 물량의 신선도도 함께 돌려준다(부패 식품만).
        public int TakeFrom(StoreBin bin, ResourceType type, int amount, out FoodLots lots)
        {
            lots = null;
            if (bin == null || amount <= 0) return 0;
            var n = Mathf.Min(amount, bin.amounts[(int)type]);
            lots = bin.Lots(type)?.Take(n);
            bin.amounts[(int)type] -= n;
            if (n > 0) OnResourcesChanged?.Invoke();
            return n;
        }
        // 창고 조작: 품목 허용을 끄면 그 품목 재고 전부를 창고 주변 바닥에 내놓는다. 품목별 꺼내기·전체 비우기도 바닥으로.
        public void SetAllowed(StoreBin bin, ResourceType type, bool on)
        {
            if (bin == null || bin.Allows(type) == on) return;
            bin.allowed = on ? bin.allowed | StoreBin.Bit(type) : bin.allowed & ~StoreBin.Bit(type);
            if (!on) Eject(bin, type);
            OnResourcesChanged?.Invoke();
        }
        public void SetPriority(StoreBin bin, int level) { if (bin != null) { bin.priority = Mathf.Clamp(level, AntColony.Units.WorkPriorities.Min, AntColony.Units.WorkPriorities.Max); OnResourcesChanged?.Invoke(); } }
        public int Eject(StoreBin bin, ResourceType type)
        {
            var n = TakeFrom(bin, type, bin?.amounts[(int)type] ?? 0, out var lots);
            if (n > 0) AntColony.World.DiplomacyManager.DropFloor(type, n, bin.Position + Scatter(), lots);
            return n;
        }

        // 부패(2026-10-12): 1초마다 칸 안 식품의 신선도를 줄이고, 다 된 물량은 썩은 식품이 된다.
        // 칸이 썩은 식품을 허용하지 않거나 가득 차면 즉시 주변 바닥에 나온다. 알림은 종류별 수량을 묶어서 띄운다.
        private float spoilTimer;
        private void Update()
        {
            spoilTimer += Time.deltaTime; if (spoilTimer < 1f) return;
            Spoil(spoilTimer); spoilTimer = 0;
        }
        public void Spoil(float seconds)
        {
            bool changed = false; SpoilNotice.Tick(seconds);
            foreach (var b in bins)
                foreach (var t in b.Stocked.Where(FoodLots.Perishable).ToList())
                {
                    var rotten = b.Lots(t).Age(seconds * b.DecayRate);
                    if (rotten <= 0) continue;
                    b.amounts[(int)t] -= rotten; changed = true; SpoilNotice.Add(t, rotten);
                    var keep = b.Allows(ResourceType.RottenFood) ? Mathf.Min(rotten, b.Free(ResourceType.RottenFood)) : 0;
                    b.amounts[(int)ResourceType.RottenFood] += keep;
                    if (rotten > keep) AntColony.World.DiplomacyManager.DropFloor(ResourceType.RottenFood, rotten - keep, b.Position + Scatter());
                }
            if (changed) OnResourcesChanged?.Invoke();
        }
        public void EjectAll(StoreBin bin) { if (bin != null) foreach (var t in bin.Stocked.ToList()) Eject(bin, t); }

        public bool CanAfford(int foodCost, int soilCost, int specialCost = 0)
        {
            return foodCost >= 0 && soilCost >= 0 && specialCost >= 0 && GetAmount(ResourceType.Food) >= foodCost
                && GetAmount(ResourceType.Soil) >= soilCost && GetAmount(ResourceType.Special) >= specialCost;
        }

        public bool TrySpend(int foodCost, int soilCost, int specialCost = 0, ResourceReason reason = ResourceReason.Other)
        {
            if (!CanAfford(foodCost, soilCost, specialCost)) return false;
            Take(ResourceType.Food, foodCost); Take(ResourceType.Soil, soilCost); Take(ResourceType.Special, specialCost);
            CampaignHistory.Resource(ResourceType.Food, foodCost, true, reason);
            CampaignHistory.Resource(ResourceType.Soil, soilCost, true, reason);
            CampaignHistory.Resource(ResourceType.Special, specialCost, true, reason);
            OnResourcesChanged?.Invoke();
            return true;
        }
        // 위치 없이 쓰는 소비는 우선순위 낮은 칸부터 꺼낸다(높은 칸을 채워 둔다).
        // ponytail: 장수가 직접 가지러 가지 않는 소비(세금·연구비 등)는 즉시 차감한다. 건설·요리처럼 운반이 기획된 곳만 실제 배송을 쓴다.
        private void Take(ResourceType type, int amount)
        {
            foreach (var b in bins.Where(b => b.Usable).OrderBy(b => b.priority))
            {
                if (amount <= 0) return;
                amount -= TakeFrom(b, type, amount);
            }
        }

        // 주재료를 고른 비용: 식량 + 주재료 수량 + 특수. 주재료가 흙이면 기존 TrySpend와 같다.
        public bool CanAfford(int foodCost, int materialCost, ResourceType material, int specialCost)
            => material == ResourceType.Soil ? CanAfford(foodCost, materialCost, specialCost)
            : material.IsMaterial() && CanAfford(foodCost, 0, specialCost) && materialCost >= 0 && GetAmount(material) >= materialCost;
        public bool TrySpend(int foodCost, int materialCost, ResourceType material, int specialCost, ResourceReason reason = ResourceReason.Other)
        {
            if (material == ResourceType.Soil) return TrySpend(foodCost, materialCost, specialCost, reason);
            if (!CanAfford(foodCost, materialCost, material, specialCost) || !TrySpend(foodCost, 0, specialCost, reason)) return false;
            Take(material, materialCost);
            CampaignHistory.Resource(material, materialCost, true, reason);
            OnResourcesChanged?.Invoke();
            return true;
        }
        public bool TrySpend(ResourceType type, int amount, ResourceReason reason = ResourceReason.Other)
        {
            if (amount < 0 || GetAmount(type) < amount) return false;
            if (amount == 0) return true;
            Take(type, amount);
            CampaignHistory.Resource(type, amount, true, reason);
            OnResourcesChanged?.Invoke();
            return true;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
