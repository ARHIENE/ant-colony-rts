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
        private RectTransform panel, content, tooltipPanel, scrollArea, legacyContent, frame;
        private GameObject toolbar;
        private Text tip;
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
            scrollArea = MenuTheme.Rect("MenuArea", panel); scrollArea.anchorMin = new Vector2(.22f, .12f); scrollArea.anchorMax = new Vector2(.78f, .88f);
            scrollArea.offsetMin = scrollArea.offsetMax = Vector2.zero;
            scrollArea.gameObject.AddComponent<Image>().color = MenuTheme.Background;
            content = legacyContent = MenuTheme.Scroll(scrollArea);
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
            // HUD v3 상단 바 왼쪽: ≡ · 작업표 · 과학 · 월드맵 · 기록. 버튼 이름 2~3글자, 단축키 글자는 숨김(단축키 전체 미정).
            // 장수 관리는 장수 바 인원수 클릭, 외교는 월드맵 안으로 옮겼다. 가운데는 장수 바(RosterBar), 달력·속도는 우상단 HudClock.
            bar.anchorMin = bar.anchorMax = new Vector2(0, 1); bar.pivot = new Vector2(0, 1); bar.anchoredPosition = new Vector2(8, -6); bar.sizeDelta = new Vector2(320, 28);
            var layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>(); layout.spacing = 4; layout.childForceExpandWidth = false;
            ToolbarButton(bar, "Menu [Esc]", "≡", Pause, "메뉴: 설정 · 저장 · 불러오기");
            ToolbarButton(bar, "Work Schedule [T]", "작업표", WorkSchedule, "장수 × 작업 체크박스");
            ToolbarButton(bar, "Science [K]", "과학", Science, "과학 연구");
            ToolbarButton(bar, "WorldMapToggle", "월드맵", () => FindFirstObjectByType<WorldMapPanel>()?.Toggle(), "월드맵 · 원정 · 외교");
            ToolbarButton(bar, "Event Log [L]", "기록", EventLog, "이벤트 기록");
            foreach (var element in bar.GetComponentsInChildren<LayoutElement>()) element.preferredWidth = Mathf.Max(30, element.preferredWidth - 16);
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
        private void OnDestroy() { if (Instance == this) Instance = null; }
        private void Update()
        {
            if (open) refreshDutyScreen?.Invoke();
            if (SaveSystem.Busy || Keyboard.current == null) return;
            if (PollRebind()) return;
            if (!open && GameSession.Instance.GameStarted)
            {
                // 스페이스 = 일시정지(P와 같음), F5~F8 = 1·2·3·5배. 출전·전투 중에도 제한 없음.
                if (Keyboard.current.spaceKey.wasPressedThisFrame) ToggleSimulation();
                for (var i = 0; i < SpeedKeys.Length; i++) if (Keyboard.current[SpeedKeys[i]].wasPressedThisFrame) SetSpeed(Speeds[i]);
            }
            if (Keyboard.current.f2Key.wasPressedThisFrame) { Guide(); return; }
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (SkillTargeting.ConsumesPointerInput || GatherDesignation.ConsumesPointerInput) return;
                if (!open && (FindFirstObjectByType<AntColony.Buildings.BuildingPlacementController>()?.IsPlacing == true
                    || FindFirstObjectByType<AttackMoveController>()?.IsAttackMode == true)) return;
                if (!open && BuildScreen.Back()) return;
                if (!open) Pause(); else if (GameSession.Instance.GameStarted) Resume(); else Main();
            }
            // Enter: 디자인 화면의 주 버튼(이어하기·게임 시작).
            if (open && frame != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame))
                frame.GetComponentsInChildren<Button>().FirstOrDefault(b => b.interactable && (b.name == "Continue" || b.name == "Start Game"))?.onClick.Invoke();
            if (Keyboard.current.f1Key.wasPressedThisFrame && GameSession.Instance.GameStarted) Roster();
            else if (!open && GameSession.Instance.GameStarted && Time.frameCount > closedFrame) GameHotkeys.Handle(this);
        }
        public static readonly float[] Speeds = { 1, 2, 3, 5 };
        private static readonly Key[] SpeedKeys = { Key.F5, Key.F6, Key.F7, Key.F8 };
        public void SetSpeed(float value)
        {
            if (SaveSystem.Busy || open || !GameSession.Instance.GameStarted) return;
            Time.timeScale = value <= 0 ? 0 : Speeds.OrderBy(s => Mathf.Abs(s - value)).First();
            if (Time.timeScale > 0) resumeScale = Time.timeScale;
        }
        public void ToggleSimulation() => SetSpeed(Time.timeScale > 0 ? 0 : Mathf.Max(1, resumeScale));
        // 화면 공통 상태 전환: 열기·일시정지·이전 화면 정리.
        private void BeginScreen(string title, float scrim)
        {
            refreshDutyScreen = null;
            if (!open) resumeScale = Time.timeScale;
            open = true; ScreenName = title; panel.gameObject.SetActive(true); toolbar.SetActive(false); Tooltip("");
            if (!GameSession.Instance.GameStarted || UserSettings.Current.pauseSimulationOnMenu) Time.timeScale = 0;
            content = legacyContent;
            foreach (Transform child in content) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            if (frame != null) { frame.gameObject.SetActive(false); Destroy(frame.gameObject); frame = null; }
            panel.GetComponent<Image>().color = new Color(.047f, .039f, .031f, scrim);
        }
        // 디자인 배치 화면: 1440×900 기준 빈 판을 돌려준다. 목록형 옛 화면 영역은 숨긴다.
        private RectTransform Frame(string title, float scrim = .62f)
        {
            BeginScreen(title, scrim);
            scrollArea.gameObject.SetActive(false);
            frame = MenuTheme.Rect("ScreenFrame", panel); MenuTheme.Stretch(frame);
            FindFirstObjectByType<AntColony.Buildings.BuildingPlacementController>()?.CancelPlacement();
            return frame;
        }
        private void Screen(string title, string displayTitle = null)
        {
            BeginScreen(title, .62f);
            scrollArea.gameObject.SetActive(true);
            var heading = MenuTheme.Text(content, displayTitle ?? title, 32, 70);
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
        public void ShowLoading() { Screen("Loading...", "불러오는 중…"); Time.timeScale = 0; }
        public void SceneReady(bool playing) { resumeScale = 1; if (playing) Resume(); else Main(); }
        public void Resume()
        {
            if (SaveSystem.Busy || !GameSession.Instance.GameStarted) return;
            if (CampaignResearch.Instance != null && CampaignResearch.Instance.Departed) { ShowDeparture(); return; }
            if (GameManager.Instance != null && GameManager.Instance.SavedDefeat) { ShowOutcome(false); return; }
            Tooltip("");
            open = false; closedFrame = Time.frameCount; ScreenName = "Game"; panel.gameObject.SetActive(false); toolbar.SetActive(true); Time.timeScale = resumeScale;
        }
        public void Guide()
        {
            Screen("FIELD GUIDE", "게임 설명서");
            MenuTheme.Text(content, "중간 목표는 미니새 보스 처치, 최종 목표는 로켓 발사입니다. 보스 처치 후에도 계속할 수 있습니다. 회복 가능한 장수를 포함해 활동 가능한 장수가 0명이면 패배합니다.", 18, 76);
            MenuTheme.Text(content, "1. 장수는 병력 없이 작업표에 켜진 일을 수행합니다. 하단 커맨드 카드 또는 장수 관리(G)의 작업표에서 작업을 켜고 끄세요. 자원 우클릭은 우선 작업 지시입니다.", 18, 95);
            MenuTheme.Text(content, "2. 건설(B)에서 건물과 대기 장수를 골라 배치하세요. 군사 탭에서 징집소를 건설한 뒤 클릭하면 출전 장수와 병력을 편성할 수 있습니다. 귀환(D)하면 생존 병력을 반납하고 자율 작업을 재개합니다.", 18, 95);
            MenuTheme.Text(content, "3. 장수 상세에서 무기를 장착하면 전투 역할이 바뀝니다. 날개는 방어구입니다. 적 우클릭은 공격, A를 누른 뒤 클릭은 공격 이동입니다. 13종 기술은 활동에 따라 성장하며 지휘 기술은 병력 한도를 높입니다.", 18, 115);
            MenuTheme.Text(content, "4. 일반개미 60마리와 2티어 병영을 확보하면 과학연구소를 지을 수 있습니다. 과학 화면에서 차량을 연구한 뒤 과학연구소를 선택해 수송수단을 건조하세요.", 18, 95);
            MenuTheme.Text(content, "5. 출전 장수를 수송수단에 태우고 월드맵에서 미니새 둥지로 출정하세요. 전장으로 전환해 표시된 보스 공격을 피하며 싸우세요. 귀환하면 장수·병력·화물이 본거지로 돌아옵니다.", 18, 100);
            MenuTheme.Text(content, "창고를 지으면 저장 한도와 반납 지점이 늘어납니다. 운반이 중단되면 저장고나 창고를 우클릭하세요. 원정지에서는 아군 수송수단을 우클릭해 자원을 반납합니다.", 18, 100);
            MenuTheme.Text(content, "기본 조작: 화면 가장자리·방향키로 카메라 이동, 휠로 확대·축소, Z/C로 회전합니다. Esc 메뉴, P 일시 정지, G/F1 장수 관리, Q 무기 기술, E 징집소, D 귀환, R 무기 교체, K 과학, M 월드맵입니다. 설정에서 단축키를 바꿀 수 있습니다.", 18, 95);
            MenuTheme.Text(content, "첫 등장 안내", 20, 32);
            foreach (var hint in FirstHints.All) MenuTheme.Text(content, hint.title + ": " + hint.body, 16, 50);
            MenuTheme.Button(content, "Back", Back);
        }

        public void ShowOutcome(bool victory)
        {
            if (!GameSession.Instance.GameStarted) return;
            Screen(victory ? "VICTORY - BETA COMPLETE" : "COLONY LOST", victory ? "보스 처치 — 중간 목표 달성" : "소굴 붕괴");
            Time.timeScale = 0;
            var seconds = GameSession.Instance.PlaySeconds;
            MenuTheme.Text(content, victory ? "보스를 처치했습니다. 소굴을 계속 확장하고 로켓 발사를 준비하세요." : "활동 가능한 장수가 없습니다. 저장을 불러오거나 새 소굴을 시작하세요.", 22, 80);
            MenuTheme.Text(content, $"플레이 시간: {(int)seconds / 60:00}:{(int)seconds % 60:00}  /  장수: {CommanderRoster.Instance?.Count ?? 0}", 18, 48);
            if (victory) MenuTheme.Button(content, "Continue Colony", () => { resumeScale = 1; Resume(); });
            MenuTheme.Button(content, "Load Game", () => Slots(false));
            MenuTheme.Button(content, "Restart Same Map", () => {
                Screen("Restart colony?", "소굴을 다시 시작할까요?");
                MenuTheme.Text(content, "저장하지 않은 진행 상황은 사라집니다. 기존 저장 슬롯은 유지됩니다.", 20, 70);
                MenuTheme.Button(content, "Restart", () => SaveSystem.NewGame(GameSession.Instance.Options.Clone()));
                MenuTheme.Button(content, "Back", () => ShowOutcome(victory));
            });
            MenuTheme.Button(content, "New Game", NewGameScreen);
        }
        private bool ReadSeed()
        { if (seed == null || !int.TryParse(seed.text, out var value)) { ToastManager.Show("시드는 -2147483648~2147483647 사이의 정수로 입력하세요."); return false; } options.seed = value; return true; }
        private void Settings()
        {
            Screen("Settings", "설정");
            var s = UserSettings.Current;
            void Apply(Action<UserSettingsData> change) { var value = s.Clone(); change(value); UserSettings.Apply(value);
                FindFirstObjectByType<AntColony.Camera.IsometricCameraController>()?.ApplySettings(value.cameraPanSpeed, value.edgeScrollThickness); Settings(); }
            MenuTheme.Button(content, "Camera speed: " + s.cameraPanSpeed + " (cycle)", () => Apply(v => v.cameraPanSpeed = v.cameraPanSpeed >= 60 ? 10 : v.cameraPanSpeed + 10));
            MenuTheme.Button(content, "Edge scrolling: " + (s.edgeScrollThickness > 0 ? "On" : "Off"), () => Apply(v => v.edgeScrollThickness = v.edgeScrollThickness > 0 ? 0 : 18));
            MenuTheme.Button(content, "Autosave: " + (s.autoSaveEnabled ? "On" : "Off"), () => Apply(v => v.autoSaveEnabled = !v.autoSaveEnabled));
            MenuTheme.Button(content, "Autosave interval: " + s.autoSaveMinutes + " minutes", () => Apply(v => v.autoSaveMinutes = v.autoSaveMinutes >= 15 ? 1 : v.autoSaveMinutes + 1));
            MenuTheme.Button(content, "Toast duration: " + s.toastSeconds + " seconds", () => Apply(v => v.toastSeconds = v.toastSeconds >= 10 ? 2 : v.toastSeconds + 1));
            MenuTheme.Button(content, "Pause simulation in menus: " + s.pauseSimulationOnMenu, () => Apply(v => v.pauseSimulationOnMenu = !v.pauseSimulationOnMenu));
            MenuTheme.Button(content, "First-time hints: " + (s.firstHints ? "On" : "Off"), () => Apply(v => v.firstHints = !v.firstHints));
            MenuTheme.Button(content, "Reset seen hints (" + s.shownHints.Count + ")", () => Apply(v => v.shownHints.Clear()));
            KeyBindingButtons();
            MenuTheme.Button(content, "Back", Back);
        }
        public static CommanderAnt[] SortedCommanders() => CommanderRoster.Instance == null ? Array.Empty<CommanderAnt>()
            : CommanderRoster.Instance.Commanders.OrderBy(c => c.CommanderName, StringComparer.Ordinal).ToArray();
        private void Book()
        {
            Screen("Encyclopedia", "도감");
            if (Encyclopedia.Entries.Count == 0) MenuTheme.Text(content, "탐험하며 개미·보스·문명을 발견하세요.", 20, 60);
            foreach (var e in Encyclopedia.Entries.OrderBy(e => e.category).ThenBy(e => e.title))
            { MenuTheme.Text(content, e.category + " / " + e.title, 23, 44); MenuTheme.Text(content, e.body, 18, 90); }
            MenuTheme.Button(content, "Back", Back);
        }
    }
}
