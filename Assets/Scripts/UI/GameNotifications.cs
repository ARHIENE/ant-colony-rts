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
