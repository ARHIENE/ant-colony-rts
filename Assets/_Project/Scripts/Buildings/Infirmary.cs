using System.Collections.Generic;
using System.Linq;
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

        // 의료용 침상(2026-10-15): 환자가 눕는 자리. 처치·투약·수술은 의료 장수가 와서 한다(CommanderHealthCare).
        public bool HasFreeBed => Unlocked && isActiveAndEnabled && !IsDead && patients.Count < Capacity;
        public bool CanTreat(CommanderAnt c) => !WorkPriorities.Red && HasFreeBed
            && c != null && c.CanChangeAllocation && !c.IsAwayFromHome && !c.IsWorking
            && (c.PersonalState.NeedsTreatment || c.Health.surgeries.Exists(s => s.Active)) && Vector3.Distance(c.Position, Position) <= 8;
        // 구조 운반 도착: 쓰러진 환자도 눕힌다.
        internal bool Admit(CommanderAnt c)
        {
            if (c == null || !HasFreeBed || patients.Contains(c)) return false;
            if (UnityEngine.AI.NavMesh.SamplePosition(Position, out var hit, 4, UnityEngine.AI.NavMesh.AllAreas)) c.Agent.Warp(hit.position);
            RestorePatient(c); return true;
        }

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

        public bool IsTreating(CommanderAnt c) => isActiveAndEnabled && !IsDead && patients.Contains(c)
            && c.isActiveAndEnabled && !c.IsDead && !c.IsAwayFromHome && !c.IsEmbarked
            && c.PersonalState.mentalBreak == MentalBreak.None && Vector3.Distance(c.Position, Position) <= 8 && c.RescueCarrier == null;

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
