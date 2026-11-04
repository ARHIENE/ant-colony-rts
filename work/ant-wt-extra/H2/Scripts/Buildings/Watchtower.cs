using System.Collections.Generic;
using System.Linq;
using AntColony.Core;
using AntColony.World;

namespace AntColony.Buildings
{
    // 감시탑 1기당 편입 거점 3곳(월드맵 순서대로)을 감시한다. 경보는 SettlementDefense가 확인한다.
    public sealed class Watchtower : BuildingBase
    {
        private static readonly List<Watchtower> Active = new List<Watchtower>();
        protected override void OnEnable() { base.OnEnable(); Active.Add(this); }
        protected override void OnDisable() { Active.Remove(this); base.OnDisable(); }

        public static bool Watches(ExpeditionSite site)
        {
            var world = WorldMapManager.Instance;
            if (world == null || site == null) return false;
            var towers = Active.Count(t => t != null && !t.IsDead);
            var annexed = world.Sites.Where(s => s.Disposition == ConquestDisposition.Annexed).ToList();
            var index = annexed.IndexOf(site);
            return index >= 0 && index < towers * GameBalance.WatchtowerSites;
        }
    }
}
