using System;
using System.Linq;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Data;

// 2026-10-05 가구 데이터 정의(약 200개): 분류·시대·필수 방·기존 건물 연결이 일관적인지 확인한다.
public static class FurnitureCatalogChecks
{
    static int checks;
    static void Check(bool value, string message) { if (!value) throw new Exception("FAIL " + message); checks++; }

    public static Task<string> Main()
    {
        checks = 0;
        var all = FurnitureCatalog.All;
        Check(all.Count >= 200, "about 200 entries: " + all.Count);
        Check(FurnitureCatalog.CategoryNames.Length == Enum.GetValues(typeof(BuildCategory)).Length, "16 category names");
        foreach (BuildCategory c in Enum.GetValues(typeof(BuildCategory))) Check(FurnitureCatalog.InCategory(c).Any(), "category has items " + c);
        var dup = all.GroupBy(f => f.Name).FirstOrDefault(g => g.Count() > 1);
        Check(dup == null, "unique names " + dup?.Key);
        Check(all.All(f => f.Era >= 1 && f.Era <= 6), "era 1~6");
        foreach (var e in Enumerable.Range(1, 6)) Check(all.Any(f => f.Era == e), "each era has furniture " + e);
        var existing = all.Where(f => f.Existing != null).ToList();
        Check(existing.GroupBy(f => f.Existing).All(g => g.Count() == 1), "one catalog entry per existing building");
        foreach (var k in new[] { BuildingKind.Toilet, BuildingKind.Washbasin, BuildingKind.Shower, BuildingKind.Treadmill, BuildingKind.WoodGenerator, BuildingKind.PowerWire, BuildingKind.Battery, BuildingKind.ElectricLamp })
            Check(FurnitureCatalog.For(k) != null, "new furniture listed " + k);
        Check(FurnitureCatalog.For(BuildingKind.Treadmill).Room == RoomKind.PowerPlant && FurnitureCatalog.For(BuildingKind.Toilet).Room == RoomKind.Bathroom, "required rooms");
        Check(FurnitureCatalog.For(BuildingKind.AirshipYard).Category == BuildCategory.Transport && FurnitureCatalog.For(BuildingKind.AirshipYard).Era == 6, "rocket pad is future transport");
        Check(all.Where(f => f.Category == BuildCategory.Automation).All(f => f.Network == NetworkLayer.Signal), "automation uses signal wires");
        Check(all.Any(f => f.Network == NetworkLayer.Pipe) && all.Where(f => f.Category == BuildCategory.Plumbing).All(f => f.Network == NetworkLayer.Pipe), "plumbing uses pipes");
        Check(!FurnitureCatalog.For(BuildingKind.PowerWire).MaterialUpgradable && FurnitureCatalog.For(BuildingKind.Dormitory).MaterialUpgradable, "material upgrade flag");
        return Task.FromResult("PASS " + checks + " furniture catalog checks (" + all.Count + " entries)");
    }
}
