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
            }
            Destroy(gameObject);
        }

        public void Cancel()
        {
            if (completedBuilding != null)
            {
                Destroy(completedBuilding);
            }
            Destroy(gameObject);
        }
    }
}
