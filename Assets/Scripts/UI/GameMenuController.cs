using System;
using System.Linq;
using AntColony.Core;
using AntColony.Save;
using AntColony.Units;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AntColony.UI
{
    [DefaultExecutionOrder(-1000)]
    public sealed partial class GameMenuController : MonoBehaviour
    {
        public static GameMenuController Instance { get; private set; }
        public static bool BlocksInput => Instance != null && (Instance.open || Time.frameCount <= Instance.closedFrame || SaveSystem.Busy);
        public string ScreenName { get; private set; }
        private RectTransform panel, content, tooltipPanel;
        private GameObject toolbar;
        private Text tip;
        private Text calendar;
        private Button speedButton;
        private bool open;
        private int closedFrame = -1;
        private float resumeScale = 1;
        private NewGameOptions options = new NewGameOptions();
        private InputField seed;
        private void Awake()
        {
            Instance = this;
            var canvas = MenuTheme.Canvas("GameMenus", transform, 100);
            panel = MenuTheme.Rect("MenuBackdrop", canvas.transform); MenuTheme.Stretch(panel);
            panel.gameObject.AddComponent<Image>().color = new Color(.047f, .039f, .031f, .62f);
            var scrollArea = MenuTheme.Rect("MenuArea", panel); scrollArea.anchorMin = new Vector2(.22f, .12f); scrollArea.anchorMax = new Vector2(.78f, .88f);
            scrollArea.offsetMin = scrollArea.offsetMax = Vector2.zero;
            scrollArea.gameObject.AddComponent<Image>().color = MenuTheme.Background;
            content = MenuTheme.Scroll(scrollArea);
            tooltipPanel = MenuTheme.Rect("Tooltip", canvas.transform);
            tooltipPanel.anchorMin = new Vector2(.2f, 1); tooltipPanel.anchorMax = new Vector2(.8f, 1);
            tooltipPanel.pivot = new Vector2(.5f, 1); tooltipPanel.anchoredPosition = new Vector2(0, -65);
            tooltipPanel.sizeDelta = new Vector2(0, 76);
            var tooltipBackground = tooltipPanel.gameObject.AddComponent<Image>();
            tooltipBackground.color = MenuTheme.Background; tooltipBackground.raycastTarget = false;
            tip = MenuTheme.Text(tooltipPanel, "", 17, 60); tip.alignment = TextAnchor.MiddleCenter;
            MenuTheme.Stretch(tip.rectTransform); tip.rectTransform.offsetMin = new Vector2(12, 8); tip.rectTransform.offsetMax = new Vector2(-12, -8);
            tooltipPanel.gameObject.SetActive(false);
            var bar = MenuTheme.Rect("MenuToolbar", canvas.transform); toolbar = bar.gameObject;
            // 디자인 상단 바 왼쪽: 메뉴 Esc · 장수 G · 과학 K · 외교 J · 로그 L (월드맵 M은 WorldMapPanel 토글).
            bar.anchorMin = bar.anchorMax = new Vector2(0, 1); bar.pivot = new Vector2(0, 1); bar.anchoredPosition = new Vector2(8, -6); bar.sizeDelta = new Vector2(404, 28);
            var layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>(); layout.spacing = 4; layout.childForceExpandWidth = false;
            ToolbarButton(bar, "Menu [Esc]", "메뉴  Esc", Pause, "Pause, save or change settings.");
            ToolbarButton(bar, "Commanders [G]", "장수  G", Roster, "All commanders, sorted by name. Inspect skills and equipment.");
            ToolbarButton(bar, "Science [K]", "과학  K", Science, "Science research: unlock buildings, transport and upgrades.");
            ToolbarButton(bar, "Diplomacy [J]", "외교  J", Diplomacy, "Contacted civilizations, treaties, war and trade.");
            ToolbarButton(bar, "Event Log [L]", "로그  L", EventLog, "Recent colony events.");
            // 가운데: 날짜 · 속도.
            var timebar = MenuTheme.Rect("CalendarToolbar", canvas.transform);
            timebar.anchorMin = timebar.anchorMax = new Vector2(0, 1); timebar.pivot = new Vector2(0, 1);
            timebar.anchoredPosition = new Vector2(440, -4); timebar.sizeDelta = new Vector2(340, 32);
            var timeLayout = timebar.gameObject.AddComponent<HorizontalLayoutGroup>(); timeLayout.spacing = 4; timeLayout.childForceExpandWidth = false;
            calendar = MenuTheme.Text(timebar, "", 14, 32); calendar.alignment = TextAnchor.MiddleRight;
            calendar.GetComponent<LayoutElement>().preferredWidth = 120;
            speedButton = ToolbarButton(timebar, "1x", "1×", () => SetSpeed(Time.timeScale >= 3 ? 1 : Time.timeScale + 1), null);
            ToolbarButton(timebar, "Pause / Play", "일시정지  P", ToggleSimulation, null);
            ShowLoading();
        }
        // 오브젝트 이름은 검사·툴팁이 찾는 기존 키를 유지하고, 표시 문구만 디자인의 한글 라벨을 쓴다.
        private static Button ToolbarButton(Transform parent, string name, string label, System.Action action, string tip)
        {
            var button = MenuTheme.Button(parent, name, action, tip);
            var text = button.GetComponentInChildren<Text>(); text.text = label; text.fontSize = 13;
            var element = button.GetComponent<LayoutElement>(); element.preferredHeight = 28;
            element.preferredWidth = Mathf.Max(56, text.preferredWidth + 18);
            return button;
        }
        private static readonly string[] SeasonNames = { "봄", "여름", "가을", "겨울" };
        private static string CalendarLabel => $"{GameCalendar.Year}년 {SeasonNames[(int)GameCalendar.CurrentSeason]} {GameCalendar.Month}월";
        private void OnDestroy() { if (Instance == this) Instance = null; }
        private void Update()
        {
            calendar.transform.parent.gameObject.SetActive(GameSession.Instance.GameStarted && !open);
            calendar.text = CalendarLabel;
            speedButton.GetComponentInChildren<Text>().text = Time.timeScale == 0 ? "정지" : Time.timeScale + "×";
            if (SaveSystem.Busy || Keyboard.current == null) return;
            if (PollRebind()) return;
            if (!open && GameSession.Instance.GameStarted)
            {
                if (Keyboard.current.equalsKey.wasPressedThisFrame || Keyboard.current.numpadPlusKey.wasPressedThisFrame) SetSpeed(Time.timeScale + 1);
                if (Keyboard.current.minusKey.wasPressedThisFrame || Keyboard.current.numpadMinusKey.wasPressedThisFrame) SetSpeed(Time.timeScale - 1);
            }
            if (Keyboard.current.f2Key.wasPressedThisFrame) { Guide(); return; }
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (SkillTargeting.ConsumesPointerInput) return;
                if (!open && (FindFirstObjectByType<AntColony.Buildings.BuildingPlacementController>()?.IsPlacing == true
                    || FindFirstObjectByType<AttackMoveController>()?.IsAttackMode == true)) return;
                if (!open) Pause(); else if (GameSession.Instance.GameStarted) Resume(); else Main();
            }
            if (Keyboard.current.f1Key.wasPressedThisFrame && GameSession.Instance.GameStarted) Roster();
            else if (!open && GameSession.Instance.GameStarted && Time.frameCount > closedFrame) GameHotkeys.Handle(this);
        }
        public void SetSpeed(float value)
        {
            if (SaveSystem.Busy || open || !GameSession.Instance.GameStarted) return;
            Time.timeScale = Mathf.Clamp(Mathf.Round(value), 0, 3);
            if (Time.timeScale > 0) resumeScale = Time.timeScale;
        }
        public void ToggleSimulation() => SetSpeed(Time.timeScale > 0 ? 0 : Mathf.Max(1, resumeScale));
        private void Screen(string title)
        {
            if (!open) resumeScale = Time.timeScale;
            open = true; ScreenName = title; panel.gameObject.SetActive(true); toolbar.SetActive(false); Tooltip("");
            if (!GameSession.Instance.GameStarted || UserSettings.Current.pauseSimulationOnMenu) Time.timeScale = 0;
            foreach (Transform child in content) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            var heading = MenuTheme.Text(content, title, 32, 70);
            heading.color = MenuTheme.Accent; heading.fontStyle = FontStyle.Bold;
            FindFirstObjectByType<AntColony.Buildings.BuildingPlacementController>()?.CancelPlacement();
        }
        private void Back() { if (GameSession.Instance.GameStarted) Pause(); else Main(); }
        public void Tooltip(string value)
        {
            if (tip == null) return;
            tip.text = value;
            tooltipPanel.gameObject.SetActive(!string.IsNullOrEmpty(value));
            if (!string.IsNullOrEmpty(value) && tooltipPanel.gameObject.activeInHierarchy) tooltipPanel.SetAsLastSibling();
        }
        public void ShowLoading() { Screen("Loading..."); Time.timeScale = 0; }
        public void SceneReady(bool playing) { resumeScale = 1; if (playing) Resume(); else Main(); }
        public void Resume()
        {
            if (SaveSystem.Busy || !GameSession.Instance.GameStarted) return;
            if (CampaignResearch.Instance != null && CampaignResearch.Instance.Departed) { ShowDeparture(); return; }
            if (GameManager.Instance != null && GameManager.Instance.SavedDefeat) { ShowOutcome(false); return; }
            Tooltip("");
            open = false; closedFrame = Time.frameCount; ScreenName = "Game"; panel.gameObject.SetActive(false); toolbar.SetActive(true); Time.timeScale = resumeScale;
        }
        public void Main()
        {
            Screen("ANT COLONY");
            var subtitle = MenuTheme.Text(content, "BUILD YOUR COLONY  /  COMMAND YOUR SWARM", 16, 48);
            subtitle.color = MenuTheme.Muted;
            var start = MenuTheme.Button(content, "New Game", NewGameScreen, "Choose a reproducible map seed, size and invasion difficulty.");
            var startColors = start.colors; startColors.normalColor = MenuTheme.Accent; startColors.highlightedColor = MenuTheme.Hex(0xffb84d); start.colors = startColors;
            start.GetComponentInChildren<Text>().color = MenuTheme.AccentInk;
            MenuTheme.Button(content, "Continue / Load", () => Slots(false));
            MenuTheme.Button(content, "Settings", Settings);
            MenuTheme.Button(content, "Encyclopedia", Book);
            MenuTheme.Button(content, "How to Play [F2]", Guide);
        }
        public void Pause()
        {
            if (CampaignResearch.Instance != null && CampaignResearch.Instance.Departed) { ShowDeparture(); return; }
            if (GameManager.Instance != null && GameManager.Instance.SavedDefeat) { ShowOutcome(false); return; }
            Screen("Paused");
            MenuTheme.Button(content, "Continue", Resume);
            MenuTheme.Button(content, "Save Game", () => Slots(true));
            MenuTheme.Button(content, "Load Game", () => Slots(false));
            MenuTheme.Button(content, "Settings", Settings);
            MenuTheme.Button(content, "Commanders", Roster);
            MenuTheme.Button(content, "Science / Airship", Science);
            MenuTheme.Button(content, "이벤트 로그 [L]", EventLog);
            MenuTheme.Button(content, "Encyclopedia", Book);
            MenuTheme.Button(content, "How to Play [F2]", Guide);
            MenuTheme.Button(content, "Save & Main Menu", () => {
                if (!SaveSystem.TrySave(true, 0, out var error)) { ToastManager.Show("Cannot leave safely: " + error); return; }
                GameSession.Instance.MarkNotStarted(); Main();
            }, "Saves to the auto slot first. If saving fails, your game stays open.");
        }
        private void NewGameScreen()
        {
            Screen("New Game");
            MenuTheme.Button(content, "Map: " + MapSizes.Label(options.mapSize), () => { ReadSeed(); options.mapSize = (MapSize)(((int)options.mapSize + 1) % 3); NewGameScreen(); });
            MenuTheme.Button(content, "Difficulty: " + DifficultyProfile.Label(options.difficulty), () => { ReadSeed(); options.difficulty = (DifficultyLevel)(((int)options.difficulty + 1) % 3); NewGameScreen(); });
            MenuTheme.Text(content, DifficultyProfile.Description(options.difficulty), 18, 60);
            MenuTheme.Button(content, "Commander death: " + options.commanderDeath, () => { ReadSeed(); options.commanderDeath = (CommanderDeathMode)(((int)options.commanderDeath + 1) % 3); NewGameScreen(); });
            MenuTheme.Text(content, "Gentle: no deaths. Normal: a second fall while severely injured may be fatal. Harsh: any fall may be fatal.", 18, 60);
            MenuTheme.Text(content, "Map seed (signed integer)", 19); seed = MenuTheme.Input(content, options.seed.ToString());
            MenuTheme.Button(content, "Random Seed", () => { options.seed = NewGameOptions.RandomSeed(); seed.text = options.seed.ToString(); });
            MenuTheme.Button(content, "Start Game", () => { if (!ReadSeed()) return; try { SaveSystem.NewGame(options); } catch (Exception e) { ToastManager.Show(e.Message); } });
            MenuTheme.Button(content, "Back", Main);
        }
        public void Guide()
        {
            Screen("FIELD GUIDE");
            MenuTheme.Text(content, "BETA GOAL: defeat a MiniBird boss. You can keep playing after victory. Losing all home buildings ends the run.", 18, 76);
            MenuTheme.Text(content, "1. Select a commander (click or drag). Right-click food or soil to gather. Produce Ant adds idle workers; select a commander and use +1 Ant to assign troops.", 18, 95);
            MenuTheme.Text(content, "2. Select an idle commander with troops, then choose a construction button. Green preview: left-click to build. Red: blocked or unreachable. Right-click / Esc cancels. Keep free ants for builders.", 18, 95);
            MenuTheme.Text(content, "3. Every commander can work and fight. Equip owned weapons in commander details to change combat style; wings use the armor slot. Right-click enemies to attack, or press A then click for attack-move. Nine skills grow through use; Command skill sets troop capacity.", 18, 115);
            MenuTheme.Text(content, "4. Reach 60 ants, unlock Fishing and upgrade a barracks to Tier 2. In World / Science, build a Science Lab, research vehicles and build a transport.", 18, 95);
            MenuTheme.Text(content, "5. Bring combat commanders near the transport, board, choose a MiniBird nest and depart. Switch to the battlefield, dodge marked boss attacks and win. Return Home brings the crew and cargo back.", 18, 100);
            MenuTheme.Text(content, "Storage: Build Storage expands resource limits and adds a drop-off point. Interrupted delivery: right-click the Queen Chamber or a Storage; on expeditions, right-click your own transport to deliver carried resources.", 18, 100);
            MenuTheme.Text(content, "Camera: screen edges, wheel to zoom, Z/C to rotate. Esc: menu, P: pause. G/F1: commanders. Q: weapon skill, E/D: troop +1/-1, R: weapon, K/M: world & science. Keys can be changed in Settings. Save from Menu when units are idle; active work or combat currently blocks saving. Leave home defenders behind before a raid.", 18, 95);
            MenuTheme.Button(content, "Back", Back);
        }

        public void ShowOutcome(bool victory)
        {
            if (!GameSession.Instance.GameStarted) return;
            Screen(victory ? "VICTORY - BETA COMPLETE" : "COLONY LOST");
            Time.timeScale = 0;
            var seconds = GameSession.Instance.PlaySeconds;
            MenuTheme.Text(content, victory ? "The boss has fallen. Your colony can continue expanding." : "All home buildings were destroyed. Load a save or start a new colony.", 22, 80);
            MenuTheme.Text(content, $"Play time: {(int)seconds / 60:00}:{(int)seconds % 60:00}  /  Commanders: {CommanderRoster.Instance?.Count ?? 0}", 18, 48);
            if (victory) MenuTheme.Button(content, "Continue Colony", () => { resumeScale = 1; Resume(); });
            MenuTheme.Button(content, "Load Game", () => Slots(false));
            MenuTheme.Button(content, "Restart Same Map", () => {
                Screen("Restart colony?");
                MenuTheme.Text(content, "Unsaved progress will be lost. Existing save slots will be kept.", 20, 70);
                MenuTheme.Button(content, "Restart", () => SaveSystem.NewGame(GameSession.Instance.Options.Clone()));
                MenuTheme.Button(content, "Back", () => ShowOutcome(victory));
            });
            MenuTheme.Button(content, "New Game", NewGameScreen);
        }
        private bool ReadSeed()
        { if (seed == null || !int.TryParse(seed.text, out var value)) { ToastManager.Show("Enter a seed between -2147483648 and 2147483647."); return false; } options.seed = value; return true; }
        private void Slots(bool save)
        {
            Screen(save ? "Save Game" : "Load Game");
            MenuTheme.Text(content, "3 manual slots + 1 auto slot. Saving overwrites the selected slot; the previous file is kept as .bak.", 17, 65);
            foreach (var slot in SaveSlots.List())
            {
                if (save && slot.Auto) continue;
                MenuTheme.Text(content, slot.DisplayName + " - " + slot.Summary(), 17, 52);
                var button = MenuTheme.Button(content, (save ? "Save " : "Load ") + slot.DisplayName, () => {
                    var ok = save ? SaveSystem.TrySave(slot.Auto, slot.Index, out var error) : SaveSystem.TryLoad(slot.Path, out error);
                    ToastManager.Show(ok ? (save ? "Saved." : "Loading save...") : error);
                    if (save) Slots(true);
                });
                button.interactable = save || slot.Valid;
            }
            MenuTheme.Button(content, "Back", Back);
        }
        private void Settings()
        {
            Screen("Settings");
            var s = UserSettings.Current;
            void Apply(Action<UserSettingsData> change) { var value = s.Clone(); change(value); UserSettings.Apply(value);
                FindFirstObjectByType<AntColony.Camera.IsometricCameraController>()?.ApplySettings(value.cameraPanSpeed, value.edgeScrollThickness); Settings(); }
            MenuTheme.Button(content, "Camera speed: " + s.cameraPanSpeed + " (cycle)", () => Apply(v => v.cameraPanSpeed = v.cameraPanSpeed >= 60 ? 10 : v.cameraPanSpeed + 10));
            MenuTheme.Button(content, "Edge scrolling: " + (s.edgeScrollThickness > 0 ? "On" : "Off"), () => Apply(v => v.edgeScrollThickness = v.edgeScrollThickness > 0 ? 0 : 18));
            MenuTheme.Button(content, "Autosave: " + (s.autoSaveEnabled ? "On" : "Off"), () => Apply(v => v.autoSaveEnabled = !v.autoSaveEnabled));
            MenuTheme.Button(content, "Autosave interval: " + s.autoSaveMinutes + " minutes", () => Apply(v => v.autoSaveMinutes = v.autoSaveMinutes >= 15 ? 1 : v.autoSaveMinutes + 1));
            MenuTheme.Button(content, "Toast duration: " + s.toastSeconds + " seconds", () => Apply(v => v.toastSeconds = v.toastSeconds >= 10 ? 2 : v.toastSeconds + 1));
            MenuTheme.Button(content, "Pause simulation in menus: " + s.pauseSimulationOnMenu, () => Apply(v => v.pauseSimulationOnMenu = !v.pauseSimulationOnMenu));
            KeyBindingButtons();
            MenuTheme.Button(content, "Back", Back);
        }
        public static CommanderAnt[] SortedCommanders() => CommanderRoster.Instance == null ? Array.Empty<CommanderAnt>()
            : CommanderRoster.Instance.Commanders.OrderBy(c => c.CommanderName, StringComparer.Ordinal).ToArray();
        public void Roster()
        {
            Screen("Commanders");
            foreach (var c in SortedCommanders())
            {
                var button = MenuTheme.Button(content, c.CommanderName + "  |  " + c.WeaponLabel + "  |  충성 " + c.Traits.Loyalty + "  |  " + Location(c), () => Details(c));
                button.GetComponentInChildren<UnityEngine.UI.Text>().color = LoyaltyColor(c.Traits.Loyalty);
            }
            MenuTheme.Button(content, "Back", Back);
        }
        public static Color LoyaltyColor(int loyalty) => loyalty <= 15 ? MenuTheme.DangerInk : loyalty <= 30 ? MenuTheme.HpMid : MenuTheme.TextColor;
        private static string Location(CommanderAnt c) => c.IsDeparting ? c.Social.departure.ToString() : c.IsCaptive ? "Captive" : c.Garrison != null ? "Garrison" : c.Transport != null ? c.Transport.State.ToString() : "Home";
        public void Details(CommanderAnt c)
        {
            if (c == null) { Roster(); return; }
            Screen(c.CommanderName);
            MenuTheme.Text(content, $"{c.WeaponLabel} | {(c.IsFlying ? "Flying" : "Ground")} | {Location(c)}\nTroops {c.TroopCount}/{c.CommandLimit} | HP {c.CurrentHealth:0.##}\n"
                + $"Attack {c.AttackDamage:0.#} | Armor {c.Armor:0.#} | Loyalty {c.Traits.Loyalty}\n"
                + $"Research: Attack {c.LabAttackLevel}, Armor {c.LabArmorLevel}", 19, 145);
            foreach (CommanderActivity skill in Enum.GetValues(typeof(CommanderActivity)))
            {
                var level = c.Talents.Level(skill);
                var progress = level == CommanderTalents.MaxLevel ? "MAX" : $"{c.Talents.Xp(skill):0.#}/{CommanderTalents.Required(level)} XP";
                MenuTheme.Text(content, $"{skill} {new string('*', c.Traits.Flame(skill))}  {level}/20  ({progress})", 18, 30);
            }
            PersonalDetails(c);
            MenuTheme.Button(content, "Back to Roster", Roster);
        }
        private void Book()
        {
            Screen("Encyclopedia");
            if (Encyclopedia.Entries.Count == 0) MenuTheme.Text(content, "Explore to discover ants, bosses and civilizations.", 20, 60);
            foreach (var e in Encyclopedia.Entries.OrderBy(e => e.category).ThenBy(e => e.title))
            { MenuTheme.Text(content, e.category + " / " + e.title, 23, 44); MenuTheme.Text(content, e.body, 18, 90); }
            MenuTheme.Button(content, "Back", Back);
        }
    }
}
