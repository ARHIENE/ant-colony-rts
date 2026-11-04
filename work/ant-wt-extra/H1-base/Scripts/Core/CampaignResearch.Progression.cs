using System;
using System.Collections.Generic;
using AntColony.Boss;

namespace AntColony.Core
{
    [Flags] public enum ProgressionItemRole { Ordinary = 0, UnimportedProgress = 1, EssentialReward = 2, SowingSeed = 4 }

    public sealed partial class CampaignResearch
    {
        public BossProgress BossProgress => new BossProgress(state.bosses);
        public bool HasSample(AntColony.Data.ResourceType type) => state.samples.Contains((int)type);
        public bool ResourceResearched(AntColony.Data.ResourceType type) => state.resourceResearch.Contains((int)type);
        public bool CanConsumeProgressionItem(string itemId, ProgressionItemRole role)
            => role == ProgressionItemRole.Ordinary || !string.IsNullOrWhiteSpace(itemId) && state.consumptionOverrides.Contains(itemId);
        public bool SetConsumptionOverride(string itemId, bool allow, bool warningConfirmed)
        {
            if (string.IsNullOrWhiteSpace(itemId) || allow && !warningConfirmed) return false;
            state.consumptionOverrides.Remove(itemId);
            if (allow) state.consumptionOverrides.Add(itemId);
            return true;
        }
        // 본거지 하역/전달 호출부는 실제로 들어온 수량만 전달한다. 연구·식사 소비 이벤트보다 먼저 호출한다.
        public void RecordSampleDelivery(AntColony.Data.ResourceType type, int amount)
        {
            if (amount < 1 || !Enum.IsDefined(typeof(AntColony.Data.ResourceType), type) || HasSample(type)) return;
            state.samples.Add((int)type);
        }
        public bool TryCompleteResourceResearch(AntColony.Data.ResourceType type)
        {
            if (!HasSample(type) || ResourceResearched(type)) return false;
            state.resourceResearch.Add((int)type);
            return true;
        }
        public bool CanRecoverSample(AntColony.Data.ResourceType type, int toolTier, int requiredTier)
            => Enum.IsDefined(typeof(AntColony.Data.ResourceType), type) && requiredTier > 0
                && toolTier >= Math.Max(0, requiredTier - 1) && !HasSample(type);
        public bool CanMine(AntColony.Data.ResourceType type, int toolTier, int requiredTier)
            => Enum.IsDefined(typeof(AntColony.Data.ResourceType), type) && requiredTier >= 0 && toolTier >= requiredTier && ResourceResearched(type);
        // 낚시·돌 도구·보온 기존 연구에는 표본 조건을 추가하지 않는다.
        private static bool ValidResourceRecords(List<int> records)
        {
            if (records == null) return true;
            var unique = new HashSet<int>();
            foreach (var i in records) if (!Enum.IsDefined(typeof(AntColony.Data.ResourceType), i) || !unique.Add(i)) return false;
            return true;
        }
        public bool DeliverBossReward(string parcelId)
        {
            if (!BossProgress.TryDeliver(parcelId)) return false;
            if (BossProgress.Delivered(BossId.D)) AcquireBlueprint();
            return true;
        }
    }
}
