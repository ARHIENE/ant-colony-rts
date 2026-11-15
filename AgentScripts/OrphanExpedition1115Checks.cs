using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.World;
using AntColony.UI;
using UnityEngine;

public static class OrphanExpedition1115Checks
{
    const string Log = "work/colony-polish-1115/orphan-expedition.txt";
    static void Check(bool ok, string note)
    {
        File.AppendAllText(Log, (ok ? "PASS " : "FAIL ") + note + "\n");
        if (!ok) throw new Exception(note);
    }
    public static async Task<string> Main()
    {
        File.WriteAllText(Log, "Natural departure loss retained; DTO variants below are isolated validation checks only.\n");
        if (Time.timeScale != 0 || string.IsNullOrEmpty(SaveStorage.RootOverride)) throw new Exception("Paused isolated run required");
        var ship = WorldMapManager.Instance.Transports.Single(t => t != null && t.Walking);
        Check(ship.State == ExpeditionState.Deployed && ship.Crew.Count == 0 && ship.CargoLoad == 12,
            "Actual orphaned expedition restored with all 12 cargo units and no resurrected crew");
        Check(ship.CargoCapacity == 0 && ship.CargoFull && ship.ReturnCargoSpace == 0 && ship.AddCargo(ResourceType.Soil, 1) == 0,
            "Zero-carrier expedition rejects new cargo");
        Check(!ship.TryReturn(), "Abandoned supplies cannot return without a carrier");
        var before = ship.CargoSummary;
        Check(SaveSystem.TrySave(false, 0, out var error), "Actual failed natural state now saves: " + error);
        var path = SaveSlots.PathFor(false, 0);
        var original = File.ReadAllText(path);
        Check(SaveValidator.TryParse(original, out var file, out error), "Saved orphan passes complete validation: " + error);
        var index = file.world.transports.FindIndex(t => t.walking);
        var dto = file.world.transports[index];
        dto.cargoAmounts[(int)ResourceType.Soil] = -1;
        Check(!SaveValidator.Validate(file, out error), "Negative cargo remains invalid");
        foreach (var validation in new[] {
            typeof(SavePreflight).GetMethod("ValidateIntegration", BindingFlags.NonPublic | BindingFlags.Static),
            typeof(SavePreflight).Assembly.GetType("AntColony.Save.SaveTransportValidation").GetMethod("Validate", BindingFlags.NonPublic | BindingFlags.Static) })
        {
            if (validation == null) throw new Exception("Required cargo validator missing");
            foreach (var amount in new[] { 80, 81 })
            {
                if (!SaveValidator.TryParse(original, out file, out error)) throw new Exception(error);
                dto = file.world.transports[index]; dto.walking = false; dto.handcart = true;
                if (dto.damage != null) dto.damage.health = Mathf.Min(dto.damage.health, TransportDamage.VehicleHealth);
                Array.Clear(dto.cargoAmounts, 0, dto.cargoAmounts.Length);
                dto.cargoAmounts[(int)ResourceType.Soil] = amount;
                bool accepted = true;
                try { validation.Invoke(null, new object[] { file }); }
                catch (TargetInvocationException e) when (e.InnerException is FormatException) { accepted = false; }
                Check(accepted == (amount == 80), validation.DeclaringType.Name + " handcart cargo " + amount + " accepted=" + accepted);
            }
        }
        if (!SaveSystem.TryLoad(path, out error)) throw new Exception(error);
        var until = DateTime.UtcNow.AddSeconds(100);
        while ((SaveSystem.Busy || !GameSession.Exists || !GameSession.Instance.GameStarted) && DateTime.UtcNow < until) await Task.Delay(100);
        SimulationPause.UserSpeed(0);
        ship = WorldMapManager.Instance.Transports.Single(t => t != null && t.Walking);
        Check(ship.Crew.Count == 0 && ship.CargoSummary == before, "Full reload preserves carrier loss and remaining supplies");
        File.Copy(path, "work/natural-campaign-1115/checkpoint-save.json", true);
        return File.ReadAllText(Log);
    }
}
