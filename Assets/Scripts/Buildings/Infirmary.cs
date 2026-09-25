using System.Collections.Generic;
using AntColony.Core;
using AntColony.Units;
using UnityEngine;

namespace AntColony.Buildings
{
    public sealed class Infirmary : BuildingBase
    {
        public const int Capacity = 2;
        private readonly List<CommanderAnt> patients = new List<CommanderAnt>();
        public IReadOnlyList<CommanderAnt> Patients => patients;
        public static bool Unlocked => CampaignResearch.Instance != null && CampaignResearch.Instance.Has(ScienceTechnology.Infirmary);

        public bool CanTreat(CommanderAnt c) => Unlocked && isActiveAndEnabled && !IsDead && patients.Count < Capacity
            && c != null && c.CanChangeAllocation && !c.IsAwayFromHome && !c.IsWorking
            && c.PersonalState.NeedsTreatment && Vector3.Distance(c.Position, Position) <= 8;

        public bool TryAdmit(CommanderAnt c)
        {
            if (!CanTreat(c)) return false;
            c.CommandStop();
            RestorePatient(c);
            return true;
        }

        internal void RestorePatient(CommanderAnt c)
        {
            patients.Add(c);
            c.TreatmentFacility = this;
            c.PersonalState.treating = true;
        }

        // 부위 재생: 영구 손상 1곳을 8분 치료로 되돌린다(Special 20). 중상이 없는 장수만 받는다.
        public bool CanRegenerate(CommanderAnt c) => ScienceEffects.Has(ScienceTechnology.Regeneration) && Unlocked && isActiveAndEnabled && !IsDead
            && patients.Count < Capacity && c != null && c.CanChangeAllocation && !c.IsAwayFromHome && !c.IsWorking
            && !c.PersonalState.HasTreatableInjury && c.PersonalState.injuries.Exists(i => i.severity == InjurySeverity.Permanent)
            && Vector3.Distance(c.Position, Position) <= 8;

        public bool TryRegenerate(CommanderAnt c)
        {
            if (!CanRegenerate(c) || ResourceManager.Instance == null || !ResourceManager.Instance.TrySpend(0, 0, GameBalance.RegenerationSpecial, reason: ResourceReason.Treatment)) return false;
            var injury = c.PersonalState.injuries.Find(i => i.severity == InjurySeverity.Permanent);
            injury.severity = InjurySeverity.Serious;
            injury.remaining = GameBalance.RegenerationSeconds;
            injury.regenerating = true;
            return TryAdmit(c);
        }

        public bool IsTreating(CommanderAnt c) => isActiveAndEnabled && !IsDead && patients.Contains(c)
            && c.isActiveAndEnabled && !c.IsDead && !c.IsAwayFromHome && !c.IsEmbarked
            && c.PersonalState.mentalBreak == MentalBreak.None && Vector3.Distance(c.Position, Position) <= 8;

        public void Release(CommanderAnt c)
        {
            patients.Remove(c);
            if (c != null && c.TreatmentFacility == this)
            {
                c.TreatmentFacility = null;
                c.PersonalState.treating = false;
            }
        }

        protected override void OnDisable()
        {
            while (patients.Count > 0) Release(patients[patients.Count - 1]);
            base.OnDisable();
        }
    }
}
