using UnityEngine;

namespace AntColony.Data
{
    public enum BuildingKind
    {
        QueenChamber,
        Barracks,
        Storage,
        DigSite,
        ResearchLab
    }

    [CreateAssetMenu(fileName = "BuildingData", menuName = "AntColony/Building Data")]
    public class BuildingData : ScriptableObject
    {
        public string displayName = "Building";
        public BuildingKind kind = BuildingKind.Storage;

        [Header("Cost")]
        public int foodCost = 0;
        public int soilCost = 20;
        public float buildTimeSeconds = 3f;

        [Header("Storage Only")]
        public int foodCapacityBonus = 0;
        public int soilCapacityBonus = 0;
    }
}
