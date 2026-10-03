using System.Linq;
using AntColony.Core;
using AntColony.Data;
using UnityEngine;
using UnityEngine.AI;

namespace AntColony.Units
{
    public partial class CommanderAnt
    {
        private GameObject acidVisual;
        public bool CanAcidRain => CanReceiveOrders && !LabUpgradeBusy && HasTroops && Role == UnitRole.Ranged
            && talents.Level(CommanderActivity.Ranged) >= SocialRules.SkillLevel && Social.acidCooldown <= 0;
        public bool CanRally => CanReceiveOrders && !LabUpgradeBusy && HasTroops && Role == UnitRole.Support
            && talents.Level(CommanderActivity.Command) >= SocialRules.SkillLevel && Social.rallyCooldown <= 0;
        public bool CanDive => CanReceiveOrders && !LabUpgradeBusy && HasTroops && IsFlying
            && Mathf.Max(talents.Level(CommanderActivity.Melee), talents.Level(CommanderActivity.Ranged)) >= SocialRules.SkillLevel && Social.diveCooldown <= 0;
        private float SkillCooldownMultiplier => 1f + .5f * personalState.Severity(InjuryPart.Head);
        private static bool FinitePoint(Vector3 p) => !float.IsNaN(p.sqrMagnitude) && !float.IsInfinity(p.sqrMagnitude);
        public bool TryAcidRain(Vector3 point)
        {
            if (!CanAcidRain || !FinitePoint(point)) return false;
            Social.acidPoint = point; Social.acidRemaining = SocialRules.AcidDuration;
            Social.acidTick = 0; Social.acidDamage = AttackDamage * .25f;
            Social.acidCooldown = SocialRules.AcidCooldown * SkillCooldownMultiplier;
            RefreshAcidVisual(); return true;
        }
        public bool TryRally()
        {
            if (!CanRally) return false;
            Social.rallyCooldown = SocialRules.RallyCooldown * SkillCooldownMultiplier;
            foreach (var c in Active.OfType<CommanderAnt>())
                if (c.IsColonyMember && !c.IsEmbarked && (c.Position - Position).sqrMagnitude <= SocialRules.RallyRadius * SocialRules.RallyRadius)
                { c.Social.rallyRemaining = SocialRules.RallyDuration; if (c.Agent != null) c.Agent.speed = c.MovementSpeed; }
            AntColony.UI.ToastManager.Show(commanderName + ": 집결 — 이동 +30% (8초)");
            return true;
        }
        public bool TryDive(Vector3 point)
        {
            if (!CanDive || !FinitePoint(point) || !NavMesh.SamplePosition(point, out var hit, 2, NavMesh.AllAreas)) return false;
            CommandStop(); Social.diving = true; Social.divePoint = hit.position;
            Social.diveCooldown = SocialRules.DiveCooldown * SkillCooldownMultiplier;
            return true;
        }
        public void TickAdvancedSkills(float seconds)
        {
            if (!(seconds > 0) || float.IsInfinity(seconds) || IsDead) return;
            Social.acidCooldown = Mathf.Max(0, Social.acidCooldown - seconds);
            Social.rallyCooldown = Mathf.Max(0, Social.rallyCooldown - seconds);
            Social.diveCooldown = Mathf.Max(0, Social.diveCooldown - seconds);
            Social.rallyRemaining = Mathf.Max(0, Social.rallyRemaining - seconds);
            var wasGrounded = Social.groundedRemaining > 0;
            Social.groundedRemaining = Mathf.Max(0, Social.groundedRemaining - seconds);
            if (wasGrounded && Social.groundedRemaining == 0 && !IsEmbarked) ApplyMovementMode();
            if (Social.acidRemaining > 0)
            {
                var elapsed = Mathf.Min(seconds, Social.acidRemaining);
                Social.acidRemaining = Mathf.Max(0, Social.acidRemaining - elapsed);
                Social.acidTick += elapsed;
                while (Social.acidTick >= 1)
                {
                    Social.acidTick -= 1;
                    DamageArea(Social.acidPoint, SocialRules.AcidRadius, Social.acidDamage, UnitRole.Ranged);
                }
            }
            RefreshAcidVisual();
            if (!Social.diving || IsEmbarked) return;
            transform.position = Vector3.MoveTowards(Position, Social.divePoint, Mathf.Max(10, MovementSpeed * 4) * seconds);
            if ((Position - Social.divePoint).sqrMagnitude > .01f) return;
            Social.diving = false; Social.groundedRemaining = SocialRules.DiveGrounded;
            ApplyMovementMode();
            DamageArea(Position, SocialRules.DiveRadius, AttackDamage * 1.5f, UnitRole.Melee);
        }
        private void DamageArea(Vector3 point, float radius, float damage, UnitRole role)
        {
            foreach (var target in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IDamageable>().ToArray())
            {
                if (!CombatTargeting.CanAttack(role, target) || (target.Position - point).sqrMagnitude > radius * radius) continue;
                bool wasAlive = CombatTargeting.IsAlive(target);
                target.TakeDamage(damage);
                Social.combatSeen = true;
                personalState.lastCombatSeconds = 0;
                if (wasAlive && !CombatTargeting.IsAlive(target))
                {
                    GainExperience(CombatActivity, CommanderProgression.KillXp(target));
                    if (target is AntColony.Boss.BossHealth && Random.value < .2f) ShiftEventTrait(CommanderTrait.Coward, true);
                }
            }
        }
        private void RefreshAcidVisual()
        {
            if (Social.acidRemaining <= 0) { if (acidVisual != null) Destroy(acidVisual); return; }
            if (acidVisual == null)
            {
                acidVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                acidVisual.name = "Acid Rain Area"; Destroy(acidVisual.GetComponent<Collider>());
                acidVisual.transform.localScale = new Vector3(SocialRules.AcidRadius * 2, .025f, SocialRules.AcidRadius * 2);
                var color = new MaterialPropertyBlock(); color.SetColor("_BaseColor", new Color(.45f, .8f, .12f)); color.SetColor("_Color", new Color(.45f, .8f, .12f));
                acidVisual.GetComponent<Renderer>().SetPropertyBlock(color);
            }
            acidVisual.transform.position = Social.acidPoint + Vector3.up * .06f;
        }
    }
}
