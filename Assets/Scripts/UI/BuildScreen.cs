using System.Collections.Generic;
using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AntColony.UI
{
    // 건설(B) 화면: 오른쪽 날개 = 분류 탭(1~5) + 건물 칸(QWERT/ASDFG), 가운데 = 맡길 장수 고르기, 배치 중에는 상단 안내 막대.
    public sealed class BuildScreen : MonoBehaviour
    {
        private readonly struct Entry
        {
            public readonly string name; public readonly BuildingKind kind; public readonly UnitRole role;
            public Entry(string name, BuildingKind kind, UnitRole role = UnitRole.Worker) { this.name = name; this.kind = kind; this.role = role; }
        }

        private static readonly string[] TabNames = { "생산", "자원", "연구", "방어", "특수" };
        private static readonly Key[] SlotKeys = { Key.Q, Key.W, Key.E, Key.R, Key.T, Key.A, Key.S, Key.D, Key.F, Key.G };
        private static readonly Entry[][] Tabs =
        {
            new[] { new Entry("큰턱 훈련장", BuildingKind.Barracks, UnitRole.Melee), new Entry("산샘 훈련장", BuildingKind.Barracks, UnitRole.Ranged),
                new Entry("갑각 훈련장", BuildingKind.Barracks, UnitRole.Defense), new Entry("페로몬 훈련장", BuildingKind.Barracks, UnitRole.Support),
                new Entry("날개 훈련장", BuildingKind.Barracks, UnitRole.Flying), new Entry("양육실", BuildingKind.Nursery) },
            new[] { new Entry("밭", BuildingKind.Farm), new Entry("저장고", BuildingKind.Storage) },
            new[] { new Entry("큰턱 연구소", BuildingKind.ResearchLab, UnitRole.Melee), new Entry("산샘 연구소", BuildingKind.ResearchLab, UnitRole.Ranged),
                new Entry("갑각 연구소", BuildingKind.ResearchLab, UnitRole.Defense), new Entry("페로몬 연구소", BuildingKind.ResearchLab, UnitRole.Support),
                new Entry("날개 연구소", BuildingKind.ResearchLab, UnitRole.Flying), new Entry("과학 연구소", BuildingKind.ScienceLab),
                new Entry("방어 연구소", BuildingKind.DefenseLab) },
            new[] { new Entry("산성탑", BuildingKind.AcidTower), new Entry("광역 산성탑", BuildingKind.AreaAcidTower), new Entry("감시탑", BuildingKind.Watchtower),
                new Entry("흙벽", BuildingKind.SoilWall), new Entry("함정", BuildingKind.TrapPit), new Entry("지뢰밭", BuildingKind.MineField) },
            new[] { new Entry("정찰 초소", BuildingKind.ScoutPost), new Entry("포로 수용소", BuildingKind.PrisonerCamp), new Entry("의무실", BuildingKind.Infirmary),
                new Entry("휴게실", BuildingKind.RestRoom), new Entry("공방", BuildingKind.Workshop), new Entry("비행선 조선소", BuildingKind.AirshipYard) }
        };

        private static BuildScreen instance;
        private BuildingPlacementController placement;
        private RectTransform buildPanel, picker, placeBar;
        private readonly List<GameObject> tabPages = new List<GameObject>();
        private readonly List<Button> tabButtons = new List<Button>();
        private readonly List<Button> pickRows = new List<Button>();
        private Text pickTitle, placeText;
        private int tab;
        private Entry? picking;

        public static bool IsOpen => instance != null && instance.buildPanel != null && instance.buildPanel.gameObject.activeSelf;

        public static bool Picking => IsOpen && instance.picking != null;

        public static void Open() { if (instance != null) instance.SetOpen(true); }
        public static void Toggle() { if (instance != null) instance.SetOpen(!IsOpen); }

        // Esc: 장수 고르기 → 건물 고르기 → 닫기 순으로 한 단계씩 물러난다. 처리했으면 true.
        public static bool Back()
        {
            if (!IsOpen) return false;
            if (instance.picking != null) instance.picking = null; else instance.SetOpen(false);
            return true;
        }

        private void Awake() => instance = this;
        private void OnDestroy() { if (instance == this) instance = null; }

        private void Start()
        {
            placement = FindFirstObjectByType<BuildingPlacementController>();
            buildPanel = MenuTheme.Rect("BuildPanel", HudConsole.Right);
            MenuTheme.Stretch(buildPanel); buildPanel.offsetMin = new Vector2(18, 10); buildPanel.offsetMax = new Vector2(-18, -10);
            for (var t = 0; t < Tabs.Length; t++)
            {
                var captured = t;
                tabButtons.Add(Cell(buildPanel, "Tab " + TabNames[t], new Vector2(t * 68, 0), new Vector2(64, 26), (t + 1).ToString(), TabNames[t], () => tab = captured));
                tabButtons[t].gameObject.AddComponent<MenuTooltip>().Message = $"{TabNames[t]} 건물 보기 ({t + 1})";
                var page = MenuTheme.Rect("Page " + TabNames[t], buildPanel);
                MenuTheme.Stretch(page); page.offsetMax = new Vector2(0, -32);
                for (var i = 0; i < Tabs[t].Length; i++)
                {
                    var entry = Tabs[t][i];
                    var button = Cell(page, ButtonName(entry), new Vector2(i % 5 * 68, -(i / 5) * 60), new Vector2(64, 56),
                        SlotKeys[i].ToString(), entry.name, () => Choose(entry));
                    button.gameObject.AddComponent<MenuTooltip>();
                }
                tabPages.Add(page.gameObject);
            }

            picker = MenuTheme.Rect("BuilderPicker", HudConsole.Center);
            MenuTheme.Stretch(picker); picker.offsetMin = new Vector2(16, 10); picker.offsetMax = new Vector2(-16, -10);
            pickTitle = Text(picker, "", 14, new Vector2(0, 0), new Vector2(800, 40));
            for (var i = 0; i < 4; i++)
            {
                var index = i;
                var row = Cell(picker, "Builder " + i, new Vector2(0, -46 - i * 34), new Vector2(800, 30), (i + 1).ToString(), "", () => Assign(index));
                row.gameObject.AddComponent<MenuTooltip>().Message = "이 장수에게 건설을 맡기고 배치할 곳을 고릅니다.";
                pickRows.Add(row);
            }

            placeBar = MenuTheme.Panel(transform, "PlacementBar", new Vector2(.5f, 1), new Vector2(520, 34), new Vector2(0, -48));
            placeText = Text(placeBar, "", 14, new Vector2(12, -2), new Vector2(500, 30));
            SetOpen(false);
        }

        private void SetOpen(bool open)
        {
            picking = null;
            buildPanel.gameObject.SetActive(open);
            if (open) placement?.CancelPlacement();
        }

        private void Update()
        {
            var open = IsOpen;
            for (var t = 0; t < tabPages.Count; t++)
            {
                tabPages[t].SetActive(t == tab);
                tabButtons[t].GetComponentsInChildren<Text>()[1].color = t == tab ? MenuTheme.Accent : MenuTheme.TextColor;
            }
            if (open) RefreshPage();
            picker.gameObject.SetActive(open && picking != null);
            if (open && picking != null) RefreshPicker(picking.Value);

            var placing = placement != null && placement.IsPlacing;
            placeBar.gameObject.SetActive(placing);
            if (placing)
                placeText.text = $"<b>{NameOf(placement.PendingKind, placement.PendingRole)} 배치</b>   ·   Esc 취소   ·   담당 장수: <color=#f2a93b>{(placement.Builder as CommanderAnt)?.CommanderName}</color>";
        }

        // 게임 단축키 대신 건설 화면 키를 읽는다(GameHotkeys가 열린 동안 넘겨준다).
        public static void HandleKeys()
        {
            var keyboard = Keyboard.current;
            if (instance == null || keyboard == null) return;
            for (var t = 0; t < Tabs.Length; t++)
                if (keyboard[Key.Digit1 + t].wasPressedThisFrame)
                {
                    if (instance.picking != null) instance.Assign(t); else instance.tab = t;
                }
            if (instance.picking != null) return;
            var entries = Tabs[instance.tab];
            for (var i = 0; i < entries.Length; i++)
                if (keyboard[SlotKeys[i]].wasPressedThisFrame) instance.Choose(entries[i]);
        }

        private void Choose(Entry entry)
        {
            var locked = BuildingPlacementController.LockReason(entry.kind);
            if (locked != null) { ToastManager.Show(locked); return; }
            picking = entry;
        }

        private void Assign(int index)
        {
            var builders = Builders();
            if (picking == null || index >= builders.Count) return;
            if (placement != null && placement.BeginPlacement(picking.Value.kind, picking.Value.role, builders[index])) SetOpen(false);
        }

        private static List<CommanderAnt> Builders() => CommanderRoster.Instance == null ? new List<CommanderAnt>()
            : CommanderRoster.Instance.Commanders.Where(c => c != null && c.CanStartConstruction)
                .OrderByDescending(c => c.Talents.Level(CommanderActivity.Building)).ToList();

        private void RefreshPage()
        {
            var entries = Tabs[tab];
            var buttons = tabPages[tab].GetComponentsInChildren<Button>(true);
            for (var i = 0; i < entries.Length; i++)
            {
                var data = Data(entries[i]);
                var locked = BuildingPlacementController.LockReason(entries[i].kind);
                var label = buttons[i].GetComponentsInChildren<Text>()[1];
                label.text = entries[i].name + (locked != null ? "\n<color=#968976>잠김</color>" : data != null ? $"\n<color=#968976>{Cost(data)}</color>" : "");
                buttons[i].GetComponent<Image>().color = locked != null ? MenuTheme.Well : Color.white;
                buttons[i].GetComponent<MenuTooltip>().Message = locked ?? (data != null
                    ? $"{entries[i].name}: 식량 {data.foodCost} · 흙 {data.soilCost}{(data.specialCost > 0 ? $" · 특수 {data.specialCost}" : "")} · 건설 개미 {data.constructionAnts}"
                    : "건물 틀을 찾을 수 없습니다.");
            }
        }

        private void RefreshPicker(Entry entry)
        {
            var builders = Builders();
            var data = Data(entry);
            pickTitle.text = $"<b><color=#f2a93b>누구에게 맡길까요?</color></b>   {entry.name}  <color=#968976>{(data != null ? Cost(data) : "")} · 대기 {builders.Count}명 · 건설 기술이 높을수록 빨리 완공</color>";
            for (var i = 0; i < pickRows.Count; i++)
            {
                var row = pickRows[i];
                row.gameObject.SetActive(i < builders.Count || (i == 0 && builders.Count == 0));
                var label = row.GetComponentsInChildren<Text>()[1];
                if (i >= builders.Count)
                {
                    row.interactable = false;
                    label.text = "둥지에 병력을 가진 대기 장수가 없습니다.";
                    continue;
                }
                var c = builders[i];
                row.interactable = true;
                label.text = $"<b>{c.CommanderName}</b>  <color=#968976>{CommandCard.RoleName(c.Role)}</color>    건설 <b>{c.Talents.Level(CommanderActivity.Building)}</b>    병력 {c.TroopCount}/{c.CommandLimit}    기분 {c.Mood:0}";
            }
        }

        private static BuildingData Data(Entry entry) => BuildingPlacementController.GetTemplate(entry.kind, entry.role)?.GetComponent<BuildingBase>()?.Data;
        private static string Cost(BuildingData data) => $"식{data.foodCost} 흙{data.soilCost}{(data.specialCost > 0 ? $" 특{data.specialCost}" : "")}";
        private static string ButtonName(Entry entry) => "Build " + entry.kind + (entry.kind == BuildingKind.Barracks || entry.kind == BuildingKind.ResearchLab ? " " + entry.role : "");
        private static string NameOf(BuildingKind kind, UnitRole role)
        {
            foreach (var page in Tabs) foreach (var entry in page) if (entry.kind == kind && (entry.role == role || entry.role == UnitRole.Worker)) return entry.name;
            return kind.ToString();
        }

        private static Button Cell(Transform parent, string name, Vector2 position, Vector2 size, string key, string label, UnityEngine.Events.UnityAction action)
        {
            var rect = MenuTheme.Rect(name, parent);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position; rect.sizeDelta = size;
            rect.gameObject.AddComponent<Image>();
            rect.gameObject.AddComponent<Outline>().effectColor = MenuTheme.Line2;
            var button = rect.gameObject.AddComponent<Button>();
            MenuTheme.StyleButton(button);
            button.onClick.AddListener(action);
            var kbd = Text(rect, key, 10, new Vector2(4, -2), new Vector2(20, 14));
            kbd.color = MenuTheme.Dim;
            var tall = size.y > 30;
            var text = Text(rect, label, 11, tall ? Vector2.zero : new Vector2(24, 0), tall ? new Vector2(size.x, size.y - 4) : new Vector2(size.x - 28, size.y));
            text.alignment = tall ? TextAnchor.LowerCenter : size.x > 100 ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;
            return button;
        }

        private static Text Text(Transform parent, string value, int size, Vector2 position, Vector2 box)
        {
            var text = MenuTheme.Text(parent, value, size);
            var rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position; rect.sizeDelta = box;
            text.supportRichText = true;
            return text;
        }
    }
}
