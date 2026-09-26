using System;
using AntColony.Core;
using UnityEngine;

namespace AntColony.World
{
    public partial class WorldMapManager
    {
        public bool LegacyLayout { get; private set; }
        public DiplomacyManager Diplomacy { get; private set; }
        private void InitializeDiplomacy()
        {
            Diplomacy = gameObject.AddComponent<DiplomacyManager>();
            Diplomacy.Initialize(this, GameSession.Instance.Options.seed);
            var saved = GameSession.Instance.PendingLoad?.diplomacy;
            if (saved != null) Diplomacy.Restore(saved);
            for (var i = 0; i < Diplomacy.Data.extraSites; i++) CreateRebelSite(i);
        }
        internal ExpeditionSite CreateRebelSite(int index)
        {
            CreateSite("Rebel Camp " + index, "Neutral", Color.gray, new Vector2(.48f + index % 4 * .02f, .48f), ExpeditionSiteKind.Empty, 1);
            return sites[sites.Count - 1];
        }
        private void GenerateWorld()
        {
            var random = new System.Random(GameSession.Instance.Options.seed);
            var rotation = (float)random.NextDouble() * Mathf.PI * 2;
            // 각 문명의 세 거점은 같은 띠의 연속된 세 위치를 차지한다.
            for (var ring = 0; ring < 3; ring++)
            {
                var count = ring == 0 ? 9 : ring == 1 ? 14 : 10;
                for (var slot = 0; slot < count; slot++)
                {
                    var civ = ring > 0 && slot < 6 ? (ring - 1) * 2 + slot / 3 : -1;
                    var independent = ring == 0 ? slot >= 5 && slot < 8 : ring == 1 && slot >= 6 && slot < 9;
                    var trade = ring == 0 ? slot == 4 : ring == 1 ? slot == 9 : slot == 6;
                    var resource = ring == 0 ? slot < 4 : ring == 1 && slot >= 12;
                    var kind = trade ? ExpeditionSiteKind.TradePost : resource ? ExpeditionSiteKind.ResourceSite
                        : civ >= 0 || independent ? ExpeditionSiteKind.Settlement : ExpeditionSiteKind.BossNest;
                    var faction = civ >= 0 ? new[] { "Amber", "Azure", "Jade", "Crimson" }[civ]
                        : independent ? "Independent " + sites.Count : trade ? "Trade" : resource ? "Neutral" : "Wildlife";
                    var color = civ >= 0 ? Color.HSVToRGB(civ * .23f + .08f, .7f, .9f)
                        : trade ? Color.yellow : resource ? Color.green : Color.gray;
                    var angle = rotation + slot * Mathf.PI * 2 / count;
                    var position = new Vector2(.5f + Mathf.Cos(angle) * (.16f + ring * .15f), .5f + Mathf.Sin(angle) * (.16f + ring * .15f));
                    CreateSite(faction + " " + kind + " " + (sites.Count + 1), faction, color, position, kind, 1 + random.Next(3));
                }
            }
        }
    }
}
