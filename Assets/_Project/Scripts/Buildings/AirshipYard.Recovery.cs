using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Core;
using AntColony.Units;
using UnityEngine;

namespace AntColony.Buildings
{
    [Serializable] public sealed class RocketBuilderWork { public string id; public float work; }

    public sealed partial class AirshipYard
    {
        public bool HasFoundation => Phase == RocketPhase.Destroyed && state.babelEligible && !state.foundationUsed;
        internal void RecordBuilder(CommanderAnt builder, float work, float total)
        {
            if (state.foundationUsed || work <= 0 || total <= 0 || !builder.Traits.Has(CommanderTrait.DiamondBuilding)) return;
            state.constructionWork = total;
            state.builders ??= new List<RocketBuilderWork>();
            var entry = state.builders.Find(b => b.id == builder.PersonalState.id);
            if (entry == null) { entry = new RocketBuilderWork { id = builder.PersonalState.id }; state.builders.Add(entry); }
            entry.work = Mathf.Min(total, entry.work + work);
            state.babelEligible |= entry.work >= total * .75f;
        }
        // 바벨은 발사대 기초만 복구한다. 부품·좌석·연료·핵심물·15일 진행은 다시 준비한다.
        public bool TryRebuildFoundation()
        {
            if (!HasFoundation || !isActiveAndEnabled || Data == null || CampaignResearch.Instance?.Departed == true) return false;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "로켓 기초 재건";
            go.transform.SetPositionAndRotation(Position, transform.rotation);
            go.transform.localScale = transform.localScale;
            go.GetComponent<Collider>().isTrigger = true;
            var site = go.AddComponent<BuildingConstructionSite>();
            site.Initialize(gameObject, Data.buildTimeSeconds * .5f);
            site.Require(AntColony.Data.ResourceType.Food, Mathf.CeilToInt(Data.foodCost * .75f));
            site.Require(MainMaterial, Mathf.CeilToInt(Data.soilCost * .75f));
            site.Require(AntColony.Data.ResourceType.Special, Mathf.CeilToInt(Data.specialCost * .75f));
            state = new State { foundationUsed = true };
            RestoreHealth(MaxHealth);
            gameObject.SetActive(false);
            CampaignHistory.Record("로켓 재건", name, "바벨 기초: 자재75%·작업50%, 부품과 핵심물은 재준비");
            return true;
        }
        private void RecordDestruction()
        {
            CampaignHistory.Record("로켓 파괴", name,
                $"준비 경과 {(PreparationSeconds - PreparationRemaining) / GameCalendar.SecondsPerDay:0.00}/15일 · 장수 {passengers.Count}명 구조 필요 · 시민 {OccupiedSeats - passengers.Count}명 사망 · 고정 재산 {state.snapshot?.Wealth ?? 0:0} / 인구 {state.snapshot?.Population ?? 0:0}", true);
        }
        private void DestroyPassengers()
        {
            int citizens = CivilianPassengers;
            state.civilianPassengers = state.civilianYoung = state.civilianOld = 0;
            CampaignHistory.Ants(citizens, true);
            var survivors = passengers.ToArray(); passengers.Clear();
            for (int i = 0; i < survivors.Length; i++)
                if (CampaignOutcome.Survivor(survivors[i])) survivors[i].SurviveRocketDestruction(Position + Vector3.right * (3 + i));
        }
        private static bool ValidFoundation(State s) => RocketSnapshot.Finite(s.constructionWork)
            && (s.builders == null || s.builders.Count <= 10000 && s.builders.All(b => b != null && !string.IsNullOrEmpty(b.id)
                && RocketSnapshot.Finite(b.work) && b.work <= s.constructionWork)
                && s.builders.Select(b => b.id).Distinct().Count() == s.builders.Count)
            && (!s.babelEligible || s.constructionWork > 0 && s.builders != null && s.builders.Any(b => b.work >= s.constructionWork * .75f));
    }
}
