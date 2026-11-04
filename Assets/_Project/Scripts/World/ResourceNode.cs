using System.Collections.Generic;
using AntColony.Data;
using AntColony.Core;
using UnityEngine;

namespace AntColony.World
{
    public partial class ResourceNode : MonoBehaviour
    {
        public static IReadOnlyList<ResourceNode> Available => Active;
        public bool GatheringForbidden { get; set; }
        internal bool IsUndergroundLayer { get; set; }
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
        // G30: 원산지별 상온 보존 기간. 낚시=생선, 균류 밭=버섯, 그 밖의 밭·야생 채집=열매(바닥 더미는 FloorFood 신선도를 쓴다).
        public FoodPreservation Preservation => RequiresFishing ? FoodPreservation.Fish
            : TryGetComponent<AntColony.Buildings.FarmPlot>(out var farm) && farm.Crop != AntColony.Buildings.FarmCrop.Honeydew ? FoodPreservation.Mushroom : FoodPreservation.Berry;
        public bool IsRaidLoot => ownerColony != null;
        public bool IsRaidLocked => ownerColony != null && ownerColony.GetComponentInParent<ExpeditionSite>()?.Disposition != ConquestDisposition.Annexed && (!ownerColony.IsDefeated
            || ownerColony.GetComponentInParent<ExpeditionSite>()?.Disposition == ConquestDisposition.Lost);
        public bool IsUnlocked => !IsRaidLocked && (GetComponentInParent<ExpeditionSite>()?.Disposition == ConquestDisposition.Annexed || DiplomacyManager.Hostile(this))
            && (!requiresFishing || (GameManager.Instance != null && GameManager.Instance.FishingUnlocked));
        public bool CanGather => !IsUndergroundLayer && !GatheringForbidden && isActiveAndEnabled && (!IsDepleted || IsFarm && NeedsSowing || HoldsHandWork) && IsUnlocked && !ColonyEvents.Flooded(this);
        public bool RequiresMiningTool => !IsLooseCargo && (resourceType == ResourceType.Stone || resourceType == ResourceType.IronOre
            || resourceType == ResourceType.CopperOre || resourceType == AntColony.Data.ResourceType.TinOre || resourceType == ResourceType.Coal || resourceType == ResourceType.Sulfur);
        // 돌 도구 연구의 재료인 석재에는 새 자원 연구 관문을 추가하지 않는다.
        public bool RequiresResourceResearch => RequiresMiningTool && resourceType != AntColony.Data.ResourceType.Stone;
        public const int MiningToolTier = 2; // 현재 제작 가능한 도구는 나무(1)/돌(2)까지다.
        public bool CanGatherWith(AntColony.Units.WorkerAnt worker) => CanGather && (!NeedsDesignation || WorkRequested && !WorkPaused) && (!RequiresMiningTool
            || worker is AntColony.Units.CommanderAnt commander && (RequiresResourceResearch
                ? CanMine(commander) || CanTakeSample(commander) : commander.PersonalState.toolTier >= MiningToolTier));
        private bool CanMine(AntColony.Units.CommanderAnt c) => CampaignResearch.Instance?.CanMine(resourceType, c.PersonalState.toolTier, MiningToolTier) == true;
        private bool CanTakeSample(AntColony.Units.CommanderAnt c)
        {
            if (amountRemaining < 1 || CampaignResearch.Instance?.CanRecoverSample(resourceType, c.PersonalState.toolTier, MiningToolTier) != true) return false;
            // 반입 전 표본도 한 개뿐: 여러 장수·노드가 같은 프레임에 중복 회수하지 않는다.
            foreach (var unit in AntColony.Units.AntUnitBase.Active)
                if (unit is AntColony.Units.WorkerAnt w && w.CarriedType == resourceType && w.CarriedAmount > 0) return false;
            foreach (var node in Active)
                if (node != null && node.IsLooseCargo && node.resourceType == resourceType && !node.IsDepleted) return false;
            return true;
        }
        public float ExtractFor(AntColony.Units.WorkerAnt worker, float amount)
        {
            if (!(amount > 0) || float.IsInfinity(amount) || !CanGatherWith(worker)) return 0;
            if (Plot != null && worker is AntColony.Units.CommanderAnt farmer) Plot.NoteFarmer(farmer);
            NotePeak();
            if (!RequiresResourceResearch) { var taken = ExtractStock(amount); TryRebirth(worker); return taken; }
            var c = (AntColony.Units.CommanderAnt)worker;
            if (CanMine(c)) { var mined = ExtractStock(amount); TryRebirth(worker); return mined; }
            // 표본은 분할 화물이 아닌 완전한 1개로 회수한다.
            return amount >= 1 && CanTakeSample(c) ? ExtractStock(1) : 0;
        }
        // 밭 손일(뿌리뽑기·비료) 중에는 수확하지 않고 FarmPlot이 작업량을 받는다(2026-10-15).
        public bool HoldsHandWork => Plot != null && Plot.HasHandWork;
        private AntColony.Buildings.FarmPlot plot;
        private AntColony.Buildings.FarmPlot Plot => plot != null ? plot : plot = GetComponent<AntColony.Buildings.FarmPlot>();
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
            if (regrowSeconds > 0f && IsDepleted && regrowTimer <= 0f && !(Plot != null && Plot.Data.fallow)) { regrowTimer = regrowSeconds; if (IsFarm) sowRemaining = GameBalance.SowSeconds; }
            if ((regrowSeconds > 0f || requiresFishing || IsRaidLoot) && GetComponent<ResourceNodeStatus>() == null)
                gameObject.AddComponent<ResourceNodeStatus>();
        }

