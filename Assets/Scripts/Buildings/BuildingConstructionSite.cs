using UnityEngine;

namespace AntColony.Buildings
{
    public class BuildingConstructionSite : MonoBehaviour
    {
        private GameObject completedBuilding;

        public float BuildTimeSeconds { get; private set; }
        public Vector3 Position => transform.position;

        public void Initialize(GameObject building, float buildTimeSeconds)
        {
            completedBuilding = building;
            BuildTimeSeconds = buildTimeSeconds;
        }

        public void Complete()
        {
            if (completedBuilding != null)
            {
                completedBuilding.SetActive(true);
                completedBuilding = null;
            }
            Destroy(gameObject);
        }

        public void Cancel()
        {
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (completedBuilding != null)
            {
                Destroy(completedBuilding);
            }
        }
    }
}
