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
                if (levels.TryGetValue(c, out var previous) && total > previous) ToastManager.Show(c.CommanderName + ": skill improved.");
                levels[c] = total;
                Encyclopedia.Discover("Ants", "weapon:" + c.WeaponLabel, c.WeaponLabel, "All commanders can work and fight. Equipment determines combat style; Command skill determines troop capacity.");
            }
            // 우리 위생 '주의' 이상은 우리 단위로 묶어 오른쪽에 고정 알림(2026-10-13).
            foreach (var pen in FindObjectsByType<AntColony.Buildings.Ranch>(FindObjectsSortMode.None))
            {
                var risk = PenHygiene.Risk(pen); var target = pen;
                ToastManager.SetPinned("hyg:" + pen.GetEntityId(), risk < .3f ? null : $"{pen.Data.displayName} 위생 {PenHygiene.Level(risk)} — 배설물 {PenHygiene.ManureIn(pen):0}개", () => GameMenuController.Instance?.ShowRanch(target), "목장");
            }
            var world = WorldMapManager.Instance;
            if (world.SettlementNotice != notice)
            { notice = world.SettlementNotice; if (!string.IsNullOrEmpty(notice)) ToastManager.Show(notice); }
            var site = world.ViewedSite;
            if (site == null) return;
            Encyclopedia.Discover("World", "site:" + site.Title, site.Title, site.Faction + " / " + site.Kind + ". Fixed difficulty " + site.Difficulty + ".");
            if (site.Boss != null) Encyclopedia.Discover("Bosses", "boss:MiniBird", "MiniBird", "Encountered at " + site.Title + ". Watch the marked ground attack areas and move away before impact. Specific weakness information is not yet discovered.");
        }
    }
}
