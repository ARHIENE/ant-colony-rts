using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Core;
using AntColony.Data;

namespace AntColony.Buildings
{
    public static class ScienceLabUpgrades
    {
        public static ScienceTechnology RequiredResearch(int tier) => tier >= 1 && tier < ScienceLab.MaxTier
            ? EraPromotionResearch.Promotions[tier - 1] : throw new ArgumentOutOfRangeException(nameof(tier));

        public static (ResourceType type, int amount)[] Cost(int tier) => tier switch
        {
            1 => new[] { (ResourceType.Iron, 40) },
            2 => new[] { (ResourceType.Steel, 60) },
            3 => new[] { (ResourceType.Steel, 80), (ResourceType.Component, 10) },
            4 => new[] { (ResourceType.Titanium, 80), (ResourceType.ElectronicComponent, 20) },
            5 => new[] { (ResourceType.StellarAlloy, 80), (ResourceType.ElectronicComponent, 40) },
            _ => Array.Empty<(ResourceType, int)>()
        };

        public static string Description(int tier) => tier >= ScienceLab.MaxTier ? "최고 등급"
            : $"{tier}→{tier + 1}등급 · {CampaignResearch.Technologies[(int)RequiredResearch(tier)].Name} 완료 · "
                + string.Join(" / ", Cost(tier).Select(c => $"{c.type.DisplayName()} {c.amount}"));

        public static bool TryPay(int tier, CampaignResearch research, ResourceManager resources)
        {
            if (tier < 1 || tier >= ScienceLab.MaxTier || research == null || resources == null
                || !research.Has(RequiredResearch(tier))) return false;
            var cost = Cost(tier);
            if (cost.Any(c => resources.GetAmount(c.type) < c.amount)) return false;
            foreach (var c in cost) resources.TrySpend(c.type, c.amount, ResourceReason.Research);
            return true;
        }

        public static IEnumerable<int> Steps(int currentTier, int targetTier)
            => Enumerable.Range(currentTier, Math.Max(0, targetTier - currentTier));
    }
}
