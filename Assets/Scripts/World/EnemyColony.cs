using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace AntColony.World
{
    // 전리품은 이 소굴에 속한 ResourceNode의 실제 잔량이다. 파괴 보상을 별도로 생성하지 않는다.
    public class EnemyColony : MonoBehaviour
    {
        private static readonly List<EnemyColony> Active = new List<EnemyColony>();
        [SerializeField] private BuildingBase[] buildings = new BuildingBase[0];

        [Header("Random Placement")]
        [SerializeField] private bool randomizeAtStart = true;
        // ponytail: 배치 수치는 1차 프로토타입 값이다. 맵 크기 옵션이 생기면 그 설정에서 가져온다.
        [SerializeField, Min(0f)] private float minPlayerDistance = 30f;
        [SerializeField, Min(0f)] private float maxPlayerDistance = 60f;
        [SerializeField, Min(1)] private int placementAttempts = 24;
        [SerializeField, Min(0.1f)] private float navMeshSearchRadius = 6f;

        private void OnEnable() => Active.Add(this);
        private void OnDisable() => Active.Remove(this);

        private void Start()
        {
            if (randomizeAtStart) TryRandomizePlacement();
        }

        private bool TryRandomizePlacement()
        {
            if (GameManager.Instance == null || buildings.Length == 0 || maxPlayerDistance < minPlayerDistance) return false;

            var center = Vector3.zero;
            var count = 0;
            foreach (var building in buildings)
            {
                if (building == null || !building.transform.IsChildOf(transform)) continue;
                center += building.Position;
                count++;
            }
            if (count == 0) return false;
            center /= count;

            var playerBuilding = GameManager.Instance.FindNearestPlayerBuilding(center);
            if (playerBuilding == null) return false;
            if (!NavMesh.SamplePosition(playerBuilding.Position, out var playerHit, navMeshSearchRadius, NavMesh.AllAreas)) return false;

            for (var i = 0; i < placementAttempts; i++)
            {
                var direction = Random.insideUnitCircle;
                if (direction.sqrMagnitude < .0001f) continue;
                direction.Normalize();
                var distance = Random.Range(minPlayerDistance, maxPlayerDistance);
                var candidate = playerBuilding.Position + new Vector3(direction.x, 0f, direction.y) * distance;
                if (!NavMesh.SamplePosition(candidate, out var hit, navMeshSearchRadius, NavMesh.AllAreas)) continue;

                var path = new NavMeshPath();
                if (!NavMesh.CalculatePath(hit.position, playerHit.position, NavMesh.AllAreas, path)
                    || path.status != NavMeshPathStatus.PathComplete) continue;

                transform.position += hit.position - center;
                return true;
            }
            return false;
        }

        public static BuildingBase FindNearestBuilding(Vector3 from, float radius, UnitRole role)
        {
            BuildingBase nearest = null;
            var distance = radius * radius;
            foreach (var colony in Active)
                foreach (var building in colony.buildings)
                {
                    if (!CombatTargeting.CanAttack(role, building)) continue;
                    var candidate = (building.Position - from).sqrMagnitude;
                    if (candidate > distance) continue;
                    distance = candidate;
                    nearest = building;
                }
            return nearest;
        }

        public int RemainingBuildings
        {
            get
            {
                var remaining = 0;
                foreach (var building in buildings)
                    if (building != null && !building.IsDead) remaining++;
                return remaining;
            }
        }

        public bool IsDefeated => buildings.Length > 0 && RemainingBuildings == 0;
    }
}
