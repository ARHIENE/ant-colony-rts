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
            TitleBackdrop.Ensure(panel); // 시작 메뉴는 게임 장면 대신 전용 배경을 보여준다.
            var side = L.Plate(f, "MainSide", 0, 0, 440, 900);
            side.anchorMin = Vector2.zero; side.anchorMax = new Vector2(0, 1); side.sizeDelta = new Vector2(440, 0); side.anchoredPosition = Vector2.zero;
            var logo = L.Well(side, "Logo", 48, 88, 344, 132);
            L.Label(logo, "소굴 RTS", 28, 18, 30, 320, 44, bold: true);
            L.Label(logo, "가제 · 정착지 경영과 장수 부대 전투", 12, 18, 78, 320, 20, MenuTheme.Dim);
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

        private static SaveSlots.SlotInfo LatestSave() => SaveSlots.List().Where(s => s.Valid && (!s.Auto || s.PlaySeconds >= 60))
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
            if (Instance != null) TitleBackdrop.Ensure(Instance.panel, loading: true); // 불러오는 동안 게임 장면을 가린다.
            ToastManager.Show(SaveSystem.TryLoad(slot.Path, out var error) ? "저장 파일을 불러오는 중…" : error);
        }

        private void NewGameScreen()
        {
            var f = Frame("New Game", .5f);
            TitleBackdrop.Ensure(panel);
            DrawCrashStart(f);
        }

        public void Pause()
        {
            if (CampaignResearch.Instance != null && CampaignResearch.Instance.Departed) { ShowDeparture(); return; }
            if (GameManager.Instance != null && GameManager.Instance.SavedDefeat) { ShowOutcome(false); return; }
            var f = Frame("Paused", .45f);
            var pm = L.Plate(f, "PauseMenu", 400, 150, 280, 452);
            var tab = L.Well(pm, "PauseTab", 12, 10, 256, 52);
            L.Label(tab, "게임 일시정지", 14, 10, 4, 236, 22, bold: true);
            L.Label(tab, HudClock.CalendarLabel +" · 게임 시간이 멈춤", 12, 10, 26, 236, 20, MenuTheme.Muted);
            var y = 72f;
            void Item(string name, string label, Action action, string tip = null) { L.Button(pm, name, label, 12, y, 256, 40, action, tip, false, 14, TextAnchor.MiddleLeft); y += 44; }
            Item("Continue", "계속하기   Esc", Resume);
            Item("Save Game", "저장하기", () => { pauseLoads = false; Pause(); });
            Item("Load Game", "불러오기", () => { pauseLoads = true; Pause(); });
            Item("Settings", "설정", Settings);
            Item("Encyclopedia", "도감", Book);
            Item("How to Play [F2]", "설명서  F2", Guide);
            Item("Save & Main Menu", "메인 메뉴로", () => {
                // 1분도 안 한 판은 저장하지 않는다(시작만 하고 나와도 '이어하기'가 생기던 문제, 2026-10-25).
                if (GameSession.Instance.PlaySeconds >= 60 && !SaveSystem.TrySave(true, 0, out var error)) { ToastManager.Show("저장하지 못해 메인 메뉴로 돌아갈 수 없습니다: " + error); return; }
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
                    ToastManager.Show(ok ? (save ? "저장했습니다." : "저장 파일을 불러오는 중…") : error);
                    if (save && open && ScreenName == "Paused") Pause();
                });
                button.interactable = save || slot.Valid;
            }
        }

        private void Slots(bool save)
        {
            var f = Frame(save ? "Save Game" : "Load Game");
            TitleBackdrop.Ensure(panel);
            SlotPanel(f, 440, 150, save);
            L.Button(f, "Back", "뒤로   Esc", 440, 150 + 60 + SaveSlots.List().Count(s => !save || !s.Auto) * 72 + 8, 110, 38, Back);
        }
    }
}
