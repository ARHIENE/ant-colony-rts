using AntColony.Core;
using UnityEngine;

namespace AntColony.Units
{
    // Phase 4(2026-10-01): 장수 나이(개월, 잠정). 어린 장수는 일·출전 없이 배우기만, 늙으면 몸은 약해지고 지혜는 는다,
    // 수명이 다하면 예고 없이 죽는다. ageMonths < 0 = 아직 안 정함(새 장수·이전 저장) → 첫 틱에 성체 나이로 정한다.
    public partial class CommanderAnt
    {
        public float AgeMonths => personalState.ageMonths;
        public bool IsChild => personalState.ageMonths >= 0 && personalState.ageMonths < GameBalance.ChildMonths;
        public bool IsElder => personalState.ageMonths >= GameBalance.ElderMonths;
        public string AgeLabel => personalState.ageMonths < 0 ? "—" : $"{Mathf.FloorToInt(personalState.ageMonths / 12f)}세";

        public void SetBorn() { personalState.ageMonths = 0; personalState.lifespanMonths = Random.Range(GameBalance.MinLifespan, GameBalance.MaxLifespan); }

        private void TickAge(float seconds)
        {
            if (personalState.ageMonths < 0)
            {
                personalState.ageMonths = Random.Range(GameBalance.ChildMonths, 72f);
                personalState.lifespanMonths = Random.Range(GameBalance.MinLifespan, GameBalance.MaxLifespan);
            }
            personalState.ageMonths += seconds / GameCalendar.SecondsPerMonth;
            if (personalState.ageMonths >= personalState.lifespanMonths && IsColonyMember && !IsCaptive && !IsEmbarked) { CommandStop(); DropCargo(); OnDowned("노환", true); }
        }

        private float AgeMoveMultiplier => IsElder ? .85f : 1f;
        private float AgeLearningMultiplier => IsChild ? 1.5f : 1f;
        private float AgeWorkMultiplier(CommanderActivity a) => !IsElder ? 1f
            : a == CommanderActivity.Command || a == CommanderActivity.Research || a == CommanderActivity.Medicine || a == CommanderActivity.Art ? 1.2f
            : a == CommanderActivity.Melee || a == CommanderActivity.Strength ? .8f : 1f;
    }
}
