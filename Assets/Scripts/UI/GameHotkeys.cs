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
            if (KeyBindings.Pressed(GameAction.AddTroop)) single.TryAssign(1);
            if (KeyBindings.Pressed(GameAction.RemoveTroop)) single.ReturnTroops(1);
            if (KeyBindings.Pressed(GameAction.CycleWeapon)) single.CycleWeapon();
        }
    }
}
