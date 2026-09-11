using System.Collections.Generic;
using AntColony.Core;
using UnityEngine;
using UnityEngine.AI;

namespace AntColony.World
{
    // 적 소굴이 일정 시간마다 침공 부대를 보낸다. 수치는 전부 임시값이다.
    [RequireComponent(typeof(EnemyColony))]
    public class ColonyInvasion : MonoBehaviour
    {
        [SerializeField] private WildMonster raiderTemplate; // 비활성 상태의 템플릿
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private float firstWaveDelay = 90f;
        [SerializeField] private float waveInterval = 120f;
        [SerializeField] private int firstWaveCount = 2;
        [SerializeField] private int waveCountGrowth = 1;
        [SerializeField] private int maxWaveCount = 6;
        [SerializeField] private int maxActiveRaiders = 12;
        [SerializeField] private float spawnScatter = 3f;

        private readonly List<WildMonster> raiders = new List<WildMonster>();
        private EnemyColony colony;
        private float timer;
        private int waveIndex;

        private void Awake()
        {
            colony = GetComponent<EnemyColony>();
            timer = firstWaveDelay;
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = waveInterval;

            if (raiderTemplate == null || spawnPoint == null) return;
            if (!colony.isActiveAndEnabled || colony.RemainingBuildings == 0) return;
            if (GameManager.Instance?.FindNearestPlayerBuilding(spawnPoint.position) == null) return;

            raiders.RemoveAll(raider => raider == null || raider.IsDead);
            var count = Mathf.Min(firstWaveCount + waveIndex * waveCountGrowth, maxWaveCount);
            count = Mathf.Min(count, maxActiveRaiders - raiders.Count);
            if (count <= 0) return;

            var before = raiders.Count;
            for (var i = 0; i < count; i++) Spawn();
            if (raiders.Count > before && waveIndex < maxWaveCount) waveIndex++;
        }

        private void Spawn()
        {
            var scatter = Random.insideUnitCircle * spawnScatter;
            var origin = spawnPoint.position + new Vector3(scatter.x, 0f, scatter.y);
            if (!NavMesh.SamplePosition(origin, out var hit, spawnScatter + 2f, NavMesh.AllAreas)) return;

            // 소굴이 파괴돼도 침공 부대는 남도록 소굴 밑에 붙이지 않는다.
            var raider = Instantiate(raiderTemplate, hit.position, spawnPoint.rotation);
            raider.MakeRaider();
            raider.gameObject.SetActive(true);
            raiders.Add(raider);
        }
    }
}
