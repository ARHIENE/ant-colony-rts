using AntColony.Data;
using AntColony.Units;
using UnityEngine;

namespace AntColony.Core
{
    // 30초마다 창고 Food를 확인한다. 바닥나면 장수만 굶주림(기분 낮은 장수는 이탈).
    // 2026-10-08: 세금은 ColonyPopulation 주간 납세로 옮김. 시민은 자체 생계라 식량 부족으로 사라지지 않는다.
    public class UpkeepManager : MonoBehaviour
    {
        [SerializeField] private float cycleInterval = 30f;
        private float timer;
        public int ConsecutiveFailures { get; private set; }
        public void RestoreFailures(int value) => ConsecutiveFailures = Mathf.Max(0, value);
        internal float SavedTimer { get => timer; set => timer = value; }

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer < cycleInterval) return;
            timer = 0f;
            RunCycle();
        }

        internal void RunCycle()
        {
            var rm = ResourceManager.Instance;
            if (rm == null || AntPool.Instance == null) return;
            if (rm.GetAmount(ResourceType.Food) > 0)
            {
                ConsecutiveFailures = 0;
                return;
            }
            ConsecutiveFailures++;
            var hungry = new System.Collections.Generic.List<AntUnitBase>(AntUnitBase.Active);
            foreach (var c in hungry)
            {
                if (c is CommanderAnt commander && commander.IsColonyMember && !commander.IsCaptive)
                {
                    commander.PersonalState.AddMood("Hunger", commander.Traits.Has(CommanderTrait.Ascetic) ? 5 : -15, cycleInterval + 1);
                    commander.OnHunger();
                }
            }
            // 굶주림의 이탈 판정은 확률 없이 발동한다. Phase 3: 기분이 높은 장수는 반란 대신 굶는다.
            foreach (var unit in hungry)
                if (unit is CommanderAnt commander && commander.IsColonyMember && !commander.IsCaptive && commander.Mood < SocialRules.StarveMood) commander.TryDeparture();
        }
    }
}
