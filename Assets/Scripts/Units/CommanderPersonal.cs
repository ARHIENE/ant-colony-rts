using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.UI;
using UnityEngine;

namespace AntColony.Units
{
    public partial class CommanderAnt
    {
        [SerializeField] private CommanderPersonalState personalState = new CommanderPersonalState();
        private float breakAttackTimer;
        public CommanderPersonalState PersonalState => personalState;
        public Infirmary TreatmentFacility { get; internal set; }
        public Workshop CraftingWorkshop { get; internal set; }
        public bool CanReceiveOrders => isActiveAndEnabled && !IsDead && !IsCaptive && !IsEmbarked
            && PersonalHealth > 0 && !IsDeparting && personalState.rageRemaining <= 0 && !Social.diving
            && CraftingWorkshop == null && !personalState.treating && personalState.mentalBreak == MentalBreak.None;
        public bool HasTrinket(TrinketEffect effect) => personalState.equipment.Exists(e => e.slot == EquipmentSlot.Trinket && e.effect == effect);
        public float EquipmentBonus(EquipmentSlot slot) => slot == EquipmentSlot.Weapon
            ? (Weapon != null && (Weapon.weapon == WeaponKind.Mandible || Weapon.weapon == WeaponKind.AcidSprayer) ? Weapon.quality + 1 : 0)
            : (EquippedArmor?.armor == ArmorKind.Coating ? EquippedArmor.quality + 1 : 0)
                + (Weapon?.weapon == WeaponKind.Shield ? 2 * (Weapon.quality + 1) : 0);
        protected override float WorkSpeed => WorkRate(ConstructionTarget != null && ConstructionTarget.IsArt ? CommanderActivity.Art : CommanderActivity.Building);
        protected override float MovementSpeed => base.MovementSpeed * traits.MoveMultiplier * AgeMoveMultiplier * BiomeRules.MoveAt(AntColony.Buildings.RoomSystem.IsIndoors(Position))
            * (1f - .3f * personalState.Severity(InjuryPart.Legs)) * (1f + TrinketBonus(TrinketEffect.Move))
            * (Weapon?.weapon == WeaponKind.Shield ? .9f : 1f) * (IsFlying ? 1f + .05f * EquippedArmor.quality : 1f)
            * AntColony.World.ColonyEvents.MoveMultiplier(this) * (Social.rallyRemaining > 0 ? 1.3f : 1f);
        public CommanderActivity CurrentActivity => ServiceTarget != null ? SkillFor(ServiceJob) : HuntTarget != null ? CombatActivity : CraftingWorkshop != null ? CommanderActivity.Crafting : LabUpgradeBusy ? CommanderActivity.Research : IsConstructing ? ConstructionTarget.IsArt ? CommanderActivity.Art : CommanderActivity.Building
            : CurrentResourceNode != null ? CurrentResourceNode.RequiresFishing ? CommanderActivity.Fishing
                : CurrentResourceNode.GetComponent<BuildingBase>() != null ? CommanderActivity.Farming : CommanderActivity.Gathering
            : CombatActivity;
        public bool HasNearbyFriend => Active.OfType<CommanderAnt>().Any(c => c != this && !c.IsDead && !c.IsEmbarked && c.IsHostile == IsHostile
            && (c.Position - Position).sqrMagnitude <= 36 && personalState.relations.Exists(r => r.otherId == c.PersonalState.id && r.value >= 40));
        public float Mood
        {
            get
            {
                var value = 60f + Decoration.MoodAt(this) + traits.BaseMood + (HasNearbyFriend ? 3 : 0) + TrinketBonus(TrinketEffect.Mood);
                foreach (var f in personalState.moodFactors) value += f.value < 0 ? f.value * traits.NegativeMoodMultiplier : f.value;
                if (IsWorking || LabUpgradeBusy) value += 3 * traits.Flame(CurrentActivity);
                if (personalState.treating) value -= 5;
                if (HasRivalFaction) value -= 3;
                if (Transport != null && Transport.Crew.Any(c => c != this && personalState.relations.Any(r => r.otherId == c.PersonalState.id && r.value <= SocialRules.Rival))) value -= 5;
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
            CraftingWorkshop?.Release();
            personalState = value == null ? new CommanderPersonalState() : JsonUtility.FromJson<CommanderPersonalState>(JsonUtility.ToJson(value));
            personalState.treating = false; // Building restoration reconnects the patient after commanders load.
            RefreshEquipment();
        }
        public bool TryReward()
        {
            if (!CanReceiveOrders || IsAwayFromHome || personalState.rewardCooldown > 0 || ResourceManager.Instance == null
                || !ResourceManager.Instance.TrySpend(30, 0, reason: ResourceReason.Reward)) return false;
            MoodEvent("포상", traits.Has(CommanderTrait.Greedy) || traits.Has(CommanderTrait.Ambitious) ? 15 : traits.Has(CommanderTrait.Ascetic) ? 3 : 8);
            personalState.rewardCooldown = GameCalendar.SecondsPerMonth;
            return true;
        }
        public void TickPersonal(float seconds)
        {
            if (!(seconds > 0) || float.IsInfinity(seconds) || IsDead) return;
            TickAge(seconds); if (IsDead) return;
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
            var recovering = personalState.treating && personalState.NeedsTreatment;
            TickInfection(seconds);
            if (IsDead) return;
            personalState.Tick(seconds, traits, TreatmentFacility != null ? TreatmentFacility.TreatmentRate : 1, TreatmentFacility != null ? TreatmentFacility.PermanentInjuryChance : .1f);
            if (personalState.treating && !personalState.NeedsTreatment)
            {
                TreatmentFacility.Release(this);
                if (recovering) MoodEvent("치료 완료", 3);
            }
            personalState.lastCombatSeconds += seconds;
            personalState.homeSeconds = IsAwayFromHome ? 0 : personalState.homeSeconds + seconds;
            if (Agent != null) Agent.speed = MovementSpeed;
            if (IsEmbarked || IsCaptive) return;
            TickSocial(seconds);
            if (IsDeparting || personalState.rageRemaining > 0) return;
            personalState.workedSeconds = IsWorking || LabUpgradeBusy ? personalState.workedSeconds + seconds : 0;
            // 피로는 CommanderSleep이 관리한다(낮 작업으로 차고 밤 수면으로 풀림). 휴게실은 휴식 기분만 준다.
                if (RestRoom.Serves(this)) personalState.AddMood("Rest", GameBalance.RestMood, 5);
            if (traits.Has(CommanderTrait.Neat) && NearCorpse) personalState.AddMood("시체 주변", -3, 2);
            else personalState.moodFactors.RemoveAll(f => f.reason == "시체 주변");
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
            CraftingWorkshop?.Release();
            personalState.mentalBreak = kind;
            Social.breakdowns++;
            if (Social.breakdowns == 5) ShiftEventTrait(CommanderTrait.Fragile, false);
            // Phase 3: 붕괴 '도주'는 말 그대로 떠난다(탈주와 같은 처리, 혼자).
            if (kind == MentalBreak.Flee) { personalState.mentalBreak = MentalBreak.None; BeginDeparture(false); return; }
            personalState.breakRemaining = kind == MentalBreak.Idle ? 60 : kind == MentalBreak.Flee ? 30 : 45;
            if (kind == MentalBreak.SelfHarm)
            {
                LoseTroops(Mathf.CeilToInt(troopCount * .2f), "정신 붕괴: 자해"); personalState.AddInjury(traits, true); EndMentalBreak();
            }
            else if (kind == MentalBreak.Binge)
            {
                var rm = ResourceManager.Instance;
                if (rm != null) rm.TrySpend(Mathf.Min(30, rm.GetAmount(ResourceType.Food)), 0, reason: ResourceReason.Upkeep);
                EndMentalBreak();
            }
            AntColony.UI.ToastManager.Show(commanderName + ": " + kind);
        }
        private void TickBreak(float seconds)
        {
            personalState.breakRemaining = Mathf.Max(0, personalState.breakRemaining - seconds);
            if (personalState.breakRemaining <= 0) { EndMentalBreak(); return; }
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
        public void LoseTroops(int count, string cause = "전투")
        {
            count = Mathf.Min(Mathf.Max(0, count), troopCount);
            if (count == 0) return;
            troopCount -= count; if (!IsDeparting) AntPool.Instance?.LoseAssigned(count);
            AntColony.World.Corpse.Drop(this, AntColony.World.CorpseKind.Ant, "일반개미", count);
            if (troopCount == 0) { pendingDamage = 0; CommandStop(); if (IsHostile) CaptureDeparting(); else if (IsDeployed) ReturnToPost(); }
        }
        private void OnDowned(string cause = "전투", bool forceFatal = false)
        {
            var mode = CommanderDeathRuntime.Mode;
            var fatal = forceFatal || (mode == CommanderDeathMode.Harsh ? Random.value < .3f
                : mode == CommanderDeathMode.Normal && personalState.HasSeriousInjury && Random.value < .25f);
            personalState.AddInjury(traits);
            if (Random.value < .5f) personalState.AddInjury(traits);
            ScienceAssignment?.ReleaseResearcher(); LabUpgradeLab?.CancelResearch();
            CraftingWorkshop?.Release();
            personalState.dead = fatal;
            WorkState.health = 0; WorkState.recoverySeconds = fatal ? 0 : GameBalance.CommanderRecoverySeconds;
            AntColony.World.DiplomacyManager.Instance?.CommanderDowned(transform.position);
            NotifyDowned(fatal, cause);
            foreach (var c in CommanderRoster.Instance != null ? CommanderRoster.Instance.Commanders : Active.OfType<CommanderAnt>().ToArray())
            {
                if (c == this || c.IsDead || c.Traits.Has(CommanderTrait.ColdBlooded)) continue;
                var relation = c.PersonalState.relations.Find(r => r.otherId == personalState.id);
                var friend = relation != null && relation.value >= 40;
                if (fatal || (c.Position - Position).sqrMagnitude <= 144)
                    c.PersonalState.AddMood((fatal ? "Death: " : "Downed: ") + commanderName,
                        (fatal ? relation?.spouse == true ? -30 : friend ? -15 : -10 : friend ? -15 : c.Traits.Has(CommanderTrait.Coward) ? -16 : -8)
                            * (c.Traits.Has(CommanderTrait.Undertaker) ? .5f : 1),
                        fatal ? relation?.spouse == true ? 600 : 300 : 180);
            }
            if (fatal)
            {
                personalState.departure = "Deceased";
                CampaignHistory.Record("사망", commanderName, cause);
                if (personalState.equipment.Count > 0)
                {
                    AntColony.World.EquipmentLoot.Drop(Position, personalState.equipment);
                    personalState.equipment.Clear();
                }
                AntColony.World.Corpse.Drop(this, AntColony.World.CorpseKind.ColonyCommander, commanderName);
                gameObject.SetActive(false);
            }
        }
        private void TickRelations(float seconds)
        {
            foreach (var other in Active.OfType<CommanderAnt>().ToArray())
            {
                if (other == this || !other.IsColonyMember || other.IsEmbarked || other.IsCaptive) continue;
                var relation = personalState.Relation(other.PersonalState.id);
                var distance = (other.Position - Position).sqrMagnitude;
                bool workingTogether = distance <= 64 && CurrentActivity == other.CurrentActivity
                    && (IsWorking || LabUpgradeBusy) && (other.IsWorking || other.LabUpgradeBusy);
                bool fightingTogether = distance <= 64 && Social.combatSeen && other.Social.combatSeen && personalState.lastCombatSeconds < 30 && other.personalState.lastCombatSeconds < 30;
                if (workingTogether || fightingTogether)
                {
                    relation.nearbySeconds += seconds;
                    while (relation.nearbySeconds >= 60)
                    {
                        relation.nearbySeconds -= 60;
                        relation.value = Mathf.Min(100, relation.value + 2 * (traits.Has(CommanderTrait.Sociable) ? 1.5f : traits.Has(CommanderTrait.ColdBlooded) ? .5f : 1)
                            * (other.Traits.Has(CommanderTrait.Undertaker) ? .8f : 1));
                    }
                }
                // 한 쌍을 한 번만 판정한다. 양쪽 관계와 손실을 같은 순간에 적용한다.
                if (string.CompareOrdinal(personalState.id, other.personalState.id) >= 0) continue;
                bool opposed = (traits.Has(CommanderTrait.Brave) || traits.Has(CommanderTrait.Reckless)) && (other.traits.Has(CommanderTrait.Cautious) || other.traits.Has(CommanderTrait.Coward))
                    || (other.traits.Has(CommanderTrait.Brave) || other.traits.Has(CommanderTrait.Reckless)) && (traits.Has(CommanderTrait.Cautious) || traits.Has(CommanderTrait.Coward));
                relation.quarrelSeconds = distance <= 64 && opposed ? relation.quarrelSeconds + seconds : 0;
                while (relation.quarrelSeconds >= SocialRules.Month)
                {
                    relation.quarrelSeconds -= SocialRules.Month;
                    if (Random.value < .1f) { ChangeRelation(other, -10); CampaignHistory.Record("말다툼", commanderName, other.commanderName, true); }
                }
                relation.duelSeconds = distance <= 100 && relation.value <= -60 && other.personalState.Relation(personalState.id).value <= -60
                    && HasTroops && other.HasTroops ? relation.duelSeconds + seconds : 0;
                while (relation.duelSeconds >= 60)
                {
                    relation.duelSeconds -= 60;
                    if (Random.value < .1f) Duel(other);
                }
            }
        }
        private void ChangeRelation(CommanderAnt other, float delta)
        {
            var a = personalState.Relation(other.personalState.id); a.value = Mathf.Clamp(a.value + delta, -100, 100);
            var b = other.personalState.Relation(personalState.id); b.value = Mathf.Clamp(b.value + delta, -100, 100);
        }
        private void Duel(CommanderAnt other)
        {
            if (!HasTroops || !other.HasTroops) return;
            var loser = AttackDamage == other.AttackDamage ? (Random.value < .5f ? this : other) : AttackDamage < other.AttackDamage ? this : other;
            // 결투는 일반 전투 사망 판정을 쓰지 않는다. 진 쪽 경상만 발생한다.
            foreach (var c in new[] { this, other })
            {
                int lost = Mathf.CeilToInt(c.troopCount * .2f);
                c.troopCount -= lost; AntPool.Instance?.LoseAssigned(lost);
                AntColony.World.Corpse.Drop(c, AntColony.World.CorpseKind.Ant, "일반개미", lost);
                if (!c.HasTroops) { c.pendingDamage = 0; c.CommandStop(); }
            }
            loser.personalState.AddInjury(loser.traits, true);
            CampaignHistory.Record("결투", commanderName + " / " + other.commanderName, loser.commanderName + " 패배", true);
            ToastManager.Show(commanderName + " / " + other.commanderName + ": 결투로 병력 손실");
        }
    }
}
