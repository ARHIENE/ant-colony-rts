using System;
using AntColony.Core;
using AntColony.Data;
using AntColony.World;
using UnityEngine;

namespace AntColony.Buildings
{
    // 퇴비통(2026-10-13 확정 기획): 운반 장수가 배설물·썩은 식품을 넣으면 상주 작업자 없이 숙성되어 비료가 된다.
    // 원료별 허용·우선순위 1~9를 가진 운반 목적지(창고 칸 재사용, 단 안의 원료는 식사·제작에 쓰이지 않는다).
    // 비료 목표 재고에 도달하면 새 투입만 멈추고 숙성 중인 원료는 계속 숙성한다. 숙성 중 꺼내면 진행도는 사라지고 원료 그대로 바닥에.
    public sealed class CompostBin : Storage
    {
        // ponytail: 한 번 숙성량·시간·산출은 기획 미정 잠정값(원료 10 → 비료 5, 1개월).
        public const int Batch = 10, Yield = 5; public const float Seconds = 300f;
        [Serializable] public sealed class State
        {
            public int manure, rotten; public float progress; public bool active;
            public int target = 20; public bool allowManure = true, allowRotten = true, configured;
        }
        public State Data2 { get; private set; } = new State();
        public void RestoreState(State s) { Data2 = s ?? new State(); Data2.configured = true; ApplyAllowed(); }
        public static bool Raw(ResourceType t) => t == ResourceType.Manure || t == ResourceType.RottenFood;
        public static int FertilizerStock => (ResourceManager.Instance?.GetAmount(ResourceType.Fertilizer) ?? 0)
            + Mathf.FloorToInt(System.Linq.Enumerable.Sum(System.Linq.Enumerable.Where(ResourceNode.Available, n => n != null && n.IsLooseCargo && n.ResourceType == ResourceType.Fertilizer), n => n.AmountRemaining));
        public bool TargetReached => FertilizerStock >= Data2.target;
        public string Status => Data2.active ? $"숙성 중 {Data2.progress / Seconds:P0} (배설물 {Data2.manure}·썩은 식품 {Data2.rotten})"
            : TargetReached ? "비료 목표 재고 도달 — 새 투입 정지" : $"원료 대기 {RawCount}/{Batch}";
        public int RawCount => Bin == null ? 0 : Bin.Amount(ResourceType.Manure) + Bin.Amount(ResourceType.RottenFood);

        protected override void OnEnable()
        {
            base.OnEnable();
            if (!Data2.configured) { Data2.configured = true; }
            ApplyAllowed();
        }
        // 원료만 받는다. 유저 허용 + 목표 미달일 때만 새 투입.
        private void ApplyAllowed()
        {
            if (Bin == null) return;
            var open = !TargetReached;
            Bin.allowed = (Data2.allowManure && open ? StoreBin.Bit(ResourceType.Manure) : 0) | (Data2.allowRotten && open ? StoreBin.Bit(ResourceType.RottenFood) : 0);
        }
        public void SetAllowed(ResourceType t, bool on)
        {
            if (t == ResourceType.Manure) Data2.allowManure = on; else if (t == ResourceType.RottenFood) Data2.allowRotten = on; else return;
            // 허용 해제: 그 원료(아직 숙성 전)는 바닥으로. 숙성 중인 몫은 계속 숙성한다.
            if (!on && Bin != null) ResourceManager.Instance?.Eject(Bin, t);
            ApplyAllowed();
        }

        private void Update() { if (!AntColony.Save.SaveSystem.Busy) Tick(Time.deltaTime); }
        public void Tick(float dt)
        {
            if (!isActiveAndEnabled || IsDead || Bin == null || !(dt > 0)) return;
            ApplyAllowed();
            if (!Data2.active)
            {
                if (RawCount < Batch || TargetReached) return;
                var rm = ResourceManager.Instance;
                var m = rm.TakeFrom(Bin, ResourceType.Manure, Batch);
                var r = rm.TakeFrom(Bin, ResourceType.RottenFood, Batch - m);
                Data2.manure = m; Data2.rotten = r; Data2.progress = 0; Data2.active = m + r > 0;
                return;
            }
            Data2.progress += dt;
            if (Data2.progress < Seconds) return;
            var total = Data2.manure + Data2.rotten;
            Data2.manure = Data2.rotten = 0; Data2.active = false; Data2.progress = 0;
            // 완성 비료는 통 옆 바닥에 두고 운반 장수가 창고로 옮긴다.
            DiplomacyManager.DropFloor(ResourceType.Fertilizer, Mathf.Max(1, total * Yield / Batch), Position + Vector3.right * 1.5f);
        }
        // 숙성 중단: 진행도는 사라지고 넣었던 원료 그대로 주변 바닥(물량 보존).
        public void Abort()
        {
            if (!Data2.active) return;
            if (Data2.manure > 0) DiplomacyManager.DropFloor(ResourceType.Manure, Data2.manure, Position + Vector3.left * 1.5f);
            if (Data2.rotten > 0) DiplomacyManager.DropFloor(ResourceType.RottenFood, Data2.rotten, Position + Vector3.left * 1.5f);
            Data2.manure = Data2.rotten = 0; Data2.active = false; Data2.progress = 0;
        }
    }
}
