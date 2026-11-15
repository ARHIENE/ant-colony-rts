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
        private Transform visualModel, muzzle;

        public float Range => attackRange + DefenseUpgrades.RangeBonus;
        public float Damage => attackDamage * ScienceEffects.AcidTowerDamageMultiplier;
        protected override bool UsesDefenseDurability => true;
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
            var target = CombatTargeting.FindNearestEnemy(Position, Range, UnitRole.Ranged);
            if (target == null) return;
            cooldown = attackInterval;
            var sprayOrigin = AimVisual(target.Position);
            if (spray != null)
            {
                spray.SetPosition(0, sprayOrigin);
                spray.SetPosition(1, target.Position);
                spray.enabled = true; sprayTime = .15f;
            }
            FormationDamage.Apply(target, Damage, Position, ranged: true);
        }

        internal void RestoreCooldown(float value) => cooldown = Mathf.Clamp(value, 0, attackInterval);

        private Vector3 AimVisual(Vector3 target)
        {
            var model = GetComponent<FacilityVisual>()?.Model;
            if (model != visualModel)
            {
                visualModel = model; muzzle = null;
                if (model) foreach (var child in model.GetComponentsInChildren<Transform>())
                    if (child.name == "AcidMuzzle") { muzzle = child; break; }
            }
            if (!muzzle) return Position + Vector3.up * 1.2f;
            var facing = muzzle.position - Position; facing.y = 0;
            var desired = target - Position; desired.y = 0;
            if (facing.sqrMagnitude > .001f && desired.sqrMagnitude > .001f)
                visualModel.RotateAround(Position, Vector3.up, Vector3.SignedAngle(facing, desired, Vector3.up));
            return muzzle.position;
        }

        protected override void OnDisable()
        {
            if (spray != null) spray.enabled = false;
            sprayTime = 0;
            base.OnDisable();
        }
    }
}
