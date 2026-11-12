using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;
using Object = UnityEngine.Object;

// Isolated fixture unlocks the facility and supplies an injury; the ordinary nursing scheduler treats it.
public static class Final1113MedicalUse
{
    public static async Task<string> Run()
    {
        if (!Application.isPlaying || UserSettings.Current.autoSaveEnabled || string.IsNullOrEmpty(AntColony.Save.SaveStorage.RootOverride))
            throw new Exception("Isolated game required");
        var actors = CommanderRoster.Instance.Commanders.Where(c => c && c.IsColonyMember).ToArray();
        var doctor = actors.First(c => c.CanDoJob(CommanderJobs.Nursing));
        var patient = actors.First(c => c != doctor);
        var jobs = actors.Select(c => c.WorkState.jobs).ToArray();
        var research = CampaignResearch.Instance.CaptureState();
        var oldHealth = patient.PersonalState.health;
        GameObject go = null;
        try
        {
            AntColony.UI.SimulationPause.UserSpeed(0);
            foreach (var c in actors) { c.CommandStop(); c.SetJobEnabled(CommanderJobs.All, false); }
            var state = CampaignResearch.Instance.CaptureState();
            state.completed.Add((int)ScienceTechnology.Herbs); state.completed.Add((int)ScienceTechnology.Infirmary);
            CampaignResearch.Instance.RestoreState(state);
            var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { BuildingKind.Infirmary, UnitRole.Worker });
            go = Object.Instantiate(template, patient.Position + Vector3.forward * 3, Quaternion.identity);
            if (Physics.Raycast(go.transform.position+Vector3.up*8,Vector3.down,out var ground,24,1<<8)) go.transform.position += Vector3.up*(ground.point.y-go.GetComponent<Renderer>().bounds.min.y); go.SetActive(true);
            var hospital = go.GetComponent<Infirmary>();
            FacilityVisual.Attach(hospital); await Task.Delay(150);
            var model = go.GetComponent<FacilityVisual>().Model;
            if (!model || !model.name.StartsWith("Infirmary")) throw new Exception("Meshy bed model missing");
            patient.PersonalState.health = new HealthState { migrated = true, selfTend = false, medicine = MedicineLevel.NoMedicine };
            var wound = new Wound { part = BodyPart.ArmL, kind = WoundKind.Cut, damage = 2 };
            patient.Health.wounds.Add(wound);
            if (!hospital.TryAdmit(patient)) throw new Exception("Actual facility admission failed");
            doctor.SetJobEnabled(CommanderJobs.Nursing, true);
            bool sawDoctor = false;
            string patientMotion = "", doctorMotion = "";
            var deadline = DateTime.UtcNow.AddSeconds(35);
            AntColony.UI.SimulationPause.UserSpeed(1);
            while (!wound.tended && DateTime.UtcNow < deadline)
            {
                if (doctor.MedicalPatient == patient && doctor.MedicalJob == MedJob.Tend)
                {
                    sawDoctor = true;
                    doctorMotion = doctor.GetComponent<AntVisual>().MotionName;
                    patientMotion = patient.GetComponent<AntVisual>().MotionName;
                }
                await Task.Delay(40);
            }
            if (!sawDoctor || !wound.tended) throw new Exception("Ordinary doctor did not complete treatment");
            var result = "Meshy Infirmary: actual admission and automatic real-frame nursing passed; doctor motion=" + doctorMotion + "; patient motion=" + patientMotion + "; bed alignment is a separate visual check";
            File.WriteAllText("work/final-motion-1113/medical-use.txt", result);
            return result;
        }
        finally
        {
            AntColony.UI.SimulationPause.UserSpeed(0);
            doctor.CommandStop(); patient.TreatmentFacility?.Release(patient);
            patient.PersonalState.health = oldHealth;
            for (int i = 0; i < actors.Length; i++) actors[i].WorkState.jobs = jobs[i];
            CampaignResearch.Instance.RestoreState(research);
            if (go) Object.Destroy(go);
        }
    }
}
