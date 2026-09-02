using System.Collections;
using AntColony.Core;
using AntColony.Units;
using UnityEngine;

namespace AntColony.Buildings
{
    public class Barracks : BuildingBase
    {
        [SerializeField] private GameObject soldierAntPrefab;
        [SerializeField] private Data.UnitData soldierAntData;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private ObjectPool pool;

        private bool isProducing;

        public bool TryProduceSoldier()
        {
            if (isProducing) return false;
            if (ResourceManager.Instance == null || soldierAntData == null) return false;
            if (!ResourceManager.Instance.TrySpend(soldierAntData.foodCost, 0)) return false;

            StartCoroutine(ProduceRoutine());
            return true;
        }

        private IEnumerator ProduceRoutine()
        {
            isProducing = true;
            yield return new WaitForSeconds(soldierAntData.buildTimeSeconds);

            var origin = spawnPoint != null ? spawnPoint.position : transform.position;
            var instance = pool != null
                ? pool.Get(soldierAntPrefab, origin, Quaternion.identity)
                : Instantiate(soldierAntPrefab, origin, Quaternion.identity);

            var soldier = instance.GetComponent<SoldierAnt>();
            soldier.Initialize(soldierAntData, pool, soldierAntPrefab);

            isProducing = false;
        }
    }
}
