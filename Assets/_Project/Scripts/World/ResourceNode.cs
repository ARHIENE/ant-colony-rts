using System.Collections.Generic;
using AntColony.Data;
using AntColony.Core;
using UnityEngine;

namespace AntColony.World
{
    public class ResourceNode : MonoBehaviour
    {
        public static IReadOnlyList<ResourceNode> Available => Active;
        public bool GatheringForbidden { get; set; }
        public bool IsLooseCargo { get; internal set; }
        private static readonly List<ResourceNode> Active = new List<ResourceNode>();

        [SerializeField] private ResourceType resourceType = ResourceType.Food;
        [SerializeField] private float amountRemaining = 200f;
        [SerializeField] private EnemyColony ownerColony;

        // 재성장(밭 등). regrowSeconds가 0이면 기존처럼 소진 시 사라진다.
        [Header("Regrowth")]
        [SerializeField, Min(0f)] private float regrowSeconds = 0f;
        [SerializeField, Min(0f)] private float regrowAmount = 0f;

        private float regrowTimer;
        // 밭(2026-10-08): 수확 뒤 장수가 파종해야 다시 자란다. 남은 파종 작업량(초, 장수 작업 속도로 줄어듦). 0이면 심어져 혼자 자람.
        private float sowRemaining;
        public bool NeedsSowing => sowRemaining > 0f;
        public float SowRemaining => sowRemaining;
        public float RegrowSeconds => regrowSeconds;
        public float RegrowAmount => regrowAmount;
        // 재성장 노드를 다 캤을 때(밭 수확 완료). 고급 균류의 Special 판정이 쓴다.
        public event System.Action Harvested;

        [Header("Fishing")]
        [SerializeField] private bool requiresFishing;
        [SerializeField, Min(1f)] private float fishingRateMultiplier = 2f;

        public bool RequiresFishing => requiresFishing;
        public bool IsRaidLoot => ownerColony != null;
        public bool IsRaidLocked => ownerColony != null && ownerColony.GetComponentInParent<ExpeditionSite>()?.Disposition != ConquestDisposition.Annexed && (!ownerColony.IsDefeated
            || ownerColony.GetComponentInParent<ExpeditionSite>()?.Disposition == ConquestDisposition.Lost);
        public bool IsUnlocked => !IsRaidLocked && (GetComponentInParent<ExpeditionSite>()?.Disposition == ConquestDisposition.Annexed || DiplomacyManager.Hostile(this))
            && (!requiresFishing || (GameManager.Instance != null && GameManager.Instance.FishingUnlocked));
        public bool CanGather => !GatheringForbidden && isActiveAndEnabled && (!IsDepleted || IsFarm && NeedsSowing) && IsUnlocked && !ColonyEvents.Flooded(this);
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
            if (regrowSeconds > 0f && IsDepleted && regrowTimer <= 0f) { regrowTimer = regrowSeconds; if (IsFarm) sowRemaining = GameBalance.SowSeconds; }
            if ((regrowSeconds > 0f || requiresFishing || IsRaidLoot) && GetComponent<ResourceNodeStatus>() == null)
                gameObject.AddComponent<ResourceNodeStatus>();
        }

        private void Update() { if (requiresFishing && !AntColony.Save.SaveSystem.Busy && GameSession.Exists && GameSession.Instance.GameStarted) RefreshFishingMonth(); TickGrowth(Time.deltaTime); }
        // 낚시터: 재성장 대신 매달 월 한도만큼 재고를 채운다. fishMonth는 마지막으로 채운 달(저장 포함).
        internal int FishMonth { get; set; } = -1;
        public bool FishedOut => requiresFishing && IsDepleted;
        public void RefreshFishingMonth()
        {
            if (FishMonth == GameCalendar.TotalMonths) return;
            FishMonth = GameCalendar.TotalMonths; amountRemaining = GameBalance.FishingMonthlyFood * BiomeRules.FishingYield; regrowTimer = 0;
        }
        // 바이옴 자원 비율(맵 생성 때 한 번).
        internal void ScaleAmount(float multiplier) => amountRemaining *= Mathf.Max(0, multiplier);
        private bool IsFarm => GetComponent<AntColony.Buildings.BuildingBase>() != null;
        public void TickGrowth(float seconds)
        {
            if (regrowTimer <= 0f || !(seconds > 0) || float.IsInfinity(seconds)) return;
            float labor = 1;
            if (IsFarm && NeedsSowing)
            {
                // 파종만 장수 일. 심은 뒤에는 환경 조건에 따라 혼자 자란다.
                labor = 0;
                foreach (var unit in AntColony.Units.AntUnitBase.Active)
                    if (unit is AntColony.Units.CommanderAnt c && c.CivilianWorkReady && c.CurrentResourceNode == this && c.IsGatheringAnimation)
                    { labor += c.WorkRate(AntColony.Units.CommanderActivity.Farming); c.GainExperience(AntColony.Units.CommanderActivity.Farming, Mathf.Min(seconds, sowRemaining / Mathf.Max(.01f, labor))); }
                sowRemaining = Mathf.Max(0, sowRemaining - seconds * labor);
                return;
            }
            regrowTimer = Mathf.Max(0, regrowTimer - seconds * labor * ColonyEvents.GrowthMultiplier(this) * (IsFarm ? BiomeRules.FarmGrowth * AntColony.Map.WeatherSystem.FarmGrowth : 1f));
            // 밭(건물 노드)만 균류 재배 수확량 보정과 가을 수확 배율을 받는다.
            if (regrowTimer <= 0f) amountRemaining = regrowAmount * (IsFarm ? ScienceEffects.FarmYieldMultiplier
                * (GameCalendar.CurrentSeason == Season.Autumn ? GameBalance.AutumnHarvestMultiplier : 1f) : 1f) * (BountifulHarvest ? 1.5f : 1f);
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
                if (requiresFishing) { }
                else if (regrowSeconds > 0f) { regrowTimer = regrowSeconds; if (IsFarm) sowRemaining = GameBalance.SowSeconds; BountifulHarvest = false; Harvested?.Invoke(); }
                else gameObject.SetActive(false);
            }
            return extracted;
        }

        // 밭 작물 변경 전용. 이미 자라는 중이면 남은 시간을 새 작물 성장 시간 안으로 줄이고, 막 심은 밭은 새 성장 시간 전체를 쓴다.
        internal void ConfigureRegrowth(float seconds, float amount)
        {
            var fresh = regrowTimer > 0f && regrowTimer == regrowSeconds;
            regrowSeconds = Mathf.Max(0f, seconds);
            regrowAmount = Mathf.Max(0f, amount);
            regrowTimer = fresh ? regrowSeconds : Mathf.Min(regrowTimer, regrowSeconds);
        }

        // 런타임 생성 노드(보스 전리품) 전용. 활성화 전에 호출해야 OnEnable 판정이 맞다.
        public void ConfigureLoot(ResourceType type, float amount)
        {
            IsLooseCargo = true;
            resourceType = type;
            amountRemaining = Mathf.Max(0f, amount);
            regrowSeconds = 0f;
            requiresFishing = false;
            ownerColony = null;
        }

        // 저장 복원 전용. 잔량과 재성장 타이머를 그대로 되돌린다.
        internal void RestoreState(float amount, float timer, float sow = 0f)
        {
            sowRemaining = Mathf.Max(0f, sow);
            amountRemaining = Mathf.Max(0f, amount);
            regrowTimer = Mathf.Max(0f, timer);
            var shouldBeActive = !IsDepleted || regrowSeconds > 0f || requiresFishing;
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
