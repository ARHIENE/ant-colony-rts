using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;

namespace AntColony.UI
{
    // 창고 설정(2026-10-12, 산소미포함식): 허용 품목·우선순위·품목별 꺼내기·전체 비우기. 일반 창고의 최대 보관량 조절은 없다.
    public sealed partial class GameMenuController
    {
        public void ShowStorage(StoreBin bin)
        {
            var rm = ResourceManager.Instance;
            if (bin == null || rm == null || !rm.Bins.Contains(bin)) { Resume(); return; }
            var window = V5Window(bin.Name + " / 창고", "허용 품목 · 우선순위 · 꺼내기");
            content = V5Section(window, "운영 설정", 20, 104, 360, 544);
            MenuTheme.Text(content, "바닥 물자는 허용한 창고 중 우선순위가 높은 곳 → 가까운 곳으로 운반합니다. 이미 보관한 물자는 다른 창고로 자동으로 옮기지 않습니다. 허용을 끄면 그 품목 재고는 바로 창고 주변 바닥에 나옵니다.", 16, 70);
            MenuTheme.Button(content, $"우선순위 {bin.priority} (−)", () => { rm.SetPriority(bin, bin.priority - 1); ShowStorage(bin); });
            MenuTheme.Button(content, $"우선순위 {bin.priority} (+)", () => { rm.SetPriority(bin, bin.priority + 1); ShowStorage(bin); });
            MenuTheme.Button(content, "전체 비우기(바닥에)", () => { rm.EjectAll(bin); ShowStorage(bin); });
            content = V5Section(window, "품목", 394, 104, 686, 544);
            foreach (var t in System.Enum.GetValues(typeof(ResourceType)).Cast<ResourceType>())
            {
                var type = t;
                MenuTheme.Button(content, $"{t.DisplayName()}: {(bin.Allows(t) ? "허용" : "금지")} · 재고 {bin.Amount(t)} / {bin.Capacity(t)}", () => { rm.SetAllowed(bin, type, !bin.Allows(type)); ShowStorage(bin); });
                if (bin.Amount(t) > 0) MenuTheme.Button(content, $"  {t.DisplayName()} 꺼내기(바닥에)", () => { rm.Eject(bin, type); ShowStorage(bin); });
            }
            V5Fit(window);
        }
    }
}
