using AntColony.Core;
using AntColony.Data;
using UnityEngine;

namespace AntColony.Buildings
{
    public sealed class AcidTower : BuildingBase
    {
        // ponytail: 1차 단일형 밸런스. 실제 플레이 결과에 따라 템플릿 Inspector에서 조정한다.
        [SerializeField, Min(1f)] private float attackRange = 14f;
        [SerializeField, Min(1f)] private float attackDamage = 18f;
        [SerializeField, Min(.1f)] private float attackInterval = 1.5f;
        [SerializeField] private LineRenderer spray;
        private float cooldown, searchTimer, sprayTime;

        public float Range => attackRange;
        public float Damage => attackDamage;
        public float AttackInterval => attackInterval;
        public float Cooldown => cooldown;

        private void Update() => Tick(Time.deltaTime);

        public void Tick(float seconds)
        {
            if (!isActiveAndEnabled || IsDead || !(seconds > 0f) || float.IsInfinity(seconds)) return;
            cooldown = Mathf.Max(0, cooldown - seconds);
            sprayTime = Mathf.Max(0, sprayTime - seconds);
            if (spray != null) spray.enabled = sprayTime > 0;
            searchTimer -= seconds;
            if (cooldown > 0 || searchTimer > 0) return;
            searchTimer = .25f;
            var target = CombatTargeting.FindNearestEnemy(Position, attackRange, UnitRole.Ranged);
            if (target == null) return;
            cooldown = attackInterval;
            if (spray != null)
            {
                spray.SetPosition(0, Position + Vector3.up * 1.2f);
                spray.SetPosition(1, target.Position);
                spray.enabled = true; sprayTime = .15f;
            }
            target.TakeDamage(attackDamage);
        }

        internal void RestoreCooldown(float value) => cooldown = Mathf.Clamp(value, 0, attackInterval);

        protected override void OnDisable()
        {
            if (spray != null) spray.enabled = false;
            sprayTime = 0;
            base.OnDisable();
        }
    }
}
