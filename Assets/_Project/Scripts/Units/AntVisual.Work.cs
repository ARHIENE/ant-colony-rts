using AntColony.Buildings;
using UnityEngine;

namespace AntColony.Units
{
    public sealed partial class AntVisual
    {
        private bool HasStationaryWork => commander != null && commander.CivilianWorkReady && !commander.IsEating && !commander.IsPlaying
            && (commander.ServiceTarget == null || (commander.ServiceTarget.Position - transform.position).sqrMagnitude <= 49)
            && (!(commander.ServiceTarget is SupplyFacility station) || commander.AtDrill(station))
            && (!(commander.ServiceTarget is Processor chemical && chemical.Chemical) || commander.AtDrill(chemical))
            && (commander.ScienceAssignment == null || (commander.ScienceAssignment.Position - transform.position).sqrMagnitude <= 64)
            && (commander.ServiceTarget != null || commander.ScienceAssignment != null || commander.CraftingWorkshop != null
                || commander.SurveyAssignment != null && commander.SurveyAssignment.Powered && commander.SurveyAssignment.CandidateId >= 0
                    && commander.SurveyWorkReady && (commander.SurveyAssignment.Position - transform.position).sqrMagnitude <= 64
                || commander.MedicalJob != MedJob.None || commander.AnimalJob != AnimalTask.None || commander.CorpseTarget != null);
        private bool CarryingPerson => commander != null && commander.MedicalPatient != null && commander.MedicalPatient.RescueCarrier == commander;
        private string JoyClip() => commander.PersonalState.joy.freeWalk ? "SkyGaze" : commander.PersonalState.joy.kind switch
        {
            2 => "Inspect", 3 or 12 => "Bathe", 1 or 4 or 5 or 6 => "PlayBoard", 7 => "Wrestle", 8 => "Throw",
            9 => "Exercise", 10 => "PlayMusic", 11 => "Swing", _ => "Rest"
        };
        private (string clip, string tool) WorkClip()
        {
            if (commander.ScienceAssignment != null) Face(commander.ScienceAssignment.Position - transform.position);
            if (commander.CraftingWorkshop != null) Face(commander.CraftingWorkshop.Position - transform.position);
            if (commander.MedicalJob != MedJob.None) return (commander.MedicalJob == MedJob.Rescue ? "CarryPerson" : commander.MedicalPatient == commander ? "SelfTreat" : "Treat", null);
            if (commander.AnimalJob != AnimalTask.None)
                return (commander.AnimalJob == AnimalTask.Carry || commander.AnimalJob == AnimalTask.HaulCarcass ? "CarryPerson"
                    : commander.AnimalJob == AnimalTask.Surgery || commander.AnimalJob == AnimalTask.Dose ? "Treat" : "GroundWork", null);
            if (commander.CorpseTarget != null) return (commander.EatingCorpse ? "Eat" : "GroundWork", null);
            if (commander.ScienceAssignment != null) return ("Inspect", null);
            if (commander.SurveyAssignment != null) { Face(commander.SurveyAssignment.Position - transform.position); return ("ControlPanel", null); }
            if (commander.CraftingWorkshop != null) return ("SmithRepair", "Tool_Hammer_Stone");
            var target = commander.ServiceTarget;
            if (target != null) Face(target.Position - transform.position);
            if (commander.ServiceJob == CommanderJobs.Firefighting) return ("BeatFire", null);
            if (commander.ServiceJob == CommanderJobs.Repair) return ("Tool_Hammer", "Tool_Hammer_Stone");
            if (commander.ServiceJob == CommanderJobs.Cooking) return ((int)(Time.time / 4) % 2 == 0 ? "Stir" : "Knead", null);
            if (commander.ServiceJob == CommanderJobs.Nursing) return (target is Processor ? "Grind" : "Treat", null);
            if (commander.ServiceJob == CommanderJobs.Administration) return ("Stamp", null);
            if (target is Processor p) return p.Data.kind == AntColony.Data.BuildingKind.Carpentry ? ("Saw", "Tool_Saw") : ("Operate", null);
            if (target is SupplyFacility drill && UndergroundProduction.IsSolidDrill(drill.Kind)) return ("ControlPanel", null);
            if (target is PowerNode) return ("Run", null);
            return ("Operate", null);
        }
    }
}
