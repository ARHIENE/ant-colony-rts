using AntColony.Core;
using UnityEngine;

namespace AntColony.Buildings
{
    public class DigSite : BuildingBase
    {
        [SerializeField] private GameObject expansionZone;
        [SerializeField] private bool isExpanded;

        public bool IsExpanded => isExpanded;

        // 저장 복원 전용.
        internal void RestoreExpanded(bool expanded)
        {
            isExpanded = expanded;
            if (expansionZone != null) expansionZone.SetActive(expanded);
        }

        public bool TryExpand()
        {
            if (isExpanded) return false;
            if (ResourceManager.Instance == null || data == null) return false;
            if (!ResourceManager.Instance.TrySpend(0, data.soilCost, reason: ResourceReason.Construction)) return false;

            isExpanded = true;
            if (expansionZone != null)
            {
                expansionZone.SetActive(true);
            }
            return true;
        }
    }
}
