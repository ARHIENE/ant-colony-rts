using System.Collections.Generic;
using AntColony.Core;
using UnityEngine;
using UnityEngine.AI;

namespace AntColony.World
{
    // 로컬 소굴 경제와 무관한 소규모 방문자. 시간/인구에 따른 병력 증가는 없다.
    public class LocalIncursions : MonoBehaviour
    {
        [SerializeField] private WildMonster raiderTemplate;
        [SerializeField] private EnemyCommander commanderTemplate;
        // ponytail: 빈도 5~8분, 병사 2+장수 1은 새 기획의 임시 밸런스다.
        [SerializeField] private float minimumDelay = 300f;
        [SerializeField] private float maximumDelay = 480f;
        private float remaining;
        internal float SavedTimer { get => remaining; set => remaining = value; }
        private readonly List<WildMonster> visitors = new List<WildMonster>();
        public IReadOnlyList<WildMonster> Visitors => visitors;
        // 난이도는 방문 간격만 바꾼다. Normal이면 배수 1.0이라 기존과 같다.
        private float NextDelay() => Random.Range(minimumDelay, maximumDelay) * AntColony.Core.DifficultyRuntime.IntervalScale;
        private void Start() => remaining = NextDelay();
        private void Update()
        {
            remaining -= Time.deltaTime;
            if (remaining > 0) return;
            TrySpawn();
            remaining = NextDelay();
        }

        public bool TrySpawn()
        {
            visitors.RemoveAll(v => v == null || v.IsDead);
            if (visitors.Count > 0 || raiderTemplate == null || commanderTemplate == null) return false;
            var home = GameManager.Instance?.FindNearestPlayerBuilding(Vector3.zero);
            if (home == null || !NavMesh.SamplePosition(home.Position, out var homeHit, 10, NavMesh.AllAreas)) return false;
            for (var i = 0; i < 16; i++)
            {
                var direction = Random.insideUnitCircle.normalized;
                var candidate = homeHit.position + new Vector3(direction.x, 0, direction.y) * 45;
                if (!NavMesh.SamplePosition(candidate, out var hit, 8, NavMesh.AllAreas)) continue;
                var path = new NavMeshPath();
                if (!NavMesh.CalculatePath(hit.position, homeHit.position, NavMesh.AllAreas, path)
                    || path.status != NavMeshPathStatus.PathComplete) continue;
                for (var n = 0; n < 3; n++)
                {
                    var template = n == 2 ? commanderTemplate : raiderTemplate;
                    var visitor = Instantiate(template, hit.position, Quaternion.identity);
                    visitor.name = n == 2 ? "Visiting Commander" : "Local Intruder";
                    visitor.MakeRaider();
                    visitor.ConfigureWeakIntruder();
                    visitor.gameObject.SetActive(true);
                    visitors.Add(visitor);
                }
                AntColony.UI.ToastManager.Show("Intruders approaching the home colony!");
                return true;
            }
            return false;
        }
    }
}
