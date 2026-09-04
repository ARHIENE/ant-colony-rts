using System;
using UnityEngine;

namespace AntColony.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public event Action OnLoopComplete;
        public event Action OnBossDefeated;

        private bool loopCompleted;
        private bool bossDefeated;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
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
