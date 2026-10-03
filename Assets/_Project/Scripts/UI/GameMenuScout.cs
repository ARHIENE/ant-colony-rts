using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Units;
using UnityEngine;
using UnityEngine.UI;
using L = AntColony.UI.MenuLayout;

namespace AntColony.UI
{
    // 정찰 파견 창(2026-10-03): 동행 장수 1명을 목록에서 골라 내보낸다. 장수는 돌아올 때까지 소굴을 비운다.
    public sealed partial class GameMenuController
    {
        public void ShowScout(ScoutPost post)
        {
            if (post == null || !post.isActiveAndEnabled) return;
            var f = Frame("정찰 초소");
            var p = L.Plate(f, "ScoutDispatch", 280, 120, 880, 610);
            L.Label(p, "정찰 초소 · 정찰 파견", 26, 24, 12, 832, 40, MenuTheme.Accent);
            L.Label(p, "동행할 장수 1명을 고르세요. 정찰이 끝날 때까지 장수는 소굴을 비우고, 성공하면 새 장수를 영입합니다.", 14, 24, 58, 832, 34, MenuTheme.Muted);
            L.Label(p, "선택   장수 / 무기 / 개인 체력 / 기분 / 현재 상태", 14, 24, 104, 832, 32);
            var list = L.List(p, 24, 144, 832, 360);
            var group = list.gameObject.AddComponent<ToggleGroup>(); group.allowSwitchOff = true;
            CommanderAnt chosen = null;
            foreach (var c in SortedCommanders().Where(c => c.IsColonyMember))
            {
                var row = L.Cell(list, "Scout " + c.CommanderName, 56, MenuTheme.Plate2);
                var toggle = DutyToggle(row, "Pick " + c.CommanderName, 8, 14, false);
                toggle.group = group;
                toggle.onValueChanged.AddListener(on => { if (on) chosen = c; else if (chosen == c) chosen = null; refreshDutyScreen?.Invoke(); });
                var label = L.Label(row, "", 14, 48, 0, 760, 56);
                refreshDutyScreen += () => {
                    var ready = c != null && c.CanScout && !post.IsDispatched;
                    toggle.interactable = ready;
                    if (!ready && toggle.isOn) toggle.SetIsOnWithoutNotify(false);
                    if (!ready && chosen == c) chosen = null;
                    label.text = c == null ? "이탈한 장수"
                        : $"{c.CommanderName} · {c.WeaponLabel}\n체력 {c.PersonalHealth:0}/{GameBalance.CommanderHealth:0} · 기분 {c.Mood:0} · {Status(c)}";
                };
            }
            var summary = L.Label(p, "", 14, 24, 512, 600, 50);
            var dispatch = L.Button(p, "Scout Dispatch", "파견", 656, 560, 200, 34, () => {
                if (!post.TryDispatch(chosen))
                { ToastManager.Show("파견할 수 없습니다. 장수 상태·식량·대기 개미를 확인하세요."); refreshDutyScreen?.Invoke(); return; }
                ToastManager.Show($"{chosen.CommanderName} 정찰 출발");
                Resume();
            }, primary: true);
            refreshDutyScreen += () => {
                var rm = ResourceManager.Instance; var free = AntPool.Instance != null ? AntPool.Instance.Free : 0;
                var affordable = (rm == null || rm.CanAfford(post.DispatchFoodCost, 0)) && free >= post.DispatchAnts;
                summary.text = post.GetStatusLabel() + (chosen != null ? $"\n동행: {chosen.CommanderName}" : "\n동행 장수를 고르세요.");
                dispatch.interactable = post.isActiveAndEnabled && !post.IsDispatched && chosen != null && affordable;
            };
            L.Button(p, "Close Scout", "닫기", 24, 560, 160, 34, Resume);
            refreshDutyScreen.Invoke();
        }
    }
}
