using AntColony.Data;
using AntColony.Units;
using UnityEngine;

namespace AntColony.Core
{
    // Phase 4(2026-10-01): 일반개미 유지비 → 세금. 30초마다 인구가 Food를 낸다(ColonyPopulation.TaxPerCycle).
    // 창고 Food가 바닥나면 식량 부족: 대기 개미가 사라지고 장수는 굶주림(기분 낮은 장수는 이탈).
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

        public int TaxIncome => ColonyPopulation.Instance != null ? ColonyPopulation.Instance.TaxPerCycle : 0;

        internal void RunCycle()
        {
            var rm = ResourceManager.Instance;
            if (rm == null || AntPool.Instance == null) return;
            rm.Add(ResourceType.Food, TaxIncome, ResourceReason.Tax);
            if (rm.GetAmount(ResourceType.Food) > 0)
            {
                ConsecutiveFailures = 0;
                return;
            }
            ConsecutiveFailures++;
            ColonyPopulation.Instance?.Starve();
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
