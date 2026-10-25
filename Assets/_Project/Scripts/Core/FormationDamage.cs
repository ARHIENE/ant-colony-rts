using AntColony.Units;
using UnityEngine;

namespace AntColony.Core
{
    public static class FormationDamage
    {
        // 출발점 없는 지속 피해는 기존 TakeDamage 경로를 사용한다.
        public static void Apply(IDamageable target, float amount, Vector3 source, bool ranged = false, bool area = false)
        {
            if (target is CommanderAnt commander) commander.TakeFormationDamage(amount, source, ranged, area);
            else target.TakeDamage(amount);
        }
    }
}
