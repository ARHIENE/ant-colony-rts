using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using UnityEngine;

// Main은 네이티브 Unity 호출 없이 실행 가능. RunP2Scene은 격리된 Play 세션에서만 실행(새 게임으로 전환).
public static class Review1101P2Checks
{
    static int count;
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    static void Check(bool ok, string label) { if (!ok) throw new Exception("P2 실패: " + label); count++; }
    static T Managed<T>() => (T)FormatterServices.GetUninitializedObject(typeof(T));
    static void Set(object target, Type type, string field, object value) => type.GetField(field, Any).SetValue(target, value);
    static object Get(object target, Type type, string field) => type.GetField(field, Any).GetValue(target);

    public static async Task<string> Main()
    {
        count = 0;
        var catalog = typeof(AntColony.Save.SaveCatalog);
        var buildings = catalog.GetField("Buildings", Any); var previous = buildings.GetValue(null);
        try
        {
            var building = Managed<AntColony.Buildings.BuildingBase>();
            buildings.SetValue(null, new[] { building });
            catalog.GetMethod("RemoveBuilding", Any).Invoke(null, new object[] { building });
            Check(((AntColony.Buildings.BuildingBase[])buildings.GetValue(null))[0] is null, "지연 Destroy 전 첫 슬롯 tombstone");
            catalog.GetMethod("RemoveBuilding", Any).Invoke(null, new object[] { building });
            Check(((AntColony.Buildings.BuildingBase[])buildings.GetValue(null)).Length == 1, "제거 중복 호출로 키 재번호 없음");
        }
        finally { buildings.SetValue(null, previous); }
        var unit = Managed<AntColony.World.EnemyCommander>();
        Set(unit, typeof(AntColony.World.WildMonster), "<GuardBoss>k__BackingField", -1);
        var talents = new AntColony.Units.CommanderTalents(); Set(unit, typeof(AntColony.World.EnemyCommander), "talents", talents);
        var restore = typeof(AntColony.World.WildMonster).GetMethod("RestoreAIMilitary", Any);
        for (int era = 1; era <= 6; era++)
        {
            Check(AntColony.World.AIEraMilitary.DefenseArmor(era) == era - 1, "장갑 완만한 성장 " + era);
            double damage = Math.Max(1, 8 - AntColony.World.AIEraMilitary.DefenseArmor(era));
            Check(Math.Ceiling(300 / damage) <= 100, "공격8 단일 건물 100타 이하 " + era);
            Check(AntColony.World.AIEraMilitary.FacilityArmor(era, false) <= AntColony.World.AIEraMilitary.FacilityArmor(era, true), "지휘소 타격 가치 " + era);
            for (int slot = 0; slot < 5; slot++)
                Check(AntColony.World.AIIndustry.FacilityName(era, slot).Length > 3, "시대·산업 역할 이름");
            for (int member = 0; member < 4; member++)
            {
                restore.Invoke(unit, new object[] { era, member, 220f });
                Check(unit.CurrentHealth == 220 && (float)Get(unit, typeof(AntColony.World.WildMonster), "maxHealth") >= 220, "고체력 구저장 보존·재저장 가능");
                Check(unit.MilitaryEra == era && unit.MilitaryMember == member && unit.MilitaryEquipment.IsValid
                    && unit.MilitaryEquipment.weapon == AntColony.World.AIEraMilitary.Weapon(era, member), "시대·장비 실물 일치");
                Check(ReferenceEquals(talents, unit.Talents), "장비 이관 시 재능 유지");
                Check(AntColony.World.AIEraProgress.CombatEra(unit) == era, "보급 전 개체 시대 전력 사용");
            }
        }
        Check(!AntColony.World.WildMonster.WantsMilitarySpacing(1, 12, 1), "초기 시대 후퇴 없음");
        Check(!AntColony.World.WildMonster.WantsMilitarySpacing(6, 1.5f, 1), "방패 전선 유지");
        Check(AntColony.World.WildMonster.WantsMilitarySpacing(2, 12, 2), "단궁 근접 압박 시 거리 확보");
        Check(!AntColony.World.WildMonster.WantsMilitarySpacing(6, 6, 4), "사거리 내 불필요한 후퇴 없음");
        var colony = Managed<AntColony.World.EnemyColony>();
        Set(colony, typeof(UnityEngine.Object), "m_CachedPtr", new IntPtr(1));
        var facilities = Enumerable.Range(0, 5).Select(_ => Managed<AntColony.Buildings.BuildingBase>()).ToArray();
        foreach (var b in facilities) { Set(b, typeof(UnityEngine.Object), "m_CachedPtr", new IntPtr(1)); Set(b, typeof(AntColony.Buildings.BuildingBase), "currentHealth", 100f); }
        Set(colony, typeof(AntColony.World.EnemyColony), "buildings", facilities);
        for (int era = 1; era <= 6; era++)
        {
            Check(AntColony.World.AIIndustry.Income(colony, era) == (6 * era, 4 * era), "실제 산업 소득 " + era);
            Set(facilities[0], typeof(AntColony.Buildings.BuildingBase), "currentHealth", 0f);
            Check(AntColony.World.AIIndustry.Income(colony, era) == (0, 3 * era), "본부 파괴로 식량 생산 중단");
            Set(facilities[0], typeof(AntColony.Buildings.BuildingBase), "currentHealth", 100f);
            Set(facilities[3], typeof(AntColony.Buildings.BuildingBase), "currentHealth", 0f);
            Check(AntColony.World.AIIndustry.CanResearch(colony, era) == (era <= 2), "후기 연구소 파괴·초기 본부 연구");
            Set(facilities[3], typeof(AntColony.Buildings.BuildingBase), "currentHealth", 100f);
        }
        var job = new AntColony.Buildings.Workshop.Job { bossJob = true, work = 23, materialsPaid = true, makerId = "old" };
        var copy = job.Copy(); copy.makerId = "next";
        Check(job.makerId == "old" && copy.work == 23 && copy.materialsPaid, "작업물 복사·결제 보존");
        foreach (bool labLost in new[] { false, true })
        {
            var civ = new AntColony.World.Civilization();
            for (int second = 1; second <= 21600; second++)
            {
                if (second % 60 == 0) { civ.resources[0] += 3 * civ.era; civ.resources[1] += 4 * civ.era; }
                AntColony.World.AIEraProgress.Tick(civ, 1, labLost && civ.era >= 3 ? 0 : 5);
            }
            Check(civ.era == (labLost ? 3 : 6) && AntColony.World.AIEraProgress.Valid(civ), "6시간 연구·산업 정체/도달 시뮬레이션");
            Check(civ.resources[0] >= 0 && civ.resources[1] >= 0, "장기 연구 잔고 비음수");
        }
        var militaryValidation = typeof(AntColony.Save.SavePreflight).GetMethod("Military", Any);
        bool Valid(AntColony.Save.MonsterDto dto, params string[] ids)
        {
            try { militaryValidation.Invoke(null, new object[] { dto, new HashSet<string>(ids) }); return true; }
            catch (TargetInvocationException e) when (e.InnerException is FormatException) { return false; }
        }
        Check(Valid(new AntColony.Save.MonsterDto()), "구저장 군사 기본값 허용");
        var saved = new AntColony.Save.MonsterDto { militaryVersion = 1, militaryEra = 4, militaryMember = 0, hasRuntime = true, health = 37,
            militaryEquipment = new AntColony.Units.EquipmentItem { slot = AntColony.Units.EquipmentSlot.Weapon, weapon = AntColony.Units.WeaponKind.Mandible } };
        Check(Valid(saved), "현재 군사 장비 유효성");
        Check(!Valid(saved, saved.militaryEquipment.id), "인벤토리·전리품과 군사 장비 중복 소유 거부");
        saved.militaryVersion = 2; Check(!Valid(saved), "알 수 없는 군사 버전 거부"); saved.militaryVersion = 1;
        saved.militaryEra = 0; Check(!Valid(saved), "잘못된 시대 거부"); saved.militaryEra = 4;
        saved.militaryMember = -1; Check(!Valid(saved), "잘못된 순번 거부"); saved.militaryMember = 0;
        saved.health = 0; Check(!Valid(saved), "죽은 병력과 드롭 장비 이중 소유 거부"); saved.health = 37;
        saved.militaryEquipment.weapon = AntColony.Units.WeaponKind.ShortBow; Check(!Valid(saved), "전투 프로필과 실물 장비 불일치 거부");
        await Task.CompletedTask;
        return "P2 오프라인 " + count + "검사 통과";
    }

