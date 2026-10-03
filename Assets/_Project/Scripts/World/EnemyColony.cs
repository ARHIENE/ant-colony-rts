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

        [Header("Stock")]
        // ponytail: 전리품 상한은 1차 프로토타입 값이다. 난이도 설정이 생기면 그 설정에서 가져온다.
        [SerializeField, Min(0f)] private float maxFoodStock = 300f;
        [SerializeField, Min(0f)] private float maxSoilStock = 200f;

        // 확장으로 늘어난 건물이 전리품 노드를 복제하더라도 원본만 창고로 쓰도록 시작 시점에 고정한다.
        private ResourceNode foodNode;
        private ResourceNode soilNode;
        private bool stockCached;

        private void Awake() => EnsureStockCache();

        private void OnEnable() => Active.Add(this);
        private void OnDisable() => Active.Remove(this);

        private void Start()
        {
            if (randomizeAtStart) TryRandomizePlacement();
        }

        public void ConfigureExpedition(int difficulty)
        {
            randomizeAtStart = false;
            difficulty = Mathf.Max(1, difficulty);
            maxFoodStock *= difficulty;
            maxSoilStock *= difficulty;
            foreach (var node in GetComponentsInChildren<ResourceNode>(true))
                node.AddStock(node.AmountRemaining * (difficulty - 1));
            foreach (var invasion in GetComponentsInChildren<ColonyInvasion>(true)) invasion.enabled = false;
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

        // 씬에 해당 전리품 노드가 아예 없으면 그 자원은 경제에서 다루지 않는다.
        public bool HasResourceNode(ResourceType type) => FindResource(type) != null;

        public float GetStock(ResourceType type)
        {
            var node = FindResource(type);
            return node == null ? 0f : node.AmountRemaining;
        }

        public void AddResources(int food, int soil)
        {
            AddCapped(FindResource(ResourceType.Food), food, maxFoodStock);
            AddCapped(FindResource(ResourceType.Soil), soil, maxSoilStock);
        }

        public bool TrySpendResources(int food, int soil)
        {
            if (food < 0 || soil < 0) return false;
            var foodSource = FindResource(ResourceType.Food);
            var soilSource = FindResource(ResourceType.Soil);
            if (food > 0 && (foodSource == null || foodSource.AmountRemaining < food)
                || soil > 0 && (soilSource == null || soilSource.AmountRemaining < soil))
                return false;
            return (food == 0 || foodSource.TryConsumeStock(food)) && (soil == 0 || soilSource.TryConsumeStock(soil));
        }

        internal BuildingBase[] Buildings => buildings;

        // 저장 복원 전용. 확장으로 늘어났던 건물을 비용 없이 같은 자리에 다시 세운다.
        internal BuildingBase RestoreExpansion(Vector3 position)
        {
            var template = FindExpansionTemplate();
            if (template == null) return null;
            var building = Instantiate(template, position, template.transform.rotation, transform);
            StripColonyOnlyParts(building);
            building.name = $"Enemy Nest Building {buildings.Length + 1}";
            System.Array.Resize(ref buildings, buildings.Length + 1);
            buildings[buildings.Length - 1] = building;
            return building;
        }

        internal void SuppressRandomPlacement() => randomizeAtStart = false;

        public bool TryExpand(int foodCost, int soilCost, int maxBuildings, float radius)
        {
            if (IsDefeated || RemainingBuildings >= maxBuildings || radius <= 0f) return false;
            // 비용을 받을 창고가 없으면 무상 확장이 되므로 확장 자체를 하지 않는다.
            if (foodCost > 0 && !HasResourceNode(ResourceType.Food)) return false;
            if (soilCost > 0 && !HasResourceNode(ResourceType.Soil)) return false;

            var template = FindExpansionTemplate();
            if (template == null) return false;

            var center = GetBuildingCenter();
            for (var i = 0; i < 8; i++)
            {
                var direction = Random.insideUnitCircle.normalized;
                var candidate = center + new Vector3(direction.x, 0f, direction.y) * radius;
                if (!NavMesh.SamplePosition(candidate, out var hit, 2f, NavMesh.AllAreas)) continue;
                if (IsTooCloseToBuilding(hit.position, 2f)) continue;
                // 확장이 본진 쪽으로 자라 초기 배치 거리를 무너뜨리지 않게 한다.
                if (IsTooCloseToPlayer(hit.position)) continue;
                if (!TrySpendResources(foodCost, soilCost)) return false;

                var height = template.Position.y;
                if (NavMesh.SamplePosition(template.Position, out var templateHit, 2f, NavMesh.AllAreas))
                    height -= templateHit.position.y;
                else
                    height = 0f;
                var building = Instantiate(template, hit.position + Vector3.up * height, template.transform.rotation, transform);
                StripColonyOnlyParts(building);
                building.name = $"Enemy Nest Building {buildings.Length + 1}";
                System.Array.Resize(ref buildings, buildings.Length + 1);
                buildings[buildings.Length - 1] = building;
                return true;
            }
            return false;
        }

        // 소굴 루트를 겸하는 건물을 복제하면 소굴 전체가 복제되므로 제외한다.
        private BuildingBase FindExpansionTemplate()
        {
            foreach (var building in buildings)
                if (building != null && !building.IsDead && building.GetComponent<EnemyColony>() == null)
                    return building;
            return null;
        }

        // 확장 건물은 건물만 복제한다. 전리품 노드나 소굴 제어 컴포넌트가 딸려오면 창고와 침공이 중복된다.
        private static void StripColonyOnlyParts(BuildingBase building)
        {
            foreach (var node in building.GetComponentsInChildren<ResourceNode>(true)) Destroy(node.gameObject);
            foreach (var invasion in building.GetComponentsInChildren<ColonyInvasion>(true)) Destroy(invasion);
            foreach (var nested in building.GetComponentsInChildren<EnemyColony>(true)) Destroy(nested);
        }

        private static void AddCapped(ResourceNode node, int amount, float cap)
        {
            if (node == null || amount <= 0) return;
            var room = cap - node.AmountRemaining;
            if (room <= 0f) return;
            node.AddStock(Mathf.Min(amount, room));
        }

        private void EnsureStockCache()
        {
            if (stockCached) return;
            stockCached = true;
            foreach (var node in GetComponentsInChildren<ResourceNode>(true))
            {
                if (foodNode == null && node.ResourceType == ResourceType.Food) foodNode = node;
                if (soilNode == null && node.ResourceType == ResourceType.Soil) soilNode = node;
            }
        }

        private ResourceNode FindResource(ResourceType type)
        {
            EnsureStockCache();
            return type == ResourceType.Food ? foodNode : type == ResourceType.Soil ? soilNode : null;
        }

        private Vector3 GetBuildingCenter()
        {
            var center = Vector3.zero;
            var count = 0;
            foreach (var building in buildings)
                if (building != null && !building.IsDead)
                {
                    center += building.Position;
                    count++;
                }
            return count == 0 ? transform.position : center / count;
        }

        private bool IsTooCloseToBuilding(Vector3 position, float distance)
        {
            var distanceSqr = distance * distance;
            foreach (var building in buildings)
                if (building != null && (building.Position - position).sqrMagnitude < distanceSqr) return true;
            return false;
        }

        private bool IsTooCloseToPlayer(Vector3 position)
        {
            if (GameManager.Instance == null || minPlayerDistance <= 0f) return false;
            var playerBuilding = GameManager.Instance.FindNearestPlayerBuilding(position);
            return playerBuilding != null
                && (playerBuilding.Position - position).sqrMagnitude < minPlayerDistance * minPlayerDistance;
        }
    }
}
