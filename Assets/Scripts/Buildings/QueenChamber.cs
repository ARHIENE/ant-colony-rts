using System.Collections;
using AntColony.Core;
using AntColony.Units;
using UnityEngine;

namespace AntColony.Buildings
{
    public class QueenChamber : BuildingBase
    {
        [SerializeField] private GameObject workerAntPrefab;
        [SerializeField] private Data.UnitData workerAntData;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private int startingWorkerCount = 3;
        [SerializeField] private ObjectPool pool;

        private bool isProducing;

        protected override bool IsDepositPoint => true;

        private void Start()
        {
            var origin = spawnPoint != null ? spawnPoint.position : transform.position;
            for (var i = 0; i < startingWorkerCount; i++)
            {
                SpawnWorker(origin);
            }
        }

        public bool TryProduceWorker()
        {
            if (isProducing) return false;
            if (ResourceManager.Instance == null || workerAntData == null) return false;
            if (!ResourceManager.Instance.TrySpend(workerAntData.foodCost, 0)) return false;

            StartCoroutine(ProduceRoutine());
            return true;
        }

        private IEnumerator ProduceRoutine()
        {
            isProducing = true;
            yield return new WaitForSeconds(workerAntData.buildTimeSeconds);

            var origin = spawnPoint != null ? spawnPoint.position : transform.position;
            SpawnWorker(origin);

            isProducing = false;
        }

        private void SpawnWorker(Vector3 position)
        {
            if (workerAntPrefab == null || workerAntData == null) return;

            var instance = pool != null
                ? pool.Get(workerAntPrefab, position, Quaternion.identity)
                : Instantiate(workerAntPrefab, position, Quaternion.identity);

            var worker = instance.GetComponent<WorkerAnt>();
            worker.Initialize(workerAntData, pool, workerAntPrefab);
        }
    }
}
