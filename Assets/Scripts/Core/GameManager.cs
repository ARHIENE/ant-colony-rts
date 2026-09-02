using System;
using UnityEngine;

namespace AntColony.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public event Action OnLoopComplete;

        private bool loopCompleted;

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
    }
}
