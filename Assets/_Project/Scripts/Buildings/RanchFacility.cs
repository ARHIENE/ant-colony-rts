using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using AntColony.World;
using UnityEngine;

namespace AntColony.Buildings
{
    // 목장 시설(2026-10-11): 먹이통·돌봄대·동물 치료대·도축대. 이름·비용·크기는 잠정.
    // - 먹이통: 먹이 전용 창고. 허용 먹이·종류별 목표량·운반 우선순위. 운반 장수가 채운다(사육 장수의 급여 작업 없음). 접근 가능한 생물이 공유.
    // - 돌봄대: 같은 우리 여러 종이 공유, 시설당 한 마리씩 사육 장수가 호출해 돌본다.
    // - 동물 치료대: 중상 수술용, 한 마리씩. 실제 수술 중에만 배고픔이 멈춘다.
    // - 도축대: 운반 장수가 사체를 옮겨 오면 요리 장수가 해체(식량·갑각).
    public sealed class RanchFacility : BuildingBase
    {
        public static readonly List<RanchFacility> All = new List<RanchFacility>();
        [Serializable] public class FeedSlot { public int type; public bool allowed; public int target; public float stock; }
        [Serializable] public class State
        {
            public List<FeedSlot> slots = new List<FeedSlot>(); public int priority = 3;
            public float meat, chitin, butcher; public int carcasses;
        }
        private State s = new State();
        public bool IsFeeder => Data != null && Data.kind == BuildingKind.Feeder;
        public bool IsCareStation => Data != null && Data.kind == BuildingKind.CareStation;
        public bool IsClinic => Data != null && Data.kind == BuildingKind.AnimalClinic;
        public bool IsButcher => Data != null && Data.kind == BuildingKind.ButcherTable;
        public static bool IsKind(BuildingKind k) => k == BuildingKind.Feeder || k == BuildingKind.CareStation || k == BuildingKind.AnimalClinic || k == BuildingKind.ButcherTable;
        public string Key => $"{Mathf.RoundToInt(Position.x * 10)}:{Mathf.RoundToInt(Position.z * 10)}";
        public static RanchFacility Find(string key) => All.FirstOrDefault(f => f != null && f.Key == key);
        // 이동 작업 중(가구 이동 예정)이면 먹기·보충·돌봄을 멈춘다.
        public bool Usable => isActiveAndEnabled && !IsDead && !Demolition.Pending(this);
        public CommanderAnt Worker { get; internal set; } // 돌봄·수술·해체 중인 장수(시설당 한 명)
        public Critter Patient => IsClinic ? Critter.All.FirstOrDefault(c => c != null && c.Clinic == this) : null;
        protected override void OnEnable() { base.OnEnable(); if (!All.Contains(this)) All.Add(this); EnsureSlots(); }
        protected override void OnDisable() { base.OnDisable(); All.Remove(this); }