        private void Start() => ResourceVisual.Attach(this);
        private void Update() { if (requiresFishing && !AntColony.Save.SaveSystem.Busy && GameSession.Exists && GameSession.Instance.GameStarted) RefreshFishingSeason(); TickGrowth(Time.deltaTime); TickRebirth(Time.deltaTime); }
        // 낚시터: 재성장 대신 계절마다 한도만큼 재고를 채운다. FishSeason은 마지막으로 채운 계절(저장 키 fishMonth).
        internal int FishSeason { get; set; } = -1;
        public bool FishedOut => requiresFishing && IsDepleted;
        public void RefreshFishingSeason()
        {
            if (FishSeason == GameCalendar.TotalSeasons) return;
            FishSeason = GameCalendar.TotalSeasons; amountRemaining = GameBalance.FishingSeasonFood * BiomeRules.FishingYield; regrowTimer = 0;
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
                    { Plot?.NoteFarmer(c); labor += c.WorkRate(AntColony.Units.CommanderActivity.Farming); c.GainExperience(AntColony.Units.CommanderActivity.Farming, Mathf.Min(seconds, sowRemaining / Mathf.Max(.01f, labor))); }
                sowRemaining = Mathf.Max(0, sowRemaining - seconds * labor);
                return;
            }
            regrowTimer = Mathf.Max(0, regrowTimer - seconds * labor * ColonyEvents.GrowthMultiplier(this) * (IsFarm ? (Plot?.Blessed == true ? Mathf.Max(1f, BiomeRules.FarmGrowth) : BiomeRules.FarmGrowth) * (Plot?.Blessed == true ? Mathf.Max(1f, AntColony.Map.WeatherSystem.FarmGrowth) : AntColony.Map.WeatherSystem.FarmGrowth) * (Plot != null ? Plot.GrowthMultiplier : 1f) : 1f));
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
            // 작업자 없는 호출로 연구/도구 관문을 우회하지 못하게 한다.
            if (RequiresMiningTool) return 0;
            return ExtractStock(amount);
        }
        private float ExtractStock(float amount)
        {
            if (amount <= 0f || !CanGather) return 0f;
            var extracted = Mathf.Min(amount, amountRemaining);
            amountRemaining -= extracted;
            if (IsDepleted)
            {
                if (NeedsDesignation) { WorkRequested = false; WorkPaused = false; WorkRequestOrder = 0; }
                if (requiresFishing) { }
                // 밭은 수확 뒤에도 심어진 채 다시 자란다(2026-10-15 사용자 지시: 뿌리뽑기 전까지 재파종 없음).
                else if (regrowSeconds > 0f) { regrowTimer = regrowSeconds; BountifulHarvest = false; Harvested?.Invoke(); }
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
        internal void SetCropResource(ResourceType type) => resourceType = type;

        // 런타임 생성 노드(보스 전리품) 전용. 활성화 전에 호출해야 OnEnable 판정이 맞다.
        public void ConfigureLoot(ResourceType type, float amount)
        {
            IsLooseCargo = true;
            resourceType = type;
            amountRemaining = Mathf.Max(0f, amount);
            regrowSeconds = 0f;
            requiresFishing = false;
            ownerColony = null;
            ProductionProp.ShowResource(this);
            if (FoodLots.Perishable(type) && !TryGetComponent<FloorFood>(out _)) gameObject.AddComponent<FloorFood>(); // 바닥 식품은 상온 부패(2026-10-12)
        }

        // 원재료 노드(2026-10-10) 전용. regrow > 0이면 다 캔 뒤 같은 양으로 다시 자란다(식물). 활성화 전에 호출.
        public void ConfigureNatural(ResourceType type, float amount, float regrow)
        {
            IsLooseCargo = false; resourceType = type; amountRemaining = Mathf.Max(0f, amount);
            regrowSeconds = Mathf.Max(0f, regrow); regrowAmount = regrow > 0 ? amountRemaining : 0f; requiresFishing = false; ownerColony = null;
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
        // 밭 전용(2026-10-15): 빈 밭에 다시 심기 / 작물 변경 직후 새 성장 / 뿌리뽑기로 비우기.
        internal void StartSowing() { amountRemaining = 0; regrowTimer = regrowSeconds; sowRemaining = GameBalance.SowSeconds; }
        internal void RestartGrowth() { if (regrowTimer > 0) regrowTimer = regrowSeconds; }
        internal void ClearCrop() { amountRemaining = 0; regrowTimer = 0; sowRemaining = 0; BountifulHarvest = false; }

        // 목장 생물이 밭 작물을 먹는다(2026-10-11): 다 자란 작물은 수확량이 줄고, 반 이상 자란 작물은 성장이 되돌아간다. 다 먹히면 다시 파종해야 한다.
        public bool CropEdible => isActiveAndEnabled && IsFarm && !NeedsSowing && (!IsRegrowing ? amountRemaining > DepletedEpsilon : regrowTimer <= regrowSeconds * .5f);
        public bool EatCrop(float amount)
        {
            if (!CropEdible) return false;
            if (IsRegrowing) { BurnCrop(amount / Mathf.Max(1f, regrowAmount)); if (regrowTimer >= regrowSeconds) sowRemaining = GameBalance.SowSeconds; return true; }
            amountRemaining = Mathf.Max(0, amountRemaining - amount);
            if (IsDepleted) { regrowTimer = regrowSeconds; sowRemaining = GameBalance.SowSeconds; BountifulHarvest = false; }
            return true;
        }

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
