using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Data;
using AntColony.Buildings;
using AntColony.Units;
using AntColony.World;
using AntColony.Save;
using UnityEngine;
using UnityEngine.AI;

// Disposable fixture only: never mix injected resources/health with natural play evidence.
public static class EarlyFlow1113Checks
{
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static int count;
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); count++; }
    static object Call(object target, string method, params object[] args)
        => target.GetType().GetMethod(method, Hidden).Invoke(target, args);
    static bool FoodFor(CommanderAnt p) => (bool)typeof(CommanderAnt).GetMethod("HasFoodFor", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { p });
    static T Build<T>(BuildingKind kind, Vector3 pos) where T : Component
    {
        var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { kind, UnitRole.Worker });
        var go = UnityEngine.Object.Instantiate(template, pos, Quaternion.identity); go.name = "FIXTURE " + kind; go.SetActive(true); return go.GetComponent<T>();
    }
    public static async Task<string> Run()
    {
        if (!Application.isPlaying || Time.timeScale != 0 || !(SaveStorage.RootOverride ?? "").Contains("EarlyFlow1113")) throw new Exception("Paused disposable EarlyFlow session required");
        count = 0;
        var team = CommanderRoster.Instance.Commanders.Where(c => c != null && !c.IsDead).ToArray();
        GameSession.Instance.MarkStarted(10, 30);
        foreach (var c in team)
        {
            c.CommandStop(); c.PersonalState.health = new HealthState { migrated = true };
            c.WorkState.health = 100; c.WorkState.resting = false; c.WorkState.duty = CommanderDuty.Civilian; c.PersonalState.treating = false;
            c.PersonalState.mentalBreak = MentalBreak.None; c.PersonalState.meal = new CommanderMealState();
            c.PersonalState.sleep = new CommanderSleepState(); c.PersonalState.hygiene = new CommanderHygieneState(); c.PersonalState.joy = new CommanderJoyState();
        }
        var doctor = team.First(c => c.CanDoJob(CommanderJobs.Nursing) && !c.IsSleepTime);
        var patient = team.First(c => c != doctor);
        doctor.SetJobEnabled(CommanderJobs.Nursing, true);
        var origin = doctor.Position;
        var storage = Build<Storage>(BuildingKind.Storage, origin + Vector3.right * 5);
        await Task.Delay(250);
        var rm = ResourceManager.Instance;
        foreach (var type in CookedFoods.MealTypes) rm.TrySpend(type, rm.GetAmount(type));
        patient.WorkState.health = 0; patient.PersonalState.meal.satiety = 0; patient.PersonalState.meal.diet = CommanderAnt.DietRaw;
        Check(!FoodFor(patient), "Empty pantry must not dispatch futile patient feeding");
        rm.AddTo(storage.Bin, ResourceType.Food, patient.RationPortion - 1);
        Check(!FoodFor(patient), "Incomplete raw portion must not dispatch patient feeding");
        rm.AddTo(storage.Bin, ResourceType.Food, 1);
        Check(FoodFor(patient), "Complete allowed portion should enable field feeding");
        if (NavMesh.SamplePosition(origin + Vector3.forward, out var hit, 3, NavMesh.AllAreas)) patient.Agent.Warp(hit.position);
        Check(patient.TreatmentFacility == null, "Fixture must have no medical bed");
        Check((bool)Call(doctor, "StartMed", MedJob.Feed, patient), "Doctor can start field feeding");
        Call(doctor, "TickMed", 100f);
        Check(patient.Satiety == 100 && rm.GetAmount(ResourceType.Food) == 0, "Field feeding consumes exactly one raw portion");
        patient.PersonalState.meal.satiety = 0; patient.PersonalState.meal.eatSeconds = 2;
        Check(FoodFor(patient), "Already acquired meal remains usable with empty pantry");
        Call(patient, "FeedInBed");
        Check(patient.Satiety == 100 && rm.GetAmount(ResourceType.Food) == 0, "In-progress patient meal completes without second serving");
        patient.PersonalState.meal.satiety = 0;
        Check(CommanderAnt.CareWaitReason(patient).Contains("급식"), "No bed must not mask food shortage");
        patient.Health.medicine = MedicineLevel.NoCare;
        Check(CommanderAnt.CareWaitReason(patient) == "치료 안 함 설정", "No-care setting remains explicit");
        patient.Health.medicine = MedicineLevel.NoMedicine; patient.PersonalState.meal.satiety = 100;
        MedicalRules.AddWound(patient.Health, BodyPart.LegL, WoundKind.Cut, 3);
        Check((bool)Call(doctor, "StartMed", MedJob.Tend, patient), "Field tending starts without medicine or bed");
        Call(doctor, "TickMed", 100f);
        Check(patient.Health.wounds.All(w => w.tended), "Field tending actually completes");
        patient.WorkState.health = 100; patient.Health.wounds.Clear();

        doctor.CommandStop();
        var sourceGo = new GameObject("FIXTURE source"); sourceGo.transform.position = doctor.Position + Vector3.left * 3;
        var source = sourceGo.AddComponent<ResourceNode>(); source.ConfigureNatural(ResourceType.Food, 100, 0);
        WorkPriorities.Set(source, 8, true);
        typeof(WorkerAnt).GetField("targetNode", Hidden).SetValue(doctor, source);
        typeof(WorkerAnt).GetMethod("RestoreCargo", Hidden).Invoke(doctor, new object[] { 8f, ResourceType.Food, null });
        var previousPiles = SceneObjects.All<ResourceNode>().ToArray();
        typeof(WorkerAnt).GetMethod("DropCargo", Hidden).Invoke(doctor, null);
        var dropped = SceneObjects.All<ResourceNode>().Single(n => !previousPiles.Contains(n) && n.IsLooseCargo);
        Check(dropped.TryGetComponent<TargetPriority>(out var priority) && priority.level == 8 && priority.yellow,
            "Dropped food retains player target priority and yellow designation");
        Check(dropped.AmountRemaining == 8 && doctor.CarriedAmount == 0, "Dropping preserves quantity without duplicating cargo");

        var researcher = team.First(c => c.CanDoJob(CommanderJobs.Research) && !c.IsSleepTime);
        researcher.CommandStop(); researcher.SetJobEnabled(CommanderJobs.Research, true);
        var lab = Build<ScienceLab>(BuildingKind.ScienceLab, researcher.Position + Vector3.right * 4);
        await Task.Delay(250);
        lab.RestoreAssignment(1, researcher);
        var research = CampaignResearch.Instance;
        var state = new CampaignResearch.State { active = (int)ScienceTechnology.Herbs, progress = 199 };
        Check(CampaignResearch.Validate(state, out var error), "Old 199/200 research state: " + error);
        research.RestoreState(state);
        Check(research.LabWaitReason(lab) == "", "Research fixture must be ready: " + research.LabWaitReason(lab));
        var expected = researcher.WorkRate(CommanderActivity.Research) * 3;
        Check(Math.Abs(research.LabRate(lab) - expected) < .001f, "Settlement research actual work rate is 3x");
        research.SetPaused(true); research.Tick(1);
        Check(research.Progress == 199 && research.ResearchRate == 0, "Pause preserves old progress");
        research.SetPaused(false); research.Tick(1 / expected + .01f);
        Check(research.Has(ScienceTechnology.Herbs) && research.Active == null, "Old saved progress completes once");
        var completed = research.CompletedCount; research.Tick(1);
        Check(research.CompletedCount == completed, "Completed research cannot complete twice");
        var later = CampaignResearch.Technologies.First(t => t.Tier == 2);
        research.RestoreState(new CampaignResearch.State { active = (int)later.Technology }); lab.RestoreAssignment(2, researcher);
        Check(Math.Abs(research.LabRate(lab) - researcher.WorkRate(CommanderActivity.Research) * CampaignResearch.LabMultiplier(2)) < .001f, "Later-era research speed unchanged");
        var result = "PASS " + count + " disposable field-care/research regression checks; injected fixture, not natural play evidence.";
        File.WriteAllText("work/early-flow-1113/regression.txt", result); return result;
    }
}
