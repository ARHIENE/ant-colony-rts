using System;
using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Units;
using UnityEngine;

namespace AntColony.UI
{
    public sealed partial class GameMenuController
    {
        public void Science()
        {
            Screen("Science / Airship");
            var research = CampaignResearch.Instance;
            if (research == null) { MenuTheme.Text(content, "Science is initializing."); MenuTheme.Button(content, "Back", Back); return; }
            MenuTheme.Text(content, research.Active == null ? "Choose one shared project. Assigned commanders contribute from eligible labs."
                : $"{research.Active.Name}: {research.Progress:0}/{research.Active.Work:0} work. Resume the game to advance.", 18, 65);
            foreach (var lab in FindObjectsByType<ScienceLab>(FindObjectsSortMode.None))
            {
                MenuTheme.Text(content, $"{lab.name} T{lab.Tier} | Researcher: {lab.Target?.CommanderName ?? "none"}", 19, 48);
                var captured = lab;
                MenuTheme.Button(content, $"Upgrade lab ({lab.Tier * 60}F / {lab.Tier * 80}S)", () => { if (!captured.TryUpgrade()) ToastManager.Show("Upgrade unavailable or insufficient resources."); Science(); }).interactable = lab.Tier < 4 && !lab.Busy;
                if (lab.Target != null) MenuTheme.Button(content, "Release researcher", () => { captured.ReleaseResearcher(); Science(); });
                else foreach (var c in SortedCommanders().Where(c => !c.IsAwayFromHome && c.CanChangeAllocation && !c.IsWorking && Vector3.Distance(c.Position, lab.Position) <= 8))
                    MenuTheme.Button(content, "Assign " + c.CommanderName, () => { if (!captured.TryAssign(c)) ToastManager.Show("Cannot assign this commander."); Science(); });
            }
            MenuTheme.Text(content, "Move an idle commander within 8m of a lab to assign. Tier must match the selected research.", 17, 60);
            foreach (var definition in CampaignResearch.Technologies)
            {
                var reason = research.BlockReason(definition.Technology);
                var done = research.Has(definition.Technology);
                var button = MenuTheme.Button(content, $"T{definition.Tier} {definition.Name} — {(done ? "Complete" : $"{definition.Food}F/{definition.Soil}S{(definition.Special > 0 ? $"/{definition.Special} Special" : "")}, {definition.Work:0} work")}",
                    () => { if (!research.TryStart(definition.Technology)) ToastManager.Show("Cannot start: check prerequisites and resources."); Science(); }, reason == "" ? "Starts one shared research project." : reason);
                button.interactable = reason == "";
            }
            MenuTheme.Text(content, "Engine blueprint: " + (research.HasBlueprint ? "acquired" : "defeat a world-map boss and bring its reward home"), 18, 55);
            MenuTheme.Button(content, "Build Infirmary (40F / 40S / 4 ants)", () => {
                Resume(); FindFirstObjectByType<BuildingPlacementController>()?.BeginInfirmaryPlacement();
            }, "Treat up to two seriously injured commanders. Assign nearby patients from commander details.").interactable = Infirmary.Unlocked;
            ScienceBuildings();
            MenuTheme.Button(content, "Build Airship Yard (100F / 150S / 10 ants)", () => {
                Resume(); FindFirstObjectByType<BuildingPlacementController>()?.BeginAirshipYardPlacement();
            });
            foreach (var yard in FindObjectsByType<AirshipYard>(FindObjectsSortMode.None))
            {
                MenuTheme.Text(content, $"{yard.name} | Hull {yard.Hull} | Engine {yard.Engine} | Cocoons {yard.Cocoons} | {yard.Remaining:0}s", 18, 55);
                foreach (AirshipPart part in Enum.GetValues(typeof(AirshipPart)))
                    MenuTheme.Button(content, "Build " + part + (part == AirshipPart.Cocoon ? $" ({GameBalance.CocoonFood}F/{GameBalance.CocoonSoil}S/{GameBalance.CocoonSpecial} Special, {yard.Cocoons}/{GameBalance.MaxCocoons})" : " (100F/150S/150 Special)"),
                        () => { if (!yard.TryBuild(part)) ToastManager.Show("Requires the matching research, resources and an idle yard."); Science(); });
                foreach (var c in SortedCommanders().Where(c => c.CanChangeAllocation && !c.IsAwayFromHome && Vector3.Distance(c.Position, yard.Position) <= 8))
                    MenuTheme.Button(content, "Board " + c.CommanderName, () => { if (!yard.TryBoard(c)) ToastManager.Show("Finish hull/engine and build a cocoon per passenger."); Science(); });
                MenuTheme.Text(content, "Aboard: " + string.Join(", ", yard.Passengers.Select(c => c.CommanderName)), 18, 50);
                MenuTheme.Button(content, "Unload airship", () => { yard.Unload(); Science(); });
                MenuTheme.Button(content, "Depart — end this run", () => { if (!yard.TryDepart()) ToastManager.Show("Finish hull and engine first."); }).interactable = yard.Ready;
            }
            MenuTheme.Button(content, "Refresh", Science);
            MenuTheme.Button(content, "Back", Back);
        }
        // 장수 상세 아래쪽 행동 버튼: 치료·재생·포상·장비.
        private void CommanderActions(CommanderAnt c)
        {
            if (c.TreatmentFacility != null)
                MenuTheme.Button(content, "Stop treatment", () => { c.TreatmentFacility?.Release(c); Details(c); }, "Treatment progress is preserved. Resume the game to recover.");
            else if (c.PersonalState.NeedsTreatment)
            {
                MenuTheme.Text(content, "Treatment: research and build an Infirmary, then move within 8m. Two patients per facility; resume to recover.", 17, 65);
                foreach (var infirmary in FindObjectsByType<Infirmary>(FindObjectsSortMode.None))
                    MenuTheme.Button(content, $"Treat at {infirmary.name} ({infirmary.Patients.Count}/{Infirmary.Capacity})",
                        () => { if (!infirmary.TryAdmit(c)) ToastManager.Show("Requires an idle injured commander nearby and a free bed."); Details(c); }).interactable = infirmary.CanTreat(c);
            }
            RegenerationButtons(c);
            MenuTheme.Button(content, "Reward (30 Food)", () => { if (!c.TryReward()) ToastManager.Show("Available at home, once per game month."); Details(c); }).interactable = c.CanReceiveOrders && !c.IsAwayFromHome && c.PersonalState.rewardCooldown <= 0;
            var inventory = EquipmentInventory.Instance;
            if (inventory == null) return;
            MenuTheme.Text(content, $"장비 보관함 {inventory.Items.Count}/{EquipmentInventory.Capacity}", 18, 36);
            foreach (var item in c.PersonalState.equipment.ToArray())
                MenuTheme.Button(content, "Unequip " + item.Label, () => { if (!inventory.Unequip(c, item)) ToastManager.Show("Cannot remove equipment while unavailable or without a safe landing point."); Details(c); });
            foreach (var item in inventory.Items.ToArray())
                MenuTheme.Button(content, "Equip " + item.Label, () => { if (!inventory.Equip(c, item)) ToastManager.Show("Cannot equip while unavailable, with injured wings or without a safe landing point."); Details(c); });
        }
        public void ShowDeparture()
        {
            var research = CampaignResearch.Instance;
            if (research == null || !research.Departed) return;
            Screen("GREAT MIGRATION — VICTORY"); Time.timeScale = 0;
            MenuTheme.Text(content, $"Your airship has departed. Game time: {research.EndingGameSeconds / 3600:0.00} years.", 22, 70);
            MenuTheme.Text(content, "Passengers: " + (research.Passengers.Count == 0 ? "None" : string.Join(", ", research.Passengers)), 18, 100);
            MenuTheme.Text(content, "Left behind: " + string.Join(", ", research.LeftBehind), 18, 150);
            MenuTheme.Button(content, "Main Menu", () => { GameSession.Instance.MarkNotStarted(); Main(); });
        }
    }
}
