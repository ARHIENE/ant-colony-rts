using System.Collections.Generic;
using System.Linq;
using AntColony.Buildings;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;
using ResourceType = AntColony.Data.ResourceType;

namespace AntColony.Core
{
    // G06(2026-10-24): 본거지에 반입한 표본 1개로 그 자원을 연구한다. 연구 전에는 표본 회수만, 연구 후 본격 채굴.
    // 과학 연구와 같은 연구소·담당 장수를 쓰며 한 번에 하나만 진행한다(과학 연구가 진행 중이면 시작 불가).
    public sealed partial class CampaignResearch
    {
        public const float ResourceResearchWork = 300f; // 잠정: 1시대 연구(낚시)와 같은 연구량
        public ResourceType? ActiveResource => state.resourceActive >= 0 ? (ResourceType)state.resourceActive : (ResourceType?)null;
        public float ResourceProgress => state.resourceProgress;
        public IEnumerable<ResourceType> PendingSamples => state.samples.Where(i => !state.resourceResearch.Contains(i)).Select(i => (ResourceType)i);

        public bool TryStartResourceResearch(ResourceType type)
        {
            if (Departed || !HasSample(type) || ResourceResearched(type) || Active != null || state.resourceActive >= 0) return false;
            state.resourceActive = (int)type; state.resourceProgress = 0;
            return true;
        }

        private void TickResourceResearch(float seconds)
        {
            foreach (var lab in AntColony.Core.SceneObjects.All<ScienceLab>())
            {
                var c = lab.Target;
                if (!lab.isActiveAndEnabled || lab.Busy || c == null || !c.CivilianWorkReady || !c.CanReceiveOrders
                    || c.IsAwayFromHome || c.ScienceAssignment != lab) continue;
                state.resourceProgress += seconds * c.WorkRate(CommanderActivity.Research) * (1f + .25f * (lab.Tier - 1));
            }
            if (state.resourceProgress < ResourceResearchWork) return;
            var type = (ResourceType)state.resourceActive;
            state.resourceActive = -1; state.resourceProgress = 0;
            TryCompleteResourceResearch(type);
            AntColony.UI.ToastManager.Show(type.DisplayName() + " 연구 완료 · 이제 본격 채굴할 수 있습니다.");
        }

        // G43: 명부에서 제거되는 이탈자를 엔딩 분류용으로 남긴다(같은 장수 1회).
        public void RecordDeparted(string id, string name)
        {
            if (string.IsNullOrEmpty(id) || state.leftCommanders.Exists(e => e.id == id)) return;
            state.leftCommanders.Add(new CampaignOutcome.Entry { id = id, name = name ?? "", category = EndingCategory.Left });
        }
        public IReadOnlyList<CampaignOutcome.Entry> LeftCommanders => state.leftCommanders;

        private static bool ValidResourceActive(State s) => s.resourceActive == -1
            || System.Enum.IsDefined(typeof(ResourceType), s.resourceActive) && s.samples != null && s.samples.Contains(s.resourceActive)
                && (s.resourceResearch == null || !s.resourceResearch.Contains(s.resourceActive)) && s.active == -1
                && !float.IsNaN(s.resourceProgress) && s.resourceProgress >= 0 && s.resourceProgress < ResourceResearchWork;
    }
}
