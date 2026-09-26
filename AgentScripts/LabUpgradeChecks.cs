namespace AntColony.Regression
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading.Tasks;
    using AntColony.Buildings;
    using AntColony.Core;
    using AntColony.Data;
    using AntColony.UI;
    using AntColony.Units;
    using UnityEngine;
    using Object = UnityEngine.Object;

    // 연구소 장수 개별 강화: 비용, 자원 부족, 소유권·중복 방지, 실제 보직 변경 유지, 즉시 취소, 파괴 후 재사용, 다중 연구소, HUD 실시간 선택.
    public static class LabUpgradeChecks
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        private const float ResearchSeconds = .3f;
        private static readonly List<string> Failures = new List<string>();
        private static int passed;

    // 새 Play 세션은 메인 메뉴(일시정지)로 시작하므로 필요하면 게임을 직접 시작한다.
    static async System.Threading.Tasks.Task StartGame()
    {
        if (AntColony.Core.GameSession.Instance.GameStarted) return;
        while (AntColony.Save.SaveSystem.Busy) await System.Threading.Tasks.Task.Delay(50);
        AntColony.Save.SaveSystem.NewGame(new AntColony.Core.NewGameOptions());
        while (AntColony.Save.SaveSystem.Busy) await System.Threading.Tasks.Task.Delay(50);
        AntColony.UI.GameMenuController.Instance.Resume(); UnityEngine.Time.timeScale = 1;
    }
    // 무기=역할 개편: 예전 보직 변경을 해당 무기(날개) 장착으로 대신한다.
    static bool Arm(AntColony.Units.CommanderAnt c, AntColony.Data.UnitRole role)
    {
        var inv = AntColony.Units.EquipmentInventory.Instance;
        var item = role == AntColony.Data.UnitRole.Flying
            ? new AntColony.Units.EquipmentItem { slot = AntColony.Units.EquipmentSlot.Armor, armor = AntColony.Units.ArmorKind.Wings, quality = 1 }
            : new AntColony.Units.EquipmentItem { slot = AntColony.Units.EquipmentSlot.Weapon, quality = 1,
                weapon = role == AntColony.Data.UnitRole.Ranged ? AntColony.Units.WeaponKind.AcidSprayer : role == AntColony.Data.UnitRole.Defense ? AntColony.Units.WeaponKind.Shield
                    : role == AntColony.Data.UnitRole.Support ? AntColony.Units.WeaponKind.Pheromone : AntColony.Units.WeaponKind.Mandible };
        if (inv.Full) inv.Items.RemoveAt(0);
        if (!inv.Add(item) || !inv.Equip(c, item)) return false;
        inv.Items.RemoveAll(e => e.slot == item.slot && e.quality == 1 && e != item);
        return role == AntColony.Data.UnitRole.Flying ? c.IsFlying : c.Role == (role == AntColony.Data.UnitRole.Worker ? AntColony.Data.UnitRole.Melee : role);
    }
        public static async Task<string> Main()
        {
            if (!Application.isPlaying) throw new Exception("Play mode required");
        await StartGame();
            Failures.Clear();
            passed = 0;
            var rm = ResourceManager.Instance;
            var template = Object.FindAnyObjectByType<CommanderAnt>();
            var created = new List<GameObject>();
            var selection = Object.FindAnyObjectByType<SelectionManager>();
            var selectedList = selection != null ? (List<SelectableObject>)typeof(SelectionManager)
                .GetField("selectedObjects", Flags).GetValue(selection) : null;
            var previousSelection = selectedList != null ? new List<SelectableObject>(selectedList) : null;
            // 식량 유지비가 자원 비교를 흔들지 않게 검사 중에만 끈다.
            var upkeep = Object.FindAnyObjectByType<UpkeepManager>();
            var upkeepWasEnabled = upkeep != null && upkeep.enabled;
            if (upkeep != null) upkeep.enabled = false;
            try
            {
                // 씬 연구소 템플릿 비용(읽기 전용 확인).
                var sceneLabs = 0;
                var sceneCostsOk = true;
                foreach (var lab in Resources.FindObjectsOfTypeAll<ResearchLab>())
                {
                    if (!lab.gameObject.scene.IsValid()) continue;
                    sceneLabs++;
                    sceneCostsOk &= lab.GetFoodCost(0) == 45 && lab.GetSoilCost(0) == 30 && lab.GetFoodCost(2) == 135 && lab.GetSoilCost(2) == 90;
                }
                Check(sceneLabs > 0 && sceneCostsOk, $"씬 연구소 {sceneLabs}개 비용 45/30·135/90");

                var a = NewCommander(template, "LabCheckA", created);
                var b = NewCommander(template, "LabCheckB", created);
                var lab1 = NewLab(UnitRole.Melee, "LabCheck1", created);
                var lab2 = NewLab(UnitRole.Melee, "LabCheck2", created);
                var ranged = NewLab(UnitRole.Ranged, "LabCheckRanged", created);
                Check(lab1.GetFoodCost(0) == 45 && lab1.GetSoilCost(0) == 30 && lab1.GetFoodCost(1) == 90
                    && lab1.GetSoilCost(1) == 60 && lab1.GetFoodCost(2) == 135 && lab1.GetSoilCost(2) == 90, "비용 45/30, 90/60, 135/90");

                // 자원 부족: 차감·잠금 없음.
                rm.TrySpend(rm.GetAmount(ResourceType.Food), rm.GetAmount(ResourceType.Soil));
                rm.Add(ResourceType.Food, 44);
                rm.Add(ResourceType.Soil, 100);
                Check(!lab1.TryResearchAttack(a) && !a.LabUpgradeBusy && !lab1.IsResearching && rm.GetAmount(ResourceType.Food) == 44, "식량 부족 시 차감·잠금 없음");
                rm.TrySpend(0, 71);
                rm.Add(ResourceType.Food, 100);
                Check(!lab1.TryResearchAttack(a) && !a.LabUpgradeBusy && rm.GetAmount(ResourceType.Soil) == 29, "흙 부족 시 차감·잠금 없음");
                rm.AddCapacity(ResourceType.Food, 5000);
                rm.AddCapacity(ResourceType.Soil, 5000);
                rm.Add(ResourceType.Food, 5000);
                rm.Add(ResourceType.Soil, 5000);

                // 소유권·다중 연구소.
                a.TryAssign(1);
                var attackBefore = a.AttackDamage;
                var food = rm.GetAmount(ResourceType.Food);
                var soil = rm.GetAmount(ResourceType.Soil);
                Check(!ranged.TryResearchAttack(a), "다른 보직 연구소는 거부");
                Check(lab1.TryResearchAttack(a), "공격 강화 시작");
                Check(rm.GetAmount(ResourceType.Food) == food - 45 && rm.GetAmount(ResourceType.Soil) == soil - 30, "1단계 비용 1회 차감");
                Check(!lab2.TryResearchArmor(a) && !lab1.TryResearchArmor(a), "같은 장수 중복 강화 불가");
                Check(!lab1.TryResearchAttack(b), "연구 중인 연구소는 다른 장수 거부");
                Check(lab2.TryResearchArmor(b), "다른 연구소는 다른 장수 강화 가능");
                await Until(() => !lab1.IsResearching && !lab2.IsResearching, 2000);
                Check(a.LabAttackLevel == 1 && a.LabArmorLevel == 0 && !a.LabUpgradeBusy && b.LabArmorLevel == 1, "각 장수 1단계 완료");
                Check(a.TroopCount > 0 && Mathf.Approximately(a.AttackDamage - attackBefore, 2f * a.TroopCount), "병력당 공격 +2");

                // 실제 TrySetRole 보직 변경 후 유지, 새 보직 연구소에서 2·3단계 후 상한.
                Check(Arm(a, UnitRole.Ranged) && a.Role == UnitRole.Ranged && a.LabAttackLevel == 1, "TrySetRole 후 강화 유지");
                Check(!lab1.TryResearchAttack(a), "옛 보직 연구소는 거부");
                food = rm.GetAmount(ResourceType.Food);
                Check(ranged.TryResearchAttack(a) && rm.GetAmount(ResourceType.Food) == food - 90, "2단계 90F");
                await Until(() => !ranged.IsResearching, 2000);
                food = rm.GetAmount(ResourceType.Food);
                soil = rm.GetAmount(ResourceType.Soil);
                Check(ranged.TryResearchAttack(a) && rm.GetAmount(ResourceType.Food) == food - 135 && rm.GetAmount(ResourceType.Soil) == soil - 90, "3단계 135F 90S");
                await Until(() => !ranged.IsResearching, 2000);
                food = rm.GetAmount(ResourceType.Food);
                Check(a.LabAttackLevel == 3 && !ranged.TryResearchAttack(a) && !a.LabUpgradeBusy && rm.GetAmount(ResourceType.Food) == food, "실제 3단계 후 상한, 비용 없음");
                Check(Arm(a, UnitRole.Melee) && a.LabAttackLevel == 3, "원래 보직 복귀 후에도 유지");

                // 연구소 비활성화 → 재활성화: 완료 안 됨, 재시작 가능.
                Check(lab1.TryResearchArmor(a), "방어 강화 시작");
                lab1.gameObject.SetActive(false);
                lab1.gameObject.SetActive(true);
                Check(!a.LabUpgradeBusy && !lab1.IsResearching, "연구소 비활성화 즉시 양쪽 해제");
                await Task.Delay(500);
                Check(a.LabArmorLevel == 0, "연구소 재활성화 후 완료되지 않음");

                // 장수 GameObject 비활성화 → 재활성화.
                Check(lab1.TryResearchArmor(a), "방어 강화 재시작");
                a.gameObject.SetActive(false);
                Check(!a.LabUpgradeBusy && !lab1.IsResearching, "장수 GameObject 비활성화 즉시 양쪽 해제");
                a.gameObject.SetActive(true);
                await Task.Delay(500);
                Check(a.LabArmorLevel == 0 && a.LabAttackLevel == 3, "재활성화 후 완료 없음, 획득 단계 유지");

                // 장수 컴포넌트 비활성화 → 재활성화.
                Check(lab1.TryResearchArmor(a), "방어 강화 재시작 2");
                a.enabled = false;
                Check(!a.LabUpgradeBusy && !lab1.IsResearching, "장수 컴포넌트 비활성화 즉시 양쪽 해제");
                a.enabled = true;
                await Task.Delay(500);
                Check(a.LabArmorLevel == 0, "컴포넌트 재활성화 후 완료 없음");

                // 대상 파괴 후 같은 연구소를 다른 장수가 재사용: 옛 코루틴이 새 대상을 조기 완료하지 않는다.
                var doomed = NewCommander(template, "LabCheckDoomed", created);
                Check(lab1.TryResearchAttack(doomed), "파괴 예정 장수 강화 시작");
                await Task.Delay(100);
                Object.Destroy(doomed.gameObject);
                await Task.Delay(50);
                Check(!lab1.IsResearching, "대상 파괴 시 연구소 해제");
                var bAttack = b.LabAttackLevel;
                Check(lab1.TryResearchAttack(b), "파괴 후 다른 장수로 재사용");
                await Task.Delay(200);
                Check(b.LabAttackLevel == bAttack && b.LabUpgradeBusy && lab1.Target == b, "옛 연구가 새 대상을 조기 완료하지 않음");
                await Until(() => !lab1.IsResearching, 2000);
                await Task.Delay(400);
                Check(b.LabAttackLevel == bAttack + 1 && !b.LabUpgradeBusy, "새 대상은 정확히 1단계만 완료");

                // HUD: 프레임 지연 없이 실시간 선택을 사용한다.
                var hud = Object.FindAnyObjectByType<HUDController>();
                if (hud == null || selectedList == null) Check(false, "HUD/SelectionManager 존재");
                else
                {
                    await Task.Delay(50);
                    food = rm.GetAmount(ResourceType.Food);
                    Select(selectedList, a, b);
                    Check(!hud.TryLabResearch(false) && rm.GetAmount(ResourceType.Food) == food, "다중 선택 즉시 강화 불가·비용 없음");
                    Select(selectedList);
                    Check(!hud.TryLabResearch(false) && rm.GetAmount(ResourceType.Food) == food, "선택 해제 즉시 강화 불가");
                    Select(selectedList, b);
                    Select(selectedList, a);
                    Check(hud.TryLabResearch(false) && a.LabUpgradeBusy && !b.LabUpgradeBusy, "같은 프레임 선택 변경 후 새 선택 대상 강화");
                    var aLab = a.LabUpgradeLab;
                    var bArmor = b.LabArmorLevel;
                    Select(selectedList, b);
                    await Task.Delay(100);
                    var labels = GameObject.Find("HUDCanvas").GetComponentsInChildren<UnityEngine.UI.Text>(true);
                    Check(!Array.Exists(labels, t => t.text.Contains("Upgrading")), "B 선택 시 A 진행 표시 안 함");
                    await Until(() => !aLab.IsResearching, 2000);
                    Check(a.LabArmorLevel == 1 && b.LabArmorLevel == bArmor, "선택 변경이 A 완료 대상을 바꾸지 않음");
                    Select(selectedList, b);
                    Check(hud.TryLabResearch(true) && b.LabUpgradeBusy, "B 선택 강화 시작");
                    await Task.Delay(100);
                    labels = GameObject.Find("HUDCanvas").GetComponentsInChildren<UnityEngine.UI.Text>(true);
                    Check(Array.Exists(labels, t => t.text.Contains("Upgrading")), "HUD 진행 중 표시");
                }
            }
            catch (Exception e)
            {
                Failures.Add("예외: " + e);
            }
            finally
            {
                if (selectedList != null)
                {
                    foreach (var s in selectedList) if (s != null) s.SetSelected(false);
                    selectedList.Clear();
                    foreach (var s in previousSelection) if (s != null) { s.SetSelected(true); selectedList.Add(s); }
                }
                foreach (var go in created) if (go != null) Object.Destroy(go);
                if (upkeep != null) upkeep.enabled = upkeepWasEnabled;
            }
            return Failures.Count == 0 ? $"LabUpgradeChecks PASS ({passed})" : $"LabUpgradeChecks FAIL ({passed} pass)\n" + string.Join("\n", Failures);
        }

        private static CommanderAnt NewCommander(CommanderAnt template, string name, List<GameObject> created)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = name;
            created.Add(go);
            go.transform.position = template.transform.position;
            var commander = go.AddComponent<CommanderAnt>();
            commander.Initialize(template.Data, null, null);
            commander.ConfigureCommander(name, default(CommanderRank), new[] { UnitRole.Melee, UnitRole.Ranged }, UnitRole.Melee);
            return commander;
        }

        private static ResearchLab NewLab(UnitRole role, string name, List<GameObject> created)
        {
            var go = new GameObject(name);
            go.SetActive(false);
            created.Add(go);
            var lab = go.AddComponent<ResearchLab>();
            typeof(ResearchLab).GetField("role", Flags).SetValue(lab, role);
            typeof(ResearchLab).GetField("researchTimeSeconds", Flags).SetValue(lab, ResearchSeconds);
            go.SetActive(true);
            return lab;
        }

        private static void Select(List<SelectableObject> list, params CommanderAnt[] commanders)
        {
            foreach (var s in list) if (s != null) s.SetSelected(false);
            list.Clear();
            foreach (var c in commanders)
            {
                var s = c.GetComponent<SelectableObject>();
                s.SetSelected(true);
                list.Add(s);
            }
        }

        private static async Task Until(Func<bool> condition, int timeoutMs)
        {
            var start = Environment.TickCount;
            while (!condition() && Environment.TickCount - start < timeoutMs) await Task.Delay(20);
        }

        private static void Check(bool condition, string label)
        {
            if (condition) passed++;
            else Failures.Add("FAIL: " + label);
        }
    }
}
