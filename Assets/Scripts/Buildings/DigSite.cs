using AntColony.Core;
using UnityEngine;

namespace AntColony.Buildings
{
    public class DigSite : BuildingBase
    {
        [SerializeField] private GameObject expansionZone;
        [SerializeField] private bool isExpanded;

        public bool IsExpanded => isExpanded;

        public bool TryExpand()
        {
            if (isExpanded) return false;
            if (ResourceManager.Instance == null || data == null) return false;
            if (!ResourceManager.Instance.TrySpend(0, data.soilCost)) return false;

            isExpanded = true;
            if (expansionZone != null)
            {
                expansionZone.SetActive(true);
            }
            return true;
        }
    }
}
