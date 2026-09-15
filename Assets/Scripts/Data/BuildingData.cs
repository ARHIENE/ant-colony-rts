using UnityEngine;

namespace AntColony.Data
{
    public enum BuildingKind
    {
        QueenChamber,
        Barracks,
        Storage,
        DigSite,
        ResearchLab,
        Farm,
        // 장수 획득 경로 3종. 기존 값의 직렬화 번호가 밀리지 않도록 뒤에 추가한다.
        Nursery,
        ScoutPost,
        PrisonerCamp
    }

    [CreateAssetMenu(fileName = "BuildingData", menuName = "AntColony/Building Data")]
    public class BuildingData : ScriptableObject
    {
        public string displayName = "Building";
        public BuildingKind kind = BuildingKind.Storage;

        [Header("Combat")]
        public float maxHealth = 300f;

        [Header("Cost")]
        public int foodCost = 0;
        public int soilCost = 20;
        public float buildTimeSeconds = 3f;
        // ponytail: 임시 건설 인력. 건물별 밸런스 확정 시 에셋에서 조정한다.
        [Min(0)] public int constructionAnts = 5;

        [Header("Storage Only")]
        public int foodCapacityBonus = 0;
        public int soilCapacityBonus = 0;
        [Min(0)] public int specialCapacityBonus = 100;
    }
}
