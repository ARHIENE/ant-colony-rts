using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Buildings;
using AntColony.Data;
using UnityEngine;

namespace AntColony.Core
{
    // 창고 한 칸(산소미포함식 실물 재고, 2026-10-12). 비축더미(Host = Stockpile 또는 없음)와 창고 건물마다 하나.
    // 소굴 보유량 = 사용 가능한 칸들의 합계다. 허용 품목·우선순위는 바닥 물자를 어디로 들일지만 정하고, 칸끼리 자동으로 옮기지 않는다.
    public sealed class StoreBin
    {
        public static readonly int TypeCount = Enum.GetValues(typeof(ResourceType)).Length;
        // 새 칸은 썩은 식품 외 전부 허용(뒤에 붙을 종류도 켜지도록 모든 비트를 켠다).
        public static ulong DefaultAllowed => ~0UL & ~Bit(ResourceType.RottenFood);
        public static ulong Bit(ResourceType t) => (int)t < 64 ? 1UL << (int)t : 0;

        public BuildingBase Host { get; internal set; }
        public int priority = AntColony.Units.WorkPriorities.Default;
        public ulong allowed = DefaultAllowed;
        private readonly HashSet<int> extraForbidden = new HashSet<int>();
        internal readonly int[] amounts = new int[TypeCount];
        // 부패하는 식품의 물량별 신선도. 총량(amounts)이 바뀌면 읽을 때 맞춘다(늘면 신선한 몫, 줄면 신선도 낮은 몫부터).
        private readonly Dictionary<ResourceType, FoodLots> food = new Dictionary<ResourceType, FoodLots>();
        public FoodLots Lots(ResourceType t)
        {
            if (!FoodLots.Perishable(t)) return null;
            if (!food.TryGetValue(t, out var l)) food[t] = l = new FoodLots();
            l.Match(amounts[(int)t], t); return l;
        }
        // 부패 속도: 상온 1, 냉장고·냉동고는 내부 식품만 늦추거나 멈춘다(방 자체 냉각 없음).
        public float DecayRate => Host is ColdStorage cold ? cold.DecayRate : 1f;
        internal Func<ResourceType, int> capacity = _ => 0;

        public bool IsBase => Host == null || Host is Stockpile;
        public bool Usable => Host == null || Host.isActiveAndEnabled && !Host.IsDead;
        // 퇴비통 칸은 운반 목적지일 뿐 소굴 재고(식사·제작·건설)로 꺼내 쓰지 않는다(2026-10-15).
        public bool Spendable => Usable && !(Host is CompostBin);
        public Vector3 Position => Host != null ? Host.Position : ResourceManager.HomePosition;
        public string Name => Host != null && Host.Data != null ? Host.Data.displayName : "비축더미";
        public bool Allows(ResourceType t) => (int)t < 64 ? (allowed & Bit(t)) != 0 : !extraForbidden.Contains((int)t) && !(Host is CompostBin) && (!(Host is ColdStorage) || ColdStorage.Holds(t));
        internal void Allow(ResourceType t, bool on)
        {
            if ((int)t < 64) allowed = on ? allowed | Bit(t) : allowed & ~Bit(t);
            else if (on) extraForbidden.Remove((int)t); else extraForbidden.Add((int)t);
        }
        public int Amount(ResourceType t) => amounts[(int)t];
        public int Capacity(ResourceType t) => Mathf.Max(0, capacity(t));
        // 다른 운반자가 이미 예약한 공간은 빼고 계산한다.
        public int Free(ResourceType t, object owner = null) => Mathf.Max(0, Capacity(t) - amounts[(int)t] - ResourceManager.Instance.ReservedFor(this, t, owner));
        public bool Accepts(ResourceType t, object owner = null) => Usable && Allows(t) && Free(t, owner) > 0;
        public IEnumerable<ResourceType> Stocked => Enumerable.Range(0, TypeCount).Where(i => amounts[i] > 0).Select(i => (ResourceType)i);

        [Serializable] public sealed class TypedLots { public int type; public FoodLots lots; }
        [Serializable] public sealed class Dto { public int priority = 5; public long allowed = -1; public int[] amounts; public List<TypedLots> food = new List<TypedLots>(); public List<int> extraForbidden = new List<int>(); }
        public Dto Capture() => new Dto { priority = priority, allowed = unchecked((long)allowed), amounts = (int[])amounts.Clone(),
            food = Stocked.Where(FoodLots.Perishable).Select(t => new TypedLots { type = (int)t, lots = JsonUtility.FromJson<FoodLots>(JsonUtility.ToJson(Lots(t))) }).ToList(), extraForbidden = extraForbidden.OrderBy(i => i).ToList() };
        internal void Apply(Dto d)
        {
            priority = Mathf.Clamp(d.priority, AntColony.Units.WorkPriorities.Min, AntColony.Units.WorkPriorities.Max);
            allowed = unchecked((ulong)d.allowed);
            extraForbidden.Clear();
            if (d.extraForbidden != null) foreach (var type in d.extraForbidden) extraForbidden.Add(type);
            Array.Clear(amounts, 0, amounts.Length);
            if (d.amounts != null) for (int i = 0; i < Mathf.Min(d.amounts.Length, TypeCount); i++) amounts[i] = Mathf.Max(0, d.amounts[i]);
            food.Clear();
            if (d.food != null) foreach (var f in d.food) if (f?.lots != null && f.lots.Valid && Enum.IsDefined(typeof(ResourceType), f.type)) food[(ResourceType)f.type] = f.lots;
        }
    }
}
