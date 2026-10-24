using System;
using System.Collections.Generic;
using AntColony.Buildings;
using AntColony.Units;
using AntColony.World;
using System.Linq;
using UnityEngine;

namespace AntColony.Core
{
    [DefaultExecutionOrder(10000)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public bool FishingUnlocked { get; internal set; }

        public event Action OnLoopComplete;
        public event Action OnBossDefeated;
        public event Action OnDefeat;
        public string DefeatReason { get; private set; }

        private readonly List<BuildingBase> buildings = new List<BuildingBase>();
        private bool loopCompleted;
        private bool bossDefeated;
        private bool defeated;
        private bool quitting;
        internal bool SavedLoop => loopCompleted;
        internal bool SavedBoss => bossDefeated;
        internal bool SavedDefeat => defeated;
        internal void RestoreFlags(bool loop, bool boss, bool defeat, string reason = null)
        { loopCompleted = loop; bossDefeated = boss; defeated = defeat; DefeatReason = defeat ? reason : null; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (GetComponent<RocketCampaign>() == null) gameObject.AddComponent<RocketCampaign>();
            if (GetComponent<AntColony.UI.RocketSafetyHud>() == null) gameObject.AddComponent<AntColony.UI.RocketSafetyHud>();
        }

        private void Start()
        {
            foreach (var building in FindObjectsByType<BuildingBase>(FindObjectsSortMode.None))
            {
                RegisterBuilding(building);
            }
        }

        // 플레이 종료 중에는 패배를 판정하지 않는다.
        private void OnApplicationQuit()
        {
            quitting = true;
        }

        private void OnDestroy()
        {
            quitting = true;
            if (Instance == this) Instance = null;
        }

        public void RegisterBuilding(BuildingBase building)
        {
            if (building == null || !building.CountsTowardPlayerDefeat || buildings.Contains(building)) return;
            buildings.Add(building);
        }

        public void UnregisterBuilding(BuildingBase building) => buildings.Remove(building);

        private void LateUpdate()
        {
            if (quitting || defeated || Save.SaveSystem.Busy || !GameSession.Exists
                || !GameSession.Instance.GameStarted || CommanderRoster.Instance == null
                || CampaignResearch.Instance != null && CampaignResearch.Instance.Departed) return;
            // Update의 피해·소유권 변경 후 탑승 갱신→발사→패배 순서로 한 곳에서 확정한다.
            foreach (var yard in FindObjectsByType<AirshipYard>(FindObjectsSortMode.None)) yard.ResolveLaunch();
            if (CampaignResearch.Instance != null && CampaignResearch.Instance.Departed
                || CommanderAnt.HasRecoveryOrRescueRoute(CommanderRoster.Instance.Commanders)
                || DiplomacyManager.Instance != null && DiplomacyManager.Instance.Data.homecomings.Any(CampaignOutcome.SafeHomecoming)) return;
            DefeatReason = CampaignOutcome.HasSurvivors ? "구조 불가: 쓰러진 장수를 회복시키거나 구조할 방법이 없습니다." : "활동 가능한 장수가 없습니다.";
            defeated = true;
            AntColony.UI.ToastManager.SetCrisis("defeat", DefeatReason);
            OnDefeat?.Invoke();
        }

        public BuildingBase FindNearestPlayerBuilding(Vector3 from)
        {
            BuildingBase nearest = null;
            var distance = float.MaxValue;
            foreach (var building in buildings)
            {
                if (!CombatTargeting.IsAlive(building)) continue;
                var candidate = (building.Position - from).sqrMagnitude;
                if (candidate >= distance) continue;
                distance = candidate;
                nearest = building;
            }
            return nearest;
        }
        public void ReportWildMonsterDefeated()
        {
            if (loopCompleted) return;
            loopCompleted = true;
            OnLoopComplete?.Invoke();
        }

        public void ReportBossDefeated()
        {
            if (bossDefeated) return;
            bossDefeated = true;
            OnBossDefeated?.Invoke();
        }
    }
}
