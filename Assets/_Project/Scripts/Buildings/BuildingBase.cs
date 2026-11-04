using System.Collections.Generic;
using AntColony.Core;
using AntColony.Data;
using UnityEngine;

namespace AntColony.Buildings
{
    public partial class BuildingBase : MonoBehaviour, IDamageable
    {
        private static readonly List<BuildingBase> DepositPoints = new List<BuildingBase>();

        [SerializeField] protected BuildingData data;
        [SerializeField] private bool countsTowardPlayerDefeat = true;
        [SerializeField, Min(1f)] private float fallbackMaxHealth = 300f;
        [SerializeField] private float currentHealth;

        public BuildingData Data => data;
        public bool CountsTowardPlayerDefeat => countsTowardPlayerDefeat;
        // 재료 업그레이드(2026-10-05 공통 원칙): 같은 가구를 더 좋은 재료로 다시 지어 올린 단계. 재료 목록·효과가 미정이라 저장만 한다.
        // TODO 재료 목록이 정해지면 FurnitureCatalog.MaterialUpgradable 가구에 업그레이드 명령·효과를 붙인다.
        public int MaterialTier { get; set; }
        private int powerPriority;
        public int PowerPriorityOverride { get => powerPriority; set => powerPriority = Mathf.Clamp(value, 0, 9); }
        // 0은 구 저장/기본값. 식품 보존을 먼저 살리되 플레이어가 1~9로 바꿀 수 있다.
        public int PowerPriority => powerPriority > 0 ? powerPriority : this is ColdStorage ? 8 : this is PowerNode ? 3 : 5;
        // 주재료(2026-10-10): 건설비의 재료 몫(soilCost)을 이 재료로 낸다. 내구도 배율·미관·단열·가연성이 재료를 따른다.
        public ResourceType MainMaterial { get; set; } = ResourceType.Soil;
        public MaterialInfo Material => MaterialInfo.For(MainMaterial);
        public virtual float MaxHealth => BaseMaxHealth * Material.durability * (UsesDefenseDurability ? DefenseUpgrades.DurabilityMultiplier : 1f);
        // 저장 검증용: 연구소 내구 라인이 최고 단계·가장 단단한 재료일 때의 체력 상한.
        internal float MaxPossibleHealth => BaseMaxHealth * 3f * (UsesDefenseDurability ? 1f + .2f * DefenseUpgrades.MaxLevel : 1f);
        private float BaseMaxHealth => data != null ? data.maxHealth : fallbackMaxHealth;
        // 방어시설(분사탑·흙벽)은 방어시설 연구소 내구 라인을 받는다. 기본 건물은 방어력 0이다.
        protected virtual bool UsesDefenseDurability => false;
        public virtual float Armor => 0f;
        public float CurrentHealth => currentHealth;
        public bool IsDead => currentHealth <= 0f;
        public Vector3 Position => transform.position;
        internal void ConfigureRuntime(BuildingData definition) { data = definition; currentHealth = MaxHealth; }

        protected virtual bool IsDepositPoint => false;
        private bool listed;

        protected virtual void Awake()
        {
            currentHealth = MaxHealth;
        }

        protected virtual void OnEnable()
        {
            if (IsDepositPoint)
            {
                DepositPoints.Add(this);
            }
            // 철거 중처럼 컴포넌트만 꺼진 건물도 FindObjectsByType처럼 찾도록 파괴될 때까지 목록에 둔다.
            if (!listed) { SceneObjects.AddBuilding(this); listed = true; }
            // 건설 중인 건물과 배치용 템플릿은 비활성 상태이므로, 완공되어 활성화된 건물만 콜로니 존속 판정에 등록된다.
            GameManager.Instance?.RegisterBuilding(this);
            RoomSystem.MarkDirty(); SpaceQuality.Invalidate(); // Phase 5: 벽·가구가 바뀌면 방을 다시 판정한다.
            FacilityVisual.Attach(this);
        }

        protected virtual void OnDisable()
        {
            DepositPoints.Remove(this);
            GameManager.Instance?.UnregisterBuilding(this);
            RoomSystem.MarkDirty(); SpaceQuality.Invalidate();
        }

        // 생산대 잔해는 0을 보존하되 파괴 부작용을 재실행하지 않는다.
        internal void RestoreHealth(float value)
        {
            currentHealth = Mathf.Clamp(value, this is Workshop || this is Processor || this is AirshipYard || this is AntColony.World.ExpeditionTransport ? 0f : 0.01f, MaxHealth);
        }

        public void TakeDamage(float amount)
        {
            if (IsDead || !(amount > 0f)) return;
            if (!AntColony.World.DiplomacyManager.TryAttack(this)) return;
            // 방어력은 피해를 깎되, 약한 공격도 최소 1(원래 피해가 더 작으면 그 값)은 들어간다.
            currentHealth -= Mathf.Max(Mathf.Min(amount, 1f), amount - Armor - AntColony.World.AIEraMilitary.FortificationArmor(this));
            if (currentHealth <= 0f)
            {
                currentHealth = 0f;
                RoomSystem.MarkDirty(); SpaceQuality.Invalidate(); // 부서진 벽은 같은 프레임부터 방 판정에서 빠진다.
                var foundation = LeaveBabelFoundation();
                Die();
                if (foundation) Destroy(gameObject);
            }
        }

        protected virtual void Die()
        {
            Destroy(gameObject);
        }

        public virtual void DepositResources(ResourceType type, int amount) => ResourceManager.Instance?.Add(type, amount);

        public static BuildingBase FindNearestDepositPoint(Vector3 from)
        {
            BuildingBase nearest = null;
            var nearestDistSqr = float.MaxValue;
            foreach (var point in DepositPoints)
            {
                if (point == null || point.IsDead) continue;
                var distSqr = (point.transform.position - from).sqrMagnitude;
                if (distSqr < nearestDistSqr)
                {
                    nearestDistSqr = distSqr;
                    nearest = point;
                }
            }
            return nearest;
        }
    }
}
