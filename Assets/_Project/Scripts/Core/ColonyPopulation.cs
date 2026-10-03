using System;
using System.Linq;
using AntColony.Buildings;
using AntColony.Data;
using UnityEngine;

namespace AntColony.Core
{
    // 병역 제도(문명 정책식). 모병제는 처음부터, 나머지는 과학 연구로 해금.
    public enum MilitaryPolicy { Volunteer, Conscription, Reserve, Total }

    // Phase 4(2026-10-01): 시티즈식 이주 인구. 여왕방 생산을 대체한다. 수치는 전부 잠정.
    // 성체 개미 = AntPool(일·병력), 어린·늙은 개미는 숫자로만. 본거지 하나에만 있다.
    public sealed class ColonyPopulation : MonoBehaviour
    {
        [Serializable] public sealed class State
        {
            public int young, old;
            public float sentiment = 60, taxRate = .2f, monthSeconds, raidSeconds = 9999;
            public MilitaryPolicy policy;
            public bool elderlyService;
        }
        public static ColonyPopulation Instance { get; private set; }
        public State S = new State();

        private void Awake() { if (Instance != null && Instance != this) { Destroy(this); return; } Instance = this; }
        private void OnEnable() => UI.EnemyAlert.Alarm += OnRaid;
        private void OnDisable() => UI.EnemyAlert.Alarm -= OnRaid;
        private void OnDestroy() { if (Instance == this) Instance = null; }
        private void OnRaid() { if (S.raidSeconds >= GameCalendar.SecondsPerMonth) S.sentiment = Mathf.Max(0, S.sentiment - GameBalance.RaidSentiment); S.raidSeconds = 0; }

        public State Capture() => JsonUtility.FromJson<State>(JsonUtility.ToJson(S));
        public void Restore(State s) => S = s == null ? new State() : JsonUtility.FromJson<State>(JsonUtility.ToJson(s));

        public static int Adults => AntPool.Instance != null ? AntPool.Instance.Total : 0;
        public int Total => S.young + Adults + S.old;
        public int HousingCapacity => GameBalance.BaseHousing + Housing.TotalCapacity;
        public bool Unrest => S.sentiment <= GameBalance.UnrestSentiment;
        public static int Facilities => FindObjectsByType<BuildingBase>(FindObjectsSortMode.None).Count(b => b.isActiveAndEnabled && !b.IsDead
            && (b is Infirmary || b is RestRoom || b is Decoration || b is RecreationSpot || b is Kitchen));

        // ── 세금: 30초마다 Food. 징병제 이상은 출전 병사가 세금을 안 내고, 국민개병은 세금 급감.
        public int Taxpayers => Mathf.Max(0, Adults - (S.policy == MilitaryPolicy.Volunteer ? 0 : AntPool.Instance?.Assigned ?? 0)) + S.old;
        public int TaxPerCycle => Mathf.RoundToInt(Taxpayers * S.taxRate * GameBalance.TaxFoodPerAnt * (S.policy == MilitaryPolicy.Total ? .5f : 1f));
        public void SetTaxRate(float rate) => S.taxRate = Mathf.Clamp(Mathf.Round(rate * 20f) / 20f, 0f, GameBalance.MaxTaxRate);

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
        public int MaxSoldiers(bool homeThreat) => Unrest ? 0 : Mathf.FloorToInt((Adults + (S.elderlyService ? S.old : 0)) * PolicyRate(S.policy, homeThreat));

        // 출전 직전 호출. 상한·민심을 확인하고, 병역 나이 확대면 모자란 성체를 늙은 개미로 채운다.
        public bool TryDraft(int count, bool homeThreat)
        {
            var pool = AntPool.Instance;
            if (pool == null || count <= 0 || pool.Assigned + count > MaxSoldiers(homeThreat)) return false;
            if (pool.Free < count && S.elderlyService)
            {
                var elders = Mathf.Min(S.old, count - pool.Free);
                S.old -= elders; pool.Breed(elders);
            }
            return pool.Free >= count;
        }

        // ── 이주 수요 0~100: 식량·시설·안전·민심 평균.
        public float FoodDemand => Mathf.Clamp01((ResourceManager.Instance?.GetAmount(ResourceType.Food) ?? 0) / Mathf.Max(1f, Total * 2f)) * 100f;
        public float FacilityDemand => Mathf.Min(100f, Facilities * 15f);
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
        public float Demand => (FoodDemand + FacilityDemand + SafetyDemand + S.sentiment) / 4f;

        private void Update() => Tick(Time.deltaTime);
        public void Tick(float seconds)
        {
            if (!(seconds > 0) || float.IsInfinity(seconds) || !GameSession.Exists || !GameSession.Instance.GameStarted) return;
            S.raidSeconds += seconds;
            S.monthSeconds += seconds;
            while (S.monthSeconds >= GameCalendar.SecondsPerMonth) { S.monthSeconds -= GameCalendar.SecondsPerMonth; Monthly(); }
        }

        // 달마다: 나이 → 민심 → 이주 → 탈주 순.
        public void Monthly()
        {
            var pool = AntPool.Instance; if (pool == null) return;
            var grown = S.young; S.young = 0; pool.Breed(grown);
            var aging = pool.RemoveFree(Mathf.RoundToInt(pool.Free * GameBalance.AdultAging), false);
            S.old = S.old + aging - Mathf.RoundToInt(S.old * GameBalance.OldDeath);
            var food = ResourceManager.Instance?.GetAmount(ResourceType.Food) ?? 0;
            var target = 60f - (S.taxRate - .2f) * 100f - PolicySentiment(S.policy) + (food >= Total ? 5 : -15)
                + (Total > HousingCapacity ? -10 : 0) + Mathf.Min(10, Facilities * 2) + (S.raidSeconds < GameCalendar.SecondsPerMonth ? -10 : 0);
            S.sentiment = Mathf.Clamp(S.sentiment + Mathf.Clamp(target - S.sentiment, -10, 10), 0, 100);
            var room = HousingCapacity - Total;
            var demand = Demand;
            if (room > 0 && demand >= GameBalance.MinImmigrationDemand)
            {
                var arrivals = Mathf.Clamp(Mathf.RoundToInt(room * demand / 100f * GameBalance.ImmigrationShare), 1, room);
                S.young += arrivals * 3 / 10; pool.Breed(arrivals - arrivals * 3 / 10);
                UI.ToastManager.Show($"이주: 개미 {arrivals}마리가 왔습니다 (수요 {demand:0})");
            }
            if (Unrest)
            {
                var left = pool.RemoveFree(Mathf.CeilToInt(pool.Free * GameBalance.UnrestDesertion), true);
                if (left > 0) UI.ToastManager.Show($"민심 바닥: 개미 {left}마리가 떠났습니다");
            }
        }

        // 식량 부족: 개미가 그냥 사라진다(대기 개미 기준).
        public int Starve() => AntPool.Instance?.RemoveFree(Mathf.CeilToInt(AntPool.Instance.Free * GameBalance.StarveDesertion), true) ?? 0;
    }
}
