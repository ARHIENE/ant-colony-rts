using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using UnityEngine;

namespace AntColony.Units
{
    public partial class CommanderAnt
    {
        [SerializeField] private CommanderPersonalState personalState = new CommanderPersonalState();
        private float breakAttackTimer;
        public CommanderPersonalState PersonalState => personalState;
        public Infirmary TreatmentFacility { get; internal set; }
        public bool CanReceiveOrders => isActiveAndEnabled && !IsDead && !IsCaptive && !IsEmbarked
            && !personalState.treating && personalState.mentalBreak == MentalBreak.None;
        public bool HasTrinket(TrinketEffect effect) => personalState.equipment.Exists(e => e.slot == EquipmentSlot.Trinket && e.effect == effect);
        public float EquipmentBonus(EquipmentSlot slot) => slot == EquipmentSlot.Weapon
            ? (Weapon != null && (Weapon.weapon == WeaponKind.Mandible || Weapon.weapon == WeaponKind.AcidSprayer) ? Weapon.quality + 1 : 0)
            : (EquippedArmor?.armor == ArmorKind.Coating ? EquippedArmor.quality + 1 : 0)
                + (Weapon?.weapon == WeaponKind.Shield ? 2 * (Weapon.quality + 1) : 0);
        protected override float WorkSpeed => traits.WorkMultiplier * talents.Multiplier(CommanderActivity.Building);
        protected override float MovementSpeed => base.MovementSpeed * traits.MoveMultiplier
            * (1f - .3f * personalState.Severity(InjuryPart.Legs)) * (1f + TrinketBonus(TrinketEffect.Move))
            * (Weapon?.weapon == WeaponKind.Shield ? .9f : 1f) * (IsFlying ? 1f + .05f * EquippedArmor.quality : 1f);
        public CommanderActivity CurrentActivity => LabUpgradeBusy ? CommanderActivity.Research : IsConstructing ? CommanderActivity.Building
            : CurrentResourceNode != null ? CurrentResourceNode.RequiresFishing ? CommanderActivity.Fishing
                : CurrentResourceNode.GetComponent<BuildingBase>() != null ? CommanderActivity.Farming : CommanderActivity.Gathering
            : CombatActivity;
        public bool HasNearbyFriend => Active.OfType<CommanderAnt>().Any(c => c != this && !c.IsDead && !c.IsEmbarked
            && (c.Position - Position).sqrMagnitude <= 36 && personalState.relations.Exists(r => r.otherId == c.PersonalState.id && r.value >= 40));
        public float Mood
        {
            get
            {
                var value = 60f + traits.BaseMood + (HasNearbyFriend ? 3 : 0) + TrinketBonus(TrinketEffect.Mood);
                foreach (var f in personalState.moodFactors) value += f.value < 0 ? f.value * traits.NegativeMoodMultiplier : f.value;
                if (IsWorking || LabUpgradeBusy) value += 3 * traits.Flame(CurrentActivity);
                if (personalState.treating) value -= 5;
                if (traits.Has(CommanderTrait.Greedy)) value += personalState.equipment.Count(e => e.quality == 3) * 3 - (personalState.equipment.Exists(e => e.slot == EquipmentSlot.Trinket) ? 0 : 5);
                if (traits.Has(CommanderTrait.Homebody)) value += IsAwayFromHome ? -5 : 3;
                if (traits.Has(CommanderTrait.Wanderer)) value += IsAwayFromHome ? 5 : personalState.homeSeconds >= 1200 ? -5 : 0;
                if (traits.Has(CommanderTrait.Bloodthirsty)) value += personalState.lastCombatSeconds < 30 ? 10 : personalState.lastCombatSeconds >= 600 ? -5 : 0;
                return Mathf.Clamp(value, 0, 100);
            }
        }
        public CommanderPersonalState CapturePersonalState() => JsonUtility.FromJson<CommanderPersonalState>(JsonUtility.ToJson(personalState));
        public void RestorePersonalState(CommanderPersonalState value)
        {
            TreatmentFacility?.Release(this);
            personalState = value == null ? new CommanderPersonalState() : JsonUtility.FromJson<CommanderPersonalState>(JsonUtility.ToJson(value));
            personalState.treating = false; // Building restoration reconnects the patient after commanders load.
            RefreshEquipment();
        }
        public bool TryReward()
        {
            if (!CanReceiveOrders || IsAwayFromHome || personalState.rewardCooldown > 0 || ResourceManager.Instance == null
                || !ResourceManager.Instance.TrySpend(30, 0)) return false;
            traits.ChangeLoyalty(traits.Has(CommanderTrait.Greedy) ? 15 : traits.Has(CommanderTrait.Ascetic) ? 3 : 8, "Reward");
            personalState.rewardCooldown = GameCalendar.SecondsPerMonth;
            return true;
        }
        public void TickPersonal(float seconds)
        {
            if (!(seconds > 0) || float.IsInfinity(seconds) || IsDead) return;
            if (HasTroops && IsInCombat && !IsWorking && CanReceiveOrders && !LabUpgradeBusy)
            {
                talents.combatSeconds += seconds;
                while (talents.combatSeconds >= 10)
                {
                    talents.combatSeconds -= 10;
                    if (CombatActivity != CommanderActivity.Command) GainExperience(CombatActivity, 2);
                    GainExperience(CommanderActivity.Command, 2);
                }
            }
            if (TreatmentFacility != null && !TreatmentFacility.IsTreating(this)) TreatmentFacility.Release(this);
            personalState.treating = TreatmentFacility != null;
            var recovering = personalState.treating && personalState.HasTreatableInjury;
            personalState.Tick(seconds, traits);
            if (personalState.treating && !personalState.HasTreatableInjury)
            {
                TreatmentFacility.Release(this);
                if (recovering) traits.ChangeLoyalty(3, "Treatment completed");
            }
            personalState.lastCombatSeconds += seconds;
            personalState.homeSeconds = IsAwayFromHome ? 0 : personalState.homeSeconds + seconds;
            if (Agent != null) Agent.speed = MovementSpeed;
            if (IsEmbarked || IsCaptive) return;
            personalState.workedSeconds = IsWorking || LabUpgradeBusy ? personalState.workedSeconds + seconds : 0;
            if (personalState.workedSeconds >= 300 && !traits.Has(CommanderTrait.Workaholic))
                personalState.AddMood("Fatigue", traits.Has(CommanderTrait.Ascetic) ? 5 : -10, 30);
            TickRelations(seconds);
            if (personalState.mentalBreak != MentalBreak.None) { TickBreak(seconds); return; }
            personalState.breakCheck += seconds;
            if (personalState.breakCheck < 60) return;
            personalState.breakCheck %= 60;
            var mood = Mood + (traits.Has(CommanderTrait.IronWill) ? 10 : traits.Has(CommanderTrait.Fragile) ? -10 : 0);
            var chance = mood <= 5 ? .4f : mood <= 20 ? .15f : mood <= 35 ? .05f : 0;
            if (Random.value >= chance) return;
            var choices = new System.Collections.Generic.List<MentalBreak> { MentalBreak.Idle, MentalBreak.Flee,
                MentalBreak.AttackBuilding, MentalBreak.AttackCommander, MentalBreak.SelfHarm, MentalBreak.Binge };
            if (traits.Has(CommanderTrait.Coward) || traits.Has(CommanderTrait.Cautious)) choices.AddRange(new[] { MentalBreak.Idle, MentalBreak.Flee, MentalBreak.Flee });
            if (traits.Has(CommanderTrait.Reckless) || traits.Has(CommanderTrait.Bloodthirsty)) choices.AddRange(new[] { MentalBreak.AttackBuilding, MentalBreak.AttackCommander });
            if (traits.Has(CommanderTrait.Loyal) || traits.Has(CommanderTrait.Ascetic)) choices.AddRange(new[] { MentalBreak.SelfHarm, MentalBreak.SelfHarm });
            if (traits.Has(CommanderTrait.Glutton)) choices.AddRange(new[] { MentalBreak.Binge, MentalBreak.Binge });
            StartMentalBreak(choices[Random.Range(0, choices.Count)]);
        }
        public void StartMentalBreak(MentalBreak kind)
        {
            if (!System.Enum.IsDefined(typeof(MentalBreak), kind) || kind == MentalBreak.None || IsDead || IsEmbarked || IsCaptive) return;
            CommandStop(); ScienceAssignment?.ReleaseResearcher(); LabUpgradeLab?.CancelResearch();
            TreatmentFacility?.Release(this);
            personalState.mentalBreak = kind;
            personalState.breakRemaining = kind == MentalBreak.Idle ? 60 : kind == MentalBreak.Flee ? 30 : 45;
            if (kind == MentalBreak.SelfHarm)
            {
                LoseTroops(Mathf.CeilToInt(troopCount * .2f)); personalState.AddInjury(traits, true); EndMentalBreak();
            }
            else if (kind == MentalBreak.Binge)
            {
                var rm = ResourceManager.Instance;
                if (rm != null) rm.TrySpend(Mathf.Min(30, rm.GetAmount(ResourceType.Food)), 0);
                EndMentalBreak();
            }
            AntColony.UI.ToastManager.Show(commanderName + ": " + kind);
        }
        private void TickBreak(float seconds)
        {
            personalState.breakRemaining = Mathf.Max(0, personalState.breakRemaining - seconds);
            if (personalState.breakRemaining <= 0) { EndMentalBreak(); return; }
            if (personalState.mentalBreak == MentalBreak.Flee)
            {
                var queen = FindFirstObjectByType<QueenChamber>();
                if (queen != null && !IsAwayFromHome) SetMoveDestination(queen.Position);
                TickFlightMovement(); return;
            }
            IDamageable target = null;
            if (personalState.mentalBreak == MentalBreak.AttackBuilding)
                target = FindObjectsByType<BuildingBase>(FindObjectsSortMode.None).Where(b => b.CountsTowardPlayerDefeat && !b.IsDead && !(b is AntColony.World.ExpeditionTransport))
                    .OrderBy(b => (b.Position - Position).sqrMagnitude).FirstOrDefault();
            if (personalState.mentalBreak == MentalBreak.AttackCommander)
                target = Active.OfType<CommanderAnt>().Where(c => c != this && !c.IsDead && !c.IsEmbarked && c.HasTroops)
                    .OrderBy(c => (c.Position - Position).sqrMagnitude).FirstOrDefault();
            if (target == null || !HasTroops || Data == null) return;
            if (GetDistanceTo(target.Position) > Data.attackRange) { SetMoveDestination(target.Position); TickFlightMovement(); return; }
            StopMoving(); breakAttackTimer -= seconds;
            if (breakAttackTimer > 0) return;
            breakAttackTimer = Data.attackInterval; target.TakeDamage(AttackDamage);
        }
        private void EndMentalBreak()
        {
            personalState.mentalBreak = MentalBreak.None; personalState.breakRemaining = 0; CommandStop();
            personalState.AddMood("Catharsis", 20, 180);
        }
        public void LoseTroops(int count)
        {
            count = Mathf.Min(Mathf.Max(0, count), troopCount);
            if (count == 0) return;
            troopCount -= count; AntPool.Instance?.LoseAssigned(count);
            if (troopCount == 0) { pendingDamage = 0; CommandStop(); OnDowned(); }
        }
        private void OnDowned()
        {
            var mode = CommanderDeathRuntime.Mode;
            var fatal = mode == CommanderDeathMode.Harsh ? Random.value < .3f
                : mode == CommanderDeathMode.Normal && personalState.HasSeriousInjury && Random.value < .25f;
            personalState.AddInjury(traits);
            if (Random.value < .5f) personalState.AddInjury(traits);
            ScienceAssignment?.ReleaseResearcher(); LabUpgradeLab?.CancelResearch();
            personalState.dead = fatal;
            foreach (var c in CommanderRoster.Instance != null ? CommanderRoster.Instance.Commanders : Active.OfType<CommanderAnt>().ToArray())
            {
                if (c == this || c.IsDead || c.Traits.Has(CommanderTrait.ColdBlooded)) continue;
                var relation = c.PersonalState.relations.Find(r => r.otherId == personalState.id);
                var friend = relation != null && relation.value >= 40;
                if (fatal || (c.Position - Position).sqrMagnitude <= 144)
                    c.PersonalState.AddMood((fatal ? "Death: " : "Downed: ") + commanderName,
                        fatal ? relation?.spouse == true ? -30 : friend ? -15 : -10 : friend ? -15 : c.Traits.Has(CommanderTrait.Coward) ? -16 : -8,
                        fatal ? relation?.spouse == true ? 600 : 300 : 180);
                if (friend) c.PersonalState.rageRemaining = c.Traits.Has(CommanderTrait.Reckless) ? 120 : 60;
            }
            if (fatal)
            {
                personalState.departure = "Deceased";
                GetComponent<AntVisual>()?.Death();
                gameObject.SetActive(false);
            }
        }
        private void TickRelations(float seconds)
        {
            foreach (var other in Active.OfType<CommanderAnt>())
            {
                if (other == this || other.IsDead || other.IsEmbarked || (other.Position - Position).sqrMagnitude > 64) continue;
                if (CurrentActivity != other.CurrentActivity || !(IsWorking || LabUpgradeBusy || personalState.lastCombatSeconds < 30)) continue;
                var relation = personalState.Relation(other.PersonalState.id);
                relation.nearbySeconds += seconds;
                if (relation.nearbySeconds < 60) continue;
                relation.nearbySeconds %= 60;
                relation.value = Mathf.Min(100, relation.value + 2 * (traits.Has(CommanderTrait.Sociable) ? 1.5f : traits.Has(CommanderTrait.ColdBlooded) ? .5f : 1));
            }
        }
    }
}