    static async Task Wait(Func<bool> condition)
    {
        var end = DateTime.UtcNow.AddSeconds(120);
        while (!condition() && DateTime.UtcNow < end) await Task.Delay(80);
        Check(condition(), "장면 전환 완료");
    }
    public static async Task<string> RunP2Scene()
    {
        if (!Application.isPlaying) throw new Exception("격리된 Play에서 실행하세요. 현재 게임을 새 게임으로 교체합니다.");
        count = 0; var root = AntColony.Save.SaveStorage.RootOverride;
        var settings = AntColony.Core.UserSettings.Current.Clone(); var scale = Time.timeScale;
        AntColony.Save.SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "P2-" + Guid.NewGuid().ToString("N"));
        try
        {
            var quiet = settings.Clone(); quiet.autoSaveEnabled = false; AntColony.Core.UserSettings.Apply(quiet, false);
            await Wait(() => !AntColony.Save.SaveSystem.Busy);
            AntColony.Save.SaveSystem.NewGame(new AntColony.Core.NewGameOptions { seed = 311033, mapSize = AntColony.Core.MapSize.Small, startingCount = 2 });
            await Wait(() => !AntColony.Save.SaveSystem.Busy);
            Time.timeScale = 0; // 추가 Delay 없이 첫 준비 완료 시점에 저장한다.
            var file = AntColony.Save.SaveSnapshot.Capture();
            Check(file.buildings.Where(b => !b.runtimeBuilt).All(b => b.kind == "Destroyed"), "새 게임 첫 프레임 삭제 슬롯 저장");
            Check(AntColony.Save.SaveSystem.TrySave(false, 0, out var firstError), "추가 대기 없는 첫 저장 " + firstError);
            var roster = AntColony.Core.CommanderRoster.Instance.Commanders.ToArray();
            var bad = JsonUtility.FromJson<AntColony.Save.SaveFileV1>(JsonUtility.ToJson(file));
            var slot = bad.buildings.First(b => !b.runtimeBuilt); slot.kind = "QueenChamber"; slot.health = 100;
            var routine = (IEnumerator)typeof(AntColony.Save.SaveSnapshot).GetMethod("Restore", Any).Invoke(null, new object[] { bad });
            bool refused = false;
            try { routine.MoveNext(); } catch (InvalidOperationException e) { refused = e.Message.Contains("복원 전 시작 시설 누락"); }
            Check(refused && AntColony.Core.CommanderRoster.Instance.Commanders.SequenceEqual(roster), "누락 시설은 장수 제거 전 거부");
            var benchObject = new GameObject("P2 boss handoff check");
            var bench = benchObject.AddComponent<AntColony.Buildings.Workshop>();
            var owner = roster[0]; var next = roster[1];
            try
            {
                var job = new AntColony.Buildings.Workshop.Job { bossJob = true, boss = 1, makerId = owner.PersonalState.id, work = 23, materialsPaid = true };
                Set(bench, typeof(AntColony.Buildings.Workshop), "jobs", new List<AntColony.Buildings.Workshop.Job> { job });
                Check(!bench.TryTakeOverBossJob(next), "살아 있는 일시 부재 제작자 인계 금지");
                owner.PersonalState.dead = true;
                Check(bench.TryTakeOverBossJob(next) && job.makerId == next.PersonalState.id && job.work == 23 && job.materialsPaid, "사망 뒤 수동 인계·진행·결제 보존");
                Check(!bench.TryTakeOverBossJob(owner), "죽은 장수에게 인계 금지");
            }
            finally { owner.PersonalState.dead = false; benchObject.SetActive(false); UnityEngine.Object.Destroy(benchObject); }
            var world = AntColony.World.WorldMapManager.Instance; var diplomacy = AntColony.World.DiplomacyManager.Instance;
            var site = world.Sites.First(s => diplomacy.Faction(s) != null && s.Colony != null);
            var faction = diplomacy.Faction(site); faction.era = 4; faction.eraWork = 0; faction.eraPaid = false;
            AntColony.World.AIEraMilitary.UpgradeGuards(site, faction);
            AntColony.World.AIIndustry.Refresh(site.Colony, 4);
            Check(site.Colony.GetComponentsInChildren<AntColony.World.AIIndustry>(true).Length > 0, "실제 건물에 산업 역할 연결");
            var colonyBuildings = typeof(AntColony.World.EnemyColony).GetProperty("Buildings", Any);
            var expansion = typeof(AntColony.World.EnemyColony).GetMethod("RestoreExpansion", Any);
            while (((AntColony.Buildings.BuildingBase[])colonyBuildings.GetValue(site.Colony)).Length < 5)
                Check(expansion.Invoke(site.Colony, new object[] { site.transform.position + Vector3.right * 8 }) != null, "산업 슬롯 검사 준비");
            await Task.Delay(100); // 복제 시 제거 예약된 전리품 노드만 정리한다. 첫 저장 재현 대기는 아님.
            site.Colony.AddResources(2000, 2000); faction.equipment.Clear();
            AntColony.World.AIIndustry.Produce(site.Colony, faction);
            Check(faction.equipment.Count == 1 && faction.equipment[0].quality == 1, "현장 재료 소비·4시대 실물 제작");
            var factory = ((AntColony.Buildings.BuildingBase[])colonyBuildings.GetValue(site.Colony))[2];
            float hp = factory.CurrentHealth;
            Set(factory, typeof(AntColony.Buildings.BuildingBase), "currentHealth", 0f);
            AntColony.World.AIIndustry.Produce(site.Colony, faction);
            Check(faction.equipment.Count == 1, "공방 파괴 시 장비 생산 정지");
            Set(factory, typeof(AntColony.Buildings.BuildingBase), "currentHealth", hp);
            Check(AntColony.Save.SaveSystem.TrySave(false, 0, out var error), "첫 저장 " + error);
            var path = AntColony.Save.SaveSlots.PathFor(false, 0);
            Check(AntColony.Save.SaveSystem.TryLoad(path, out error), "전체 불러오기 시작 " + error);
            await Wait(() => !AntColony.Save.SaveSystem.Busy);
            Time.timeScale = 0;
            var after = AntColony.Save.SaveSnapshot.Capture();
            Check(AntColony.Save.SavePreflight.ForScene(after, out error), "재저장 검증 " + error);
            Check(after.monsters.Any(m => m.militaryVersion == 1 && m.militaryEra == 4 && m.militaryEquipment != null), "시대·실물 장비 저장 왕복");
            var legacy = JsonUtility.FromJson<AntColony.Save.SaveFileV1>(JsonUtility.ToJson(after));
            foreach (var m in legacy.monsters) { m.militaryVersion = 0; m.militaryEra = 0; m.militaryMember = 0; m.militaryEquipment = null; }
            var optional = typeof(AntColony.Save.SaveSnapshot).Assembly.GetType("AntColony.Save.SaveOptionalStates");
            var legacyJson = (string)optional.GetMethod("ToJson", Any).Invoke(null, new object[] { legacy, false });
            AntColony.Save.SaveStorage.WriteAtomic(path, legacyJson);
            Check(AntColony.Save.SaveSystem.TryLoad(path, out error), "구장비 필드 누락 저장 불러오기 " + error);
            await Wait(() => !AntColony.Save.SaveSystem.Busy); Time.timeScale = 0;
            var migrated = AntColony.Save.SaveSnapshot.Capture();
            Check(migrated.monsters.Any(m => m.militaryVersion == 1 && m.militaryEra == 4 && m.militaryEquipment != null), "구저장 시대4 장비 1회 소급");
            Check(AntColony.Save.SavePreflight.ForScene(migrated, out error), "마이그레이션 후 재저장 " + error);
            return "P2 장면 " + count + "검사 통과; 별도 실전·화면 확인 필요";
        }
        finally { AntColony.Core.UserSettings.Apply(settings, false); AntColony.Save.SaveStorage.RootOverride = root; Time.timeScale = scale; }
    }
}
