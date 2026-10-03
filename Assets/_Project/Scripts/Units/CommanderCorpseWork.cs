using System.Linq;
using AntColony.Core;
using AntColony.Data;
using AntColony.World;
using UnityEngine;

namespace AntColony.Units
{
    public partial class CommanderAnt
    {
        public Corpse CorpseTarget { get; private set; }
        public bool EatingCorpse { get; private set; }
        public float CorpseProgress { get; private set; }
        public float CorpseWorkRate => WorkFactor * (!EatingCorpse && traits.Has(CommanderTrait.Neat) ? 1.5f : 1);
        public bool NearCorpse => !IsEmbarked && !IsCaptive && Corpse.All.Any(c => c.Available && (c.Position - Position).sqrMagnitude <= 64);

        public bool StartCorpseWork(Corpse corpse, bool eat = false)
        {
            if (!CivilianWorkReady || !CanReceiveOrders || LabUpgradeBusy || IsCarrying || IsConstructing || IsEating || IsPlaying || IsFoodPoisoned
                || corpse == null || !corpse.Available || corpse.Handler != null && corpse.Handler != this
                || eat && (!traits.Has(CommanderTrait.Cannibal) || !corpse.Edible)
                || !SameBattlefield(corpse.Position) || !TryWorkApproach(corpse.Position, out var approach)
                || (approach - corpse.Position).sqrMagnitude > 4) return false;
            CommandStop();
            if (!corpse.Claim(this)) return false;
            CorpseTarget = corpse; EatingCorpse = eat; WorkState.resting = false;
            SetMoveDestination(approach); return true;
        }
        private void ReleaseCorpse()
        {
            CorpseTarget?.Release(this); CorpseTarget = null; CorpseProgress = 0; EatingCorpse = false;
        }
        internal void RestoreCorpseWork(Corpse corpse)
        {
            if (StartCorpseWork(corpse, corpse.Data.eating)) CorpseProgress = corpse.Data.progress;
        }
        private bool FindCorpseWork(bool eat)
        {
            foreach (var corpse in Corpse.All.Where(c => c.Available && c.Handler == null && (!eat || c.Edible)
                && (eat || c.GetComponent<ResourceNode>() == null || c.Priority))
                .OrderByDescending(c => c.Priority).ThenBy(c => (c.Position - Position).sqrMagnitude))
                if (StartCorpseWork(corpse, eat)) return true;
            return false;
        }
        private bool TickCorpseWork(float seconds)
        {
            if (CorpseTarget == null) return false;
            var corpse = CorpseTarget;
            if (!CivilianWorkReady || !CanReceiveOrders || !corpse.Available || corpse.Handler != this
                || EatingCorpse && !traits.Has(CommanderTrait.Cannibal)) { CommandStop(); return false; }
            if (GetDistanceTo(corpse.Position) > 2)
            {
                if (!TryWorkApproach(corpse.Position, out var approach) || (approach - corpse.Position).sqrMagnitude > 4) CommandStop();
                else { SetMoveDestination(approach); TickFlightMovement(); }
                return true;
            }
            StopMoving(); GetComponent<AntVisual>()?.Action(EatingCorpse ? "Eat" : "Attack");
            CorpseProgress += seconds * (EatingCorpse ? 1 : CorpseWorkRate);
            if (CorpseProgress < Corpse.WorkSeconds) return true;
            var eat = EatingCorpse; var colonyCommander = corpse.Data.kind == CorpseKind.ColonyCommander;
            if (corpse.Finish(this, eat))
            {
                if (eat) EatCorpse(colonyCommander);
                else if (traits.Has(CommanderTrait.Undertaker)) personalState.AddMood("시체 정리", 5, 180);
            }
            CommandStop(); return true;
        }
        private void EatCorpse(bool colonyCommander)
        {
            ResourceManager.Instance?.Add(ResourceType.Food, 10);
            MealState.satiety = 100; MealState.retrySeconds = 0;
            personalState.moodFactors.RemoveAll(f => f.reason == "Hunger");
            personalState.AddMood("동족 포식", 15, 600);
            foreach (var other in SocialRoster.Where(c => c.IsColonyMember))
            {
                if (colonyCommander) other.MoodEvent("아군 장수 시체 포식", -3);
                if (other == this || !other.isActiveAndEnabled || other.IsCaptive || other.IsEmbarked
                    || other.Traits.Has(CommanderTrait.Cannibal) || other.Traits.Has(CommanderTrait.ColdBlooded)
                    || (other.Position - Position).sqrMagnitude > 144) continue;
                other.PersonalState.AddMood("동족 포식 목격", -8, 300);
                var relation = other.PersonalState.Relation(personalState.id); relation.value = Mathf.Max(-100, relation.value - 10);
            }
            CampaignHistory.Record("동족 포식", commanderName, colonyCommander ? "아군 장수 시체" : "개미 시체");
        }
    }
}
