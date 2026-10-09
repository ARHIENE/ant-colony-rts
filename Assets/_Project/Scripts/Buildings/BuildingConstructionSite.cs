using UnityEngine;
using AntColony.Core;
using AntColony.Data;

namespace AntColony.Buildings
{
    public class BuildingConstructionSite : MonoBehaviour
    {
        private GameObject completedBuilding;
        private AntPool workforcePool;
        private int reservedAnts;
        private bool finished;
        private int refundFood, refundSoil, refundSpecial; // 취소 시 전액 반환할 건설비(재개발 보상비 제외).

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
            if (TryGetComponent<DemolitionSite>(out var demolition)) demolition.Finish();
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

        public void SetRefund(int food, int soil, int special) { refundFood = food; refundSoil = soil; refundSpecial = special; }

        // 플레이어 취소: 건설 전·시공 중 모두 건설비 100% 반환. 재개발이면 기존 집은 그대로 남고 지급한 보상비·발생한 불만은 돌려받지 않는다.
        // 창고가 가득 차면 넘치는 반환분은 창고 주변 바닥에 두고 운반 장수가 옮긴다(외교 수령과 같은 처리).
        public void Cancel()
        {
            if (finished) return;
            finished = true;
            AntColony.World.DiplomacyManager.StoreResource(ResourceType.Food, refundFood, ResourceReason.Refund);
            AntColony.World.DiplomacyManager.StoreResource(ResourceType.Soil, refundSoil, ResourceReason.Refund);
            AntColony.World.DiplomacyManager.StoreResource(ResourceType.Special, refundSpecial, ResourceReason.Refund);
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
