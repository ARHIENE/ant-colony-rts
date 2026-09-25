using System.Collections.Generic;
using AntColony.Data;
using AntColony.Core;
using UnityEngine;

namespace AntColony.World
{
    public class ResourceNode : MonoBehaviour
    {
        private static readonly List<ResourceNode> Active = new List<ResourceNode>();

        [SerializeField] private ResourceType resourceType = ResourceType.Food;
        [SerializeField] private float amountRemaining = 200f;
        [SerializeField] private EnemyColony ownerColony;

        // 재성장(밭 등). regrowSeconds가 0이면 기존처럼 소진 시 사라진다.
        [Header("Regrowth")]
        [SerializeField, Min(0f)] private float regrowSeconds = 0f;
        [SerializeField, Min(0f)] private float regrowAmount = 0f;

        private float regrowTimer;
        public float RegrowSeconds => regrowSeconds;
        public float RegrowAmount => regrowAmount;
        // 재성장 노드를 다 캤을 때(밭 수확 완료). 고급 균류의 Special 판정이 쓴다.
        public event System.Action Harvested;

        [Header("Fishing")]
        [SerializeField] private bool requiresFishing;
        [SerializeField, Min(1f)] private float fishingRateMultiplier = 2f;

        public bool RequiresFishing => requiresFishing;
        public bool IsRaidLoot => ownerColony != null;
        public bool IsRaidLocked => ownerColony != null && (!ownerColony.IsDefeated
            || ownerColony.GetComponentInParent<ExpeditionSite>()?.Disposition == ConquestDisposition.Lost);
        public bool IsUnlocked => !IsRaidLocked && (!requiresFishing || (GameManager.Instance != null && GameManager.Instance.FishingUnlocked));
        public bool CanGather => isActiveAndEnabled && !IsDepleted && IsUnlocked && !ColonyEvents.Flooded(this);
        public float GatherRateMultiplier => requiresFishing ? fishingRateMultiplier : 1f;

        public ResourceType ResourceType => resourceType;
        // 운반 용량에 딱 맞춰 캐면 부동소수 잔량이 남아 노드가 영원히 채집 가능 상태로 남는다.
        private const float DepletedEpsilon = 0.001f;
        public bool IsDepleted => amountRemaining <= DepletedEpsilon;
        public bool IsRegrowing => regrowTimer > 0f;
        public float AmountRemaining => amountRemaining;
        public float RegrowTimeRemaining => Mathf.Max(0f, regrowTimer);
        public bool BountifulHarvest { get; internal set; }

        private void OnEnable()
        {
            Active.Add(this);
            // 갓 지어진 밭은 비어 있는 상태로 시작해 한 번 성장한 뒤 수확 가능해진다.
            if (regrowSeconds > 0f && IsDepleted) regrowTimer = regrowSeconds;
            if ((regrowSeconds > 0f || requiresFishing || IsRaidLoot) && GetComponent<ResourceNodeStatus>() == null)
                gameObject.AddComponent<ResourceNodeStatus>();
        }

        private void Update() => TickGrowth(Time.deltaTime);
        public void TickGrowth(float seconds)
        {
            if (regrowTimer <= 0f || !(seconds > 0) || float.IsInfinity(seconds)) return;
            regrowTimer = Mathf.Max(0, regrowTimer - seconds * ColonyEvents.GrowthMultiplier(this));
            // 밭(건물 노드)만 균류 재배 수확량 보정을 받는다.
            if (regrowTimer <= 0f) amountRemaining = regrowAmount
                * (GetComponent<AntColony.Buildings.BuildingBase>() != null ? ScienceEffects.FarmYieldMultiplier : 1f) * (BountifulHarvest ? 1.5f : 1f);
        }
        public void GrantBountifulHarvest()
        {
            if (BountifulHarvest) return;
            BountifulHarvest = true;
            if (!IsDepleted && !IsRegrowing) amountRemaining *= 1.5f;
        }
        public void BurnCrop(float fraction)
        {
            if (regrowSeconds <= 0) return;
            if (IsRegrowing)
            {
                regrowTimer += (regrowSeconds - regrowTimer) * Mathf.Clamp01(fraction);
                if (fraction >= 1) BountifulHarvest = false;
                return;
            }
            amountRemaining *= 1 - Mathf.Clamp01(fraction);
            if (IsDepleted) { regrowTimer = regrowSeconds; BountifulHarvest = false; }
        }

        private void OnDisable()
        {
            Active.Remove(this);
        }

        public float Extract(float amount)
        {
            if (amount <= 0f || !CanGather) return 0f;
            var extracted = Mathf.Min(amount, amountRemaining);
            amountRemaining -= extracted;
            if (IsDepleted)
            {
                if (regrowSeconds > 0f) { regrowTimer = regrowSeconds; BountifulHarvest = false; Harvested?.Invoke(); }
                else gameObject.SetActive(false);
            }
            return extracted;
        }

        // 밭 작물 변경 전용. 이미 자라는 중이면 남은 시간을 새 작물 성장 시간 안으로 줄인다.
        internal void ConfigureRegrowth(float seconds, float amount)
        {
            regrowSeconds = Mathf.Max(0f, seconds);
            regrowAmount = Mathf.Max(0f, amount);
            regrowTimer = Mathf.Min(regrowTimer, regrowSeconds);
        }

        // 런타임 생성 노드(보스 전리품) 전용. 활성화 전에 호출해야 OnEnable 판정이 맞다.
        public void ConfigureLoot(ResourceType type, float amount)
        {
            resourceType = type;
            amountRemaining = Mathf.Max(0f, amount);
            regrowSeconds = 0f;
            requiresFishing = false;
            ownerColony = null;
        }

        // 저장 복원 전용. 잔량과 재성장 타이머를 그대로 되돌린다.
        internal void RestoreState(float amount, float timer)
        {
            amountRemaining = Mathf.Max(0f, amount);
            regrowTimer = Mathf.Max(0f, timer);
            var shouldBeActive = !IsDepleted || regrowSeconds > 0f;
            if (gameObject.activeSelf != shouldBeActive) gameObject.SetActive(shouldBeActive);
        }

        internal float RegrowTimer => regrowTimer;

        public void AddStock(float amount)
        {
            if (!(amount > 0f) || float.IsInfinity(amount)) return;
            var depleted = IsDepleted;
            amountRemaining += amount;
            if (depleted && !IsDepleted) gameObject.SetActive(true);
        }

        public bool TryConsumeStock(float amount)
        {
            if (amount < 0f || amountRemaining < amount) return false;
            amountRemaining -= amount;
            return true;
        }

        public static ResourceNode FindNearestActive(Vector3 from)
        {
            ResourceNode nearest = null;
            var nearestDistSqr = float.MaxValue;
            foreach (var node in Active)
            {
                if (node == null || !node.CanGather) continue;
                var distSqr = (node.transform.position - from).sqrMagnitude;
                if (distSqr < nearestDistSqr)
                {
                    nearestDistSqr = distSqr;
                    nearest = node;
                }
            }
            return nearest;
        }
    }
}
