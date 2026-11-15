using AntColony.Data;
using AntColony.Units;
using UnityEngine;

namespace AntColony.Core
{
    // 30초마다 개인 포만과 실제 접근 가능한 식사를 확인한다.
    // 2026-10-08: 세금은 ColonyPopulation 주간 납세로 옮김. 시민은 자체 생계라 식량 부족으로 사라지지 않는다.
    public class UpkeepManager : MonoBehaviour
    {
        [SerializeField] private float cycleInterval = 30f;
        private float timer;
        public int ConsecutiveFailures { get; private set; }
        public void RestoreFailures(int value) => ConsecutiveFailures = Mathf.Max(0, value);
        internal float SavedTimer { get => timer; set => timer = value; }

        // 경고와 실제 기아 판정이 동일한 식단·개인 식량·접근성 기준을 사용한다.
        public static bool HasMealShortage(CommanderAnt c) => c != null && c.IsColonyMember && !c.IsCaptive
            && c.CarriedRationMeals <= 0 && !c.HasAccessibleMeal;
        public static bool NeedsMealWarning(CommanderAnt c) => c != null && !c.IsDead
            && c.Satiety <= 25 && HasMealShortage(c);

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
            var hungry = new System.Collections.Generic.List<AntUnitBase>(AntUnitBase.Active);
            // 원식량 창고가 비어도 조리된 식사·바닥 음식·섭취 중인 몫이 있으면
            // 직접 가지러 가는 시간을 준다. 식단과 경로 판정은 실제 식사 코드와 같다.
            hungry.RemoveAll(unit => unit is not CommanderAnt c || c.Satiety > 0 || !HasMealShortage(c));
            if (hungry.Count == 0) { ConsecutiveFailures = 0; return; }
            ConsecutiveFailures++;
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
