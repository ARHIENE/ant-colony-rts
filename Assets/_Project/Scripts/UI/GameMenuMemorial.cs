using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.World;
using UnityEngine;
using L = AntColony.UI.MenuLayout;

namespace AntColony.UI
{
    // V5 사망 기록·묘비(2026-10-15)와 퇴비통 화면. 장례식·추모 지시는 없다(지나가던 장수·친한 장수가 스스로 추모).
    public sealed partial class GameMenuController
    {
        private Tombstone tombPick;

        public void ShowMemorial(Tombstone pick = null)
        {
            tombPick = pick ?? (tombPick != null && tombPick.isActiveAndEnabled ? tombPick : Tombstone.All.FirstOrDefault());
            var window = V5Window("사망 기록 / 묘비", "묘비 하나에 한 구 · 시신을 못 찾으면 이름만 등록 · 시신이 있는 묘비는 철거 불가");
            content = V5Section(window, $"사망 기록 ({Memorial.Deaths.Count})", 20, 104, 420, 544);
            if (Memorial.Deaths.Count == 0) MenuTheme.Text(content, "사망한 장수가 없습니다.", 15, 30);
            foreach (var r in Memorial.Deaths.Reverse())
            {
                var tomb = Tombstone.Of(r.id); var body = Tombstone.BodyOf(r.id);
                var where = tomb != null ? (tomb.Data2.body ? "묘비 안치" : "묘비 등록(시신 미안치)") : body != null ? (body.Remains ? "유해 현장 방치" : "시신 현장 방치") : "시신 없음";
                MenuTheme.Text(content, $"{r.name} · {GameCalendar.Format(r.date)} · {r.cause}\n<color=#b1b3a7>{where}</color>", 15, 46);
                if (tombPick != null && tombPick.Empty)
                {
                    var rec = r; var block = tombPick.RegisterBlock(rec);
                    MenuTheme.Button(content, "선택한 묘비에 등록", () => { tombPick.Register(rec); ShowMemorial(tombPick); }, block).interactable = block == "";
                }
            }
            content = V5Section(window, tombPick == null ? "묘비 없음" : $"묘비 · {(tombPick.Empty ? "비어 있음" : tombPick.Data2.name)}", 454, 104, 626, 544);
            if (Tombstone.All.Count == 0) { MenuTheme.Text(content, "건설 메뉴(의료)에서 묘비를 지으세요.", 15, 30); V5Fit(window); return; }
            var row = L.Cell(content, "Tombs", 44); var i = 0;
            foreach (var t in Tombstone.All.Take(5).ToArray())
            { var pickT = t; L.Button(row, "Tomb " + i, t.Empty ? "빈 묘비" : t.Data2.name, i++ * 118, 0, 112, 40, () => ShowMemorial(pickT), primary: t == tombPick); }
            if (tombPick != null && !tombPick.Empty)
            {
                var d = tombPick.Data2;
                MenuTheme.Text(content, $"{d.name} · 사망 {GameCalendar.Format(d.date)} · {d.cause}\n" + (d.body ? (d.remains ? "유해 안치됨" : "시신 안치됨(부패하지 않음)") : d.burialOrdered ? "안치 운반 대기 중" : "시신 미안치"), 15, 50);
                if (!d.body)
                {
                    var block = tombPick.BurialBlock;
                    MenuTheme.Button(content, d.burialOrdered ? "안치 지시 취소" : "시신 안치 지시(운반 장수)", () => { tombPick.OrderBurial(!d.burialOrdered); ShowMemorial(tombPick); }, block)
                        .interactable = d.burialOrdered || block == "";
                    if (!d.burialOrdered && block != "") MenuTheme.Text(content, "<color=#f2c94c>" + block + "</color>", 14, 26);
                }
                else
                {
                    MenuTheme.Button(content, "시신 꺼내기(옆 바닥)", () => { tombPick.TakeOut(); ShowMemorial(tombPick); });
                    foreach (var other in Tombstone.All.Where(x => x.Empty).Take(3).ToArray())
                    { var dest = other; MenuTheme.Button(content, "이장: 빈 묘비로 옮기기", () => { tombPick.TakeOut(dest); ShowMemorial(dest); }); }
                }
                MenuTheme.Text(content, "지나가던 장수는 관계에 따라 짧게 추모하고, 친한 장수는 여가에 찾아옵니다. 위로는 겹치지 않습니다.", 14, 44);
            }
            V5Fit(window);
        }

        public void ShowCompost(CompostBin b)
        {
            if (b == null) { Resume(); return; }
            var window = V5Window("퇴비통", "배설물·썩은 식품 → 비료 · 상주 작업자 없이 숙성 · 운반 장수가 넣고 꺼냄");
            content = V5Section(window, "상태", 20, 104, 520, 544);
            var status = MenuTheme.Text(content, "", 16, 90);
            void Refresh() { if (b != null) status.text = $"{b.Status}\n원료 칸: 배설물 {b.Bin?.Amount(ResourceType.Manure) ?? 0} · 썩은 식품 {b.Bin?.Amount(ResourceType.RottenFood) ?? 0}\n비료 재고 {CompostBin.FertilizerStock}/{b.Data2.target}"; }
            Refresh(); refreshDutyScreen = Refresh;
            if (b.Data2.active)
                MenuTheme.Button(content, "숙성 중단 — 진행도 사라지고 원료 그대로 바닥으로", () => { b.Abort(); ShowCompost(b); });
            content = V5Section(window, "설정", 554, 104, 526, 544);
            MenuTheme.Button(content, (b.Data2.allowManure ? "☑" : "☐") + " 배설물 허용", () => { b.SetAllowed(ResourceType.Manure, !b.Data2.allowManure); ShowCompost(b); });
            MenuTheme.Button(content, (b.Data2.allowRotten ? "☑" : "☐") + " 썩은 식품 허용", () => { b.SetAllowed(ResourceType.RottenFood, !b.Data2.allowRotten); ShowCompost(b); });
            var target = L.Cell(content, "CompostTarget", 44);
            L.Label(target, $"비료 목표 재고 {b.Data2.target}", 15, 0, 6, 220, 30, MenuTheme.Muted);
            L.Button(target, "Target Minus", "−", 230, 0, 44, 40, () => { b.Data2.target = Mathf.Max(0, b.Data2.target - 5); ShowCompost(b); });
            L.Button(target, "Target Plus", "+", 280, 0, 44, 40, () => { b.Data2.target = Mathf.Min(999, b.Data2.target + 5); ShowCompost(b); });
            if (b.Bin != null)
            {
                var prio = L.Cell(content, "CompostPriority", 44);
                L.Label(prio, $"운반 우선순위 {b.Bin.priority}", 15, 0, 6, 220, 30, MenuTheme.Muted);
                L.Button(prio, "Prio Minus", "−", 230, 0, 44, 40, () => { b.Bin.priority = Mathf.Max(1, b.Bin.priority - 1); ShowCompost(b); });
                L.Button(prio, "Prio Plus", "+", 280, 0, 44, 40, () => { b.Bin.priority = Mathf.Min(9, b.Bin.priority + 1); ShowCompost(b); });
            }
            MenuTheme.Text(content, "목표 재고에 도달하면 새 투입만 멈추고 숙성 중인 원료는 계속 숙성합니다. 허용을 끄면 그 원료는 바닥으로 나옵니다. 퇴비통 안 원료는 먹지 못합니다.", 14, 70);
            V5Fit(window);
        }
    }
}
