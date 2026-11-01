using System.Collections.Generic;
using AntColony.Core;
using AntColony.Units;
using AntColony.World;
using UnityEngine;

namespace AntColony.UI
{
    public sealed class GameNotifications : MonoBehaviour
    {
        private readonly Dictionary<CommanderAnt, int> levels = new Dictionary<CommanderAnt, int>();
        private string notice;
        private float next;
        private void Update()
        {
            if (!GameSession.Instance.GameStarted || Save.SaveSystem.Busy || Time.unscaledTime < next) return;
            next = Time.unscaledTime + .5f;
            foreach (var c in CommanderRoster.Instance.Commanders)
            {
                int total = 0; foreach (var level in c.Talents.levels) total += level;
                if (levels.TryGetValue(c, out var previous) && total > previous) ToastManager.Show(c.CommanderName + ": 기술이 올랐습니다.");
                levels[c] = total;
                Encyclopedia.Discover("Ants", "weapon:" + c.WeaponLabel, c.WeaponName, "모든 장수는 일하고 싸울 수 있습니다. 장비가 전투 방식을, 지휘 기술이 병력 한도를 정합니다.");
            }
            // 우리 위생 '주의' 이상은 우리 단위로 묶어 오른쪽에 고정 알림(2026-10-13).
            foreach (var pen in AntColony.Core.SceneObjects.All<AntColony.Buildings.Ranch>())
            {
                var risk = PenHygiene.Risk(pen); var target = pen;
                ToastManager.SetPinned("hyg:" + pen.GetEntityId(), risk < .3f ? null : $"{pen.Data.displayName} 위생 {PenHygiene.Level(risk)} — 배설물 {PenHygiene.ManureIn(pen):0}개", () => GameMenuController.Instance?.ShowRanch(target), "목장");
            }
            var world = WorldMapManager.Instance;
            if (world.SettlementNotice != notice)
            { notice = world.SettlementNotice; if (!string.IsNullOrEmpty(notice)) ToastManager.Show(notice); }
            var site = world.ViewedSite;
            if (site == null) return;
            Encyclopedia.Discover("World", "site:" + site.Title, SiteNames.Name(site), SiteNames.Faction(site) + " · " + SiteNames.Korean(site.Kind.ToString()) + " · 고정 위험도 D" + site.Difficulty);
            if (site.Boss != null) Encyclopedia.Discover("Bosses", "boss:MiniBird", "미니버드", SiteNames.Name(site) + "에서 조우. 바닥에 표시되는 공격 범위를 보고 착탄 전에 피하세요. 구체적인 약점은 아직 밝혀지지 않았습니다.");
        }
    }
}
