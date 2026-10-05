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
    // 건설(B) 화면: 오른쪽 날개 = 분류 탭 16종(2줄, 숫자키 1~0) + 건물 칸(QWERT/ASDFG), 가운데 = 맡길 장수 고르기, 배치 중에는 상단 안내 막대.
    public sealed class BuildScreen : MonoBehaviour
    {
        private readonly struct Entry
        {
            public readonly string name; public readonly BuildingKind kind; public readonly UnitRole role;
            public Entry(string name, BuildingKind kind, UnitRole role = UnitRole.Worker) { this.name = name; this.kind = kind; this.role = role; }
        }

        // 건설 분류 16종(2026-10-05). 탭 이름 2~3글자라 '이동수단'은 '이동'. 빈 분류는 가구가 생기면 채운다.
        private static readonly string[] TabNames = { "타일", "생활", "저장", "환경", "전력", "자동화", "배관", "식량", "작업", "의료", "방어", "휴게", "장식", "군사", "마을", "이동" };
        private const int TabsPerRow = 8;
        private static readonly Key[] SlotKeys = { Key.Q, Key.W, Key.E, Key.R, Key.T, Key.A, Key.S, Key.D, Key.F, Key.G };
        private static readonly Entry[][] Tabs =
        {
            new[] { new Entry("흙벽", BuildingKind.SoilWall), new Entry("나뭇잎 벽", BuildingKind.LeafWall), new Entry("병뚜껑 벽", BuildingKind.CapWall),
                new Entry("문", BuildingKind.Door) },
            new[] { new Entry("숙소", BuildingKind.Dormitory), new Entry("식당", BuildingKind.Kitchen), new Entry("양육실", BuildingKind.Nursery),
                new Entry("화장실", BuildingKind.Toilet), new Entry("세면대", BuildingKind.Washbasin), new Entry("샤워기", BuildingKind.Shower) },
            new[] { new Entry("저장고", BuildingKind.Storage) },
            new[] { new Entry("전등", BuildingKind.ElectricLamp) }, // 환경: TODO 화롯불·등불·난로·환풍구
            new[] { new Entry("쳇바퀴", BuildingKind.Treadmill), new Entry("장작 발전기", BuildingKind.WoodGenerator), new Entry("전선", BuildingKind.PowerWire),
                new Entry("배터리", BuildingKind.Battery) },
            new Entry[0], // 자동화: TODO 센서·논리(신호선)
            new Entry[0], // 배관: TODO 배관·펌프·밸브·액체 탱크
            new[] { new Entry("밭", BuildingKind.Farm) },
            new[] { new Entry("큰턱 연구소", BuildingKind.ResearchLab, UnitRole.Melee), new Entry("산샘 연구소", BuildingKind.ResearchLab, UnitRole.Ranged),
                new Entry("갑각 연구소", BuildingKind.ResearchLab, UnitRole.Defense), new Entry("페로몬 연구소", BuildingKind.ResearchLab, UnitRole.Support),
                new Entry("날개 연구소", BuildingKind.ResearchLab, UnitRole.Flying), new Entry("과학 연구소", BuildingKind.ScienceLab),
                new Entry("방어 연구소", BuildingKind.DefenseLab), new Entry("공방", BuildingKind.Workshop) },
            new[] { new Entry("의무실", BuildingKind.Infirmary) },
            new[] { new Entry("성벽", BuildingKind.CastleWall), new Entry("성문", BuildingKind.Gate), new Entry("함정", BuildingKind.TrapPit),
                new Entry("지뢰밭", BuildingKind.MineField), new Entry("산성탑", BuildingKind.AcidTower), new Entry("광역 산성탑", BuildingKind.AreaAcidTower),
                new Entry("감시탑", BuildingKind.Watchtower) },
            new[] { new Entry("휴게실", BuildingKind.RestRoom), new Entry("이야기 모닥불", BuildingKind.Campfire), new Entry("도박장", BuildingKind.GamblingDen) },
            new[] { new Entry("꽃 화분", BuildingKind.FlowerPot), new Entry("조개껍데기", BuildingKind.ShellDecoration), new Entry("구슬 모자이크", BuildingKind.MarbleMosaic),
                new Entry("병뚜껑 모빌", BuildingKind.BottleMobile), new Entry("반딧불 램프", BuildingKind.FireflyLamp) },
            new[] { new Entry("큰턱 훈련장", BuildingKind.Barracks, UnitRole.Melee), new Entry("산샘 훈련장", BuildingKind.Barracks, UnitRole.Ranged),
                new Entry("갑각 훈련장", BuildingKind.Barracks, UnitRole.Defense), new Entry("페로몬 훈련장", BuildingKind.Barracks, UnitRole.Support),
                new Entry("날개 훈련장", BuildingKind.Barracks, UnitRole.Flying), new Entry("징집소", BuildingKind.ConscriptionPost),
                new Entry("정찰 초소", BuildingKind.ScoutPost), new Entry("포로 수용소", BuildingKind.PrisonerCamp) },
            new[] { new Entry("초가집", BuildingKind.Hut), new Entry("흙집", BuildingKind.House), new Entry("큰 아파트", BuildingKind.Apartment) },
            new[] { new Entry("로켓 발사대", BuildingKind.AirshipYard) }
        };
        private static int TabOf(string name) => System.Array.IndexOf(TabNames, name);

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
        public static void OpenDormitory()
        {
            if (instance == null) return;
            instance.SetOpen(true); instance.tab = TabOf("생활");
            instance.Choose(Tabs[instance.tab][0]);
        }
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
            MenuTheme.Stretch(buildPanel); buildPanel.offsetMin = new Vector2(14, 10); buildPanel.offsetMax = new Vector2(-14, -HudConsole.WingTop);
            for (var t = 0; t < Tabs.Length; t++)
            {
                var captured = t;
                tabButtons.Add(Cell(buildPanel, "Tab " + TabNames[t], new Vector2(t * 36, 0), new Vector2(34, 26), (t + 1).ToString(), TabNames[t], () => tab = captured));
                tabButtons[t].gameObject.AddComponent<MenuTooltip>().Message = $"{(TabNames[t] == "이동" ? "이동수단" : TabNames[t])} 건물 보기{(t < 10 ? $" ({(t + 1) % 10})" : "")}";
                var page = MenuTheme.Rect("Page " + TabNames[t], buildPanel);
                MenuTheme.Stretch(page); page.offsetMax = new Vector2(0, -60);
                for (var i = 0; i < Tabs[t].Length; i++)
                {
                    var entry = Tabs[t][i];
                    var button = Cell(page, ButtonName(entry), new Vector2(i % 4 * 70, -(i / 4) * 48), new Vector2(66, 44),
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
            var width = buildPanel.rect.width;
            for (var i = 0; i < tabButtons.Count; i++)
                MenuLayout.Place((RectTransform)tabButtons[i].transform, i % TabsPerRow * width / TabsPerRow, i / TabsPerRow * 28, width / TabsPerRow - 2, 26);
            foreach (var page in tabPages)
            {
                var buttons = page.GetComponentsInChildren<Button>(true);
                for (var i = 0; i < buttons.Length; i++)
                {
                    MenuLayout.Place((RectTransform)buttons[i].transform, i % 3 * width / 3, i / 3 * 52, width / 3 - 4, 48);
                    MenuTheme.Stretch(buttons[i].GetComponentsInChildren<Text>(true)[1].rectTransform);
                }
            }
            pickTitle.rectTransform.sizeDelta = new Vector2(picker.rect.width, 40);
            foreach (var row in pickRows)
            {
                ((RectTransform)row.transform).sizeDelta = new Vector2(picker.rect.width, 30);
                MenuTheme.Stretch(row.GetComponentsInChildren<Text>(true)[1].rectTransform);
            }
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
            for (var t = 0; t < Mathf.Min(Tabs.Length, 10); t++) // 숫자키 1~0 = 앞 10개 탭
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
                    ? $"{entries[i].name}: 식량 {data.foodCost} · 재료 {data.soilCost}{(data.specialCost > 0 ? $" · 특수 {data.specialCost}" : "")} · 건설 개미 {data.constructionAnts}"
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
                    label.text = "둥지에 건설 가능한 대기 장수가 없습니다.";
                    continue;
                }
                var c = builders[i];
                row.interactable = true;
                label.text = $"<b>{c.CommanderName}</b>  <color=#968976>{CommandCard.RoleName(c.Role)}</color>    건설 <b>{c.Talents.Level(CommanderActivity.Building)}</b>    병력 {c.TroopCount}/{c.CommandLimit}    기분 {c.Mood:0}";
            }
        }

        private static BuildingData Data(Entry entry) => BuildingPlacementController.GetTemplate(entry.kind, entry.role)?.GetComponent<BuildingBase>()?.Data;
        private static string Cost(BuildingData data) => $"식{data.foodCost} 재료{data.soilCost}{(data.specialCost > 0 ? $" 특{data.specialCost}" : "")}";
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
            kbd.color = MenuTheme.Dim; kbd.enabled = false; // 단축키 전체 미정(HUD v3): 키 글자 숨김, 텍스트 순서는 유지.
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
