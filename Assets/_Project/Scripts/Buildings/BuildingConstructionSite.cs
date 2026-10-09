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

        public AntColony.Data.BuildingKind? BuildingKind => completedBuilding != null ? completedBuilding.GetComponent<BuildingBase>()?.Data?.kind : null;
        public bool IsArt => BuildingKind.HasValue && Decoration.IsKind(BuildingKind.Value);
        public bool HasBuilder => System.Linq.Enumerable.Any(AntColony.Units.AntUnitBase.Active, u => u is AntColony.Units.WorkerAnt w && w.ConstructionTarget == this);
        public float BuildTimeSeconds { get; private set; }
        public float RemainingWork { get; internal set; }
        public Vector3 Position => transform.position;

        public void Initialize(GameObject building, float buildTimeSeconds, AntPool pool = null, int workforce = 0)
        {
            completedBuilding = building;
            BuildTimeSeconds = buildTimeSeconds;
            RemainingWork = buildTimeSeconds;
            workforcePool = pool;
            reservedAnts = workforce;
        }

        public void Complete(AntColony.Units.CommanderAnt builder = null)
        {
            if (finished) return;
            finished = true;
            ReturnWorkforce();
            if (completedBuilding != null)
            {
                if (builder != null && completedBuilding.GetComponent<Decoration>() is Decoration decor)
                    decor.Quality = AntColony.Units.EquipmentRecipes.Quality(builder.Talents.Level(AntColony.Units.CommanderActivity.Art), Random.value, Random.value);
                if (TryGetComponent<RedevelopmentSite>(out var redevelop)) redevelop.Finish(); // 재개발이면 완공 순간 기존 집을 허문다.
                completedBuilding.SetActive(true);
                AntColony.UI.ToastManager.Show(completedBuilding.name + " construction complete.");
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
