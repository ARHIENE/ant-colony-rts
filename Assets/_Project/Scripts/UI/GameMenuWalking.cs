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
        public void ExpeditionEscort(ExpeditionTransport party)
        {
            Screen("출정 호위 · 원군");
            if (party == null) { MenuTheme.Text(content, "월드맵에서 원정대를 선택하세요.", 14, 40); return; }
            MenuTheme.Text(content, party.EscortStatus, 14, 60);
            MenuTheme.Text(content, "호위는 본거지에 남아 길목까지 이동합니다. 원정대 출발 뒤에는 전투하며 복귀합니다. 이동·공격은 직접 지휘할 수 있고, 외부 원정대 귀환에는 맞이 호위가 필요하지 않습니다.", 13, 88);
            foreach (var commander in SortedCommanders().Where(c => c.IsColonyMember && !c.IsAwayFromHome && !c.IsDead && c.CanReceiveOrders))
            {
                var c = commander;
                MenuTheme.Button(content, (party.Escorts.Contains(c) ? "호위 해제: " : "호위·원군 배정: ") + c.CommanderName, () => {
                    if (party.Escorts.Contains(c)) { party.CancelCrewOrder(c); c.ReturnToPost(); }
                    else if (!party.AssignEscorts(new[] { c })) ToastManager.Show("배정 불가 · 장수 상태와 길목까지의 경로를 확인하세요.");
                    ExpeditionEscort(party);
                });
            }
            MenuTheme.Button(content, "호위대 복귀", () => { party.RecallEscorts(); Resume(); });
            MenuTheme.Button(content, "길목 보기", () => {
                WorldMapManager.Instance.ViewSite(null);
                AntColony.Core.SceneObjects.Any<AntColony.Camera.IsometricCameraController>()?.RestoreView(party.HomePosition, 0, 24);
                Resume();
            });
            MenuTheme.Button(content, "닫기", Resume);
        }

        public void WalkingFormation(Action<ExpeditionTransport> done)
        {
            Screen("도보 원정 편성");
            if (ExpeditionRules.Paused)
            {
                MenuTheme.Text(content, ExpeditionRules.PauseReason, 13, 52);
                MenuTheme.Button(content, "닫기", () => { Resume(); done(null); });
                return;
            }
            MenuTheme.Text(content, "장수 1명부터 도보 편성을 구성합니다. 장수 본인의 장비와 건강 상태를 확인하세요.", 13, 52);
            MenuTheme.Text(content, "일반 탐험은 처음부터 가능합니다. 식량·재료·표본을 반입해 보급과 연구를 준비하세요. 필수 보스 B/C/D는 탐지기로 위치를 공개해야 진입합니다. 먼 자원지는 왕복 보급과 본거지 무방비 시간을 비교하세요.", 13, 72);
            var choices = new List<(CommanderAnt commander, Toggle toggle)>();
            foreach (var c in SortedCommanders().Where(c => c.IsColonyMember && !c.IsAwayFromHome && !c.IsDead && !c.PersonalState.health.Downed && c.CanChangeAllocation))
            {
                var row = MenuTheme.Rect("Walking " + c.CommanderName, content);
                row.gameObject.AddComponent<LayoutElement>().preferredHeight = 44;
                var toggle = DutyToggle(row, "Walking Select " + c.CommanderName, 0, 8, false);
                MenuLayout.Label(row, $"{c.CommanderName} · 소지 식량 {c.PersonalState.carriedRations?.Total ?? 0}", 16, 42, 0, 620, 44);
                choices.Add((c, toggle));
            }
            MenuTheme.Button(content, "편성", () => {
                var party = WorldMapManager.Instance?.CreateWalkingParty(choices.Where(c => c.toggle.isOn).Select(c => c.commander).ToArray());
                if (party == null) { ToastManager.Show("장수를 선택하고 상태·출정 길목 경로를 확인하세요."); return; }
                Resume(); done(party);
            });
            MenuTheme.Button(content, "취소", () => { Resume(); done(null); });
            var world = WorldMapManager.Instance;
            if (world != null) foreach (var site in world.Sites.Where(WorldMapManager.SiteRevealed))
            {
                var faction = world.Diplomacy?.Faction(site);
                MenuTheme.Text(content, $"{SiteNames.Name(site)} · {AntColony.Core.BiomeRules.Name(site.Biome)} · 도보 편도 {ExpeditionRules.WalkSeconds(site) / AntColony.Core.GameCalendar.SecondsPerDay:0.0}일"
                    + $" · {(faction == null ? "중립/야생" : faction.name + (faction.war ? " 전쟁" : " 평화"))}", 12, 48);
            }
        }
    }
}
