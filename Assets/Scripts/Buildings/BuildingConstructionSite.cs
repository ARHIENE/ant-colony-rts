using UnityEngine;
using AntColony.Core;

namespace AntColony.Buildings
{
    public class BuildingConstructionSite : MonoBehaviour
    {
        private GameObject completedBuilding;
        private AntPool workforcePool;
        private int reservedAnts;
        private bool finished;

        public float BuildTimeSeconds { get; private set; }
        public Vector3 Position => transform.position;

        public void Initialize(GameObject building, float buildTimeSeconds, AntPool pool = null, int workforce = 0)
        {
            completedBuilding = building;
            BuildTimeSeconds = buildTimeSeconds;
            workforcePool = pool;
            reservedAnts = workforce;
        }

        public void Complete()
        {
            if (finished) return;
            finished = true;
            ReturnWorkforce();
            if (completedBuilding != null)
            {
                completedBuilding.SetActive(true);
                completedBuilding = null;
            }
            Destroy(gameObject);
        }

        public void Cancel()
        {
            if (finished) return;
            finished = true;
            ReturnWorkforce();
            Destroy(gameObject);
        }

        private void ReturnWorkforce()
        {
            var count = reservedAnts;
            reservedAnts = 0;
            if (workforcePool != null) workforcePool.ReleaseReserved(count);
        }

        private void OnDestroy()
        {
            ReturnWorkforce();
            if (completedBuilding != null)
            {
                Destroy(completedBuilding);
            }
        }
    }
}
