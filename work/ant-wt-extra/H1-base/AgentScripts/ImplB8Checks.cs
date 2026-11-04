using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.World;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

public static class ImplB8Checks
{
    static int checks;
    static readonly List<GameObject> created = new List<GameObject>();
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool ok, string name) { if (!ok) throw new Exception("B8 FAIL " + checks + ": " + name); checks++; }
    static bool Near(float a, float b) => Mathf.Abs(a - b) < .001f;
    static void Pool(int free, int assigned = 0, int reserved = 0) => typeof(AntPool).GetMethod("RestoreCounts", Hidden)
        .Invoke(AntPool.Instance, new object[] { free, assigned, reserved });
    static ColonyPopulation Reset(int free = 0, int assigned = 0, int reserved = 0)
    {
        Pool(free, assigned, reserved);
        var p = ColonyPopulation.Instance;
        p.Restore(new ColonyPopulation.State { immigrationStopped = true }); return p;
    }
    static T Building<T>(string name, BuildingKind kind, Vector3 position) where T : BuildingBase
    {
        var go = new GameObject(name); created.Add(go); go.SetActive(false); go.transform.position = position;
        var b = go.AddComponent<T>();
        var data = ScriptableObject.CreateInstance<BuildingData>(); data.kind = kind; data.constructionAnts = 0;
        data.foodCapacityBonus = data.soilCapacityBonus = data.specialCapacityBonus = 10000;
        typeof(BuildingBase).GetField("data", Hidden).SetValue(b, data); go.SetActive(true); return b;
    }
    static CivicService Service(CivicServiceKind kind)
    {
        var go = new GameObject("B8 " + kind); created.Add(go); go.SetActive(false);
        var service = go.AddComponent<CivicService>(); service.Kind = kind; go.SetActive(true); return service;
    }
    static void CivicBuildingPersistence(BuildingKind kind, CivicServiceKind serviceKind)
    {
        const BindingFlags internalStatic = BindingFlags.Static | BindingFlags.NonPublic;
        var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", internalStatic)
            .Invoke(null, new object[] { kind, UnitRole.Worker });
        Check(template != null && !template.activeSelf && BuildingPlacementController.LockReason(kind) == null,
            "civic construction template " + kind);
        var go = Object.Instantiate(template, new Vector3(4900, 0, 4900), Quaternion.identity);
        created.Add(go); go.SetActive(true);
        var service = go.GetComponent<CivicService>();
        Check(service != null && service.Kind == serviceKind && service.Data.kind == kind, "civic building identity " + kind);
        var buildingSaves = typeof(SaveCatalog).Assembly.GetType("AntColony.Save.SaveBuildings", true);
        var commanders = CommanderRoster.Instance.Commanders.ToList();
        var dto = (BuildingDto)buildingSaves.GetMethod("Capture", internalStatic)
            .Invoke(null, new object[] { service, "new:B8-" + kind, true, commanders });
        dto = JsonUtility.FromJson<BuildingDto>(JsonUtility.ToJson(dto));
        Check(dto.runtimeBuilt && dto.kind == kind.ToString(), "civic catalog JSON kind " + kind);
        go.SetActive(false);
        var restored = (BuildingBase)buildingSaves.GetMethod("Resolve", internalStatic).Invoke(null, new object[] { dto });
        created.Add(restored.gameObject);
        buildingSaves.GetMethod("Restore", internalStatic).Invoke(null, new object[] { restored, dto, commanders });
        Check(restored is CivicService civic && civic.Kind == serviceKind && civic.Data.kind == kind
            && Near(restored.CurrentHealth, dto.health), "civic building save reconstruction " + kind);
        restored.gameObject.SetActive(false);
    }
    static async Task Ready()
    {
        var end = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(50);
        Check(!SaveSystem.Busy, "load complete"); Time.timeScale = 0;
    }

    public static async Task<string> Main()
    {
        checks = 0; Check(Application.isPlaying, "Play mode required");
        var root = SaveStorage.RootOverride; var settings = UserSettings.Current.Clone();
        var isolated = settings.Clone(); isolated.autoSaveEnabled = false; UserSettings.Apply(isolated, false);
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "ImplB8-" + Guid.NewGuid().ToString("N"));
        var random = UnityEngine.Random.state;
        try
        {
            await Ready();
            var options = new NewGameOptions { startingCount = 1, seed = 261024, mapSize = MapSize.Small,
                biome = MapBiome.Forest, startingTeam = new[] { StartingCandidate.Roll(0) } };
            options.startingTeam[0].traits.values.Clear(); CrashLandingStart.Begin(options); await Ready();
            var commander = CommanderRoster.Instance.Commanders.Single();
            commander.CommandStop(); commander.SetJobEnabled(AntColony.Units.CommanderJobs.All, false);
            CivicBuildingPersistence(BuildingKind.School, CivicServiceKind.School);
            CivicBuildingPersistence(BuildingKind.CitizenClinic, CivicServiceKind.Clinic);
            CivicBuildingPersistence(BuildingKind.CivicPublicService, CivicServiceKind.PublicService);
            // 불시착 시작의 비축더미/시민/시설을 가정하지 않는다.
            var pile = Building<Stockpile>("B8 pile", BuildingKind.QueenChamber, commander.Position + Vector3.right * 12);
            Building<Storage>("B8 store", BuildingKind.Storage, pile.Position + Vector3.right * 6);
            var rm = ResourceManager.Instance;
            rm.Add(AntColony.Data.ResourceType.Food, 5000);
            rm.Add(AntColony.Data.ResourceType.Soil, 5000);
            rm.Add(AntColony.Data.ResourceType.MedicineBasic, 5000);
            Check(rm.GetAmount(AntColony.Data.ResourceType.Food) >= 100 && rm.GetAmount(AntColony.Data.ResourceType.MedicineBasic) >= 100, "own operating stock");
            var p = Reset(); Check(Near(p.S.taxRate, .2f), "G17 default tax twenty percent");
            Check(p.PublicServiceCoverage == 1 && p.AdminDemandWork == 0 && p.Administration == 0, "empty population boundaries");
            Pool(500); Check(p.PublicServiceCoverage == 1 && p.AdminDemandWork == 1, "first five hundred baseline");
            Pool(1000); Check(Near(p.PublicServiceCoverage, .5f) && p.AdminDemandWork == 2, "expansion workload and service shortage");
            var raw = GameBalancePopulation.Demand(p.FacilityDemand / 100f * .5f + p.MedicalCoverage * .5f, (p.SafetyDemand - 100) / 100f * .5f,
                (p.S.sentiment - 60) / 100, GameBalance.AdminDemand * p.Administration / 100);
            Check(Near(p.ImmigrationDemand, raw * .5f), "G17 coverage multiplies immigration");
            var civic = Service(CivicServiceKind.PublicService);
            Check(p.PublicServiceCapacity == 1000 && p.PublicServiceCoverage == 1, "one service covers five hundred");
            civic.gameObject.SetActive(false); Check(p.PublicServiceCapacity == 500, "disabled service excluded");
            p.AddAdministration(2); p.Tick(GameCalendar.SecondsPerDay);
            Check(Near(p.S.adminWork, 0), "two work consumed per day at one thousand");
            p = Reset(80); p.S.young = 20;
            var school = Service(CivicServiceKind.School); var clinic = Service(CivicServiceKind.Clinic);
            Check(p.EducationCoverage == 1 && Near(p.MedicalCoverage, .5f), "school twenty and clinic fifty");
            p.S.ages = new int[65]; p.S.ages[17] = 20; p.S.ages[18] = 80;
            p.AgeYear(); Check(p.S.young == 0 && ColonyPopulation.Adults == 100, "adulthood preserves old age transition");
            Check(Near(p.S.educationBonus, .1f) && p.S.educationSeconds == GameCalendar.SecondsPerSeason, "adulthood education one season");
            Check(Near(p.CivicSentiment, 7.5f), "education and medical sentiment capped independently");
            var educatedTax = p.MonthlyTax; p.S.educationBonus = 0;
            Check(Near(educatedTax, p.MonthlyTax * 1.1f), "education boosts taxable production ten percent");
            p.S.educationBonus = .1f;
            var save = p.Capture(); p.Restore(save);
            Check(Near(p.S.educationBonus, .1f) && ColonyPopulation.ValidCivicState(p.S), "education JSON roundtrip");
            var foodBefore = rm.GetAmount(AntColony.Data.ResourceType.Food); var medicineBefore = rm.GetAmount(AntColony.Data.ResourceType.MedicineBasic);
            p.S.taxRate = 0; p.Tick(GameCalendar.SecondsPerDay);
            Check(Near(p.S.schoolFoodDebt, .3f) && Near(p.S.schoolMaterialDebt, .15f)
                && Near(p.S.clinicFoodDebt, .3f) && Near(p.S.clinicMedicineDebt, .15f), "daily fractional costs");
            Check(rm.GetAmount(AntColony.Data.ResourceType.Food) == foodBefore, "fractional cost not rounded up daily");
            p.Tick(GameCalendar.SecondsPerDay * 6);
            Check(rm.GetAmount(AntColony.Data.ResourceType.Food) == foodBefore - 4
                && rm.GetAmount(AntColony.Data.ResourceType.MedicineBasic) == medicineBefore - 1, "seven-day aggregate integer upkeep");
            Check(p.S.educationBonus == 0 && p.S.educationSeconds == 0, "education expires after six days");
            p.S.clinicMedicineDebt = 2;
            rm.TrySpend(AntColony.Data.ResourceType.MedicineBasic, rm.GetAmount(AntColony.Data.ResourceType.MedicineBasic));
            p.PayCivicUpkeep(); Check(!p.ClinicsOperating && p.MedicalCoverage == 0, "unpaid clinic suspends medical service");
            rm.Add(AntColony.Data.ResourceType.MedicineBasic, 5); p.PayCivicUpkeep();
            Check(p.ClinicsOperating && p.S.clinicMedicineDebt == 0, "payment resumes service without double charge");
            school.gameObject.SetActive(false); clinic.gameObject.SetActive(false);

            p = Reset(100, 20, 10); p.S.old = 30; p.S.young = 10;
            Check(p.Taxpayers == 140, "mobilized excluded and elders included");
            var expected = p.MonthlyTax / GameCalendar.SecondsPerMonth;
            p.Tick(40); var actual = p.S.taxFood + p.S.taxSoil;
            Check(Near(actual, expected * 40), "time integrated production before pool change");
            Pool(80, 40, 10); expected = p.MonthlyTax / GameCalendar.SecondsPerMonth;
            p.Tick(40); Check(Near(p.S.taxFood + p.S.taxSoil, actual + expected * 40), "mobilization only affects future tax");
            Check(FinalBattleEconomy.DefenseSeconds == 2250 && FinalBattleEconomy.DefenseDays == 15
                && FinalBattleEconomy.RecommendedFood(4) == 80, "fifteen days and twenty-day reserve");
            Check(FinalBattleEconomy.ReserveLines().Any(l => l.Contains("연료·탄약 필요량") && l.Contains("연결 대기")), "missing rocket demand clearly disclosed");
            p.S.taxWeek = p.S.taxFood = p.S.taxSoil = 0;
            var resources = (AntColony.Data.ResourceType[])Enum.GetValues(typeof(AntColony.Data.ResourceType));
            var beforeTax = resources.Sum(rm.GetAmount);
            var fifteenDayTax = p.MonthlyTax * FinalBattleEconomy.DefenseSeconds / GameCalendar.SecondsPerMonth;
            for (var day = 0; day < FinalBattleEconomy.DefenseDays; day++)
            {
                p.Tick(GameCalendar.SecondsPerDay);
                Check(p.S.taxWeek == 0 && p.S.taxFood < 1 && p.S.taxSoil < 1, "daily tax settlement " + day);
            }
            Check(Mathf.Abs(resources.Sum(rm.GetAmount) - beforeTax + p.S.taxFood + p.S.taxSoil - fifteenDayTax) < .02f, "fifteen settlements conserve integrated production");

            p = Reset(80); var hut = Building<Housing>("B8 hut", BuildingKind.Hut, pile.Position + Vector3.forward * 10);
            p.SyncCivilianLocations(); var home = p.CivilianLocations.First(l => l.housing && l.count > 0);
            var total = p.Total;
            Check(p.DamageCivilians(home.id, 100) == 0 && p.Total == total, "protected home immune to civilian damage");
            hut.gameObject.SetActive(false); p.SyncCivilianLocations();
            Check(p.Total == total && !home.sheltered, "lost housing protection alone causes no death");
            Check(p.DamageCivilians(home.id, 9) == 0 && home.damage == 9, "sublethal damage retained by location");
            save = p.Capture(); p.Restore(save); home = p.S.civilianLocations.Find(l => l.id == home.id);
            Check(p.DamageCivilians(home.id, 1) == 1 && p.Total == total - 1 && home.damage == 0, "damage remainder survives JSON restore");
            Check(p.DamageCivilians(home.id, 20) == 2, "twenty damage kills two");
            Check(Corpse.All.Any(c => c.Available && c.Data.name == "시민" && c.Data.count == 3
                && (c.Position - home.position).sqrMagnitude < 1), "same-location deaths share counted remains");
            var first = p.S.civilianLocations[0];
            Check(p.DamageCivilians(first.id, float.NaN) == 0 && p.DamageCivilians(first.id, float.PositiveInfinity) == 0, "nonfinite damage ignored");
            // 자체 NavMesh 패치로 이동 검사가 맵의 우연한 지형/주거 배치에 의존하지 않게 한다.
            var settingsNav = NavMesh.GetSettingsByIndex(0);
            var origin = new Vector3(5000, 0, 5000);
            var source = new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box, transform = Matrix4x4.TRS(origin, Quaternion.identity, Vector3.one), size = new Vector3(100, 1, 100), area = 0 };
            var navData = NavMeshBuilder.BuildNavMeshData(settingsNav, new List<NavMeshBuildSource> { source }, new Bounds(origin, new Vector3(110, 20, 110)), Vector3.zero, Quaternion.identity);
            Check(navData != null, "own evacuation navigation");
            var navInstance = NavMesh.AddNavMeshData(navData);
            try
            {
                p = Reset(120); p.SyncCivilianLocations(); var open = p.S.civilianLocations.First(l => l.count > 0);
                open.position = origin + Vector3.up;
                var groupId = p.BeginCivilianEvacuation(open.id, origin + Vector3.right * 30 + Vector3.up, 100);
                var group = p.S.civilianLocations.Find(l => l.id == groupId);
                Check(group != null && group.count == 50 && group.moving, "evacuation group cap fifty");
                Check(p.TakeArrivedCivilians(groupId, 50) == 0, "moving group cannot board");
                var start = group.position; p.S.taxRate = 0; p.Tick(1);
                Check(Vector3.Distance(start, group.position) > 0 && !group.arrived, "actual path movement before arrival");
                Check(p.DamageCivilians(groupId, 20) == 2 && group.count == 48, "moving group casualties reduce survivors");
                save = p.Capture(); p.Restore(save); group = p.S.civilianLocations.Find(l => l.id == groupId);
                Check(group.count == 48 && group.moving && group.position != start, "movement JSON persists without duplicate departure");
                p.Tick(20); Check(group.arrived && !group.moving, "path reconstructed to destination");
                var before = p.Total;
                Check(p.TakeArrivedCivilians(groupId, 10) == 10 && group.count == 38 && p.Total == before - 10, "only actual survivors consume seats");
                Check(p.TakeArrivedCivilians(groupId, 100) == 38 && p.TakeArrivedCivilians(groupId, 100) == 0, "no repeated boarding reward");
                Check(p.CivilianLocations.Sum(l => l.count) == p.LocatedCivilians, "group totals remain conserved");
                open = p.S.civilianLocations.Find(l => l.id == open.id);
                p.DamageCivilians(open.id, 9);
                var remainingDamage = open.damage;
                var splitId = p.BeginCivilianEvacuation(open.id, origin + Vector3.right * 30 + Vector3.up, 50);
                var split = p.S.civilianLocations.Find(l => l.id == splitId);
                Check(split != null && split.damage == 0 && open.damage == remainingDamage, "partial departure retains source damage");
                var lastId = p.BeginCivilianEvacuation(open.id, origin + Vector3.right * 30 + Vector3.up, 50);
                var last = p.S.civilianLocations.Find(l => l.id == lastId);
                Check(last != null && last.damage == 9 && open.damage == 0, "full departure transfers accumulated damage");
                save = p.Capture(); p.Restore(save);
                Check(p.DamageCivilians(lastId, 1) == 1, "departure and JSON restore cannot reset sublethal damage");
                var bad = p.Capture(); bad.civilianLocations[0].position.x = float.NaN;
                Check(!ColonyPopulation.ValidCivicState(bad), "invalid position rejected");
                bad = p.Capture(); bad.educationSeconds = float.PositiveInfinity;
                Check(!ColonyPopulation.ValidCivicState(bad), "invalid civic timer rejected");
            }
            finally { navInstance.Remove(); Object.Destroy(navData); }
            p.Restore(new ColonyPopulation.State { civilianLocations = null });
            Check(p.CivilianLocations != null && ColonyPopulation.ValidCivicState(p.S), "old v19 field defaults restored");
            return "PASS " + checks;
        }
        finally
        {
            foreach (var go in created) if (go != null) Object.DestroyImmediate(go);
            created.Clear(); SaveStorage.RootOverride = root; UserSettings.Apply(settings, false); UnityEngine.Random.state = random; Time.timeScale = 0;
        }
    }
}
