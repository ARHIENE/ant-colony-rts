using System;
using System.Linq;
using AntColony.Core;
using AntColony.Save;
using UnityEngine;
using UnityEngine.UI;
using L = AntColony.UI.MenuLayout;

namespace AntColony.UI
{
    // 디자인 「메인 메뉴 · 새 게임 · 일시정지」 화면. 오브젝트 이름은 검사가 찾는 기존 키를 유지한다.
    public sealed partial class GameMenuController
    {
        private bool pauseLoads;

        public void Main()
        {
            var f = Frame("ANT COLONY", .35f);
            var side = L.Plate(f, "MainSide", 0, 0, 440, 900);
            side.anchorMin = Vector2.zero; side.anchorMax = new Vector2(0, 1); side.sizeDelta = new Vector2(440, 0); side.anchoredPosition = Vector2.zero;
            var logo = L.Well(side, "Logo", 48, 88, 344, 132);
            L.Label(logo, "로고 (자리 표시)", 11, 8, 4, 200, 16, MenuTheme.Muted);
            L.Label(logo, "개미 소굴 RTS", 28, 18, 34, 320, 44, bold: true);
            L.Label(logo, "(가제)", 12, 18, 80, 200, 20, MenuTheme.Dim);
            L.Line(side, 48, 252, 344);

            var latest = LatestSave();
            var go = L.Button(side, "Continue", latest != null
                    ? $"이어하기\n<size=12>{SlotName(latest)} · {SlotTime(latest)}</size>" : "이어하기\n<size=12>저장 없음</size>",
                48, 280, 344, 72, () => LoadSlot(latest), "가장 최근 저장을 불러옵니다.", true, 17, TextAnchor.MiddleLeft);
            go.interactable = latest != null;
            var y = 356f;
            void Item(string name, string label, Action action, string tip = null) { L.Button(side, name, label, 48, y, 344, 44, action, tip, false, 14, TextAnchor.MiddleLeft); y += 48; }
            Item("New Game", "새 게임", NewGameScreen, "맵 크기·침공 난이도·장수 사망·시드를 고릅니다.");
            Item("Continue / Load", "불러오기", () => Slots(false));
            y += 8;
            Item("Settings", "설정", Settings);
            Item("Encyclopedia", "도감", Book);
            Item("How to Play [F2]", "설명서  F2", Guide);
            y += 8;
            Item("Quit", "종료", Application.Quit);
            L.Label(side, "Enter 이어하기    Esc 뒤로", 12, 48, 726, 344, 20, MenuTheme.Muted);
            L.Line(side, 48, 754, 344);
            L.Label(side, "프로토타입 빌드", 11, 48, 762, 344, 20, MenuTheme.Dim);
        }

        private static SaveSlots.SlotInfo LatestSave() => SaveSlots.List().Where(s => s.Valid)
            .OrderByDescending(s => DateTime.TryParse(s.SavedAtUtc, out var t) ? t : DateTime.MinValue).FirstOrDefault();
        private static string SlotTime(SaveSlots.SlotInfo s) =>
            (DateTime.TryParse(s.SavedAtUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out var t) ? t.ToLocalTime().ToString("yyyy-MM-dd HH:mm") : "")
            + $" · 플레이 {Mathf.FloorToInt(s.PlaySeconds / 60f)}분";
        private static string SlotDesc(SaveSlots.SlotInfo s) => !s.Exists ? "빈 슬롯 · 새 저장을 만들 수 있음"
            : !s.Valid ? "손상됨: " + s.Error : SlotTime(s) + "\n" + s.Label;
        private static string SlotName(SaveSlots.SlotInfo s) => s.Auto ? $"자동 저장 {s.Index + 1}" : $"수동 {s.Index + 1}";
        private static void LoadSlot(SaveSlots.SlotInfo slot)
        {
            if (slot == null) return;
            ToastManager.Show(SaveSystem.TryLoad(slot.Path, out var error) ? "Loading save..." : error);
        }

