using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using System.Collections.Generic;
using UnityEngine;

namespace AntColony.World
{
    // 전리품은 이 소굴에 속한 ResourceNode의 실제 잔량이다. 파괴 보상을 별도로 생성하지 않는다.
    public class EnemyColony : MonoBehaviour
    {
        private static readonly List<EnemyColony> Active = new List<EnemyColony>();
        [SerializeField] private BuildingBase[] buildings = new BuildingBase[0];

        private void OnEnable() => Active.Add(this);
        private void OnDisable() => Active.Remove(this);

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
