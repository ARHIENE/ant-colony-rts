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
                return seats > 0 ? weighted / seats : 1f;
            }
        }
        public static float ResearchProductivity => CampaignResearch.Instance == null ? 0f
            : Mathf.Min(GameBalance.MaxResearchProductivity, CampaignResearch.Technologies.Count(t => CampaignResearch.Instance.Has(t.Technology)) * GameBalance.ResearchProductivity);
        public static float FacilityProductivity => Mathf.Min(GameBalance.MaxFacilityProductivity, Facilities * GameBalance.FacilityProductivity);
        public static float Productivity => HousingProductivity * (1f + ResearchProductivity) * (1f + FacilityProductivity);

        // G17: 500명당 하루 1업무. 저장된 완료 업무를 소모하며 성과는 하루 필요량 대비 0~1이다.
        public float AdminDemandWork => Total / 500f;
        public float Administration => AdminDemandWork <= 0 ? 0f : Mathf.Clamp01(S.adminWork / AdminDemandWork);
        public bool NeedsAdministration => Total > 0 && S.adminWork < AdminDemandWork;
        public float TaxCollection => 1f - GameBalance.AdminTaxLoss * (1f - Administration);
        // 처리된 행정 업무는 하루 동안 선형 소모된다. 프레임 시작값을 전체 구간에 적용하지 않는다.
        private float AverageTaxCollection(float seconds)
        {
            if (!(seconds > 0)) return TaxCollection;
            var start = Administration;
            var active = Mathf.Min(seconds, start * GameCalendar.SecondsPerDay);
            var end = Mathf.Max(0, start - active / GameCalendar.SecondsPerDay);
            return 1f - GameBalance.AdminTaxLoss + GameBalance.AdminTaxLoss * (start + end) * .5f * active / seconds;
        }
        public void AddAdministration(float work)
        {
            if (work > 0 && !float.IsInfinity(work)) S.adminWork = Mathf.Min(AdminDemandWork, S.adminWork + work);
        }
        // 처리한 업무는 다음 하루 동안 선형 소모한다.
        private void TickAdministration(float seconds)
        {
            S.adminWork = Mathf.Max(0, S.adminWork - AdminDemandWork * seconds / GameCalendar.SecondsPerDay);
            Redevelopment.TickDelays(this, seconds);
        }
    }
}
