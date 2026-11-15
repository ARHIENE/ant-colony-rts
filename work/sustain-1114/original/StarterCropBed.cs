using System;
using AntColony.Core;
using AntColony.World;
using UnityEngine;

namespace AntColony.Buildings
{
    // 독립적인 1칸 작물. 도시 포장 위에도 화분·버섯 배지를 놓을 수 있다.
    public sealed class StarterCropBed : BuildingBase
    {
        [Serializable] public sealed class State { public bool planted; public float growth, wateredSeconds; }
        public State Crop { get; private set; } = new State();
        public void RestoreCrop(State value) => Crop = value ?? new State();
        public static bool Valid(State s) => s != null && Finite(s.growth, StartSupplyRules.CropSeconds)
            && Finite(s.wateredSeconds, GameCalendar.SecondsPerDay) && (s.planted || s.growth == 0 && s.wateredSeconds == 0);
        private static bool Finite(float v, float max) => !float.IsNaN(v) && !float.IsInfinity(v) && v >= 0 && v < max;
        public bool Plant()
        {
            if (!isActiveAndEnabled || IsDead || Crop.planted || ResourceManager.Instance == null
                || !ResourceManager.Instance.TrySpend(AntColony.Data.ResourceType.PlantingMaterial, 1, ResourceReason.Production)) return false;
            Crop.planted = true; return true;
        }
        private void Update()
        {
            if (!AntColony.Save.SaveSystem.Busy && GameSession.Exists && GameSession.Instance.GameStarted) Tick(Time.deltaTime);
        }
        public void Tick(float seconds)
        {
            if (!isActiveAndEnabled || IsDead || !Crop.planted || !(seconds > 0) || float.IsInfinity(seconds)) return;
            while (seconds > 0)
            {
                if (Crop.wateredSeconds <= 0)
                {
                    if (!FluidNetwork.Take(this, AntColony.Data.ResourceType.Water, StartSupplyRules.MushroomWaterPerDay)
                        && (ResourceManager.Instance == null || !ResourceManager.Instance.TrySpend(AntColony.Data.ResourceType.Water,
                            StartSupplyRules.MushroomWaterPerDay, ResourceReason.Production))) return;
                    Crop.wateredSeconds = GameCalendar.SecondsPerDay;
                }
                var dt = Mathf.Min(seconds, Mathf.Min(Crop.wateredSeconds, StartSupplyRules.CropSeconds - Crop.growth));
                Crop.wateredSeconds -= dt; Crop.growth += dt; seconds -= dt;
                if (Crop.growth < StartSupplyRules.CropSeconds) continue;
                Crop.growth = 0;
                var lots = new FoodLots(); lots.Add(StartSupplyRules.DailyFood, FoodLots.ShelfLife(FoodPreservation.Mushroom));
                DiplomacyManager.DropFloor(AntColony.Data.ResourceType.Food, StartSupplyRules.DailyFood, Position + Vector3.right, lots);
            }
        }
    }
}
