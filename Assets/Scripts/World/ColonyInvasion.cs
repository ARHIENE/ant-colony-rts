using System.Collections.Generic;
using AntColony.Core;
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

        private void Awake()
        {
            colony = GetComponent<EnemyColony>();
            timer = firstWaveDelay;
            economyTimer = economyTickSeconds;
            WarnMissingStock();
        }

        // 전리품 노드 설정이 빠지면 경제가 조용히 멈추므로 시작할 때 알린다.
        private void WarnMissingStock()
        {
            if (raiderFoodCost > 0 && !colony.HasResourceNode(ResourceType.Food))
                Debug.LogWarning($"{name}: Food 전리품 노드가 없어 침공 자원 소비를 건너뛴다.", this);
            if (expansionSoilCost > 0 && !colony.HasResourceNode(ResourceType.Soil))
                Debug.LogWarning($"{name}: Soil 전리품 노드가 없어 자동 확장을 건너뛴다.", this);
        }

        private void Update()
        {
            TickEconomy();
            timer -= Time.deltaTime;
            if (timer > 0f) return;

            if (raiderTemplate == null || spawnPoint == null
                || !colony.isActiveAndEnabled || colony.RemainingBuildings == 0
                || GameManager.Instance?.FindNearestPlayerBuilding(spawnPoint.position) == null)
            {
                timer = waveInterval;
                return;
            }

            raiders.RemoveAll(raider => raider == null || raider.IsDead);
            var count = Mathf.Min(firstWaveCount + waveIndex * waveCountGrowth, maxWaveCount);
            count = Mathf.Min(count, maxActiveRaiders - raiders.Count);
            if (count <= 0)
            {
                timer = waveInterval;
                return;
            }

            var spawned = 0;
            for (var i = 0; i < count; i++)
                if (Spawn()) spawned++;

            // 병력이 실제로 나간 파동에만 장수가 따라붙는다. 빈 파동에 장수만 보내지 않는다.
            if (spawned > 0) SpawnCommander();

            // 자원 부족이나 스폰 위치 실패로 한 마리도 못 냈다면 다음 정규 파동까지 기다리지 않는다.
            timer = spawned > 0 ? waveInterval : waveRetryDelay;
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

        private bool Spawn()
        {
            var scatter = Random.insideUnitCircle * spawnScatter;
            var origin = spawnPoint.position + new Vector3(scatter.x, 0f, scatter.y);
            if (!NavMesh.SamplePosition(origin, out var hit, spawnScatter + 2f, NavMesh.AllAreas)) return false;
            if (!TryPayRaiderCost()) return false;

            // 소굴이 파괴돼도 침공 부대는 남도록 소굴 밑에 붙이지 않는다.
            var raider = Instantiate(raiderTemplate, hit.position, spawnPoint.rotation);
            raider.MakeRaider();
            raider.gameObject.SetActive(true);
            raiders.Add(raider);
            return true;
        }

        // 파동을 이끄는 적 장수. 쓰러지면 플레이어의 포로 수용소로 넘어간다.
        private void SpawnCommander()
        {
            if (commanderTemplate == null) return;
            if (raiders.Exists(raider => raider is EnemyCommander)) return;

            var scatter = Random.insideUnitCircle * spawnScatter;
            var origin = spawnPoint.position + new Vector3(scatter.x, 0f, scatter.y);
            if (!NavMesh.SamplePosition(origin, out var hit, spawnScatter + 2f, NavMesh.AllAreas)) return;
            if (commanderFoodCost > 0 && colony.HasResourceNode(ResourceType.Food)
                && !colony.TrySpendResources(commanderFoodCost, 0)) return;

            var commander = Instantiate(commanderTemplate, hit.position, spawnPoint.rotation);
            commander.ConfigureCommander($"{colony.name} Commander {waveIndex + 1}",
                CommanderRank.Sergeant, null, CommanderTraits.Random());
            commander.MakeRaider();
            commander.gameObject.SetActive(true);
            raiders.Add(commander);
        }

        // Food 전리품 노드가 없는 소굴은 경제 도입 전처럼 비용 없이 침공한다.
        private bool TryPayRaiderCost()
        {
            if (raiderFoodCost <= 0 || !colony.HasResourceNode(ResourceType.Food)) return true;
            return colony.TrySpendResources(raiderFoodCost, 0);
        }
    }
}
