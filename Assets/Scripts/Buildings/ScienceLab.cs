using AntColony.Core;
using AntColony.World;
using AntColony.Units;
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
        public int Tier { get; private set; } = 1;
        public CommanderAnt Target { get; private set; }
        private float remaining;
        private bool aircraft;
        private bool constructing;
        private Vector3 spawnPosition;

        internal bool Aircraft => aircraft;
        internal bool Constructing => constructing;
        internal Vector3 SpawnPosition => spawnPosition;

        public bool TryAssign(CommanderAnt commander)
        {
            if (!isActiveAndEnabled || Busy || Target != null || commander == null || !commander.isActiveAndEnabled
                || commander.IsDead || commander.IsAwayFromHome || commander.LabUpgradeBusy || !commander.CanChangeAllocation
                || commander.IsWorking || Vector3.Distance(commander.Position, Position) > 8) return false;
            commander.CommandStop();
            Target = commander;
            commander.ScienceAssignment = this;
            return true;
        }

        public void ReleaseResearcher()
        {
            if (Target != null && Target.ScienceAssignment == this) Target.ScienceAssignment = null;
            Target = null;
        }

        public bool TryUpgrade()
        {
            if (!isActiveAndEnabled || Busy || Tier >= 4 || ResourceManager.Instance == null
                || !ResourceManager.Instance.TrySpend(Tier * 60, Tier * 80, reason: ResourceReason.Research)) return false;
            Tier++;
            return true;
        }

        public void RestoreAssignment(int tier, CommanderAnt target)
        {
            ReleaseResearcher();
            Tier = Mathf.Clamp(tier, 1, 4);
            Target = target;
            if (target != null) target.ScienceAssignment = this;
        }

        // 저장 복원 전용. 연구 중이었다면 WorldMapManager.Researcher 자리도 다시 잡아 준다.
        internal void RestoreState(float savedRemaining, bool air, bool build, Vector3 spawn)
        {
            remaining = Mathf.Max(0f, savedRemaining);
            aircraft = air;
            constructing = build;
            spawnPosition = spawn;
            var world = WorldMapManager.Instance;
            if (remaining > 0f && !build && world != null && world.Researcher == null) world.Researcher = this;
        }

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
            if (CampaignResearch.Instance != null)
                return isActiveAndEnabled && CampaignResearch.Instance.TryStart(air ? ScienceTechnology.Aircraft : ScienceTechnology.Vehicle);
            var world = WorldMapManager.Instance;
            if (!isActiveAndEnabled || Busy || world == null || world.Researcher != null
                || (air ? !world.VehicleResearched || world.AircraftResearched : world.VehicleResearched)) return false;
            if (ResourceManager.Instance == null || !ResourceManager.Instance.TrySpend(air ? 100 : 60, air ? 80 : 50, reason: ResourceReason.Research)) return false;
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
            if (ResourceManager.Instance == null || !ResourceManager.Instance.TrySpend(air ? 80 : 50, air ? 100 : 60, reason: ResourceReason.Expedition)) return false;
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
            AntColony.UI.ToastManager.Show((aircraft ? "Aircraft" : "Vehicle") + (constructing ? " construction complete." : " research complete."));
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
            ReleaseResearcher();
            remaining = 0;
            if (WorldMapManager.Instance != null && WorldMapManager.Instance.Researcher == this)
                WorldMapManager.Instance.Researcher = null;
            base.OnDisable();
        }
    }
}