        // ---------- 먹이통 ----------
        private void EnsureSlots()
        {
            if (!IsFeeder) return;
            foreach (var t in SpeciesInfo.AllFeeds)
                if (!s.slots.Any(x => x.type == (int)t)) s.slots.Add(new FeedSlot { type = (int)t, allowed = !SpeciesInfo.Precious(t), target = SpeciesInfo.Precious(t) ? 0 : GameBalance.FeederDefaultTarget });
        }
        private FeedSlot Slot(ResourceType t) { EnsureSlots(); return s.slots.Find(x => x.type == (int)t); }
        public IEnumerable<ResourceType> FeedTypes => SpeciesInfo.AllFeeds;
        public float Stock(ResourceType t) => Slot(t)?.stock ?? 0;
        public bool Allowed(ResourceType t) => Slot(t)?.allowed == true;
        public int Target(ResourceType t) => Slot(t)?.target ?? 0;
        public int Priority { get => s.priority; set => s.priority = Mathf.Clamp(value, 1, 5); }
        private readonly Dictionary<ResourceType, float> incoming = new Dictionary<ResourceType, float>(); // 운반 중인 양(예정 재고)
        public float Incoming(ResourceType t) => incoming.TryGetValue(t, out var v) ? v : 0;
        internal void Reserve(ResourceType t, float amount) => incoming[t] = Mathf.Max(0, Incoming(t) + amount);
        // 허용 해제: 그 먹이 전부를 바로 바닥에. 목표량 감소: 초과분을 바닥에.
        public void ToggleAllowed(ResourceType t) { var x = Slot(t); x.allowed = !x.allowed; if (!x.allowed) Spill(x, 0); }
        public void SetTarget(ResourceType t, int target) { var x = Slot(t); x.target = Mathf.Clamp(target, 0, GameBalance.FeederMaxTarget); Spill(x, x.target); }
        public void DumpAll(ResourceType t) => Spill(Slot(t), 0); // 직접 반출(재보충을 피하려면 목표량도 함께 낮춘다)
        private void Spill(FeedSlot x, float keep)
        {
            var extra = Mathf.FloorToInt(x.stock - keep); if (extra <= 0) return;
            x.stock -= extra; DiplomacyManager.DropFloor((ResourceType)x.type, extra, Position);
        }
        // 보충할 것: 허용·목표 미달(운반 중 포함)인 먹이 중 창고에 있는 것.
        public (ResourceType type, int amount)? FillNeed()
        {
            if (!IsFeeder || !Usable || ResourceManager.Instance == null) return null;
            foreach (var x in s.slots.Where(x => x.allowed))
            {
                var t = (ResourceType)x.type; var need = Mathf.FloorToInt(x.target - x.stock - Incoming(t)); var have = ResourceManager.Instance.GetAmount(t);
                if (need >= 1 && have >= 1) return (t, Mathf.Min(need, have));
            }
            return null;
        }
        // 도착 시 최신 설정만큼 넣고 남은 것은 돌려준다(창고, 넘치면 바닥).
        internal void Deposit(ResourceType t, int amount)
        {
            var x = Slot(t); var fit = x.allowed ? Mathf.Clamp(Mathf.FloorToInt(x.target - x.stock), 0, amount) : 0;
            x.stock += fit;
            if (amount > fit) DiplomacyManager.StoreResource(t, amount - fit, ResourceReason.Refund, Position);
        }
        // 동물이 먹는다(같은 보관 장소면 아무거나 하나). 식성에 맞는 것만.
        internal bool TakeFood(SpeciesInfo info)
        {
            var x = s.slots.FirstOrDefault(y => y.stock >= 1 && info.Eats((ResourceType)y.type));
            if (x == null) return false; x.stock -= 1; return true;
        }

        // ---------- 도축대 ----------
        public int Carcasses => s.carcasses;
        public float ButcherProgress => s.butcher;
        internal void AddCarcass(CritterCarcass k) { s.meat += k.Meat; s.chitin += k.Chitin; s.carcasses++; }
        public bool NeedsButcher => IsButcher && Usable && s.carcasses > 0;
        internal bool Butcher(CommanderAnt c, float seconds)
        {
            if (!NeedsButcher) return false;
            s.butcher += seconds * c.WorkRate(CommanderActivity.Cooking); c.GainExperience(CommanderActivity.Cooking, seconds);
            if (s.butcher < GameBalance.ButcherSeconds) return true;
            var meat = Mathf.FloorToInt(s.meat / s.carcasses); var chitin = Mathf.FloorToInt(s.chitin / s.carcasses);
            s.meat -= meat; s.chitin -= chitin; s.carcasses--; s.butcher = 0;
            DiplomacyManager.StoreResource(ResourceType.Food, meat, ResourceReason.Production, Position);
            DiplomacyManager.StoreResource(ResourceType.Chitin, chitin, ResourceReason.Production, Position);
            if (s.carcasses == 0) { s.meat = s.chitin = 0; return false; }
            return true;
        }

        // 철거: 먹이통·도축대 내용물은 바닥에, 치료대 환자는 그 자리에(부상 그대로).
        internal void OnRemoved()
        {
            foreach (var x in s.slots.ToList()) Spill(x, 0);
            if (s.carcasses > 0) { DiplomacyManager.DropFloor(ResourceType.Food, Mathf.FloorToInt(s.meat), Position); DiplomacyManager.DropFloor(ResourceType.Chitin, Mathf.FloorToInt(s.chitin), Position); s.carcasses = 0; }
            Patient?.ExitClinic(Position + Vector3.right * 1.5f);
        }

        internal string CaptureState() => JsonUtility.ToJson(s);
        internal void RestoreState(string json)
        {
            s = string.IsNullOrEmpty(json) ? new State() : JsonUtility.FromJson<State>(json) ?? new State();
            s.slots ??= new List<FeedSlot>();
            s.slots.RemoveAll(x => x == null || !SpeciesInfo.AllFeeds.Contains((ResourceType)x.type));
            foreach (var x in s.slots) { x.target = Mathf.Clamp(x.target, 0, GameBalance.FeederMaxTarget); x.stock = float.IsNaN(x.stock) ? 0 : Mathf.Max(0, x.stock); }
            s.priority = Mathf.Clamp(s.priority, 1, 5); s.carcasses = Mathf.Max(0, s.carcasses);
            if (!(s.meat >= 0)) s.meat = 0; if (!(s.chitin >= 0)) s.chitin = 0; if (!(s.butcher >= 0)) s.butcher = 0;
            EnsureSlots();
        }
    }
}
