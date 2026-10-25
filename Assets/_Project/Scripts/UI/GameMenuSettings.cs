using System;
using System.Linq;
using AntColony.Core;
using UnityEngine;
using UnityEngine.UI;
using L = AntColony.UI.MenuLayout;

namespace AntColony.UI
{
    // 설정 화면(2026-10-25 재구성): 화면·소리·조작·게임·정보 탭. 실제로 동작하는 항목만 둔다.
    public sealed partial class GameMenuController
    {
        private int settingsTab;
        private float settingsScroll = 1; // 값을 바꿔 다시 그려도 목록 위치 유지(맨 위로 튀어 누른 게 안 된 것처럼 보이던 문제)
        private static readonly string[] SettingsTabs = { "화면", "소리", "조작", "게임", "정보" };

        private void Settings()
        {
            var f = Frame("Settings");
            var p = L.Plate(f, "SettingsPanel", 220, 70, 1000, 760);
            L.Label(p, "설정", 20, 24, 10, 300, 38, bold: true);
            for (var i = 0; i < SettingsTabs.Length; i++)
            {
                var tab = i;
                var b = L.Button(p, "Settings Tab " + i, SettingsTabs[i], 24 + i * 116, 60, 110, 36, () => { settingsTab = tab; rebinding = null; settingsScroll = 1; Settings(); });
                if (i == settingsTab) MarkSelected(b);
            }
            L.Line(p, 0, 106, 1000);
            var list = L.List(p, 24, 118, 952, 560);
            var s = UserSettings.Current;
            void Commit(UserSettingsData v) { UserSettings.Apply(v); v = UserSettings.Current;
                SceneObjects.Any<AntColony.Camera.IsometricCameraController>()?.ApplySettings(v.cameraPanSpeed, v.edgeScrollThickness); Settings(); }
            void Apply(Action<UserSettingsData> change) { var v = s.Clone(); change(v); Commit(v); }
            RectTransform Row(string title, string note = null)
            {
                var row = L.Cell(list, "Setting " + title, 48);
                L.Label(row, title, 15, 12, 0, 300, note == null ? 48 : 30, bold: true);
                if (note != null) L.Label(row, note, 11, 12, 26, 320, 20, MenuTheme.Muted);
                return row;
            }
            // 고정 선택지 줄: 지금 값은 MarkSelected로 칠한다.
            void Options<T>(string title, T[] values, Func<T, string> label, T current, Action<UserSettingsData, T> set, string note = null)
            {
                var row = Row(title, note);
                var w = Mathf.Min(140, 600f / values.Length - 6);
                for (var i = 0; i < values.Length; i++)
                {
                    var value = values[i];
                    var b = L.Button(row, title + " " + i, label(value), 340 + i * (w + 6), 7, w, 34, () => Apply(v => set(v, value)));
                    if (Equals(value, current)) MarkSelected(b);
                }
            }
            // 이전·다음으로 넘기는 줄(선택지가 많은 해상도·속도).
            void Stepper(string title, string value, Action prev, Action next, string note = null)
            {
                var row = Row(title, note);
                L.Button(row, title + " Prev", "◀", 340, 7, 44, 34, prev);
                L.Label(row, value, 15, 390, 0, 200, 48, align: TextAnchor.MiddleCenter);
                L.Button(row, title + " Next", "▶", 596, 7, 44, 34, next);
            }
            switch (settingsTab)
            {
                case 0:
                    Options("화면 모드", new[] { 0, 1, 2 }, m => new[] { "전체 화면(창)", "창 모드", "전체 화면" }[m], s.screenMode, (v, m) => v.screenMode = m,
                        "전체 화면(창) = 테두리 없는 창");
                    var res = UnityEngine.Screen.resolutions.Select(r => (r.width, r.height)).Distinct().OrderBy(r => r.width * 10000 + r.height).ToList();
                    var cur = res.FindIndex(r => r.width == s.resolutionWidth && r.height == s.resolutionHeight);
                    // '모니터 기본'이면 지금 모니터 해상도에서 한 칸 움직인다(전엔 양쪽 다 최대 해상도로 갔다).
                    var from = cur >= 0 ? cur : res.FindIndex(r => r.width == UnityEngine.Screen.currentResolution.width && r.height == UnityEngine.Screen.currentResolution.height);
                    if (from < 0) from = res.Count - 1;
                    void Res(int d) => Apply(v => { var i = Mathf.Clamp(from + d, 0, res.Count - 1); if (res.Count > 0) { v.resolutionWidth = res[i].width; v.resolutionHeight = res[i].height; } });
                    Stepper("해상도", s.resolutionWidth > 0 ? $"{s.resolutionWidth} × {s.resolutionHeight}" : "모니터 기본", () => Res(-1), () => Res(1),
                        Application.isEditor ? "에디터에서는 게임 창 크기를 따릅니다" : null);
                    Options("수직 동기화", new[] { true, false }, b => b ? "켜기" : "끄기", s.vSync, (v, b) => v.vSync = b, "화면 찢어짐 방지");
                    Options("최대 프레임", new[] { 30, 60, 120, 0 }, n => n == 0 ? "제한 없음" : n + "", s.frameLimit, (v, n) => v.frameLimit = n,
                        s.vSync ? "수직 동기화를 끄면 적용됩니다" : "노트북 발열이 크면 낮추세요");
                    var names = QualitySettings.names;
                    Options("그래픽 품질", Enumerable.Range(-1, names.Length + 1).ToArray(), q => q < 0 ? "기본" : names[q], s.quality, (v, q) => v.quality = q);
                    break;
                case 1:
                    Stepper("전체 볼륨", Mathf.RoundToInt(s.masterVolume * 100) + "%",
                        () => Apply(v => v.masterVolume = Mathf.Max(0, Mathf.Round(v.masterVolume * 10 - 1) / 10)),
                        () => Apply(v => v.masterVolume = Mathf.Min(1, Mathf.Round(v.masterVolume * 10 + 1) / 10)));
                    break;
                case 2:
                    Stepper("카메라 이동 속도", s.cameraPanSpeed.ToString("0"), () => Apply(v => v.cameraPanSpeed = Mathf.Max(10, v.cameraPanSpeed - 5)),
                        () => Apply(v => v.cameraPanSpeed = Mathf.Min(80, v.cameraPanSpeed + 5)));
                    Options("화면 가장자리 스크롤", new[] { true, false }, b => b ? "켜기" : "끄기", s.edgeScrollThickness > 0, (v, b) => v.edgeScrollThickness = b ? 18 : 0,
                        "마우스를 화면 끝에 대면 카메라 이동");
                    foreach (GameAction action in Enum.GetValues(typeof(GameAction)))
                    {
                        var a = action; var row = Row(ActionName(a));
                        var b = L.Button(row, "Key " + a, rebinding == a ? "키를 누르세요 (Esc 취소)" : KeyBindings.Get(a).ToString(), 340, 7, 240, 34,
                            () => { rebinding = a; Settings(); });
                        if (rebinding == a) MarkSelected(b);
                    }
                    var fixedKeys = Row("고정 조작");
                    L.Label(fixedKeys, "좌클릭 선택 · 드래그 범위 선택 · 우클릭 이동/채집/공격 · 휠 확대 · Space 일시정지 · F5~F8 배속 · F1 장수 · F2 설명서 · Esc 메뉴",
                        12, 340, 0, 600, 48, MenuTheme.Muted);
                    L.Button(Row("단축키 초기화"), "Reset Keys", "기본값으로", 340, 7, 140, 34, () => Apply(v => v.keyBindings = new System.Collections.Generic.List<string>()));
                    break;
                case 3:
                    Options("자동 저장", new[] { true, false }, b => b ? "켜기" : "끄기", s.autoSaveEnabled, (v, b) => v.autoSaveEnabled = b);
                    Options("자동 저장 간격", new[] { 3f, 5f, 10f, 15f, 30f }, m => m + "분", s.autoSaveMinutes, (v, m) => v.autoSaveMinutes = m);
                    Options("알림 표시 시간", new[] { 3f, 6f, 10f, 15f }, t => t + "초", s.toastSeconds, (v, t) => v.toastSeconds = t);
                    Options("새 위협 시 자동 정지", new[] { true, false }, b => b ? "켜기" : "끄기", s.autoPauseThreat, (v, b) => v.autoPauseThreat = b);
                    Options("장수 쓰러짐 시 자동 정지", new[] { true, false }, b => b ? "켜기" : "끄기", s.autoPauseDowned, (v, b) => v.autoPauseDowned = b);
                    Options("로켓 파괴 시 자동 정지", new[] { true, false }, b => b ? "켜기" : "끄기", s.autoPauseRocket, (v, b) => v.autoPauseRocket = b);
                    Options("처음 보는 기능 도움말", new[] { true, false }, b => b ? "켜기" : "끄기", s.firstHints, (v, b) => v.firstHints = b);
                    L.Button(Row("본 도움말 다시 보기", $"지금까지 본 도움말 {s.shownHints.Count}개"), "Reset Hints", "초기화", 340, 7, 140, 34,
                        () => Apply(v => v.shownHints.Clear()));
                    break;
                default:
                    L.Label(Row("버전"), "프로토타입 빌드 · Unity " + Application.unityVersion, 13, 340, 0, 600, 48, MenuTheme.Muted);
                    var folder = Row("저장 폴더", AntColony.Save.SaveStorage.Root);
                    L.Button(folder, "Open Save Folder", "폴더 열기", 340, 7, 140, 34, () => Application.OpenURL("file:///" + AntColony.Save.SaveStorage.Root.Replace('\\', '/')));
                    L.Button(Row("설정 초기화", "모든 설정을 처음 값으로 되돌립니다"), "Reset Settings", "초기화", 340, 7, 140, 34, () => { settingsScroll = 1; Commit(new UserSettingsData()); });
                    break;
            }
            var scroll = list.GetComponentInParent<ScrollRect>();
            Canvas.ForceUpdateCanvases(); scroll.verticalNormalizedPosition = settingsScroll;
            scroll.onValueChanged.AddListener(v => settingsScroll = v.y);
            L.Line(p, 0, 690, 1000);
            L.Button(p, "Back", "뒤로", 24, 706, 120, 38, () => { rebinding = null; Back(); });
        }

        private static string ActionName(GameAction a) => a switch
        {
            GameAction.RotateLeft => "카메라 왼쪽 회전", GameAction.RotateRight => "카메라 오른쪽 회전", GameAction.WeaponSkill => "무기 기술",
            GameAction.WingSkill => "날개 기술", GameAction.AddTroop => "징집소", GameAction.RemoveTroop => "귀환 (평시: 휴식·치료)", GameAction.CycleWeapon => "무기 바꾸기",
            GameAction.Roster => "장수 목록", GameAction.Pause => "일시정지", GameAction.SciencePanel => "과학", GameAction.EventLog => "사건 기록",
            GameAction.WorldMap => "월드맵", GameAction.Diplomacy => "외교", GameAction.Build => "건설", GameAction.WorkSchedule => "작업 일정", _ => a.ToString()
        };
    }
}
