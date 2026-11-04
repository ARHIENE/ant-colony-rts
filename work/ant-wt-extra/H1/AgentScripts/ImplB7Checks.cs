using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Save;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using Object = UnityEngine.Object;

public static class ImplB7Checks
{
    static int count;
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    static void Check(bool yes, string label) { if (!yes) throw new Exception("B7 FAIL " + count + ": " + label); count++; }
    static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, Hidden).Invoke(target, args);
    static T Copy<T>(T value) => JsonUtility.FromJson<T>(JsonUtility.ToJson(value));
    static void Near(float a, float b, string label) => Check(Mathf.Abs(a - b) < .002f, label);
    static async Task Ready()
    {
        var end = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(50);
        Check(!SaveSystem.Busy, "scene ready"); Time.timeScale = 0;
    }

    public static async Task<string> Main()
    {
        count = 0; Check(Application.isPlaying, "Play mode required");
        var oldRoot = SaveStorage.RootOverride; var oldSettings = UserSettings.Current.Clone();
        var settings = oldSettings.Clone(); settings.autoSaveEnabled = false; UserSettings.Apply(settings, false);
        var oldScale = Time.timeScale; var random = UnityEngine.Random.state; var oldRoll = MedicalRules.Roll;
        var oldSeeds = FarmSeeds.Capture(); var oldMemorial = Memorial.Capture(); var objects = new List<Object>();
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "ImplB7-" + Guid.NewGuid().ToString("N"));
        GameObject Make(string name) { var go = new GameObject(name); objects.Add(go); return go; }
        try
        {
            await Ready();
            var options = new NewGameOptions { startingCount = 2, seed = 261024, mapSize = MapSize.Small,
                biome = MapBiome.Forest, startingTeam = new[] { StartingCandidate.Roll(0), StartingCandidate.Roll(1) } };
            foreach (var candidate in options.startingTeam) candidate.traits.values.Clear();
            CrashLandingStart.Begin(options); await Ready();
            var roster = CommanderRoster.Instance.Commanders.ToArray(); Check(roster.Length == 2, "crash start two commanders");
            var c = roster[0]; var other = roster[1]; c.CommandStop(); other.CommandStop();
            c.PersonalState.mentalBreak = MentalBreak.None; c.PersonalState.rageRemaining = 0;
            c.PersonalState.sleep.asleep = false; c.PersonalState.health = new HealthState { migrated = true };
            c.PersonalState.work = new CommanderWorkState(); c.PersonalState.ageMonths = CommanderAnt.AdultAgeMonths; c.PersonalState.ageScaleVersion = 1;
            c.Traits.values.Clear();

            var site = Make("B7 construction").AddComponent<BuildingConstructionSite>();
            site.Initialize(null, 10); site.Require(AntColony.Data.ResourceType.Soil, 5);
            site.Needs[0].reserved = 3; site.Needs[0].delivered = 2;
            typeof(WorkerAnt).GetField("targetConstruction", Hidden).SetValue(c, site);
            typeof(WorkerAnt).GetMethod("SuspendWork", Hidden).Invoke(c, null);
            Check(site.Needs[0].reserved == 0 && site.Needs[0].delivered == 2, "G13 mobilization releases only unused reservation");
            site.Needs[0].reserved = 3; site.enabled = false;
            Check(site.Needs[0].reserved == 0 && site.Needs[0].delivered == 2, "G13 disabled destination releases reservations and keeps delivered goods");

            var skills = new CommanderTalents { potential = 200 };
            skills.levels[(int)CommanderActivity.Melee] = 12;
            skills.Train(CommanderActivity.Firefighting, 599, _ => 0);
            Near(skills.Value((int)CommanderActivity.Melee), 12, "G25 no decay before sample");
            skills.Train(CommanderActivity.Firefighting, 1, _ => 0);
            Near(skills.Value((int)CommanderActivity.Melee), 12, "G25 sample threshold does not retroactively decay");
            skills.Train(CommanderActivity.Firefighting, 600, _ => 0);
            Near(skills.Value((int)CommanderActivity.Melee), 11.9f, "G25 max .1 per 600 effective seconds");
            skills.Train(CommanderActivity.Firefighting, 20000, _ => 0);
            Near(skills.Value((int)CommanderActivity.Melee), 10, "G25 floor ten");
            Near(skills.recentWork.Sum(u => u.seconds), 1800, "G25 bounded work window");
            skills.levels[(int)CommanderActivity.Strength] = 12;
            skills.Train(CommanderActivity.Gathering, 1800, _ => 0);
            Near(skills.Value((int)CommanderActivity.Strength), 12, "G25 auxiliary use maintains skill");
            Check(Copy(skills).Validate(), "G25 history JSON valid");
            Near(Copy(skills).UsageShare(CommanderActivity.Strength), .2f, "G25 auxiliary history preserved");

            var age = new CommanderPersonalState { ageMonths = 12 };
            typeof(CommanderAnt).GetMethod("MigrateAge", Hidden).Invoke(null, new object[] { age });
            Near(age.ageMonths, 216, "G37 old one year maps to eighteen");
            age = new CommanderPersonalState { ageMonths = 96 };
            typeof(CommanderAnt).GetMethod("MigrateAge", Hidden).Invoke(null, new object[] { age });
            Near(age.ageMonths, 780, "G37 old eight years maps to sixty five");
            Near(CommanderAnt.AgingChance(64), 0, "G37 no aging below sixty five");
            Near(CommanderAnt.AgingChance(65), .02f, "G37 two percent at sixty five");
            Near(CommanderAnt.AgingChance(80), .05f, "G37 per year increment");
            c.SetBorn(); MedicalRules.Roll = () => 1;
            Call(c, "TickAge", GameCalendar.SecondsPerYear);
            Near(c.AgeMonths, CommanderAnt.AdultAgeMonths, "G37 child matures in one calendar year");
            c.PersonalState.ageMonths = CommanderAnt.ExpectedAgeMonths;
            Call(c, "TickAge", 1f); Check(!c.IsDead, "G37 eighty is not death deadline");
            c.PersonalState.ageMonths = CommanderAnt.AdultAgeMonths;

            c.PersonalState.moodFactors.Clear(); c.PersonalState.deathEvents.Clear(); c.PersonalState.relations.Clear();
            var beforeMood = c.Mood;
            Check((bool)Call(c, "RecordDeathMood", "b7-colleague", "colleague"), "G64 first event");
            Check(!(bool)Call(c, "RecordDeathMood", "b7-colleague", "colleague"), "G64 duplicate event rejected");
            c.PersonalState.Relation("b7-friend").value = SocialRules.Friend;
            Call(c, "RecordDeathMood", "b7-friend", "friend");
            c.PersonalState.Relation("b7-spouse").spouse = true;
            Call(c, "RecordDeathMood", "b7-spouse", "spouse");
            Near(c.PersonalState.moodFactors.Single(f => f.reason == "DeathEvent:b7-colleague").value, -10, "G64 colleague");
            Near(c.PersonalState.moodFactors.Single(f => f.reason == "DeathEvent:b7-friend").value, -15, "G64 friend");
            Near(c.PersonalState.moodFactors.Single(f => f.reason == "DeathEvent:b7-spouse").value, -30, "G64 spouse only one tier");
            Near(beforeMood - c.Mood, 40, "G64 combined floor minus forty");
            var personal = c.CapturePersonalState(); c.RestorePersonalState(personal);
            Check(!(bool)Call(c, "RecordDeathMood", "b7-colleague", "colleague"), "G64 ID survives personal roundtrip");

            var tomb = Make("B7 tomb").AddComponent<Tombstone>();
            tomb.transform.position = c.Position;
            tomb.RestoreState(new Tombstone.State { deceasedId = "b7-spouse", name = "spouse" });
            Check(Memorial.Comfort(c, tomb), "G64 first comfort");
            Check(!Memorial.Comfort(c, tomb), "G64 once per deceased per day");
            Near(c.PersonalState.moodFactors.Single(f => f.reason == "추모: b7-spouse").value, 3, "G64 comfort plus three");
            c.RestorePersonalState(c.CapturePersonalState());
            Check(!Memorial.Comfort(c, tomb), "G64 comfort day survives personal roundtrip");
            var body = Corpse.Spawn(new Corpse.State { kind = CorpseKind.EnemyCommander, commanderId = "b7-enemy", name = "enemy", remaining = 1 }, Make("B7 body visual"));
            objects.Add(body.gameObject); body.Tick(301);
            Check(body.Available && body.Remains && !body.Edible, "G63 enemy remains permanent and inedible");
            Check(body.Claim(c) && !body.Finish(c, false), "G63 direct cleaning cannot delete commander remains");
            body.Release(c);
            tomb.RestoreState(new Tombstone.State { deceasedId = "b7-enemy", name = "enemy" });
            Check(Tombstone.BodyOf("b7-enemy") == body, "G63 enemy body burial lookup");
            Call(tomb, "Inter", body);
            Check(tomb.Data2.body && tomb.Data2.enemyBody && tomb.RemovalBlock != "", "G63 enemy burial preserves kind and blocks demolition");
            var taken = tomb.TakeOut(); objects.Add(taken.gameObject);
            Check(taken.Remains && taken.Data.kind == CorpseKind.EnemyCommander, "G63 exhumation preserves enemy remains");
            Memorial.Tick(c, 1);
            var sight = c.PersonalState.moodFactors.Single(f => f.reason == "미안치 시신 목격");
            Near(sight.value, -3, "G64 remains sight penalty");
            sight.remaining = 1; Memorial.Tick(c, 1); Near(sight.remaining, 1, "G64 sight does not refresh existing day penalty");
            var executed = Corpse.Drop(tomb, CorpseKind.EnemyCommander, "B7 executed enemy"); objects.Add(executed.gameObject);
            Check(!string.IsNullOrEmpty(executed.Data.commanderId) && Memorial.Find(executed.Data.commanderId) != null, "G63 enemy body without actor identity can still be buried");
            var ordinary = Corpse.Spawn(new Corpse.State { kind = CorpseKind.Ant, name = "B7 ordinary" }, Make("B7 ordinary visual"));
            objects.Add(ordinary.gameObject); ordinary.Tick(301); Check(!ordinary.Available, "G63 ordinary bodies still expire");

            FarmSeeds.Restore(null);
            var source = Make("B7 wild crop").AddComponent<FarmSeedSource>(); source.Configure(FarmCrop.Fungus);
            Check(FarmSeeds.Collect(source) && !FarmSeeds.Collect(source), "G28 wild source yields exactly once");
            Check(FarmSeeds.Count(FarmCrop.Fungus) == 1, "G28 one seed per source");
            FarmSeeds.Restore(FarmSeeds.Capture()); Check(!FarmSeeds.Collect(source), "G28 source identity survives state roundtrip");
            FarmSeeds.Restore(null);
            var farmGo = Make("B7 farm"); farmGo.SetActive(false);
            farmGo.AddComponent<ResourceNode>(); farmGo.AddComponent<BuildingBase>();
            var farm = farmGo.AddComponent<FarmPlot>(); farm.Configure(FarmCrop.Fungus, false); farmGo.SetActive(true);
            farm.Tick(1); Check(farm.Data.seedPending && farm.Node.AmountRemaining == 0, "G28 missing seed blocks new plot");
            FarmSeeds.Add(FarmCrop.Fungus, 1); farm.Tick(1);
            Check(!farm.Data.seedPending && farm.Node.NeedsSowing && FarmSeeds.Count(FarmCrop.Fungus) == 0, "G28 initial sow spends exactly one");
            farm.Tick(1); Check(FarmSeeds.Count(FarmCrop.Fungus) == 0, "G28 no repeated seed charge");
            Call(farm, "OnHarvested"); Call(farm, "OnHarvested"); Check(FarmSeeds.Count(FarmCrop.Fungus) == 0, "G28 first two harvests no seed");
            Call(farm, "OnHarvested"); Check(FarmSeeds.Count(FarmCrop.Fungus) == 1, "G28 third harvest adds seed");
            farm.RestoreState(Copy(farm.Data));
            Check(farm.Data.harvests == 0, "G28 harvest cycle survives state roundtrip");
            farm.RestoreState(new FarmPlot.State { fallow = true, seedVersion = 1 }); farm.Replant(); farm.Tick(1);
            Check(FarmSeeds.Count(FarmCrop.Fungus) == 0 && farm.Node.NeedsSowing, "G28 replant spends seed");
            var seedState = FarmSeeds.Capture(); seedState.counts[0] = -1; Check(!FarmSeeds.Valid(seedState), "G28 rejects negative saved seed count");

            // 검사에 필요한 비축더미는 직접 만든다(불시착 시작에 존재한다고 가정하지 않는다).
            Make("B7 test stockpile").AddComponent<Stockpile>();
            var rm = ResourceManager.Instance; Call(rm, "RestoreState", 50, 100, 30, 200, 200, 100);
            var shop = Make("B7 workshop").AddComponent<Workshop>();
            var job = new Workshop.Job { recipe = EquipmentRecipe.Mandible, work = 1, makerId = c.PersonalState.id, materialsPaid = true };
            var shopState = new Workshop.State { materialVersion = 1, jobs = new List<Workshop.Job> { job } };
            Call(shop, "RestoreState", shopState, roster.ToList());
            Check(shop.CanContinue(c) && !shop.CanContinue(other), "G18 original maker only");
            shop.TakeDamage(float.MaxValue); Check(shop.Ruined && shop.Jobs.Count == 1, "G13/G18 broken tool retains unfinished item");
            var soil = rm.GetAmount(AntColony.Data.ResourceType.Soil); var special = rm.GetAmount(AntColony.Data.ResourceType.Special);
            Check(shop.Cancel(0), "G18 cancel ruined unfinished item");
            Check(rm.GetAmount(AntColony.Data.ResourceType.Soil) - soil == 28, "G18 general material seventy percent");
            Check(rm.GetAmount(AntColony.Data.ResourceType.Special) - special == 5, "G18 essential core fully returned");
            Check(!shop.Cancel(0), "G18 cancellation cannot refund twice");

            var definition = ScriptableObject.CreateInstance<AntColony.Data.BuildingData>(); objects.Add(definition);
            definition.kind = AntColony.Data.BuildingKind.Carpentry;
            var processor = Make("B7 processor").AddComponent<Processor>();
            typeof(BuildingBase).GetMethod("ConfigureRuntime", Hidden).Invoke(processor, new object[] { definition });
            Call(processor, "RestoreState", new Processor.State { current = 0, progress = 1, makerId = c.PersonalState.id });
            Check(processor.CanContinue(c) && !processor.CanContinue(other), "G18 processor original maker only");
            processor.TakeDamage(float.MaxValue);
            Check(processor.IsDead && processor.Current != null && !processor.enabled, "G13/G18 ruined processor keeps unfinished batch");
            var batch = (Processor.State)Call(processor, "CaptureState");
            Call(processor, "RestoreState", Copy(batch));
            Check(!processor.enabled && !processor.NeedsWork && processor.Current != null, "G18 ruined processor roundtrip keeps batch inactive");
            processor.CancelCurrent(); Check(processor.Current == null, "G18 ruined processor cancellation available");

            c.CommandStop(); c.PersonalState.health = new HealthState { migrated = true }; c.PersonalState.work = new CommanderWorkState();
            c.PersonalState.meal.satiety = 100; c.PersonalState.treating = false;
            Call(c, "Mobilize", 2, c.Position);
            Check(c.IsDeployed && c.WorkState.rations.Total >= c.DeploymentPortion, "G61 daily rations allocated at mobilization");
            var food = c.WorkState.rations.Total; Check(c.OrderDeploymentMeal(), "G61 explicit meal order");
            Call(c, "TickDeploymentMeal", 2f); Check(c.DeploymentEating && c.WorkState.rations.Total == food, "G61 unconsumed until three seconds");
            c.CommandStop(); Check(!c.DeploymentEating && c.WorkState.rations.Total == food, "G61 stop preserves unused ration");
            c.OrderDeploymentMeal(); c.PersonalState.meal.satiety = 10; Call(c, "TickDeploymentMeal", 3f);
            Check(c.IsDeployed && !c.DeploymentEating && c.WorkState.rations.Total == food - c.DeploymentPortion && c.Satiety == 100, "G61 meal consumes once and preserves muster");
            var fatigue = c.Fatigue; Call(c, "TickDeploymentMeal", 1f); Check(c.Fatigue > fatigue, "G61 fatigue progresses during muster");
            c.PersonalState.rageRemaining = 130; Check(!c.OrderDeploymentMeal() && c.OrderRefusalReason.Contains("분노"), "G42 refusal precedes direct meal order");
            c.PersonalState.rageRemaining = 110; Call(c, "StartRevenge", true); Check(c.PersonalState.rageRemaining <= 120, "G42 additional rage bounded");
            c.PersonalState.rageRemaining = 0;
            Call(AntPool.Instance, "RestoreCounts", 0, 2, 0);
            Check(c.RecallTroops(1) && c.TroopCount == 1 && c.IsDeployed, "G61 partial recall preserves remaining muster");
            Call(c, "RestoreTroops", 1, .5f);
            Check(!c.RecallTroops(1) && c.TroopCount == 1, "G61 partial recall cannot heal the last wounded troop");
            Near(c.TroopHealth, .5f, "G61 partial troop damage preserved");
            Call(c, "FinishReturn");

            c.PersonalState.health = new HealthState { migrated = true }; c.WorkState.health = GameBalance.CommanderHealth;
            c.PersonalState.meal.satiety = 100;
            Check(c.CanSelfTend, "G55 conscious self treatment allowed");
            Check(!(bool)Call(c, "StartMed", MedJob.Surgery, c), "G55 self surgery forbidden");
            c.PersonalState.sleep.asleep = true; Check(!c.CanSelfTend, "G55 asleep cannot self treat"); c.PersonalState.sleep.asleep = false;
            c.Health.illnesses.Add(new Illness { kind = IllnessKind.Mold, severity = .1f }); Check(c.CanSelfTend, "G55 mold self medication permitted");
            rm.Add(AntColony.Data.ResourceType.MedicineBasic, 1); c.Health.medicine = MedicineLevel.Basic;
            var medicine = rm.GetAmount(AntColony.Data.ResourceType.MedicineBasic);
            Check((bool)Call(c, "StartMed", MedJob.Tend, c), "G55 mold self medication starts"); Call(c, "TickMed", 100f);
            Check(c.Health.illnesses[0].tendLeft > 0 && rm.GetAmount(AntColony.Data.ResourceType.MedicineBasic) == medicine - 1, "G55 mold medication consumes one dose");
            c.Health.illnesses.Clear(); c.Health.wounds.Add(new Wound { part = BodyPart.ArmL, kind = WoundKind.Cut, damage = 1 });
            c.Health.medicine = MedicineLevel.NoMedicine;
            Check((bool)Call(c, "StartMed", MedJob.Tend, c), "G55 no medicine self treatment starts");
            Call(c, "TickMed", 1f); Check(!c.Health.wounds[0].tended, "G11 self treatment requires doubled work");
            Call(c, "TickMed", 100f); Check(c.Health.wounds[0].tended, "G55 self treatment completes");
            var q = MedicalRules.TendQuality(5, -1, false, 1, false);
            Near(MedicalRules.TendQuality(5, -1, false, 1, true), q * .7f, "G11/G55 self quality seventy percent");
            c.PersonalState.health = new HealthState { migrated = true }; c.WorkState.health = 0; c.WorkState.unconsciousSeconds = 149; c.PersonalState.meal.satiety = 100;
            Check(!c.CanSelfTend, "G11 unconscious cannot self treat");
            Check(CommanderAnt.HasRecoveryOrRescueRoute(new[] { c }), "G11 recovery route inside survival window");
            c.TickDuty(1); Near(c.PersonalHealth, GameBalance.CommanderHealth * .2f, "G11 one day recovery restores twenty percent");
            // 의식 불명 포만도 감소 ×0.5: 포만도 100에서 쓰러져도 급식 없이 하루 뒤 깨어난다. 70 이하는 급식이 필요하다.
            c.WorkState.health = 0; c.WorkState.unconsciousSeconds = 0; c.PersonalState.meal.satiety = 100;
            Check(CommanderAnt.HasRecoveryOrRescueRoute(new[] { c }), "G11 full satiety survives a day unconscious");
            for (var t = 0; t <= GameCalendar.SecondsPerDay && c.PersonalHealth <= 0; t++) c.TickDuty(1);
            Check(c.PersonalHealth > 0 && c.Satiety > 0, "G11 full satiety recovers without feeding " + c.Satiety);
            c.WorkState.health = 0; c.WorkState.unconsciousSeconds = 0; c.PersonalState.meal.satiety = 69;
            Check(!CommanderAnt.HasRecoveryOrRescueRoute(new[] { c }), "G11 satiety 69 alone starves before waking");
            c.WorkState.health = 0; c.WorkState.unconsciousSeconds = 0; c.PersonalState.meal.satiety = 0;
            Check(!CommanderAnt.HasRecoveryOrRescueRoute(new[] { c }), "G11 no recovery or rescue when all unconscious and starving");
            Check(CommanderAnt.HasRecoveryOrRescueRoute(new[] { c, other }), "G11 conscious survivor prevents impossible rescue defeat");
            c.PersonalState.meal.satiety = 100;
            c.Health.wounds.Add(new Wound { part = BodyPart.LegL, kind = WoundKind.Missing, age = 100 });
            c.Health.wounds.Add(new Wound { part = BodyPart.LegR, kind = WoundKind.Missing, age = 100 });
            c.Health.wounds.Add(new Wound { part = BodyPart.ArmL, kind = WoundKind.Cut, damage = 1 });
            Check(c.CanSelfTend, "G55 conscious immobile patient can tend self");
            Check(!CommanderAnt.HasRecoveryOrRescueRoute(new[] { c }), "G11 self tending alone is not a viable recovery route");
            c.PersonalState.health = new HealthState { migrated = true };
            c.WorkState.unconsciousSeconds = GameCalendar.SecondsPerDay - 1;
            var unchanged = UnityEngine.Random.state; var healthBefore = JsonUtility.ToJson(c.Health);
            CommanderAnt.HasRecoveryOrRescueRoute(new[] { c });
            Check(healthBefore == JsonUtility.ToJson(c.Health) && JsonUtility.ToJson(unchanged) == JsonUtility.ToJson(UnityEngine.Random.state), "G11 prediction does not mutate health or consume random state");
            Check(Copy(c.WorkState).Valid, "B7 new work fields JSON valid");
            return "PASS " + count;
        }
        finally
        {
            foreach (var item in objects.AsEnumerable().Reverse()) if (item != null) Object.DestroyImmediate(item);
            MedicalRules.Roll = oldRoll; UnityEngine.Random.state = random; FarmSeeds.Restore(oldSeeds); Memorial.Restore(oldMemorial);
            SaveStorage.RootOverride = oldRoot; UserSettings.Apply(oldSettings, false); Time.timeScale = oldScale;
        }
    }
}
