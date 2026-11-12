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
        private readonly Dictionary<CommanderAnt, bool> navigationUpdates = new Dictionary<CommanderAnt, bool>();
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
            if (c == null || patients.Contains(c) || patients.Count >= Capacity) return;
            patients.Add(c);
            if (c.Agent && c.Agent.enabled)
            {
                navigationUpdates[c] = c.Agent.updatePosition;
                c.Agent.updatePosition = false;
            }
            c.TreatmentFacility = this;
            c.PersonalState.treating = true;
        }

        public bool TryRestPose(CommanderAnt c, out Vector3 position, out Vector3 headDirection)
        {
            position = Position; headDirection = -transform.right;
            if (!IsTreating(c)) return false;
            int slot = patients.IndexOf(c);
            var model = GetComponent<FacilityVisual>()?.Model;
            var rest = model ? model.Find("Rest" + slot) : null;
            if (rest) { position = rest.position; headDirection = rest.forward; return true; }
            // Retain treatment support for the existing material-specific bed variants.
            var renderer = GetComponent<Renderer>();
            float floor = renderer ? renderer.bounds.min.y : Position.y;
            position = new Vector3(Position.x, floor + .8f, Position.z)
                + transform.forward * (slot == 0 ? -.4f : .4f);
            return true;
        }

        public bool IsTreating(CommanderAnt c) => isActiveAndEnabled && !IsDead && patients.Contains(c)
            && c.isActiveAndEnabled && !c.IsDead && !c.IsAwayFromHome && !c.IsEmbarked
            && c.PersonalState.mentalBreak == MentalBreak.None && Vector3.Distance(c.Position, Position) <= 8 && c.RescueCarrier == null;

        private void LateUpdate()
        {
            foreach (var c in patients)
                if (c && TryRestPose(c, out var rest, out _))
                {
                    var box = GetComponent<Renderer>();
                    // Keep identity, selection and treatment targets at the occupied bed, too.
                    // The stopped agent retains its reachable exit point until discharge.
                    c.transform.position = new Vector3(rest.x, box ? box.bounds.min.y : Position.y, rest.z);
                }
        }

        public void Release(CommanderAnt c)
        {
            patients.Remove(c);
            if (c != null && navigationUpdates.TryGetValue(c, out var updatePosition))
            {
                if (c.Agent && c.Agent.enabled)
                {
                    if (c.Agent.isOnNavMesh) c.Agent.Warp(c.Agent.nextPosition);
                    c.Agent.updatePosition = updatePosition;
                }
                navigationUpdates.Remove(c);
            }
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
