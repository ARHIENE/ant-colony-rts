using System;
using System.IO;
using System.Linq;
using System.Reflection;
using AntColony.Core;
using AntColony.Save;
using UnityEngine;
using R = AntColony.Data.ResourceType;
public static class ResourceCompatibility1028Checks
{
    public static string Main()
    {
        int count = 0; void Check(bool ok, string label) { if (!ok) throw new Exception("Resource compatibility " + label); count++; }
        var path = Directory.GetDirectories(Application.temporaryCachePath, "FoodIndustry-*").SelectMany(d => Directory.GetFiles(d, "*.json", SearchOption.AllDirectories)).OrderByDescending(File.GetLastWriteTimeUtc).First();
        Check(SaveValidator.TryParse(File.ReadAllText(path), out var file, out var error), "fixture parse " + error);
        var ship = new TransportDto { cargoAmounts = new int[60], cargoFood = 3 }; ship.cargoAmounts[0] = 3;
        file.world.transports.Add(ship); file.version = 19;
        Check(SaveValidator.Validate(file, out error), "v19 cargo migration fully validates " + error);
        Check(file.version == 20 && ship.cargoAmounts.Length == StoreBin.TypeCount && ship.cargoAmounts[0] == 3 && ship.cargoAmounts.Skip(60).All(n => n == 0), "cargo padded with zero, previous quantities retained");
        ship.cargoAmounts = new int[StoreBin.TypeCount + 1]; file.version = 19;
        Check(!SaveValidator.Validate(file, out error), "oversized cargo rejected");
        var bin = new StoreBin(); var dto = new StoreBin.Dto { amounts = new int[60], allowed = unchecked((long)(StoreBin.DefaultAllowed & ~StoreBin.Bit(R.Sand))), extraForbidden = null }; dto.amounts[(int)R.Iron] = 7;
        typeof(StoreBin).GetMethod("Apply", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(bin, new object[] { dto });
        Check(bin.Amount(R.Iron) == 7 && !bin.Allows(R.Sand) && bin.Allows(R.Rubber), "old bin amounts/filter kept, new high item independent");
        Check(bin.Capture().amounts.Length == StoreBin.TypeCount && bin.Capture().extraForbidden.Count == 0, "old bin reserializes into expanded catalog");
        var result = "PASS " + count + " legacy v19 cargo/bin migration and oversized cargo rejection checks"; File.WriteAllText("Temp/resource-compatibility1028-result.txt", result); return result;
    }
}
