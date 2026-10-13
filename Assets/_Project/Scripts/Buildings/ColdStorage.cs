using System.Collections.Generic;
using AntColony.Core;
using AntColony.Data;
using UnityEngine;

namespace AntColony.Buildings
{
    // 냉장고·냉동고(2026-10-12): 별도 식품 보관 건물. 일반 창고 규칙(허용 품목·우선순위·반출)을 쓰고, 보존 효과는 내부 식품에만 준다.
    // 냉장고 = 부패를 늦춤, 냉동고 = 부패 정지. 신선도 회복 없음. 목표 온도 없이 고정 성능.
    // 전력이 끊기면 냉기가 서서히 빠지고, 다시 들어와도 냉각 시간이 필요하다. 해동 작업은 없다.
    public sealed class ColdStorage : Storage, PowerGrid.IUser
    {
        // ponytail: 냉각 60초·냉기 소멸 120초·냉장 부패 속도 25%·전력은 전등의 1배/2배(잠정). 확정 수치가 나오면 여기만 고친다.
        public const float CoolSeconds = 60f, WarmSeconds = 120f, FridgeRate = .25f;
        public bool IsFreezer => Data != null && Data.kind == BuildingKind.Freezer;
        public float Coldness { get; internal set; } // 0 상온 ~ 1 최대 냉각
        public bool Powered { get; set; }
        public float Demand => isActiveAndEnabled && !IsDead ? GameBalance.LampWatts * (IsFreezer ? 2f : 1f) : 0f;
        public IEnumerable<Vector2Int> Cells => RoomSystem.Footprint(this);
        public float DecayRate => Mathf.Lerp(1f, IsFreezer ? 0f : FridgeRate, Coldness);
        public string State => !Powered ? (Coldness > .01f ? $"정전 · 냉기 {Coldness:P0} 남음" : "정전 · 상온") : Coldness < .99f ? $"냉각 중 {Coldness:P0}" : "가동 중";

        // 식품 전용: 정상 식품만 기본 허용(썩은 식품 제외). 다른 품목은 허용할 수 없다.
        public static bool Holds(ResourceType t) => t.IsFood();
        protected override void OnEnable()
        {
            base.OnEnable();
            PowerGrid.Register(this);
            if (Bin != null && !configured) { configured = true; foreach (ResourceType t in System.Enum.GetValues(typeof(ResourceType))) if (!t.IsEdible() && Bin.Allows(t)) Bin.allowed &= ~StoreBin.Bit(t); }
        }
        private bool configured;
        protected override void OnDisable() { PowerGrid.Unregister(this); Powered = false; base.OnDisable(); }
        private void Update()
        {
            var dt = Time.deltaTime; if (dt <= 0) return;
            Coldness = Mathf.Clamp01(Coldness + (Powered && isActiveAndEnabled ? dt / CoolSeconds : -dt / WarmSeconds));
        }
    }
}
