using System;
using System.Linq;
using AntColony.Buildings;
using AntColony.Data;
using UnityEngine;

namespace AntColony.Core
{
    // 병역 제도(문명 정책식). 모병제는 처음부터, 나머지는 과학 연구로 해금.
    public enum MilitaryPolicy { Volunteer, Conscription, Reserve, Total }
    // 납세 방침(2026-10-08 기획). 식량·재료 비율은 잠정.
    public enum TaxFocus { Balanced, Food, Material }

    // Phase 4(2026-10-01): 시티즈식 이주 인구. 여왕방 생산을 대체한다. 수치는 전부 잠정.
    // 성체 개미 = AntPool(일·병력), 어린·늙은 개미는 숫자로만. 본거지 하나에만 있다.
    public sealed partial class ColonyPopulation : MonoBehaviour
    {
        [Serializable] public sealed class State
        {
            public int young, old;
            public int agingDue; // 노년 집계에 포함됐지만 풀에서는 복귀 때 제거할 동원·예약 인원.
            public int[] ages; // 0~64세 집계. 65세 이상은 old로 집계한다.
            public float immigrationSeconds, immigrationRemainder, ageSeconds;
            public bool immigrationStopped;
            public float educationBonus, educationSeconds;
            public float schoolFoodDebt, schoolMaterialDebt, clinicFoodDebt, clinicMedicineDebt;
            public bool schoolsSuspended, clinicsSuspended;
            public int nextCivilianLocation = 1;
            public System.Collections.Generic.List<CivilianLocation> civilianLocations = new System.Collections.Generic.List<CivilianLocation>();
            public float sentiment = 60, taxRate = .2f, monthSeconds, raidSeconds = 9999;
            public MilitaryPolicy policy;
            public bool elderlyService;
            public TaxFocus taxFocus;
            public float taxWeek, taxFood, taxSoil; // 이번 납세 주기 경과 시간(저장 키 유지)·누적 세액(소수 포함). 납세 때 정수만 지급한다.
            public float adminWork; // 행정 업무 누적(서서히 감소). 성과 = 업무량 / 필요량(ColonyPopulation.Admin.cs).
            public float redevelopCompensation = 1f; // 재개발 보상 수준(기본 보상 배율).
            public ResourceType taxMaterial = ResourceType.Soil; // 우선 납부 재료(2026-10-10). 지역 기초 원재료 중 하나.
        }
        public static ColonyPopulation Instance { get; private set; }
        public State S = new State();

        private void Awake() { if (Instance != null && Instance != this) { Destroy(this); return; } Instance = this; }
        private void OnEnable() => UI.EnemyAlert.Alarm += OnRaid;
        private void OnDisable() => UI.EnemyAlert.Alarm -= OnRaid;
        private void OnDestroy() { ClearCivilianViews(); if (Instance == this) Instance = null; }
        private void OnRaid() { if (S.raidSeconds >= GameCalendar.SecondsPerMonth) S.sentiment = Mathf.Max(0, S.sentiment - GameBalance.RaidSentiment); S.raidSeconds = 0; }

        public State Capture() => JsonUtility.FromJson<State>(JsonUtility.ToJson(S));
        public void Restore(State s)
        {
            S = s == null ? new State() : JsonUtility.FromJson<State>(JsonUtility.ToJson(s));
            if (S.ages == null || S.ages.Length == 0) // JsonUtility는 없는 배열을 빈 배열로 복원한다.
            {
                // 구 저장의 agingDue는 아직 old에 포함되지 않았던 인원이다.
                S.old += S.agingDue; S.ages = new int[65];
            }
            S.civilianLocations ??= new System.Collections.Generic.List<CivilianLocation>();
            S.nextCivilianLocation = Mathf.Max(1, S.nextCivilianLocation);
            civilianPaths.Clear();
            ClearCivilianViews();
        }

        public static int Adults => AntPool.Instance != null ? Mathf.Max(0, AntPool.Instance.Total - (Instance != null ? Instance.S.agingDue : 0)) : 0;
        public int Total => S.young + Adults + S.old;
        public int HousingCapacity => GameBalance.BaseHousing + Housing.TotalCapacity;
        public bool Unrest => S.sentiment <= GameBalance.UnrestSentiment;
        public static int Facilities => FindObjectsByType<BuildingBase>(FindObjectsSortMode.None).Count(b => b.isActiveAndEnabled && !b.IsDead
            && (b is Infirmary || b is RestRoom || b is Decoration || b is RecreationSpot || b is Kitchen));

