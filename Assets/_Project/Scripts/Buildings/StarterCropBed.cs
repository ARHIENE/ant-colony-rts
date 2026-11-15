using System;
using System.Linq;
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
        // 배관으로 공급받지 못하는 작물은 창고에 한 성장 주기분을 확보한다.
        public static int WaterReserveTarget => SceneObjects.All<StarterCropBed>().Count(b => b.isActiveAndEnabled && !b.IsDead
            && b.Crop.planted && !FluidNetwork.Has(b, AntColony.Data.ResourceType.Water, 1))
            * Mathf.CeilToInt(StartSupplyRules.CropSeconds / GameCalendar.SecondsPerDay) * StartSupplyRules.MushroomWaterPerDay;
        public static bool NeedsWaterSupply => ProductionStock.Count(AntColony.Data.ResourceType.Water) < WaterReserveTarget;
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
            HarvestIfReady(); // 기존 저장의 성장 완료 직전 부동소수점 오차도 복구한다.
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
                HarvestIfReady();
            }
        }
        private void HarvestIfReady()
        {
            // 프레임마다 더한 성장과 뺀 급수 시간이 다르게 반올림되어 물 1회를 더 요구하지 않게 한다.
            if (Crop.growth < StartSupplyRules.CropSeconds - .1f) return;
            Crop.growth = 0;
            var lots = new FoodLots(); lots.Add(StartSupplyRules.CropFood, FoodLots.ShelfLife(FoodPreservation.Mushroom));
            DiplomacyManager.DropFloor(AntColony.Data.ResourceType.Food, StartSupplyRules.CropFood, Position + Vector3.right, lots);
        }
    }
}
