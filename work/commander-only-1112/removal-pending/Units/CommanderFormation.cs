using System.Collections.Generic;
using AntColony.Core;
using UnityEngine;

namespace AntColony.Units
{
    public partial class CommanderAnt
    {
        public bool HasLegion => false; // Retired trait ID remains readable in old saves.
        public FormationState FormationState => personalState.formation ??= new FormationState();
        public Formation SelectedFormation => Formation.Plain;
        public IReadOnlyList<Formation> FormationAffinities => System.Array.Empty<Formation>();
        public bool CanSelectFormation => false;
        public string FormationBlockReason(Formation f)
        { return "진형은 사용하지 않습니다."; }
        public bool TrySelectFormation(Formation f)
        { return false; }
        private void EnsureFormationAffinities()
        {  }
        private void TickFormation(float seconds)
        {  }
        private void FaceFormation(Vector3 delta)
        {
            delta.y = 0;
            if (delta.sqrMagnitude > .000001f) FormationState.forward = delta.normalized;
        }
        private IDamageable formationHitTarget;
        private bool restoreFormationHit;
        private static string FormationTargetKey(IDamageable target) => target is Object obj && obj != null ? obj.GetEntityId().ToString() : null;
        internal void RestoreFormationHit()
        {
            if (!restoreFormationHit) return;
            restoreFormationHit = false;
            formationHitTarget = Save.SaveReferences.Get<IDamageable>(FormationState.hitTarget.Substring(6));
            if (formationHitTarget == null || !CombatTargeting.IsAlive(formationHitTarget)) FormationState.ResetHits();
            else FormationState.hitTarget = FormationTargetKey(formationHitTarget);
        }
        internal FormationState CaptureFormationForSave()
        {
            var copy = JsonUtility.FromJson<FormationState>(JsonUtility.ToJson(FormationState));
            if (restoreFormationHit) return copy;
            if (copy.hits > 0 && formationHitTarget != null && CombatTargeting.IsAlive(formationHitTarget))
                copy.hitTarget = "saved:" + Save.SaveReferences.Key(formationHitTarget);
            else copy.ResetHits();
            return copy;
        }
        private FormationModifiers FormationEffect(IDamageable target = null)
        { return new FormationModifiers(); }
        public FormationModifiers CurrentFormationModifiers => FormationEffect(CombatTarget);
        public float EffectiveAttackRange => AttackRange;
        public float EffectiveAttackInterval => AttackInterval;
        public float FormationWorldMoveMultiplier => 1f;
        protected override float AttackRange => base.AttackRange * (1 + FormationEffect().Range) * PierceRangeMultiplier;
        protected override float AttackInterval => base.AttackInterval / (1 + FormationEffect().AttackSpeed) / WrathFactor;
        public void TakeFormationDamage(float amount, Vector3? source, bool ranged, bool area, float reduction = 0)
        {
            var m = FormationEffect();
            float increase = source.HasValue && FormationRules.Rear(FormationState.forward, source.Value - Position) ? m.RearIncrease : 0;
            var before = TroopHealth + PersonalHealth;
            TakeTroopDamage(amount, FormationRules.ReceivedMultiplier(increase, reduction + (ranged ? m.RangedReduction : 0) + (area ? m.AreaReduction : 0)));
            AntColony.UI.GameFeel.Hit(this, before - (TroopHealth + PersonalHealth), !IsHostile); // 실제로 줄어든 부대·본인 체력만 보여 준다.
        }
    }
}
