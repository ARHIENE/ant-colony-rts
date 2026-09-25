using System;
using UnityEngine;

namespace AntColony.Units
{
    public enum DepartureState { None, Fleeing, Rebellion, Retreating, Left, Imprisoned }

    [Serializable]
    public sealed class CommanderSocialState
    {
        public DepartureState departure;
        public Vector3 escapePoint;
        public float rebellionSeconds, attackSeconds;
        public float rootRemaining;
        public int captiveDebt, seriousInjuries, breakdowns, expeditions;
        public float acidCooldown, rallyCooldown, diveCooldown, rallyRemaining, groundedRemaining;
        public float acidRemaining, acidTick, acidDamage;
        public Vector3 acidPoint;
        public bool diving;
        public bool pendingDeparture;
        public bool injuryTraitChanged, combatSeen;
        public Vector3 divePoint;
        public bool Validate()
        {
            if (!Enum.IsDefined(typeof(DepartureState), departure) || captiveDebt < 0 || seriousInjuries < 0 || breakdowns < 0 || expeditions < 0) return false;
            foreach (var n in new[] { rebellionSeconds, attackSeconds, rootRemaining, acidCooldown, rallyCooldown, diveCooldown, rallyRemaining,
                groundedRemaining, acidRemaining, acidTick, acidDamage })
                if (float.IsNaN(n) || float.IsInfinity(n) || n < 0) return false;
            foreach (var v in new[] { escapePoint, acidPoint, divePoint })
                if (float.IsNaN(v.sqrMagnitude) || float.IsInfinity(v.sqrMagnitude)) return false;
            return acidRemaining <= 4 && acidTick <= 1 && groundedRemaining <= 3 && rallyRemaining <= 8;
        }
    }

    // 5단계 잠정 수치를 한 곳에서 조정한다.
    public static class SocialRules
    {
        public const float Month = 300, Friend = 40, Lover = 70, Rival = -40;
        public const float RebellionSeconds = 60, SocialRadius = 8, DuelRadius = 10;
        public const float AcidRadius = 3, AcidDuration = 4, AcidCooldown = 25;
        public const float RallyRadius = 8, RallyDuration = 8, RallyCooldown = 30;
        public const float DiveRadius = 2, DiveGrounded = 3, DiveCooldown = 30;
        public const int SkillLevel = 5;
    }
}