        // ── 세금(2026-10-08): 시민 현물 납세. 2026-10-23 G08부터 매일 아침 식량·재료 지급. 누적은 초 단위라 주기 중 변화는 그 시간만 반영된다.
        // 동원 중 병력·어린 개미는 납세 제외(모든 병역 제도). 국민개병은 세금 절반.
        // 시민 주거 주변 미관: 민심 보정에 사용한다(±5, 2초마다 다시 계산).
        private float housingBeauty, housingBeautyAt = -10;
        public float HousingBeauty
        {
            get
            {
                if (Time.unscaledTime - housingBeautyAt < 2f) return housingBeauty;
                housingBeautyAt = Time.unscaledTime;
                var homes = FindObjectsByType<Housing>(FindObjectsSortMode.None).Where(h => h.isActiveAndEnabled && !h.IsDead).ToArray();
                housingBeauty = homes.Length == 0 ? 0 : Mathf.Clamp(homes.Average(h => SpaceQuality.BeautyAt(h.Position)), -5, 5);
                return housingBeauty;
            }
        }
        public static float TaxSeconds => GameCalendar.SecondsPerDay; // 매일 아침 납세(2026-10-23 G08). 월 세액 기준 유지, 하루 몫 = 월 × 150/300
        public int Taxpayers => Mathf.Max(0, Total - S.young - (AntPool.Instance?.Assigned ?? 0));
        public static float FoodShare(TaxFocus f) => f switch { TaxFocus.Food => .8f, TaxFocus.Material => .2f, _ => .5f };
        public static string FocusName(TaxFocus f) => f switch { TaxFocus.Food => "식량 중심", TaxFocus.Material => "재료 중심", _ => "균형" };
        public float MonthlyTax => Taxpayers * S.taxRate * GameBalance.TaxPerAntMonth * (S.policy == MilitaryPolicy.Total ? .5f : 1f) * Productivity * TaxCollection * (1f + S.educationBonus);
        public int DailyFood => Mathf.RoundToInt(MonthlyTax * FoodShare(S.taxFocus) * TaxSeconds / GameCalendar.SecondsPerMonth);
        public int DailySoil => Mathf.RoundToInt(MonthlyTax * (1f - FoodShare(S.taxFocus)) * TaxSeconds / GameCalendar.SecondsPerMonth);
        public float NextTaxSeconds => Mathf.Max(0, TaxSeconds - S.taxWeek);
        public void SetTaxRate(float rate) => S.taxRate = Mathf.Clamp(Mathf.Round(rate * 100f) / 100f, 0f, GameBalance.MaxTaxRate);
        public void SetTaxFocus(TaxFocus f) { if (Enum.IsDefined(typeof(TaxFocus), f)) S.taxFocus = f; }
        private void AccrueTax(float seconds)
        {
            var month = MonthlyTax * seconds / GameCalendar.SecondsPerMonth;
            S.taxFood += month * FoodShare(S.taxFocus); S.taxSoil += month * (1f - FoodShare(S.taxFocus));
        }
        public void PayTax()
        {
            int food = Mathf.FloorToInt(S.taxFood), soil = Mathf.FloorToInt(S.taxSoil);
            S.taxFood -= food; S.taxSoil -= soil;
            var rm = ResourceManager.Instance; if (rm == null) return;
            rm.Add(ResourceType.Food, food, ResourceReason.Tax);
            // 재료 세금(2026-10-10): 지역 기초 원재료로 받고, 우선 납부 재료가 대부분을 차지한다. 맵 매장량과는 무관하다.
            var basics = TaxMaterials; var main = Array.IndexOf(basics, S.taxMaterial) >= 0 ? S.taxMaterial : basics[0];
            int first = Mathf.CeilToInt(soil * GameBalance.TaxMainMaterialShare), rest = soil - first, others = basics.Length - 1;
            rm.Add(main, first, ResourceReason.Tax);
            int k = 0; foreach (var m in basics) if (m != main) { rm.Add(m, rest / others + (k++ < rest % others ? 1 : 0), ResourceReason.Tax); }
        }
        // 지역 기초 원재료에 G14 공통 목재·석재를 합친다. 기존 세액 총량은 나눠 지급한다.
        public static ResourceType[] TaxMaterials => (BiomeRules.Current switch
        {
            MapBiome.Forest => new[] { ResourceType.Wood, ResourceType.Leaf, ResourceType.Soil },
            MapBiome.Garden => new[] { ResourceType.Fiber, ResourceType.Soil, ResourceType.Leaf },
            MapBiome.Waterside => new[] { ResourceType.Clay, ResourceType.Sand, ResourceType.Soil },
            MapBiome.City => new[] { ResourceType.Sand, ResourceType.Stone, ResourceType.Soil },
            MapBiome.Desert => new[] { ResourceType.Sand, ResourceType.Stone, ResourceType.Clay },
            MapBiome.Cave => new[] { ResourceType.Stone, ResourceType.Clay, ResourceType.Soil },
            _ => new[] { ResourceType.Soil, ResourceType.Wood, ResourceType.Stone }
        }).Concat(new[] { AntColony.Data.ResourceType.Wood, AntColony.Data.ResourceType.Stone }).Distinct().ToArray();
        public void SetTaxMaterial(ResourceType m) { if (Array.IndexOf(TaxMaterials, m) >= 0) S.taxMaterial = m; }

