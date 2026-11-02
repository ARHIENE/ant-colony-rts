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
        private float remaining;
        internal float SavedTimer { get => remaining; set => remaining = value; }
        private readonly List<WildMonster> visitors = new List<WildMonster>();
        public IReadOnlyList<WildMonster> Visitors => visitors;
        internal WildMonster RestoreSpawn(bool commander, Vector3 position) =>
            Instantiate(commander ? commanderTemplate : raiderTemplate, position, Quaternion.identity);
        internal void RestoreVisitor(WildMonster monster) { if (!visitors.Contains(monster)) visitors.Add(monster); }
        // 2026-11-02 웨이브 수성: 독립 방문 주기(5~8분·사건 감독 간격 보정)는 WaveDefense의 평화/웨이브 일정으로 대체했다.
        // remaining은 구저장 호환용으로만 저장·복원한다.
        private void Start() { Storyteller.Reset(); WaveDefense.Ensure(); }

        public bool TrySpawn()
            => !RocketCampaign.SuppressNewCrises && DiplomacyManager.Instance?.TryDispatchIndependentRaid() == true;

        public bool TrySpawnFaction(Civilization faction, int count) => faction != null && faction.contacted && faction.war && Spawn(faction, Mathf.Clamp(count, 1, 20));
        public bool TrySpawnIndependent(int count) => Spawn(null, Mathf.Clamp(count, 1, 20));

        // 이미 출발한 적은 G39의 신규 추첨 차단 대상이 아니다. 이동 침공대 도착 경로에서만 호출한다.
        public bool TryArriveRaid(Civilization faction, int count) => (faction == null || faction.contacted && faction.war)
            && Spawn(faction, Mathf.Clamp(count, 1, 20), true);

        // 웨이브 병력 한 무리(최대 20). 첫 무리만 장수가 이끈다.
        public bool TrySpawnWaveGroup(int count, bool withCommander) => Spawn(null, Mathf.Clamp(count, 1, 20), false, true, withCommander);
        // 무리 유닛 하나(체력 = 마릿수 × 한 마리 체력).
        public bool TrySpawnHorde(int bodies) => Spawn(null, 1, false, true, false, Mathf.Max(1, bodies));

        private bool Spawn(Civilization faction, int count, bool alreadyDispatched = false, bool wave = false, bool withCommander = true, int hordeBodies = 0)
        {
            if (!alreadyDispatched && RocketCampaign.SuppressNewCrises) return false;
            if (faction == null && ExpeditionRules.HomeCommanders == 0) return false;
            visitors.RemoveAll(v => v == null || v.IsDead);
            if ((faction == null && !wave && visitors.Count > 0) || raiderTemplate == null || commanderTemplate == null) return false;
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
                for (var n = 0; n < count; n++)
                {
                    var template = withCommander && n == count - 1 ? commanderTemplate : raiderTemplate;
                    var visitor = Instantiate(template, hit.position, Quaternion.identity);
                    visitor.name = n == 2 ? "Visiting Commander" : "Local Intruder";
                    visitor.MakeRaider();
                    if (hordeBodies > 0) { visitor.ConfigureHorde(hordeBodies); visitor.name = $"침입 무리 {hordeBodies}"; }
                    else if (faction == null) visitor.ConfigureWeakIntruder();
                    else
                    {
                        visitor.name = faction.name + " 침공군 " + (n + 1);
                        visitor.DiplomaticFactionId = faction.id;
                        if (visitor is EnemyCommander commander) commander.ConfigureCommander(faction.leader, AntColony.Data.CommanderRank.Sergeant, null, AntColony.Units.CommanderTraits.Random());
                        visitor.ConfigureAIMilitary(faction.era, n);
                    }
                    visitor.gameObject.SetActive(true);
                    visitors.Add(visitor);
                }
                if (!wave) { AntColony.UI.ToastManager.Show("침입자가 본거지로 접근합니다!"); Storyteller.MarkCrisis(); }
                return true;
            }
            return false;
        }
    }
}
