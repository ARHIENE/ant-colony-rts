using System.Linq;
using AntColony.Buildings;
using AntColony.Data;
using UnityEngine;

namespace AntColony.Core
{
    // 2026-10-08 기획: 시민 생산력(연구·주거·시설)과 행정(정치 장수의 실제 업무). 수치는 GameBalance의 잠정값.
    public sealed partial class ColonyPopulation
    {
        // ── 시민 생산력: 주거 종류 배율(살 자리 가중 평균) × (1 + 연구 가산) × (1 + 공공시설 가산).
        public static float HousingProductivity
        {
            get
            {
                float seats = GameBalance.BaseHousing, weighted = GameBalance.BaseHousing;
                foreach (var h in Housing.All) { seats += h.Capacity; weighted += h.Capacity * Housing.Productivity(h.Data.kind); }
                return weighted / seats;
            }
        }
        public static float ResearchProductivity => CampaignResearch.Instance == null ? 0f
            : Mathf.Min(GameBalance.MaxResearchProductivity, CampaignResearch.Technologies.Count(t => CampaignResearch.Instance.Has(t.Technology)) * GameBalance.ResearchProductivity);
        public static float FacilityProductivity => Mathf.Min(GameBalance.MaxFacilityProductivity, Facilities * GameBalance.FacilityProductivity);
        public static float Productivity => HousingProductivity * (1f + ResearchProductivity) * (1f + FacilityProductivity);

        // ── 행정: 행정 장수 없이도 세금·이주는 돈다. 성과 0~1이 징수 손실·민심·이주·재개발을 보정한다.
        public float AdminDemandWork => Total * GameBalance.AdminWorkPerAnt;
        public float Administration => AdminDemandWork <= 0 ? 0f : Mathf.Clamp01(S.adminWork / AdminDemandWork);
        public bool NeedsAdministration => Total > 0 && S.adminWork < AdminDemandWork;
        public float TaxCollection => 1f - GameBalance.AdminTaxLoss * (1f - Administration);
        public void AddAdministration(float work)
        {
            if (work > 0 && !float.IsInfinity(work)) S.adminWork = Mathf.Min(AdminDemandWork, S.adminWork + work);
        }
        // 업무 성과는 시간 상수만큼 천천히 줄어 식사·수면 중에도 바로 사라지지 않는다.
        private void TickAdministration(float seconds)
        {
            S.adminWork *= Mathf.Exp(-seconds / GameBalance.AdminMemorySeconds);
            Redevelopment.TickDelays(this, seconds);
        }
    }
}
