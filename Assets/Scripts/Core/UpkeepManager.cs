using AntColony.Units;
using UnityEngine;

namespace AntColony.Core
{
    public class UpkeepManager : MonoBehaviour
    {
        [SerializeField] private float cycleInterval = 30f;
        [SerializeField, Min(0)] private int foodPerAnt = 1;
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

        // 일반개미(원정·주둔 병력 포함, 풀에 남아 있음) + 장수 본인. 포로·사망 장수는 청구하지 않는다.
        public int FoodDue
        {
            get
            {
                var pool = AntPool.Instance;
                var commanders = 0f;
                if (CommanderRoster.Instance != null)
                    foreach (var c in CommanderRoster.Instance.Commanders)
                        if (c.IsColonyMember && !c.IsCaptive) commanders += GameBalance.CommanderUpkeepFood * c.Traits.FoodMultiplier;
                return (pool != null ? pool.Total * foodPerAnt : 0) + Mathf.CeilToInt(commanders);
            }
        }

        internal void RunCycle()
        {
            var pool = AntPool.Instance;
            if (ResourceManager.Instance == null || pool == null) return;
            if (ResourceManager.Instance.TrySpend(FoodDue, 0, reason: ResourceReason.Upkeep))
            {
                ConsecutiveFailures = 0;
                foreach (var c in AntUnitBase.Active)
                    if (c is CommanderAnt commander && commander.IsColonyMember) commander.PersonalState.AddMood("Fed", commander.Traits.Has(CommanderTrait.Glutton) ? 10 : 5, cycleInterval + 1);
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
            // 굶주림의 이탈 판정은 확률 없이 발동한다. 파벌 이탈은 첫 장수의 판정에서 함께 처리된다.
            foreach (var unit in hungry)
                if (unit is CommanderAnt commander && commander.IsColonyMember && !commander.IsCaptive) commander.TryDeparture();
        }
    }
}
