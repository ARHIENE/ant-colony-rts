using AntColony.Core;
using AntColony.UI;
using UnityEngine;

namespace AntColony.Boss.AoE
{
    public static class BossTelegraphTiming
    {
        public static void Prepare()
        {
            if (!UserSettings.Current.autoPauseThreat || Time.timeScale <= 1) return;
            GameMenuController.Instance?.SetSpeed(1);
            if (Time.timeScale == 1) ToastManager.Show("보스 범위 공격 예고 · 대응할 수 있도록 1배속으로 전환했습니다.");
        }
    }
}
