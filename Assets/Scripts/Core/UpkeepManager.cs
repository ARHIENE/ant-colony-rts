using AntColony.Units;
using UnityEngine;

namespace AntColony.Core
{
    public class UpkeepManager : MonoBehaviour
    {
        [SerializeField] private float cycleInterval = 30f;
        [SerializeField, Min(0)] private int foodPerAnt = 1;
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
            var pool = AntPool.Instance;
            if (ResourceManager.Instance == null || pool == null) return;
            if (ResourceManager.Instance.TrySpend(pool.Total * foodPerAnt, 0)) return;
            if (pool.StarveOne()) return;
            foreach (var unit in AntUnitBase.Active)
            {
                if (unit is CommanderAnt commander && commander.HasTroops)
                {
                    commander.TakeDamage(commander.Armor + 1f);
                    return;
                }
            }
            // ponytail: 건설 인력만 남은 경우 손실 정책은 미정. 예약은 유지하고 다음 주기에 다시 청구한다.
        }
    }
}
