using AntColony.Core;
using AntColony.Units;
using UnityEngine;

namespace AntColony.UI
{
    // 게임 화면 단축키. 카메라 회전(Z/C)은 카메라가 직접 읽는다.
    // Q/W는 준비된 선택 장수 전원에게 무기/날개 스킬을 지시한다. J(외교)는 6단계에서 연결한다.
    public static class GameHotkeys
    {
        public static void Handle(GameMenuController menu)
        {
            if (KeyBindings.Pressed(GameAction.Pause)) menu.ToggleSimulation();
            if (KeyBindings.Pressed(GameAction.Build)) { BuildScreen.Toggle(); return; }
            if (BuildScreen.IsOpen) { BuildScreen.HandleKeys(); return; }
            if (KeyBindings.Pressed(GameAction.Roster)) { menu.Roster(); return; }
            if (KeyBindings.Pressed(GameAction.WorkSchedule)) { menu.WorkSchedule(); return; }
            if (KeyBindings.Pressed(GameAction.EventLog)) { menu.EventLog(); return; }
            if (KeyBindings.Pressed(GameAction.Diplomacy)) { menu.Diplomacy(); return; }
            if (KeyBindings.Pressed(GameAction.SciencePanel) || KeyBindings.Pressed(GameAction.WorldMap))
                Object.FindFirstObjectByType<WorldMapPanel>()?.Toggle();

            var selection = Object.FindFirstObjectByType<SelectionManager>();
            if (selection != null && (KeyBindings.Pressed(GameAction.WeaponSkill) || KeyBindings.Pressed(GameAction.WingSkill)))
            {
                var commanders = new System.Collections.Generic.List<CommanderAnt>();
                foreach (var selected in selection.GetSelectedObjects()) if (selected != null && selected.isActiveAndEnabled) commanders.Add(selected.GetComponent<CommanderAnt>());
                SkillTargeting.Begin(commanders, KeyBindings.Pressed(GameAction.WingSkill));
            }
            var single = SelectedUnitPanel.FindSingleSelectedCommander(selection);
            if (single == null) return;
            if (KeyBindings.Pressed(GameAction.AddTroop)) menu.OpenConscription();
            if (KeyBindings.Pressed(GameAction.RemoveTroop))
            {
                if (single.IsDeployed) { if (!single.ReturnToPost()) ToastManager.Show("지금은 징집소로 귀환할 수 없습니다."); }
                else Object.FindFirstObjectByType<CommandCard>()?.RestOrTreat();
            }
            if (KeyBindings.Pressed(GameAction.CycleWeapon)) single.CycleWeapon();
            // 커맨드 카드 고정 글자(HUD v2): S 정지(출전) · F 우선 작업 · X 휴식 · V 포상.
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null) return;
            if (single.IsDeployed) { if (keyboard.sKey.wasPressedThisFrame) single.CommandStop(); return; }
            if (keyboard.fKey.wasPressedThisFrame) CommandCard.PriorityHint();
            if (keyboard.xKey.wasPressedThisFrame && single.CanRest) single.SendToRest();
            if (keyboard.vKey.wasPressedThisFrame) DetailTabs.Reward(single);
        }
    }
}
