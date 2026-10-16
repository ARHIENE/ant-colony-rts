using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using Object = UnityEngine.Object;
using Resource = AntColony.Data.ResourceType;

public static class Spec1013ClinicChecks
{
    static int checks;
    static void Check(bool ok, string message) { if (!ok) throw new Exception("FAIL " + message); checks++; }
    static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
    static Critter.State State(Critter c) => (Critter.State)typeof(Critter).GetField("s", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(c);
    static T Put<T>(BuildingKind kind, Vector3 at) where T : Component
    {
        var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { kind, UnitRole.Worker });
        var go = Object.Instantiate(template, at + Vector3.up * template.transform.localScale.y * .5f, Quaternion.identity);
        go.SetActive(true); return go.GetComponent<T>();
    }
    static async Task Ready()
    {
        var end = DateTime.UtcNow.AddSeconds(60);
        while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(100);
        Check(!SaveSystem.Busy, "world ready"); Time.timeScale = 0;
    }
    static void Warp(CommanderAnt c, Vector3 at)
    {
        var agent = c.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (UnityEngine.AI.NavMesh.SamplePosition(at, out var hit, 4, -1)) at = hit.position;
        agent.Warp(at); c.transform.position = at;
    }
    public static async Task<string> Main()
    {
        checks = 0; var oldRoot = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "Spec1013Clinic-" + Guid.NewGuid().ToString("N"));
        try
        {
            Check(Application.isPlaying, "play mode");
            SaveSystem.NewGame(new NewGameOptions { seed = 261111, mapSize = MapSize.Small }); await Ready();
            var rm = ResourceManager.Instance;
            var nurse = CommanderRoster.Instance.Commanders.First(c => c.IsColonyMember);
            foreach (var c in CommanderRoster.Instance.Commanders) { c.SetJobEnabled(CommanderJobs.All, false); c.CommandStop(); }
            nurse.SetJobEnabled(CommanderJobs.Nursing, true);
            var home = rm.BaseBin.Position;
            var clinic = Put<RanchFacility>(BuildingKind.AnimalClinic, home + new Vector3(5, 0, 4));
            var source = Object.FindObjectsByType<Storage>(FindObjectsSortMode.None).First(b => b.Bin != null);
            foreach (var t in SpeciesInfo.AllFeeds) rm.TrySpend(t, rm.GetAmount(t));
            Check(rm.AddTo(source.Bin, Resource.Leaf, 5) == 5, "food only in remote store");
            var patient = Critter.Spawn(Species.Aphid, clinic.Position, true);
            State(patient).hunger = 5; Call(patient, "EnterClinic", clinic);
            Warp(nurse, home - Vector3.right * 2);
            Check(nurse.StartAnimalTask(AnimalTask.FeedPatient, patient), "nurse accepts clinic feeding");
            Call(nurse, "TickAnimalFeeding", patient, .1f);
            Check(!nurse.IsCarrying && source.Bin.Amount(Resource.Leaf) == 5, "empty nearby store cannot remotely consume food");
            nurse.CommandStop();
            Check(AnimalMedicalStock.Reserved(Resource.Leaf) == 0, "cancel before pickup releases reservation");
            Warp(nurse, source.Position + Vector3.right * 2);
            Check(nurse.StartAnimalTask(AnimalTask.FeedPatient, patient), "feeding can be requested again");
            Call(nurse, "TickAnimalFeeding", patient, .1f);
            Check(nurse.CarriedAmount == 1 && source.Bin.Amount(Resource.Leaf) == 4, "physical pickup consumes one");
            var snapshot = SaveSnapshot.Capture();
            Check(snapshot.commanders.Any(c => c.personalState.id == nurse.PersonalState.id && c.carriedAmount == 1 && c.carriedType == (int)Resource.Leaf), "food in transit serialized as cargo");
            var before = ResourceNode.Available.Where(n => n.ResourceType == Resource.Leaf).Sum(n => n.AmountRemaining);
            nurse.CommandStop(); nurse.CommandStop();
            Check(nurse.CarriedAmount == 0 && ResourceNode.Available.Where(n => n.ResourceType == Resource.Leaf).Sum(n => n.AmountRemaining) == before + 1, "cancel drops food exactly once");
            Check(nurse.StartAnimalTask(AnimalTask.FeedPatient, patient), "start delivery");
            nurse.SetJobEnabled(CommanderJobs.Nursing, false); // 시작한 급식만 마치고 다음 급식 예약은 막는다.
            Time.timeScale = 1;
            var deliveryDeadline = DateTime.UtcNow.AddSeconds(60);
            while (patient.Hunger <= 5 && DateTime.UtcNow < deliveryDeadline) await Task.Delay(100);
            Time.timeScale = 0;
            Check(patient.Hunger > 5 && nurse.CarriedAmount == 0 && nurse.AnimalJob == AnimalTask.None, "feed after delivery consumes carried food");
            patient.ContractDisease(AnimalDisease.Contagious); patient.Disease.Tick(.3f); patient.Disease.medication = .8f;
            State(patient).awaitDecision = true;
            var feeder = Put<RanchFacility>(BuildingKind.Feeder, home + Vector3.left * 3);
            Check(feeder.Priority == 5, "feeder default priority five"); feeder.Priority = 9;
            Check(feeder.Priority == 9, "feeder priority up to nine");
            var id = patient.Id;
            var nurseId = nurse.PersonalState.id;
            var carriedFood = new FoodLots(); carriedFood.Add(1, 17);
            typeof(WorkerAnt).GetMethod("RestoreCargo", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(nurse, new object[] { 1f, Resource.Food, carriedFood });
            Check(SaveSystem.TrySave(false, 0, out var error), "save: " + error);
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "load: " + error); await Ready();
            var restored = Critter.All.First(c => c.Id == id);
            Check(RanchFacility.All.Any(f => f.IsFeeder && f.Priority == 9), "priority nine survives save/load");
            Check(restored.Clinic != null && restored.AwaitingDecision && restored.Contagious && Mathf.Abs(restored.Disease.medication - .8f) < .01f, "clinic decision and disease survive actual save/load");
            var loadedNurse = SaveSnapshot.Capture().commanders.First(c => c.personalState.id == nurseId);
            Check(loadedNurse.carriedAmount == 1 && loadedNurse.carriedLots != null && Mathf.Abs(loadedNurse.carriedLots.Stalest - 17) < .01f, "cargo freshness survives actual save/load");
            return "PASS " + checks + " clinic checks";
        }
        finally { Time.timeScale = 0; SaveStorage.RootOverride = oldRoot; }
    }
}
