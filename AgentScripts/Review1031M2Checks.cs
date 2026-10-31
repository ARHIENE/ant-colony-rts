using System;
using System.Collections.Generic;
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

public static class Review1031M2Checks
{
    private static int count;
    private static void Check(bool ok, string label) { if (!ok) throw new Exception("M2 실패: " + label); count++; }
    private static T Managed<T>() => (T)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(T));
    private static FieldInfo Field(Type type, string name) => type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
    private static object Get(object target, string name) => Field(typeof(WildMonster), name).GetValue(target);
    private static void Set(object target, string name, object value) => Field(target.GetType(), name).SetValue(target, value);
    private static FieldInfo Singleton(Type type) => type.GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);

    public static Task<string> Main()
    {
        count = 0;
        Military(); MilitaryRestore(); SiteMilitaryEra(); Rocket(); HousingAddress(); CraftPolicy();
        return Task.FromResult($"M2 오프라인 {count}검사 통과");
    }
    private static void Military()
    {
        var expected = new[] {
            new[] { WeaponKind.WoodenSword },
            new[] { WeaponKind.WoodenSword, WeaponKind.WoodenSword, WeaponKind.ShortBow },
            new[] { WeaponKind.Mandible, WeaponKind.Shield, WeaponKind.ShortBow },
            new[] { WeaponKind.Mandible, WeaponKind.Shield, WeaponKind.AcidSprayer },
            new[] { WeaponKind.Shield, WeaponKind.AcidSprayer, WeaponKind.AcidSprayer },
            new[] { WeaponKind.Shield, WeaponKind.AcidSprayer, WeaponKind.AcidSprayer, WeaponKind.AcidSprayer }
        };
        var configure = typeof(WildMonster).GetMethod("ConfigureAIMilitary", BindingFlags.NonPublic | BindingFlags.Instance);
        var signatures = new HashSet<string>();
        for (int era = 1; era <= 6; era++)
        {
            var weapons = expected[era - 1];
            Check(Enumerable.Range(0, weapons.Length * 2).All(i => AIEraMilitary.Weapon(era, i) == weapons[i % weapons.Length]), "시대별 편성 반복 " + era);
            signatures.Add(AIEraMilitary.Summary(era));
            Check(AIEraMilitary.DefenseGrade(era) == era && AIEraMilitary.DefenseArmor(era) == 2 * (era - 1), "시대별 시설 장갑 " + era);
            for (int member = 0; member < weapons.Length; member++)
            {
                var unit = Managed<EnemyCommander>();
                var talents = new CommanderTalents(); Set(unit, "talents", talents);
                Field(typeof(WildMonster), "currentHealth").SetValue(unit, 70f);
                configure.Invoke(unit, new object[] { era, member });
                var weapon = weapons[member];
                var ranged = weapon == WeaponKind.ShortBow || weapon == WeaponKind.AcidSprayer;
                Check((float)Get(unit, "attackRange") == (weapon == WeaponKind.ShortBow ? 12 : ranged ? 6 : 1.5f)
                    && unit.Roles.Contains(ranged ? UnitRole.Ranged : weapon == WeaponKind.Shield ? UnitRole.Defense : UnitRole.Melee), "실제 사거리·역할 " + era + "/" + member);
                Check((float)Get(unit, "maxHealth") == (weapon == WeaponKind.Shield ? 180 : 150) && unit.CurrentHealth == 70, "방패 체력·부상 보존");
                Check(ReferenceEquals(unit.Talents, talents) && (float)Get(unit, "attackInterval") == (weapon == WeaponKind.ShortBow ? 2 : weapon == WeaponKind.Shield ? 1.5f : 1.25f), "공격 주기·재능 보존");
                configure.Invoke(unit, new object[] { era, member });
                Check((float)Get(unit, "maxHealth") <= 180 && unit.CurrentHealth == 70, "승급 중복 누적·치유 없음");
            }
        }
        Check(signatures.Count == 6, "6시대 주력 구분");
        Check(AIEraMilitary.Weapon(0, -1) == WeaponKind.WoodenSword && AIEraMilitary.DefenseGrade(99) == 6, "표시 범위 보호");
    }
    private static void MilitaryRestore()
    {
        var restore = typeof(WildMonster).GetMethod("RestoreAIMilitary", BindingFlags.Instance | BindingFlags.NonPublic);
        var unit = Managed<EnemyCommander>();
        var talents = new CommanderTalents(); Set(unit, "talents", talents);
        for (int era = 1; era <= 6; era++)
            for (int member = 0; member < 4; member++)
                foreach (float health in new[] { 0f, 70f, 175f, 220f })
                {
                    // 1시대 템플릿으로 돌아간 상태를 현재 시대 편성으로 복구한다.
                    Field(typeof(WildMonster), "maxHealth").SetValue(unit, 150f);
                    restore.Invoke(unit, new object[] { era, member, health });
                    var weapon = AIEraMilitary.Weapon(era, member);
                    Check(unit.CurrentHealth == health && (float)Get(unit, "maxHealth") == AIEraMilitary.Health(weapon), "복원 체력 무회복·무절삭");
                    Check((float)Get(unit, "attackRange") == AIEraMilitary.Range(weapon)
                        && (float)Get(unit, "attackInterval") == AIEraMilitary.Interval(weapon)
                        && (float)Get(unit, "attackDamage") == (weapon == WeaponKind.Shield ? 6 : 8)
                        && unit.Roles.Contains(AIEraMilitary.Role(weapon)) && ReferenceEquals(unit.Talents, talents), "복원 현재 시대 수치·역할·재능");
                }
        var runtimeType = typeof(SaveSnapshot).Assembly.GetType("AntColony.Save.SaveRuntime");
        var members = runtimeType.GetMethod("MilitaryMembers", BindingFlags.Static | BindingFlags.NonPublic);
        MonsterDto Guard(int key, int number) => new MonsterDto { key = "monster:" + key, hasRuntime = true,
            runtime = new WildMonsterDto { parentSite = 0, commander = true, commanderName = "현장 Commander " + number } };
        MonsterDto Visitor(int key, string name, bool commander = false) => new MonsterDto { key = "runtime:" + key, hasRuntime = true,
            diplomaticFactionId = "a", runtime = new WildMonsterDto { visitor = true, name = name, commander = commander } };
        var guard2 = Guard(10, 2); var guard10 = Guard(2, 10);
        var invader = Guard(20, 1); invader.runtime.defenseSite = 0; invader.runtime.name = "현장 Invader 12";
        var old1 = Visitor(2, "구저장 침공군"); var old2 = Visitor(10, "구저장 침공군", true);
        var newWave = Visitor(11, "구저장 침공군");
        var survivor = Visitor(12, "세력 침공군 4", true);
        var boss = Guard(30, 1); boss.guardBoss = 0;
        var ally = Guard(31, 1); ally.runtime.allied = true;
        var rebel = Guard(32, 1); rebel.runtime.rebel = "반란";
        var wild = Visitor(33, "야생"); wild.runtime.visitor = false;
        var data = new[] { survivor, guard2, boss, old2, guard10, invader, old1, ally, rebel, wild, newWave };
        var result = ((IEnumerable<(MonsterDto monster, int member)>)members.Invoke(null, new object[] { data })).ToDictionary(x => x.monster, x => x.member);
        Check(result[guard2] == 1 && result[guard10] == 9 && result[invader] == 11, "수비·침공 생성 번호 유지·두 자리 번호");
        Check(result[old1] == 0 && result[old2] == 1 && result[newWave] == 0 && result[survivor] == 3, "방문대 숫자 키·파동·생존자 생성 번호");
        Check(result.Count == 7, "보스·동맹·반란·야생 제외");
    }
    private static void SiteMilitaryEra()
    {
        var site = Managed<ExpeditionSite>(); var map = Managed<WorldMapManager>(); var diplomacy = Managed<DiplomacyManager>();
        foreach (var obj in new UnityEngine.Object[] { site, map, diplomacy })
            Field(typeof(UnityEngine.Object), "m_CachedPtr").SetValue(obj, new IntPtr(1));
        Set(map, "sites", new List<ExpeditionSite> { site }); Set(diplomacy, "world", map);
        var faction = new Civilization { id = "era-test", era = 5 };
        Set(diplomacy, "<Data>k__BackingField", new DiplomacyManager.State { owners = new List<string> { faction.id }, civilizations = new List<Civilization> { faction } });
        var era = typeof(ExpeditionSite).GetProperty("MilitaryEra", BindingFlags.Instance | BindingFlags.NonPublic);
        var old = Singleton(typeof(DiplomacyManager)).GetValue(null);
        try
        {
            Singleton(typeof(DiplomacyManager)).SetValue(null, null);
            Check((int)era.GetValue(site) == 1, "수비 생성 외교 관리자 없음→1시대");
            Singleton(typeof(DiplomacyManager)).SetValue(null, diplomacy);
            Check((int)era.GetValue(site) == 5, "수비 생성 현장 세력 현재 시대");
            faction.era = 6;
            Check((int)era.GetValue(site) == 6, "수비 생성 승급 반영");
            diplomacy.Data.owners[0] = "없는 세력";
            Check((int)era.GetValue(site) == 1, "수비 생성 Faction 없음→1시대");
        }
        finally { Singleton(typeof(DiplomacyManager)).SetValue(null, old); }
    }
    private static void Rocket()
    {
        Check(AirshipYard.SeatOverflow(10, 4, 6) == 0 && AirshipYard.SeatOverflow(10, 4, 7) == 1, "정원 경계·탑승과 예약 합산");
        var roster = Managed<CommanderRoster>(); var pop = Managed<ColonyPopulation>(); var yard = Managed<AirshipYard>();
        var commander = Managed<CommanderAnt>();
        // 네이티브 오브젝트를 만들지 않고 Unity null 비교만 통과시킨다. 앱/Editor 호출 없음.
        Field(typeof(UnityEngine.Object), "m_CachedPtr").SetValue(commander, new IntPtr(1));
        Set(commander, "personalState", new CommanderPersonalState { id = "m2-reserved" }); Set(commander, "troopCount", 4);
        Set(roster, "commanders", new List<CommanderAnt> { commander });
        pop.S = new ColonyPopulation.State();
        var state = new AirshipYard.State { civilianPassengers = 2 };
        state.reservedCommanders.Add("m2-reserved");
        Set(yard, "state", state); Set(yard, "passengers", new List<CommanderAnt>());
        var oldRoster = Singleton(typeof(CommanderRoster)).GetValue(null); var oldPop = Singleton(typeof(ColonyPopulation)).GetValue(null);
        try
        {
            Singleton(typeof(CommanderRoster)).SetValue(null, roster); Singleton(typeof(ColonyPopulation)).SetValue(null, pop);
            Check(yard.ReservedSeats == 5 && yard.ReservationOverflow == 0 && yard.ReservationWarning == "", "예약 직후 경고 없음");
            Set(commander, "troopCount", yard.Capacity);
            Check(yard.ReservedSeats == yard.Capacity + 1 && yard.ReservationOverflow == 3 && yard.UnreservedSeats == 0, "예약 이후 실제 장수 증원 반영");
            Check(yard.ReservationWarning.Contains("3석 초과") && state.reservedCommanders.Count == 1 && commander.TroopCount == yard.Capacity, "초과량 표시·예약/병력 유지");
            state.cocoons = 1;
            Check(yard.ReservationOverflow == 0 && yard.ReservationWarning == "", "좌석 확장 후 경고 해제");
            state.cocoons = 0; yard.CancelReservation("m2-reserved");
            Check(yard.ReservationOverflow == 0 && commander.TroopCount == yard.Capacity, "수동 예약 취소·증원 유지");
        }
        finally { Singleton(typeof(CommanderRoster)).SetValue(null, oldRoster); Singleton(typeof(ColonyPopulation)).SetValue(null, oldPop); }
    }
    private static void HousingAddress()
    {
        var at = new Vector3(4, 0, 5);
        var resident = new CivilianLocation { id = 1, count = 10, housing = true, sheltered = true, position = at };
        var incoming = new CivilianLocation { id = 2, count = 5, moving = true, rehousing = true, housingDestination = at };
        var arrived = new CivilianLocation { id = 3, count = 3, arrived = true, rehousing = true, housingDestination = at };
        var other = new CivilianLocation { id = 4, count = 9, housing = true, sheltered = true, position = at + Vector3.right };
        var temporary = new CivilianLocation { id = 5, count = 2, temporary = true, sheltered = true, position = at };
        var state = new ColonyPopulation.State { nextCivilianLocation = 6, civilianLocations = new List<CivilianLocation> { resident, incoming, arrived, other, temporary } };
        ColonyPopulation.InvalidateHousing(state, at);
        Check(!resident.housing && !resident.sheltered && resident.count == 10, "파괴 순간 주민 노출·인원 보존");
        Check(!incoming.rehousing && incoming.moving && !arrived.rehousing && arrived.arrived, "이동/도착 입주 예약 무효화·이동 유지");
        Check(other.housing && other.sheltered && temporary.sheltered && temporary.temporary, "다른 집·임시 거처 보존");
        Check(!state.civilianLocations.Any(g => g.housing && (g.position - at).sqrMagnitude < .01f
            || g.rehousing && (g.housingDestination - at).sqrMagnitude < .01f), "같은 위치 신축 집의 기존 점유·예약 없음");
        ColonyPopulation.InvalidateHousing(state, at);
        Check(state.civilianLocations.Sum(g => g.count) == 29, "무효화 반복 인원 보존");
        var valid = typeof(ColonyPopulation).GetMethod("ValidCivilianState", BindingFlags.NonPublic | BindingFlags.Static);
        Check((bool)valid.Invoke(null, new object[] { state }), "무효화 결과 기존 저장 검증 통과");
        var legacy = new CivilianLocation { id = 1, count = 5, housing = true, sheltered = true, position = at };
        var file = new SaveFileV1(); file.population.civilianLocations.Add(legacy); file.population.nextCivilianLocation = 2;
        SaveL2.Normalize(file);
        Check(legacy.housing && legacy.sheltered && SaveL2.Valid(file), "구저장 주소 기본값 보존");
    }
    private static void CraftPolicy()
    {
        var bench = Managed<Workshop>();
        var job = new Workshop.Job { bossJob = true, boss = 1, work = 20, materialsPaid = true, makerId = "departed" };
        Set(bench, "jobs", new List<Workshop.Job> { job });
        var replacement = Managed<CommanderAnt>();
        Field(typeof(UnityEngine.Object), "m_CachedPtr").SetValue(replacement, new IntPtr(1));
        Set(replacement, "personalState", new CommanderPersonalState { id = "replacement" });
        Check(!bench.CanContinue(replacement), "원제작자 이탈 뒤 자동 인계 없음");
        replacement.PersonalState.id = "departed";
        Check(bench.CanContinue(replacement) && job.work == 20, "일시 이탈 원제작자 복귀 진행 보존");
        // 기존 원제작자 소유권은 Job 복사/저장으로 유지된다. 실제 환급은 L2 보스 제작 회귀 검사도 실행한다.
        Check(job.Copy().makerId == "departed" && job.Copy().work == 20 && job.Copy().materialsPaid, "사망/이탈 보스 주문 소유권·진행 보존");
        var progress = new AntColony.Boss.BossProgress.State { craftBoss = 1, craftRemaining = 40 };
        var boss = new AntColony.Boss.BossProgress(progress); boss.CancelCraft(); boss.CancelCraft();
        Check(progress.home[0] == 1 && progress.craftBoss == -1, "직접 취소 핵심물 1회 환급 유지");
    }
    // 실행 잔여: 격리된 시작 완료 Play 장면에서 통합 담당자가 호출한다. 이 작업에서는 실행하지 않는다.
    public static async Task<string> RunScene()
    {
        var result = await Main();
        if (ColonyPopulation.Instance == null || DiplomacyManager.Instance == null) throw new Exception("시작 완료 격리 장면 필요");
        var before = SaveSnapshot.Capture();
        Check(SaveValidator.TryParse(JsonUtility.ToJson(before), out var parsed, out var error), "장면 JSON " + error);
        Check(SavePreflight.Validate(parsed, out error), "장면 Preflight " + error);
        // JsonUtility는 null 런타임을 기본 객체로 되살리므로 런타임 보유 몬스터만 비교한다.
        Check(before.monsters.Where(m => m.hasRuntime).Select(m => (m.runtime.range, m.runtime.interval, m.runtime.maxHealth))
            .SequenceEqual(parsed.monsters.Where(m => m.hasRuntime).Select(m => (m.runtime.range, m.runtime.interval, m.runtime.maxHealth))), "편성 기존 런타임 저장 필드 보존");
        SceneMilitaryRestore();
        return result + "; 장면 JSON/Preflight·격리 몬스터 복원 통과. 실전투·실파괴·증원 UI·전체 복원은 별도 확인 필요.";
    }
    private static void SceneMilitaryRestore()
    {
        var assembly = typeof(SaveSnapshot).Assembly;
        var catalog = typeof(SaveCatalog).GetField("Monsters", BindingFlags.Static | BindingFlags.NonPublic);
        var oldMonsters = catalog.GetValue(null);
        var oldWorld = Singleton(typeof(WorldMapManager)).GetValue(null);
        var oldDiplomacy = Singleton(typeof(DiplomacyManager)).GetValue(null);
        var references = assembly.GetType("AntColony.Save.SaveReferences");
        var dictionaries = new[] { "objects", "keys" }.Select(n =>
            (System.Collections.IDictionary)references.GetField(n, BindingFlags.Static | BindingFlags.NonPublic).GetValue(null)).ToArray();
        var backups = dictionaries.Select(d => new System.Collections.Hashtable(d)).ToArray();
        var local = SceneObjects.Any<LocalIncursions>();
        var visitors = local == null ? null : (List<WildMonster>)Field(typeof(LocalIncursions), "visitors").GetValue(local);
        var oldVisitors = visitors?.ToArray();
        var root = new GameObject("M2 격리 복원 검사"); root.SetActive(false);
        try
        {
            var site = root.AddComponent<ExpeditionSite>();
            var world = Managed<WorldMapManager>(); var diplomacy = Managed<DiplomacyManager>();
            foreach (var obj in new UnityEngine.Object[] { world, diplomacy })
                Field(typeof(UnityEngine.Object), "m_CachedPtr").SetValue(obj, new IntPtr(1));
            Set(world, "sites", new List<ExpeditionSite> { site }); Set(diplomacy, "world", world);
            var faction = new Civilization { id = "m2-scene", era = 5 };
            Set(diplomacy, "<Data>k__BackingField", new DiplomacyManager.State { owners = new List<string> { faction.id }, civilizations = new List<Civilization> { faction } });
            Singleton(typeof(WorldMapManager)).SetValue(null, world); Singleton(typeof(DiplomacyManager)).SetValue(null, diplomacy);
            var file = new SaveFileV1(); file.world.sites.Add(new SiteDto());
            var units = new List<WildMonster>();
            for (int i = 0; i < 3; i++)
            {
                var go = new GameObject("복원 장수"); go.transform.SetParent(root.transform);
                go.AddComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;
                units.Add(go.AddComponent<EnemyCommander>());
                file.monsters.Add(new MonsterDto { key = "monster:" + i, hasRuntime = true, diplomaticFactionId = i == 0 ? null : faction.id,
                    health = i == 0 ? 175 : i == 1 ? 220 : 0,
                    runtime = new WildMonsterDto { parentSite = 0, commander = true, commanderName = "현장 Commander 1",
                        name = i == 1 ? "현장 Invader 2" : "세력 침공군 3", defenseSite = i == 1 ? 0 : -1, visitor = i == 2,
                        maxHealth = 150, range = 1.5f, interval = 1.25f, damage = 8, speed = 3, attackTimer = .75f } });
            }
            catalog.SetValue(null, units.ToArray());
            // 실제 기존 복원 경로를 실행해 RestoreRuntime 뒤 편성이 덮어써지는지 검사한다.
            assembly.GetType("AntColony.Save.SaveRuntime").GetMethod("RestoreMonsters", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { file });
            for (int i = 0; i < units.Count; i++)
            {
                var weapon = AIEraMilitary.Weapon(faction.era, i);
                Check(units[i].CurrentHealth == file.monsters[i].health && (float)Get(units[i], "maxHealth") == AIEraMilitary.Health(weapon), "장면 복원 저장 체력·시대 상한");
                Check((float)Get(units[i], "attackRange") == AIEraMilitary.Range(weapon)
                    && ((EnemyCommander)units[i]).Roles.Contains(AIEraMilitary.Role(weapon))
                    && (float)Get(units[i], "attackTimer") == .75f, "장면 복원 수비·침공·방문 역할/타이머");
            }
        }
        finally
        {
            catalog.SetValue(null, oldMonsters);
            Singleton(typeof(WorldMapManager)).SetValue(null, oldWorld); Singleton(typeof(DiplomacyManager)).SetValue(null, oldDiplomacy);
            for (int i = 0; i < dictionaries.Length; i++)
            {
                dictionaries[i].Clear();
                foreach (System.Collections.DictionaryEntry entry in backups[i]) dictionaries[i].Add(entry.Key, entry.Value);
            }
            if (visitors != null) { visitors.Clear(); visitors.AddRange(oldVisitors); }
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