        // ── 병역: 제도별 병력 상한(성체 기준, 병역 나이 확대 시 늙은 개미 포함). 예비군제는 침입 중에만 25%.
        public static float PolicyRate(MilitaryPolicy p, bool homeThreat) => p switch
        {
            MilitaryPolicy.Conscription => .15f, MilitaryPolicy.Reserve => homeThreat ? .25f : .05f, MilitaryPolicy.Total => .4f, _ => .05f
        };
        public static int PolicySentiment(MilitaryPolicy p) => p switch { MilitaryPolicy.Conscription => 5, MilitaryPolicy.Reserve => 10, MilitaryPolicy.Total => 20, _ => 0 };
        public static ScienceTechnology? PolicyTech(MilitaryPolicy p) => p switch
        {
            MilitaryPolicy.Conscription => ScienceTechnology.ConscriptionLaw, MilitaryPolicy.Reserve => ScienceTechnology.ReserveForces,
            MilitaryPolicy.Total => ScienceTechnology.TotalMobilization, _ => null
        };
        public static string PolicyName(MilitaryPolicy p) => p switch
        { MilitaryPolicy.Conscription => "징병제", MilitaryPolicy.Reserve => "예비군제", MilitaryPolicy.Total => "국민개병", _ => "모병제" };
        public bool CanUse(MilitaryPolicy p) { var t = PolicyTech(p); return t == null || CampaignResearch.Instance != null && CampaignResearch.Instance.Has(t.Value); }
        public bool TrySetPolicy(MilitaryPolicy p) { if (!Enum.IsDefined(typeof(MilitaryPolicy), p) || !CanUse(p)) return false; S.policy = p; return true; }
        // G04(2026-10-23): 상한·편성 계산은 ColonyPopulation.Military.cs로 일원화.
        public int MaxSoldiers(bool homeThreat) => MobilizationLimit(homeThreat);
        public bool TryDraft(int count, bool homeThreat) => TryMilitaryDraft(count, homeThreat);

        // ── 이주 수요 0~100: 주거·공공서비스·안전·민심 평균(2026-10-08: 창고 식량 제외).
        public float HousingDemand => Mathf.Clamp01((HousingCapacity - Total) / Mathf.Max(1f, HousingCapacity * .1f)) * 100f;
        public float FacilityDemand => PublicServiceCoverage * 100f;
        // 안전: 지난 한 달 침입이 있으면 30. 성벽이 있으면 성벽 밖 주거 비율만큼 최대 -50(Phase 5).
        public float SafetyDemand
        {
            get
            {
                var value = S.raidSeconds < GameCalendar.SecondsPerMonth ? 30f : 100f;
                var homes = Housing.All.ToList();
                if (!RoomSystem.HasCastleWalls || homes.Count == 0) return value;
                return value * (1f - .5f * homes.Count(h => !RoomSystem.InsideCastle(h.Position)) / homes.Count);
            }
        }
        // 기존 HUD는 백분율을 표시하므로 D=1을 100으로 노출한다.
        public float ImmigrationDemand => (Total == 0 ? 1f : GameBalancePopulation.Demand(
            FacilityDemand / 100f * .5f + MedicalCoverage * .5f, (SafetyDemand - 100f) / 100f * .5f,
            (S.sentiment - 60f) / 100f, GameBalance.AdminDemand * Administration / 100f)) * PublicServiceCoverage;
        public float Demand => ImmigrationDemand * 100f;

        public void SetImmigrationStopped(bool stopped) => S.immigrationStopped = stopped;
        public void Immigrate()
        {
            var pool = AntPool.Instance;
            if (pool == null || S.immigrationStopped) return;
            var room = Mathf.Max(0, HousingCapacity - Total);
            S.immigrationRemainder += room * GameBalancePopulation.ImmigrationShare * ImmigrationDemand;
            var arrivals = Mathf.Min(room, Mathf.FloorToInt(S.immigrationRemainder));
            S.immigrationRemainder -= arrivals;
            if (arrivals <= 0) return;
            EnsureAges();
            var groups = GameBalancePopulation.RollArrivalAges(arrivals);
            AddAgeGroup(0, 18, groups[0]); S.young += groups[0];
            AddAgeGroup(18, 65, groups[1]); pool.Breed(groups[1]); S.old += groups[2];
            UI.ToastManager.Show($"이주: 시민 {arrivals}명이 왔습니다 (수요 {Demand:0})");
        }

