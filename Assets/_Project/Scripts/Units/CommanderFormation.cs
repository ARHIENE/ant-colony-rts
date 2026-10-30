using System.Collections.Generic;
using AntColony.Core;
using UnityEngine;

namespace AntColony.Units
{
    public partial class CommanderAnt
    {
        public bool HasLegion => traits.Has(CommanderTrait.Legion);
        public FormationState FormationState => personalState.formation ??= new FormationState();
        public Formation SelectedFormation => FormationState.selected;
        public IReadOnlyList<Formation> FormationAffinities { get { EnsureFormationAffinities(); return FormationState.affinities; } }
        public bool CanSelectFormation => CanMobilize && !IsDeployed && !IsAwayFromHome && !IsInCombat;
        public string FormationBlockReason(Formation f)
        {
            if (!FormationRules.Defined(f)) return "알 수 없는 진형입니다.";
            if (f == Formation.Plain) return "";
            if (FormationRules.Traditional(f))
            {
                EnsureFormationAffinities();
                return HasLegion || FormationState.affinities.Contains(f) ? "" : "진형 적성이 없습니다.";
            }
            return FormationResearch.Unlocked(f) ? "" : "진형 전술 연구가 필요합니다.";
        }
        public bool TrySelectFormation(Formation f)
        {
            if (!CanSelectFormation || FormationBlockReason(f) != "") return false;
            FormationState.selected = f; FormationState.ResetHits(); return true;
        }
        private void EnsureFormationAffinities()
        {
            var state = FormationState;
            var due = 1 + talents.Level(CommanderActivity.Command) / 10;
            while (state.awarded < due && state.affinities.Count < 3)
            {
                var candidates = new List<Formation>();
                for (var f = Formation.FishScale; f <= Formation.Snake; f++) if (!state.affinities.Contains(f)) candidates.Add(f);
                state.affinities.Add(candidates[Random.Range(0, candidates.Count)]); state.awarded++;
            }
        }
        private void TickFormation(float seconds)
        {
            if (Save.SaveSystem.Busy) return;
            RestoreFormationHit(); EnsureFormationAffinities(); FormationState.Tick(seconds);
            if (CombatTarget != null) FormationState.Target(FormationTargetKey(CombatTarget));
            var delta = CombatTarget != null ? CombatTarget.Position - Position
                : Agent != null && Agent.enabled ? Agent.velocity : Vector3.zero;
            FaceFormation(delta);
        }
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
        {
            bool front = target != null && FormationRules.Front(FormationState.forward, target.Position - Position);
            var site = Transport?.Site ?? Garrison?.Site;
            var biome = site != null ? BiomeRules.ForSite(site.Title) : BiomeRules.Current;
            return FormationRules.Modifiers(IsDeployed || Transport != null || Garrison != null || IsHostile ? SelectedFormation : Formation.Plain,
                HasLegion, Role == AntColony.Data.UnitRole.Ranged, biome == MapBiome.Forest || biome == MapBiome.Cave,
                front, IsDirectMoving, target == null || FormationState.hitTarget == FormationTargetKey(target) ? FormationState.hits : 0);
        }
        public FormationModifiers CurrentFormationModifiers => FormationEffect(CombatTarget);
        public float EffectiveAttackRange => AttackRange;
        public float EffectiveAttackInterval => AttackInterval;
        public float FormationWorldMoveMultiplier => 1 + FormationRules.Modifiers(SelectedFormation, HasLegion).WorldMove;
        protected override float AttackRange => base.AttackRange * (1 + FormationEffect().Range) * PierceRangeMultiplier;
        protected override float AttackInterval => base.AttackInterval / (1 + FormationEffect().AttackSpeed) / WrathFactor;
        public void TakeFormationDamage(float amount, Vector3? source, bool ranged, bool area, float reduction = 0)
        {
            var m = FormationEffect();
            float increase = source.HasValue && FormationRules.Rear(FormationState.forward, source.Value - Position) ? m.RearIncrease : 0;
            TakeTroopDamage(amount, FormationRules.ReceivedMultiplier(increase, reduction + (ranged ? m.RangedReduction : 0) + (area ? m.AreaReduction : 0)));
        }
    }
}