        private void NewGameScreen()
        {
            var f = Frame("New Game", .5f);
            var p = L.Plate(f, "NewGamePanel", 160, 112, 1120, 664);
            L.Label(p, "새 게임", 19, 20, 0, 100, 56, bold: true);
            L.Label(p, "개미굴을 세울 땅과 규칙을 고르세요", 12, 110, 0, 500, 56, MenuTheme.Dim);
            L.Line(p, 0, 56, 1120);
            const float cw = 250f;
            void Legend(float y, string title, string small)
            {
                L.Label(p, $"<b>{title}</b>   <color=#968976><size=12>{small}</size></color>", 13, 20, y, 760, 22);
            }
            RectTransform Card(string name, int index, float y, bool selected, Action pick)
            {
                var button = L.Button(p, name, "", 20 + index * (cw + 8), y, cw, 100, () => { ReadSeed(); pick(); NewGameScreen(); });
                if (selected)
                {
                    button.GetComponent<Outline>().effectColor = MenuTheme.Accent;
                    var colors = button.colors; colors.normalColor = MenuTheme.Hex(0x2a2219); button.colors = colors;
                }
                return (RectTransform)button.transform;
            }

            Legend(72, "맵 크기", "로컬 개미굴 맵 한 변 길이");
            foreach (MapSize size in Enum.GetValues(typeof(MapSize)))
            {
                var scale = MapSizes.Scale(size); var captured = size;
                var c = Card("Map " + size, (int)size, 98, options.mapSize == size, () => options.mapSize = captured);
                L.Label(c, $"<b>{new[] { "소", "중", "대" }[(int)size]}</b>   <size=20>{400 * scale:0}</size><color=#968976>m</color>{(size == MapSize.Medium ? "   <size=11><color=#b8ac9a>권장</color></size>" : "")}", 15, 12, 8, 226, 28);
                L.Label(c, new[] { "빠른 판 · 자원과 적이 가까움", "확장과 방어의 균형", "긴 캠페인 · 탐색과 원정 거리 큼" }[(int)size], 12, 12, 40, 226, 20, MenuTheme.Muted);
                L.Meter(c, 12, 78, 160, 6, scale / 1.6f, MenuTheme.Hex(0xd8ccb8));
                L.Label(c, $"한 변 {400 * scale:0}m", 12, 180, 70, 60, 22, MenuTheme.Muted);
            }

            Legend(216, "침공 난이도", "침공 빈도와 규모");
            foreach (DifficultyLevel level in Enum.GetValues(typeof(DifficultyLevel)))
            {
                var captured = level;
                var c = Card("Difficulty " + level, (int)level, 242, options.difficulty == level, () => options.difficulty = captured);
                L.Label(c, $"<b>{new[] { "온화", "보통", "가혹" }[(int)level]}</b>", 15, 12, 8, 226, 28);
                L.Label(c, "침공 빈도   " + new string('■', (int)level + 1) + new string('□', 2 - (int)level), 12, 12, 40, 226, 20, MenuTheme.Muted);
                L.Label(c, $"침공 간격 ×{DifficultyProfile.IntervalScale(level):0.##} · 규모 ×{DifficultyProfile.StrengthScale(level):0.##}", 12, 12, 66, 226, 22, MenuTheme.Dim);
            }

            Legend(360, "장수 사망", "전투에서 쓰러진 장수가 죽을 수 있는지");
            foreach (CommanderDeathMode mode in Enum.GetValues(typeof(CommanderDeathMode)))
            {
                var captured = mode;
                var c = Card("Death " + mode, (int)mode, 386, options.commanderDeath == mode, () => options.commanderDeath = captured);
                L.Label(c, $"<b>{new[] { "관대", "보통", "가혹" }[(int)mode]}</b>", 15, 12, 8, 226, 28);
                L.Label(c, new[] { "사망 없음", "중상에서 다시 쓰러지면", "쓰러질 때마다" }[(int)mode], 15, 12, 38, 226, 24, bold: true);
                L.Label(c, new[] { "쓰러져도 부상으로만 남음", "사망할 수 있음", "사망할 수 있음" }[(int)mode], 12, 12, 66, 226, 22, MenuTheme.Muted);
            }

            Legend(504, "맵 시드", "같은 시드와 맵 크기면 같은 지형이 만들어짐");
            seed = MenuTheme.Input(p, options.seed.ToString());
            L.Place((RectTransform)seed.transform, 20, 530, 300, 34);
            seed.textComponent.fontSize = 17; seed.textComponent.font = MenuTheme.NumberFont;
            L.Button(p, "Random Seed", "무작위", 328, 530, 90, 34, () => { options.seed = NewGameOptions.RandomSeed(); seed.text = options.seed.ToString(); });
            L.Label(p, "정수 (-2147483648 ~ 2147483647)", 12, 20, 568, 500, 20, MenuTheme.Dim);

            var sum = L.Box(p, "Summary", 808, 57, 312, 542, MenuTheme.Hex(0x17130f));
            L.Label(sum, "선택한 설정", 13, 20, 12, 272, 22, bold: true);
            var preview = L.Well(sum, "Preview", 20, 44, 272, 132);
            L.Label(preview, "지형 미리보기 (자리 표시)", 11, 8, 104, 200, 22, MenuTheme.Dim);
            var extent = 400 * MapSizes.Scale(options.mapSize);
            L.Label(preview, $"{extent:0} × {extent:0}m", 12, 150, 104, 114, 22, MenuTheme.Muted, TextAnchor.MiddleRight);
            string[][] rows =
            {
                new[] { "맵 크기", new[] { "소", "중", "대" }[(int)options.mapSize] + $"  <color=#b8ac9a><size=12>{extent:0}m</size></color>" },
                new[] { "침공 난이도", new[] { "온화", "보통", "가혹" }[(int)options.difficulty] + $"  <color=#b8ac9a><size=12>간격 ×{DifficultyProfile.IntervalScale(options.difficulty):0.##} · 규모 ×{DifficultyProfile.StrengthScale(options.difficulty):0.##}</size></color>" },
                new[] { "장수 사망", new[] { "관대", "보통", "가혹" }[(int)options.commanderDeath] },
                new[] { "맵 시드", options.seed.ToString() }
            };
            for (var i = 0; i < rows.Length; i++)
            {
                var y = 188 + i * 54;
                L.Line(sum, 20, y, 272);
                L.Label(sum, rows[i][0], 12, 20, y + 6, 272, 18, MenuTheme.Dim);
                L.Label(sum, rows[i][1], 13, 20, y + 24, 272, 24, bold: true);
            }

            L.Line(p, 0, 600, 1120);
            L.Button(p, "Back", "뒤로   Esc", 20, 613, 110, 38, Main);
            L.Button(p, "Start Game", "게임 시작   Enter", 900, 613, 200, 38,
                () => { if (!ReadSeed()) return; try { SaveSystem.NewGame(options); } catch (Exception e) { ToastManager.Show(e.Message); } }, null, true);
        }

        public void Pause()
        {
            if (CampaignResearch.Instance != null && CampaignResearch.Instance.Departed) { ShowDeparture(); return; }
            if (GameManager.Instance != null && GameManager.Instance.SavedDefeat) { ShowOutcome(false); return; }
            var f = Frame("Paused", .45f);
            var pm = L.Plate(f, "PauseMenu", 400, 150, 280, 452);
            var tab = L.Well(pm, "PauseTab", 12, 10, 256, 52);
            L.Label(tab, "게임 일시정지", 14, 10, 4, 236, 22, bold: true);
            L.Label(tab, CalendarLabel + " · 게임 시간이 멈춤", 12, 10, 26, 236, 20, MenuTheme.Muted);
            var y = 72f;
            void Item(string name, string label, Action action, string tip = null) { L.Button(pm, name, label, 12, y, 256, 40, action, tip, false, 14, TextAnchor.MiddleLeft); y += 44; }
            Item("Continue", "계속하기   Esc", Resume);
            Item("Save Game", "저장하기", () => { pauseLoads = false; Pause(); });
            Item("Load Game", "불러오기", () => { pauseLoads = true; Pause(); });
            Item("Settings", "설정", Settings);
            Item("Encyclopedia", "도감", Book);
            Item("How to Play [F2]", "설명서  F2", Guide);
            Item("Save & Main Menu", "메인 메뉴로", () => {
                if (!SaveSystem.TrySave(true, 0, out var error)) { ToastManager.Show("Cannot leave safely: " + error); return; }
                GameSession.Instance.MarkNotStarted(); Main();
            }, "먼저 자동 슬롯에 저장합니다. 저장에 실패하면 게임은 그대로 열려 있습니다.");
            L.Line(pm, 12, 402, 256);
            var auto = SaveSlots.Inspect(true, 0);
            L.Label(pm, "마지막 자동 저장 · " + (auto.Valid ? SlotTime(auto) : "없음"), 11, 12, 408, 256, 36, MenuTheme.Dim);
            SlotPanel(f, 692, 150, !pauseLoads);
        }

        // 저장/불러오기 슬롯 목록. 버튼 이름("Save Slot 1" 등)은 옛 화면과 같다.
        private void SlotPanel(Transform parent, float x, float y, bool save)
        {
            var slots = SaveSlots.List().Where(s => !save || !s.Auto).OrderByDescending(s => s.Auto).ToList();
            var panelRect = L.Plate(parent, "SlotPanel", x, y, 560, 60 + slots.Count * 72);
            L.Label(panelRect, save ? "<b>저장하기</b>" : "<b>불러오기</b>", 15, 14, 8, 200, 28);
            L.Label(panelRect, $"자동 {SaveSlots.AutoSlotCount} · 수동 {SaveSlots.ManualSlotCount} · 덮어쓰면 이전 파일은 .bak로 보관", 12, 110, 8, 436, 28, MenuTheme.Dim);
            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                var row = L.Box(panelRect, "Slot " + slot.DisplayName, 14, 44 + i * 72, 532, 66, MenuTheme.Plate2, true);
                var thumb = L.Well(row, "Preview", 6, 6, 96, 54);
                L.Label(thumb, "미리보기", 11, 0, 0, 96, 54, MenuTheme.Dim, TextAnchor.MiddleCenter);
                L.Label(row, $"<b>{SlotName(slot)}</b>", 14, 114, 6, 300, 24);
                L.Label(row, SlotDesc(slot), 12, 114, 28, 300, 34, MenuTheme.Muted);
                var button = L.Button(row, (save ? "Save " : "Load ") + slot.DisplayName, save ? "저장" : "불러오기", 426, 16, 96, 34, () => {
                    var ok = save ? SaveSystem.TrySave(slot.Auto, slot.Index, out var error) : SaveSystem.TryLoad(slot.Path, out error);
                    ToastManager.Show(ok ? (save ? "Saved." : "Loading save...") : error);
                    if (save && open && ScreenName == "Paused") Pause();
                });
                button.interactable = save || slot.Valid;
            }
        }

        private void Slots(bool save)
        {
            var f = Frame(save ? "Save Game" : "Load Game");
            SlotPanel(f, 440, 150, save);
            L.Button(f, "Back", "뒤로   Esc", 440, 150 + 60 + SaveSlots.List().Count(s => !save || !s.Auto) * 72 + 8, 110, 38, Back);
        }
    }
}
