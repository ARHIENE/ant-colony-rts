using System;
using System.Collections.Generic;
using AntColony.Buildings;
using UnityEngine;

namespace AntColony.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public event Action OnLoopComplete;
        public event Action OnBossDefeated;
        public event Action OnDefeat;

        private readonly List<BuildingBase> buildings = new List<BuildingBase>();
        private bool loopCompleted;
        private bool bossDefeated;
        private bool defeated;
        private bool hasRegisteredPlayerBuilding;
        private bool quitting;

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

        // 플레이 종료 시 모든 건물이 한꺼번에 해제되면서 패배로 오인되는 것을 막는다.
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
            hasRegisteredPlayerBuilding = true;
        }

        // 등록된 적이 있는 건물만 해제에 성공하므로, 해제 성공 + 잔여 0개면 콜로니가 전멸한 것이다.
        public void UnregisterBuilding(BuildingBase building)
        {
            if (!buildings.Remove(building)) return;
            if (quitting || defeated || !hasRegisteredPlayerBuilding || buildings.Count > 0) return;
            defeated = true;
            OnDefeat?.Invoke();
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
