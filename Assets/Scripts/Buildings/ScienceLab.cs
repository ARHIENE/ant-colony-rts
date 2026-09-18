using AntColony.Core;
using AntColony.World;
using UnityEngine;

namespace AntColony.Buildings
{
    public class ScienceLab : BuildingBase
    {
        // ponytail: 과학 진입/시간/비용은 1차 프로토타입 수치다. 확정 밸런스 때 조정한다.
        public const int RequiredPopulation = 60;
        public const float ResearchSeconds = 15;
        public const float ConstructionSeconds = 10;
        public bool Busy => remaining > 0;
        public float Remaining => remaining;
        private float remaining;
        private bool aircraft;
        private bool constructing;
        private Vector3 spawnPosition;

        public static bool PrerequisitesMet
        {
            get
            {
                if (AntPool.Instance == null || AntPool.Instance.Total < RequiredPopulation
                    || GameManager.Instance == null || !GameManager.Instance.FishingUnlocked) return false;
                foreach (var barracks in FindObjectsByType<Barracks>(FindObjectsSortMode.None))
                    if (barracks.isActiveAndEnabled && barracks.CurrentTier >= 2) return true;
                return false;
            }
        }

        public bool TryResearch(bool air)
        {
            var world = WorldMapManager.Instance;
            if (!isActiveAndEnabled || Busy || world == null || world.Researcher != null
                || (air ? !world.VehicleResearched || world.AircraftResearched : world.VehicleResearched)) return false;
            if (ResourceManager.Instance == null || !ResourceManager.Instance.TrySpend(air ? 100 : 60, air ? 80 : 50)) return false;
            world.Researcher = this;
            aircraft = air;
            constructing = false;
            remaining = ResearchSeconds;
            return true;
        }

        public bool TryConstruct(bool air)
        {
            var world = WorldMapManager.Instance;
            if (!isActiveAndEnabled || Busy || world == null || !(air ? world.AircraftResearched : world.VehicleResearched)
                || !world.CanCreateTransport(Position, out spawnPosition)) return false;
            if (ResourceManager.Instance == null || !ResourceManager.Instance.TrySpend(air ? 80 : 50, air ? 100 : 60)) return false;
            aircraft = air;
            constructing = true;
            remaining = ConstructionSeconds;
            return true;
        }

        private void Update() => Tick(Time.deltaTime);
        public void Tick(float seconds)
        {
            if (!isActiveAndEnabled || !Busy || !(seconds > 0) || float.IsInfinity(seconds)) return;
            remaining = Mathf.Max(0, remaining - seconds);
            if (Busy) return;
            var world = WorldMapManager.Instance;
            if (world == null) return;
            if (constructing) world.CreateTransport(aircraft, spawnPosition);
            else
            {
                if (aircraft) world.AircraftResearched = true;
                else world.VehicleResearched = true;
                if (world.Researcher == this) world.Researcher = null;
            }
        }

        protected override void OnDisable()
        {
            remaining = 0;
            if (WorldMapManager.Instance != null && WorldMapManager.Instance.Researcher == this)
                WorldMapManager.Instance.Researcher = null;
            base.OnDisable();
        }
    }
}
