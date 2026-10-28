using System.Collections.Generic;
using AntColony.Core;
using AntColony.Buildings;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;
using UnityEngine.AI;

namespace AntColony.World
{
    // 적 소굴이 일정 시간마다 침공 부대를 보낸다. 수치는 전부 임시값이다.
    [RequireComponent(typeof(EnemyColony))]
    public class ColonyInvasion : MonoBehaviour
    {
        [SerializeField] private WildMonster raiderTemplate; // 비활성 상태의 템플릿
        // 비워두면 적 장수 없이 기존과 똑같이 동작한다.
        [SerializeField] private EnemyCommander commanderTemplate;
        [SerializeField, Min(0)] private int commanderFoodCost = 10;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private float firstWaveDelay = 90f;
        [SerializeField] private float waveInterval = 120f;
        // 난이도 배수를 먹인 실제 간격. Normal이면 배수 1.0이라 기존 동작과 같다.
        private float ScaledWaveInterval => waveInterval * AntColony.Core.DifficultyRuntime.IntervalScale;
        [SerializeField] private int firstWaveCount = 2;
        [SerializeField] private int waveCountGrowth = 1;
        [SerializeField] private int maxWaveCount = 6;
        [SerializeField] private int maxActiveRaiders = 12;
        [SerializeField] private float spawnScatter = 3f;
        // 스폰이 한 마리도 성공하지 못한 파동은 통째로 사라지지 않고 이 간격으로 다시 시도한다.
        [SerializeField, Min(1f)] private float waveRetryDelay = 15f;

        [Header("Economy")]
        // ponytail: 미정인 AI 성장 속도의 1차 프로토타입 값이다. 난이도 설정이 생기면 그 값을 사용한다.
        [SerializeField, Min(0.1f)] private float economyTickSeconds = 10f;
        [SerializeField, Min(0)] private int foodIncomePerBuilding = 2;
        [SerializeField, Min(0)] private int soilIncomePerBuilding = 1;
        [SerializeField, Min(0)] private int raiderFoodCost = 5;
        [SerializeField, Min(0)] private int expansionFoodCost = 0;
        [SerializeField, Min(0)] private int expansionSoilCost = 30;
        [SerializeField, Min(1)] private int maxBuildings = 5;
        [SerializeField, Min(0.1f)] private float expansionRadius = 5f;

        private readonly List<WildMonster> raiders = new List<WildMonster>();
        private EnemyColony colony;
        private float timer;
        private float economyTimer;
        private int waveIndex;

        private static ColonyInvasion FinalSource()
        {
            foreach (var source in FindObjectsByType<ColonyInvasion>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID))
                if (source.GetComponentInParent<ExpeditionSite>() == null && source.raiderTemplate != null && source.spawnPoint != null) return source;
            return null;
        }
        public static bool FinalAssaultAvailable => FinalSource() != null && AntColony.Core.SceneObjects.Any<LocalIncursions>() != null;
        internal static float FinalFirstDelay => FinalSource()?.firstWaveDelay * DifficultyRuntime.IntervalScale ?? 90f * DifficultyRuntime.IntervalScale;
        // 잠정(결정 원문에 없음): 인구 10명/재산 100당 파동 병력 +1. 기존 파동 상한·난이도는 유지한다.
        private const float FinalPopulationPerRaider = 10f, FinalWealthPerRaider = 100f;
        private int WaveCount(int index, RocketSnapshot snapshot = null)
        {
            var strength = snapshot == null ? 0 : snapshot.Population / FinalPopulationPerRaider + snapshot.Wealth / FinalWealthPerRaider;
            var baseline = firstWaveCount + (double)index * waveCountGrowth + strength;
            return Mathf.Min(DifficultyRuntime.ScaleCount((int)System.Math.Min(int.MaxValue / 4, baseline)), maxWaveCount);
        }
        // G45 가동 전 미리보기: 고정 입력으로 계산한 첫 파동 병력(생성기가 없으면 -1).
        public static int PredictedFirstWave(RocketSnapshot snapshot) { var source = FinalSource(); return source == null ? -1 : source.WaveCount(0, snapshot); }
        // 평시 위기 차단과 별도인 최종 공세. 기존 생성 위치·분산·병력 상한·장수 구성을 재사용한다.
        internal static bool SpawnFinalWave(RocketSnapshot snapshot, int index, out float nextDelay)
        {
            var source = FinalSource();
            var local = AntColony.Core.SceneObjects.Any<LocalIncursions>();
            nextDelay = source != null ? source.waveRetryDelay : 15f;
            if (source == null || local == null) return false;
            source.raiders.RemoveAll(r => r == null || r.IsDead);
            var active = 0;
            foreach (var visitor in local.Visitors) if (visitor != null && !visitor.IsDead) active++;
            var count = Mathf.Min(source.WaveCount(index, snapshot), source.maxActiveRaiders - active);
            var spawned = 0; var fallback = 0;
            var elapsed = Mathf.Max(0, GameCalendar.GameSeconds - snapshot.at);
            for (var i = 0; i < count; i++)
            {
                Vector3? approach = null;
                if (FinalAssaultPlan.Phase(elapsed) > 0 && TryFinalApproach(elapsed, index, i, out var point)) approach = point;
                if (source.Spawn(false, approach)) { spawned++; if (FinalAssaultPlan.Phase(elapsed) > 0 && !approach.HasValue) fallback++; }
            }
            if (spawned == 0) return false;
            var hasCommander = false;
            foreach (var visitor in local.Visitors)
                if (visitor is EnemyCommander && !visitor.IsDead) hasCommander = true;
            if (!hasCommander) source.SpawnCommander(false);
            nextDelay = source.ScaledWaveInterval;
            AntColony.UI.ToastManager.Show(FinalAssaultPlan.Description(elapsed) + " · 최종 공세 " + spawned + "마리"
                + (fallback > 0 ? $" · 외곽 경로 부족으로 기존 진입 {fallback}마리" : ""));
            return true;
        }

