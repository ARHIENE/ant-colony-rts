using System.Collections.Generic;
using AntColony.Core;
using UnityEngine;
using UnityEngine.AI;

namespace AntColony.World
{
    // 로컬 소굴 경제와 무관한 소규모 방문자. 시간/인구에 따른 병력 증가는 없다.
    public class LocalIncursions : MonoBehaviour
    {
        // 부분 경로는 본거지 근처의 실제 파괴 가능한 벽 앞에서 끝날 때만 허용한다. 절벽·물에 갇힌 적 생성 방지.
        private const float BlockedPathReach = 30f;
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
        public bool TrySpawnHorde(int bodies, EnemyAim aim = EnemyAim.General) => Spawn(null, 1, false, true, false, Mathf.Max(1, bodies), aim);

        private static bool HasEntryBarrier(Vector3 endpoint)
        {
            // 경로 끝은 벽 표면에서 에이전트 여유 폭만큼 떨어진다. 가까운 실제 벽/문만 인정한다.
            foreach (var hit in Physics.OverlapSphere(endpoint, 2f, ~0, QueryTriggerInteraction.Ignore))
            {
                var building = hit.GetComponentInParent<AntColony.Buildings.BuildingBase>();
                if (building != null && !building.IsDead && WildMonster.IsBarrier(building)) return true;
            }
            return false;
        }

        private bool Spawn(Civilization faction, int count, bool alreadyDispatched = false, bool wave = false, bool withCommander = true, int hordeBodies = 0, EnemyAim aim = EnemyAim.General)
        {
            if (!alreadyDispatched && RocketCampaign.SuppressNewCrises) return false;
            if (!wave && faction == null && ExpeditionRules.HomeCommanders == 0) return false;
            visitors.RemoveAll(v => v == null || v.IsDead);
            if ((faction == null && !wave && visitors.Count > 0) || raiderTemplate == null || commanderTemplate == null) return false;
            var home = GameManager.Instance?.FindNearestPlayerBuilding(Vector3.zero);
            // 잔해를 철거했거나 시설을 모두 잃어도 본거지 방어는 계속된다.
            if (!NavMesh.SamplePosition(home != null ? home.Position : HomeReturnPoint.Anchor, out var homeHit, 10, NavMesh.AllAreas)) return false;
            for (var i = 0; i < 16; i++)
            {
                var direction = Random.insideUnitCircle.normalized;
                var candidate = homeHit.position + new Vector3(direction.x, 0, direction.y) * 45;
                if (!NavMesh.SamplePosition(candidate, out var hit, 8, NavMesh.AllAreas)) continue;
                var path = new NavMeshPath();
                // 벽으로 둘러싼 본거지는 경로가 벽 앞에서 끊긴다(Partial). 끊긴 곳이 본거지 근처면 벽을 부수러 오게 허용한다(2026-11-03: 완전 경로만 받아 벽 고리가 웨이브를 통째로 없앴음).
                if (!NavMesh.CalculatePath(hit.position, homeHit.position, NavMesh.AllAreas, path) || path.status == NavMeshPathStatus.PathInvalid
                    || path.status == NavMeshPathStatus.PathPartial && (path.corners.Length == 0 || Vector3.Distance(path.corners[path.corners.Length - 1], homeHit.position) > BlockedPathReach
                        || !HasEntryBarrier(path.corners[path.corners.Length - 1]))) continue;
                for (var n = 0; n < count; n++)
                {
                    var template = withCommander && n == count - 1 ? commanderTemplate : raiderTemplate;
                    var visitor = Instantiate(template, hit.position, Quaternion.identity);
                    visitor.name = n == 2 ? "Visiting Commander" : "Local Intruder";
                    visitor.MakeRaider();
                    if (hordeBodies > 0) { visitor.ConfigureHorde(hordeBodies); visitor.ConfigureAim(aim); visitor.name = $"{WildMonster.AimLabel(aim)} 무리 {hordeBodies}"; }
                    else if (faction == null)
                    {
                        // 독립 웨이브 장수도 초기화해야 옛 템플릿의 짧은 기술 배열이 저장에 섞이지 않는다.
                        if (visitor is EnemyCommander leader)
                            leader.ConfigureCommander(leader.CommanderName, leader.Rank, leader.Roles, leader.Traits);
                        visitor.ConfigureWeakIntruder(wave && visitor is EnemyCommander);
                    }
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
