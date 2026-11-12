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
            var post = AntColony.Core.SceneObjects.All<ConscriptionPost>()
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
            L.Label(p, "소집한 장수는 일을 멈추고 전투 태세로 대기하며 가까운 적과 스스로 교전합니다. 병력은 없고 장수 본인의 체력·장비·기술로 싸웁니다.", 13, 24, 56, 1032, 44, MenuTheme.Muted);
            L.Label(p, "선택   장수 / 무기 / 개인 체력 / 기분 / 현재 상태", 14, 24, 104, 700, 32);
            L.Label(p, "소집 상태", 14, 756, 104, 270, 32);
            var list = L.List(p, 24, 144, 1032, 420);
            var autoReturn = DutyToggle(p, "Home Defense Auto Return", 620, 608, true);
            L.Label(p, "방어 자동 귀환 20초", 13, 650, 602, 200, 34);
            var choices = new List<(CommanderAnt commander, Toggle toggle)>();
            foreach (var c in SortedCommanders().Where(c => !c.Traits.Has(CommanderTrait.Pacifist)))
            {
                var row = L.Cell(list, "Recruit " + c.CommanderName, 64, MenuTheme.Plate2);
                var toggle = DutyToggle(row, "Deploy " + c.CommanderName, 8, 18, false);
                var label = L.Label(row, "", 14, 48, 0, 500, 64);
                var amount = L.Label(row, "", 13, 732, 0, 264, 30);
                choices.Add((c, toggle));
                refreshDutyScreen += () => {
                    var ready = post != null && post.CanDeploy(c); // 2026-11-12: 병력 없이 장수만 소집
                    toggle.interactable = ready;
                    if (!ready) toggle.SetIsOnWithoutNotify(false);
                    if (c == null) { label.text = "이탈한 장수"; return; }
                    label.text = $"{c.CommanderName} · {c.WeaponName}\n체력 {c.PersonalHealth:0}/{GameBalance.CommanderHealth:0} · 기분 {c.Mood:0} · {Status(c)}";
                    amount.text = ready ? "소집 가능" : c.IsDeployed ? "소집 중" : "소집 불가";
                };
            }
            var summary = L.Label(p, "", 15, 24, 570, 690, 32);
            var deploy = L.Button(p, "Deploy Formation", "출전", 856, 604, 200, 34, () => {
                var selected = choices.Where(x => x.toggle.isOn).ToArray();
                if (post == null || !post.TryDeploy(selected.Select(x => x.commander).ToArray()))
                { ToastManager.Show("소집할 수 없습니다. 장수 상태와 소집 지점 접근 경로를 확인하세요."); refreshDutyScreen?.Invoke(); return; }
                foreach (var choice in selected) AntColony.World.WorldMapManager.Instance.DefenseReturn.Set(choice.commander, autoReturn.isOn);
                Resume();
            }, primary: true);
            refreshDutyScreen += () => {
                var selected = choices.Where(x => x.toggle.isOn).ToArray();
                summary.text = $"소집 {selected.Length}명";
                deploy.interactable = post != null && post.isActiveAndEnabled && !post.IsDead && selected.Length > 0;
            };
            L.Button(p, "Close Conscription", "닫기", 24, 604, 160, 34, Resume);
            refreshDutyScreen.Invoke();
        }

    }
}