        private static bool TryFinalApproach(float elapsed, int wave, int member, out Vector3 point)
        {
            point = default;
            var home = RocketCampaign.Instance?.Home ?? Vector3.zero;
            if (!NavMesh.SamplePosition(home, out var center, 10, NavMesh.AllAreas)) return false;
            var homes = AntColony.Core.SceneObjects.All<BuildingBase>();
            var radius = 45f;
            foreach (var b in homes)
                if (b.isActiveAndEnabled && !b.IsDead && b.CountsTowardPlayerDefeat && b.GetComponentInParent<ExpeditionSite>() == null)
                    radius = Mathf.Max(radius, Vector3.Distance(home, b.Position) + 15);
            var candidate = center.position + FinalAssaultPlan.Direction(elapsed, wave, member) * radius;
            if (!NavMesh.SamplePosition(candidate, out var hit, 8, NavMesh.AllAreas)) return false;
            foreach (var b in homes)
                if (b.isActiveAndEnabled && !b.IsDead && b.CountsTowardPlayerDefeat && (b.Position - hit.position).sqrMagnitude < 144) return false;
            var path = new NavMeshPath();
            if (!NavMesh.CalculatePath(hit.position, center.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete) return false;
            point = hit.position; return true;
        }

        private void Awake()
        {
            colony = GetComponent<EnemyColony>();
            timer = firstWaveDelay * AntColony.Core.DifficultyRuntime.IntervalScale;
            economyTimer = economyTickSeconds;
            WarnMissingStock();
        }

        // 저장 복원 전용.
        internal void RestoreTimers(float waveTimer, float economy, int savedWaveIndex)
        {
            timer = Mathf.Max(0f, waveTimer);
            economyTimer = Mathf.Max(0f, economy);
            waveIndex = Mathf.Max(0, savedWaveIndex);
        }

        // 공세 병력 상한과 저장 복원 검증에 쓴다.
        internal int ActiveRaiderCount
        {
            get
            {
                var count = 0;
                foreach (var raider in raiders) if (raider != null && !raider.IsDead) count++;
                return count;
            }
        }

        internal WildMonster RestoreSpawn(bool commander, Vector3 position) =>
            Instantiate(commander ? commanderTemplate : raiderTemplate, position, Quaternion.identity);
        internal bool Owns(WildMonster monster) => raiders.Contains(monster);
        internal void RestoreRaider(WildMonster monster) { if (!raiders.Contains(monster)) raiders.Add(monster); }
        internal float WaveTimer => timer;
        internal float EconomyTimer => economyTimer;
        internal int WaveIndex => waveIndex;

        // 전리품 노드 설정이 빠지면 경제가 조용히 멈추므로 시작할 때 알린다.
        private void WarnMissingStock()
        {
            if (raiderFoodCost > 0 && !colony.HasResourceNode(AntColony.Data.ResourceType.Food))
                Debug.LogWarning($"{name}: Food 전리품 노드가 없어 침공 자원 소비를 건너뛴다.", this);
            if (expansionSoilCost > 0 && !colony.HasResourceNode(AntColony.Data.ResourceType.Soil))
                Debug.LogWarning($"{name}: Soil 전리품 노드가 없어 자동 확장을 건너뛴다.", this);
        }

        private void Update()
        {
            TickEconomy();
            if (RocketCampaign.SuppressNewCrises) return;
            // 월드 거점의 출발·이동은 DiplomacyManager가 관리한다. 로컬 소굴만 이 경로를 사용한다.
            if (GetComponentInParent<ExpeditionSite>() != null) return;
            if (ExpeditionRules.HomeCommanders == 0) return;
            // 밤 위협(2026-09-28 확정): 밤에는 침공이 1.5배 잦다.
            timer -= Time.deltaTime * (GameCalendar.IsNight ? EventRules.NightThreatScale : 1f);
            if (timer > 0f) return;

            if (raiderTemplate == null || spawnPoint == null
                || !colony.isActiveAndEnabled || colony.RemainingBuildings == 0
                || GameManager.Instance?.FindNearestPlayerBuilding(spawnPoint.position) == null)
            {
                timer = ScaledWaveInterval;
                return;
            }

            raiders.RemoveAll(raider => raider == null || raider.IsDead);
            var count = WaveCount(waveIndex);
            count = Mathf.Min(count, maxActiveRaiders - raiders.Count);
            if (count <= 0)
            {
                timer = ScaledWaveInterval;
                return;
            }

            var spawned = 0;
            for (var i = 0; i < count; i++)
                if (Spawn()) spawned++;

            // 병력이 실제로 나간 파동에만 장수가 따라붙는다. 빈 파동에 장수만 보내지 않는다.
            if (spawned > 0) SpawnCommander();
            if (spawned > 0) AntColony.UI.ToastManager.Show("침공 접근 중: " + spawned + "마리");

            // 자원 부족이나 스폰 위치 실패로 한 마리도 못 냈다면 다음 정규 파동까지 기다리지 않는다.
            timer = spawned > 0 ? ScaledWaveInterval : waveRetryDelay;
            if (spawned > 0 && waveIndex < maxWaveCount) waveIndex++;
        }

        private void TickEconomy()
        {
            economyTimer -= Time.deltaTime;
            if (economyTimer > 0f) return;
            economyTimer = economyTickSeconds;
            if (!colony.isActiveAndEnabled || colony.IsDefeated) return;

            var buildings = colony.RemainingBuildings;
            colony.AddResources(buildings * foodIncomePerBuilding, buildings * soilIncomePerBuilding);
            colony.TryExpand(expansionFoodCost, expansionSoilCost, maxBuildings, expansionRadius);
        }

        private bool Spawn(bool payCost = true, Vector3? finalApproach = null)
        {
            var scatter = Random.insideUnitCircle * spawnScatter;
            var origin = finalApproach ?? spawnPoint.position + new Vector3(scatter.x, 0f, scatter.y);
            if (!NavMesh.SamplePosition(origin, out var hit, spawnScatter + 2f, NavMesh.AllAreas)) return false;
            if (payCost && !TryPayRaiderCost()) return false;

            // 소굴이 파괴돼도 침공 부대는 남도록 소굴 밑에 붙이지 않는다.
            var raider = Instantiate(raiderTemplate, hit.position, spawnPoint.rotation);
            raider.MakeRaider();
            raider.gameObject.SetActive(true);
            raiders.Add(raider);
            if (!payCost) AntColony.Core.SceneObjects.Any<LocalIncursions>()?.RestoreVisitor(raider);
            return true;
        }

        // 파동을 이끄는 적 장수. 쓰러지면 플레이어의 포로 수용소로 넘어간다.
        private void SpawnCommander(bool payCost = true)
        {
            if (commanderTemplate == null) return;
            if (raiders.Exists(raider => raider is EnemyCommander)) return;

            var scatter = Random.insideUnitCircle * spawnScatter;
            var origin = spawnPoint.position + new Vector3(scatter.x, 0f, scatter.y);
            if (!NavMesh.SamplePosition(origin, out var hit, spawnScatter + 2f, NavMesh.AllAreas)) return;
            if (payCost && commanderFoodCost > 0 && colony.HasResourceNode(AntColony.Data.ResourceType.Food)
                && !colony.TrySpendResources(commanderFoodCost, 0)) return;

            var commander = Instantiate(commanderTemplate, hit.position, spawnPoint.rotation);
            commander.ConfigureCommander($"{(colony != null ? colony.name : "최종 공세")} Commander {waveIndex + 1}", // 최종 공세 원본은 소굴이 없을 수 있다
                CommanderRank.Sergeant, null, CommanderTraits.Random());
            commander.MakeRaider();
            commander.gameObject.SetActive(true);
            raiders.Add(commander);
            if (!payCost) AntColony.Core.SceneObjects.Any<LocalIncursions>()?.RestoreVisitor(commander);
        }

        // Food 전리품 노드가 없는 소굴은 경제 도입 전처럼 비용 없이 침공한다.
        private bool TryPayRaiderCost()
        {
            if (raiderFoodCost <= 0 || !colony.HasResourceNode(AntColony.Data.ResourceType.Food)) return true;
            return colony.TrySpendResources(raiderFoodCost, 0);
        }
    }
}
