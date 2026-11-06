using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using UnityEngine;
using UnityEngine.UI;
using L = AntColony.UI.MenuLayout;

namespace AntColony.UI
{
    // 창고 설정(2026-10-12, 산소미포함식): 허용 품목·우선순위·품목별 꺼내기·전체 비우기. 일반 창고의 최대 보관량 조절은 없다.
    public sealed partial class GameMenuController
    {
        private bool[] storageAllowedClipboard;
        private bool storageClipboardCold;
        public void ShowStorage(StoreBin bin)
        {
            var rm = ResourceManager.Instance;
            if (bin == null || rm == null || !rm.Bins.Contains(bin)) { Resume(); return; }
            var window = V5Window(bin.Name + " / 창고", "품목 행을 누르면 허용 전환 · 꺼낸 물자는 바닥에 놓이며 삭제되지 않음");
            ManagementTabs(window, "물류·식품", "창고");
            var bins = rm.Bins.ToArray();
            GameMenuSelect.Create(window, bins.Select(b => b.Name).ToArray(), System.Array.IndexOf(bins, bin), 20, 677, 300, i => ShowStorage(bins[i]));
            content = V5Section(window, "운영 설정", 20, 104, 360, 544);
            MenuTheme.Text(content, "바닥 물자는 허용한 창고 중 우선순위가 높은 곳 → 가까운 곳으로 운반합니다. 이미 보관한 물자는 다른 창고로 자동으로 옮기지 않습니다. 허용을 끄면 그 품목 재고는 바로 창고 주변 바닥에 나옵니다.", 16, 70);
            MenuTheme.Button(content, $"우선순위 {bin.priority} (−)", () => { rm.SetPriority(bin, bin.priority - 1); ShowStorage(bin); });
            MenuTheme.Button(content, $"우선순위 {bin.priority} (+)", () => { rm.SetPriority(bin, bin.priority + 1); ShowStorage(bin); });
            if (!(bin.Host is CompostBin))
            {
                MenuTheme.Text(content, "허용 품목만 복사합니다. 일반 창고끼리, 냉장·냉동고끼리 붙여넣을 수 있습니다. 붙여넣기로 금지된 재고는 바닥에 나옵니다. 우선순위·재고는 복사하지 않습니다.", 14, 80);
                MenuTheme.Button(content, "허용 품목 복사", () =>
                {
                    storageAllowedClipboard = System.Enum.GetValues(typeof(ResourceType)).Cast<ResourceType>().Select(bin.Allows).ToArray();
                    storageClipboardCold = bin.Host is ColdStorage;
                    ShowStorage(bin);
                });
                MenuTheme.Button(content, "허용 품목 붙여넣기 (같은 창고 종류)", () =>
                {
                    if (storageAllowedClipboard == null || storageClipboardCold != (bin.Host is ColdStorage)) return;
                    for (int i = 0; i < storageAllowedClipboard.Length; i++) rm.SetAllowed(bin, (ResourceType)i, storageAllowedClipboard[i]);
                    ShowStorage(bin);
                }).interactable = storageAllowedClipboard != null && storageClipboardCold == (bin.Host is ColdStorage);
            }
            MenuTheme.Button(content, "전체 비우기(바닥에)", () => { rm.EjectAll(bin); ShowStorage(bin); });
            content = V5Section(window, "품목", 394, 104, 686, 544);
            foreach (var t in System.Enum.GetValues(typeof(ResourceType)).Cast<ResourceType>())
            {
                var type = t;
                var row = L.Cell(content, "Storage Row " + type, 46, MenuTheme.Plate2);
                PrototypeResourceIcon.Create(row, type, 8, 12);
                L.Label(row, type.DisplayName(), 13, 38, 2, 150, 24);
                L.Label(row, $"{bin.Amount(type):N0} / {bin.Capacity(type):N0}", 12, 38, 24, 150, 20, MenuTheme.Muted);
                var allow = L.Button(row, "Allow " + type, bin.Allows(type) ? "✓ 허용" : "금지", 0, 5, 76, 36,
                    () => { rm.SetAllowed(bin, type, !bin.Allows(type)); ShowStorage(bin); }, primary: bin.Allows(type));
                var ar = (RectTransform)allow.transform; ar.anchorMin = ar.anchorMax = ar.pivot = Vector2.one; ar.anchoredPosition = new Vector2(-88, -5);
                var eject = L.Button(row, "Eject " + type, "반출", 0, 5, 72, 36, () => { rm.Eject(bin, type); ShowStorage(bin); }, "바닥으로 꺼냅니다. 물자는 삭제되지 않습니다.");
                var er = (RectTransform)eject.transform; er.anchorMin = er.anchorMax = er.pivot = Vector2.one; er.anchoredPosition = new Vector2(-8, -5);
                eject.interactable = bin.Amount(type) > 0;
            }
            V5Fit(window);
        }
    }
}
