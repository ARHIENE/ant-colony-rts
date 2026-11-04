using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Units;
using AntColony.World;
using UnityEngine.UI;

namespace AntColony.UI
{
    public sealed partial class GameMenuController
    {
        public void WalkingFormation(Action<ExpeditionTransport> done)
        {
            Screen("도보 원정 편성");
            MenuTheme.Text(content, "장수 1명·병력 0명부터 출발할 수 있습니다. 병력 추가는 징집소에서 편성하세요. 원정 중에는 진형을 바꿀 수 없습니다.", 13, 52);
            MenuTheme.Text(content, "일반 탐험은 처음부터 가능합니다. 식량·재료·표본을 반입해 보급과 연구를 준비하세요. 필수 보스 B/C/D는 탐지기로 위치를 공개해야 진입합니다. 먼 자원지는 왕복 보급과 본거지 무방비 시간을 비교하세요.", 13, 72);
            var choices = new List<(CommanderAnt commander, Toggle toggle)>();
            foreach (var c in SortedCommanders().Where(c => c.IsColonyMember && !c.IsAwayFromHome && !c.IsDead && !c.PersonalState.health.Downed && c.CanChangeAllocation))
            {
                var row = MenuTheme.Rect("Walking " + c.CommanderName, content);
                row.gameObject.AddComponent<LayoutElement>().preferredHeight = 44;
                var toggle = DutyToggle(row, "Walking Select " + c.CommanderName, 0, 8, false);
                MenuLayout.Label(row, $"{c.CommanderName} · 병력 {c.TroopCount} · 소지 식량 {c.PersonalState.carriedRations?.Total ?? 0}", 16, 42, 0, 620, 44);
                choices.Add((c, toggle));
            }
            MenuTheme.Button(content, "편성", () => {
                var party = WorldMapManager.Instance?.CreateWalkingParty(choices.Where(c => c.toggle.isOn).Select(c => c.commander).ToArray());
                if (party == null) { ToastManager.Show("장수를 선택하고 상태·병역 제도를 확인하세요."); return; }
                Resume(); done(party);
            });
            MenuTheme.Button(content, "취소", () => { Resume(); done(null); });
            var world = WorldMapManager.Instance;
            if (world != null) foreach (var site in world.Sites.Where(WorldMapManager.SiteRevealed))
            {
                var faction = world.Diplomacy?.Faction(site);
                MenuTheme.Text(content, $"{site.Title} · {AntColony.Core.BiomeRules.Name(site.Biome)} · 도보 편도 {ExpeditionRules.WalkSeconds(site) / AntColony.Core.GameCalendar.SecondsPerDay:0.0}일"
                    + $" · {(faction == null ? "중립/야생" : faction.name + (faction.war ? " 전쟁" : " 평화"))}", 12, 48);
            }
        }
    }
}
