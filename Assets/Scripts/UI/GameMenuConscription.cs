using System.Collections.Generic;
using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Units;
using UnityEngine;
using UnityEngine.UI;
using L = AntColony.UI.MenuLayout;

namespace AntColony.UI
{
    public sealed partial class GameMenuController
    {
        public void OpenConscription()
        {
            var post = FindObjectsByType<ConscriptionPost>(FindObjectsSortMode.None)
                .FirstOrDefault(p => !p.IsDead && p.CountsTowardPlayerDefeat);
            if (post == null) { ToastManager.Show("건설(B) → 특수에서 징집소를 지으세요. 본거지에 1개만 지을 수 있습니다."); return; }
            ShowConscription(post);
        }

        public void ShowConscription(ConscriptionPost post)
        {
            if (post == null || post.IsDead || !post.CountsTowardPlayerDefeat) return;
            var f = Frame("징집소");
            var p = L.Plate(f, "Conscription", 180, 100, 1080, 650);
            L.Label(p, "징집소 · 출전 편성", 26, 24, 12, 1032, 40, MenuTheme.Accent);
            L.Label(p, "장수와 병력을 선택하세요. 출전하면 작업을 중단하고, 귀환하면 생존 병력을 반납합니다.", 14, 24, 58, 1032, 34, MenuTheme.Muted);
            L.Label(p, "선택   장수 / 무기 / 개인 체력 / 기분 / 현재 상태", 14, 24, 104, 700, 32);
            L.Label(p, "병력 / 지휘한도", 14, 756, 104, 270, 32);
            var list = L.List(p, 24, 144, 1032, 420);
            var choices = new List<(CommanderAnt commander, Toggle toggle, Slider slider)>();
            foreach (var c in SortedCommanders())
            {
                var row = L.Cell(list, "Recruit " + c.CommanderName, 64, MenuTheme.Plate2);
                var toggle = DutyToggle(row, "Deploy " + c.CommanderName, 8, 18, false);
                var label = L.Label(row, "", 14, 48, 0, 640, 64);
                var track = L.Box(row, "Troops " + c.CommanderName, 732, 34, 264, 18, MenuTheme.Well);
                var handle = L.Box(track, "Handle", 0, 0, 16, 18, MenuTheme.Accent);
                var slider = track.gameObject.AddComponent<Slider>();
                slider.handleRect = handle; slider.targetGraphic = handle.GetComponent<Image>();
                slider.wholeNumbers = true; slider.minValue = 1; slider.maxValue = Mathf.Max(1, c.CommandLimit); slider.value = 1;
                var amount = L.Label(row, "", 13, 732, 0, 264, 30);
                choices.Add((c, toggle, slider));
                refreshDutyScreen += () => {
                    var ready = post != null && post.isActiveAndEnabled && !post.IsDead && c != null && c.CanMobilize;
                    toggle.interactable = ready;
                    if (!ready) toggle.SetIsOnWithoutNotify(false);
                    slider.interactable = ready && toggle.isOn;
                    if (c == null) { label.text = "이탈한 장수"; return; }
                    slider.maxValue = Mathf.Max(1, c.CommandLimit);
                    label.text = $"{c.CommanderName} · {c.WeaponLabel}\n체력 {c.PersonalHealth:0}/{GameBalance.CommanderHealth:0} · 기분 {c.Mood:0} · {Status(c)}";
                    amount.text = $"{slider.value:0} / {c.CommandLimit}";
                };
            }
            var summary = L.Label(p, "", 15, 24, 570, 690, 32);
            var deploy = L.Button(p, "Deploy Formation", "출전", 856, 604, 200, 34, () => {
                var selected = choices.Where(x => x.toggle.isOn).ToArray();
                if (post == null || !post.TryDeploy(selected.Select(x => x.commander).ToArray(), selected.Select(x => (int)x.slider.value).ToArray()))
                { ToastManager.Show("출전할 수 없습니다. 장수 상태·지휘한도·대기 개미와 징집소 접근 경로를 확인하세요."); refreshDutyScreen?.Invoke(); return; }
                Resume();
            }, primary: true);
            refreshDutyScreen += () => {
                var selected = choices.Where(x => x.toggle.isOn).ToArray();
                var total = selected.Sum(x => (int)x.slider.value);
                var free = AntPool.Instance != null ? AntPool.Instance.Free : 0;
                summary.text = $"출전 {selected.Length}명 · 편성 {total}마리 / 대기 {free}마리";
                deploy.interactable = post != null && post.isActiveAndEnabled && !post.IsDead && selected.Length > 0 && total <= free;
            };
            L.Button(p, "Close Conscription", "닫기", 24, 604, 160, 34, Resume);
            refreshDutyScreen.Invoke();
        }
    }
}
