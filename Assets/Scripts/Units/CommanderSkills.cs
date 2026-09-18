using UnityEngine;

namespace AntColony.Units
{
    // 장수 액티브 스킬 런타임 상태. 수동 시전 전용이며 자동 시전은 없다.
    // 재사용 대기 시각은 Time.time 기준이라 보직 변경·병력 변화와 무관하게 유지된다.
    public class CommanderSkills
    {
        // ponytail: 프로토타입 잠정 수치. 밸런스가 확정되면 데이터 에셋으로 옮긴다.
        public const int PowerStrikeLevel = 2;
        public const float PowerStrikeMultiplier = 2f;
        public const float PowerStrikeCooldown = 15f;
        public const int DefensiveStanceLevel = 3;
        public const float DefensiveStanceArmor = 5f;
        public const float DefensiveStanceDuration = 5f;
        public const float DefensiveStanceCooldown = 20f;

        private bool powerStrikeArmed;
        private float powerStrikeReadyTime = float.NegativeInfinity;
        private float stanceReadyTime = float.NegativeInfinity;
        private float stanceEndTime = float.NegativeInfinity;

        public bool PowerStrikeArmed => powerStrikeArmed;
        public float PowerStrikeCooldownLeft => Mathf.Max(0f, powerStrikeReadyTime - Time.time);
        public bool DefensiveStanceActive => Time.time < stanceEndTime;
        public float DefensiveStanceTimeLeft => Mathf.Max(0f, stanceEndTime - Time.time);
        public float DefensiveStanceCooldownLeft => Mathf.Max(0f, stanceReadyTime - Time.time);

        // 재사용 대기는 장전 시점부터 센다. 장전 중이거나 대기 중이면 중복 시전을 거부한다.
        public bool CanArmPowerStrike(int level) => level >= PowerStrikeLevel && !powerStrikeArmed && PowerStrikeCooldownLeft <= 0f;
        public bool CanStartDefensiveStance(int level) =>
            level >= DefensiveStanceLevel && !DefensiveStanceActive && DefensiveStanceCooldownLeft <= 0f;

        public bool TryArmPowerStrike(int level)
        {
            if (!CanArmPowerStrike(level)) return false;
            powerStrikeArmed = true;
            powerStrikeReadyTime = Time.time + PowerStrikeCooldown;
            return true;
        }

        // 실제 타격 한 번에만 소모된다.
        public bool ConsumePowerStrike()
        {
            if (!powerStrikeArmed) return false;
            powerStrikeArmed = false;
            return true;
        }

        public bool TryStartDefensiveStance(int level)
        {
            if (!CanStartDefensiveStance(level)) return false;
            stanceEndTime = Time.time + DefensiveStanceDuration;
            stanceReadyTime = Time.time + DefensiveStanceCooldown;
            return true;
        }

        // 비활성화 시 장전·태세 효과만 끊는다. 재사용 대기는 환급하지 않는다.
        public void CancelEffects()
        {
            powerStrikeArmed = false;
            stanceEndTime = float.NegativeInfinity;
        }
    }
}
