using AntColony.Units;
using UnityEngine;

namespace AntColony.Core
{
    // 개미 개체별 식량 유지비. 일정 주기마다 활성 유닛의 유지비 총합을 식량에서 차감하고,
    // 부족하면 무작위 개체 하나를 아사시키거나 반란(야생화)시킨다.
    public class UpkeepManager : MonoBehaviour
    {
        [SerializeField] private float cycleInterval = 30f;
        [SerializeField] private float rebellionChance = 0.5f;

        private float timer;

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer < cycleInterval) return;
            timer = 0f;
            RunCycle();
        }

        private void RunCycle()
        {
            if (ResourceManager.Instance == null) return;

            var totalUpkeep = 0;
            foreach (var unit in AntUnitBase.Active)
            {
                if (unit != null) totalUpkeep += unit.Data.foodUpkeep;
            }

            if (totalUpkeep <= 0) return;
            if (ResourceManager.Instance.TrySpend(totalUpkeep, 0)) return;

            var victim = PickRandomActiveUnit();
            if (victim == null) return;

            if (Random.value < rebellionChance)
            {
                victim.Rebel();
            }
            else
            {
                victim.TakeDamage(float.MaxValue);
            }
        }

        private AntUnitBase PickRandomActiveUnit()
        {
            var list = AntUnitBase.Active;
            if (list.Count == 0) return null;
            return list[Random.Range(0, list.Count)];
        }
    }
}