        private void Update() => Tick(Time.deltaTime);
        public void Tick(float seconds)
        {
            if (!(seconds > 0) || float.IsInfinity(seconds) || !GameSession.Exists || !GameSession.Instance.GameStarted) return;
            while (seconds > 0)
            {
                var step = Mathf.Min(seconds, TaxSeconds - S.taxWeek, GameCalendar.SecondsPerMonth - S.monthSeconds,
                    GameCalendar.SecondsPerYear - S.ageSeconds,
                    S.educationSeconds > 0 ? S.educationSeconds : float.PositiveInfinity,
                    S.immigrationStopped ? float.PositiveInfinity : GameBalancePopulation.ImmigrationSeconds - S.immigrationSeconds);
                S.raidSeconds += step;
                AccrueTax(step); S.taxWeek += step; TickAdministration(step);
                TickCivicEconomy(step); TickCivilianLocations(step);
                S.monthSeconds += step; S.ageSeconds += step;
                if (!S.immigrationStopped) S.immigrationSeconds += step;
                seconds -= step;
                if (S.taxWeek >= TaxSeconds) { S.taxWeek -= TaxSeconds; PayTax(); PayCivicUpkeep(); }
                if (S.ageSeconds >= GameCalendar.SecondsPerYear) { S.ageSeconds -= GameCalendar.SecondsPerYear; AgeYear(); }
                if (S.monthSeconds >= GameCalendar.SecondsPerMonth) { S.monthSeconds -= GameCalendar.SecondsPerMonth; Monthly(); }
                if (S.immigrationSeconds >= GameBalancePopulation.ImmigrationSeconds) { S.immigrationSeconds -= GameBalancePopulation.ImmigrationSeconds; Immigrate(); }
            }
        }

        // 기존 300초 주기: 민심·탈주. 이주·연령은 별도 주기다.
        public void Monthly()
        {
            var pool = AntPool.Instance; if (pool == null) return;
            // 세율이 높을수록 불이익이 가파르다(30% 초과분 제곱 가산, 잠정). 창고 식량은 시민 민심에 반영하지 않는다.
            var over = Mathf.Max(0, S.taxRate - .3f);
            var target = 60f - (S.taxRate - .2f) * 100f - over * over * 500f - PolicySentiment(S.policy)
                + (Total > HousingCapacity ? -10 : 0) + Mathf.Min(10, Facilities * 2) + CivicSentiment + (S.raidSeconds < GameCalendar.SecondsPerMonth ? -10 : 0) + GameBalance.AdminSentiment * Administration + GameBalance.HousingBeautySentiment * HousingBeauty;
            S.sentiment = Mathf.Clamp(S.sentiment + Mathf.Clamp(target - S.sentiment, -10, 10), 0, 100);
            if (Unrest)
            {
                var left = pool.RemoveFree(Mathf.CeilToInt(pool.Free * GameBalance.UnrestDesertion), true);
                if (left > 0) UI.ToastManager.Show($"민심 바닥: 시민 {left}명이 떠났습니다");
            }
            // 과도한 징세는 민심 보정(시설·행정)으로 완전히 지울 수 없다: 세율 40% 초과분에 비례해 매달 일부가 떠난다(잠정).
            var taxFlight = Mathf.Clamp01((S.taxRate - GameBalance.HighTaxFlightRate) / (GameBalance.MaxTaxRate - GameBalance.HighTaxFlightRate));
            if (!Unrest && taxFlight > 0)
            {
                var fled = pool.RemoveFree(Mathf.CeilToInt(pool.Free * GameBalance.UnrestDesertion * taxFlight), true);
                if (fled > 0) UI.ToastManager.Show($"높은 세금: 시민 {fled}명이 떠났습니다");
            }
        }

        // 이미 노년으로 집계한 인원을 복귀 시 풀에서 정산한다.
        internal void OnAntsReturned(int returned)
        {
            var pool = AntPool.Instance; if (pool == null || S.agingDue <= 0) return;
            var aged = pool.RemoveFree(Mathf.Min(S.agingDue, returned), false);
            S.agingDue -= aged;
        }

        internal void OnAntsLost(int count)
        {
            var elders = Mathf.Min(S.agingDue, count);
            S.agingDue -= elders; S.old -= elders;
        }
    }
}
