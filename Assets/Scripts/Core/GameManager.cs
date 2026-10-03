using System;
using System.Collections.Generic;
using AntColony.Buildings;
using UnityEngine;

namespace AntColony.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public bool FishingUnlocked { get; internal set; }

        public event Action OnLoopComplete;
        public event Action OnBossDefeated;
        public event Action OnDefeat;

        private readonly List<BuildingBase> buildings = new List<BuildingBase>();
        private bool loopCompleted;
        private bool bossDefeated;
        private bool defeated;
        private bool quitting;
        internal bool SavedLoop => loopCompleted;
        internal bool SavedBoss => bossDefeated;
        internal bool SavedDefeat => defeated;
        internal void RestoreFlags(bool loop, bool boss, bool defeat)
        { loopCompleted = loop; bossDefeated = boss; defeated = defeat; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
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
            // 회복 가능한 쓰러짐·수면·치료·원정·수송은 생존 전력에 포함한다.
            foreach (var commander in CommanderRoster.Instance.Commanders)
                if (!commander.IsDead && !commander.IsCaptive && !commander.IsDeparting && !commander.IsHostile) return;
            defeated = true;
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
