using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AntColony.Buildings
{
    public enum FarmCrop { Fungus, Honeydew, AdvancedFungus }

    // 밭(2026-10-15 V5): 수확 뒤에도 심어진 채 다시 자란다. 뿌리뽑기(확인 후 장수 작업) → 빈 밭, 다시 심기 지시 전까지 비워 둔다.
    // 병충해는 실제 칸 좌표의 상하좌우 밭으로만 퍼진다(대각선 없음). 비료는 성장 주기당 1회, 농사 장수가 넣는다.
    [RequireComponent(typeof(ResourceNode))]
    public sealed class FarmPlot : MonoBehaviour
    {
        public const float WideFactor = 2f;
        // ponytail: 병충해·비료·뿌리뽑기 수치는 기획 미정 잠정값. 확정되면 여기만 고친다.
        public const float PestGrowth = .5f, FertilizedGrowth = 1.5f, UprootSeconds = 4f, FertilizeSeconds = 2f;
        public const float PestCheckSeconds = 30f, PestSpreadChance = .25f, PestOutbreakChance = .005f;

        [Serializable] public sealed class State
        {
            public bool fallow, pest, uproot, fertilize, fertilized;
            public float uprootWork, fertilizeWork, pestTimer;
            public int nextCrop = -1;
        }
        public State Data { get; private set; } = new State();
        public FarmCrop Crop { get; private set; }
        public bool Wide { get; private set; }
        public static readonly List<FarmPlot> All = new List<FarmPlot>();
        private ResourceNode node;
        public ResourceNode Node => node != null ? node : node = GetComponent<ResourceNode>();

        public void Configure(FarmCrop crop, bool wide)
        {
            node = GetComponent<ResourceNode>();
            if (wide && !Wide)
            {
                var scale = transform.localScale; scale.x *= WideFactor; transform.localScale = scale;
                Wide = true;
            }
            Crop = crop;
            node.ConfigureRegrowth(
                crop == FarmCrop.Honeydew ? GameBalance.HoneydewSeconds : crop == FarmCrop.AdvancedFungus ? GameBalance.AdvancedFungusSeconds : GameBalance.FungusSeconds,
                crop == FarmCrop.Honeydew ? GameBalance.HoneydewFood : crop == FarmCrop.AdvancedFungus ? GameBalance.AdvancedFungusFood : GameBalance.FungusFood);
        }
        public void RestoreState(State s) { Data = s ?? new State(); }

        // 가뭄 성장 감소(4단계 이벤트가 사용). 감로는 항상 -20%만 받는다.
        public float DroughtPenalty => Crop == FarmCrop.Honeydew ? .2f : ScienceEffects.DroughtGrowthPenalty;
        public bool Planted => !Data.fallow && !Node.NeedsSowing;
        public bool Growing => Planted && Node.IsRegrowing;
        // ResourceNode.TickGrowth가 곱하는 밭 고유 배율(병충해·비료).
        public float GrowthMultiplier => (Data.pest ? PestGrowth : 1f) * (Data.fertilized ? FertilizedGrowth : 1f);
        public bool NeedsFertilizer => Data.fertilize && !Data.fertilized && Growing && ResourceManager.Instance != null && ResourceManager.Instance.GetAmount(ResourceType.Fertilizer) > 0;
        // 장수가 와서 해야 할 손일(파종·수확 외).
        public bool HasHandWork => Data.uproot || NeedsFertilizer;

        // 상태 → 대기 사유(UI·작업 관리 공용). 빈 문자열이 아니면 성장/작업이 막혀 있다.
        public string Status
        {
            get
            {
                if (Data.uproot) return "뿌리뽑기 지시 대기";
                if (Data.fallow) return "빈 밭 · 다시 심기 필요";
                if (Node.NeedsSowing) return "파종 대기";
                if (ColonyEvents.Flooded(Node)) return "홍수 · 작업 불가";
                if (Node.IsRegrowing && GameCalendar.CurrentSeason == Season.Winter) return "겨울 · 성장 정지";
                if (Node.IsRegrowing) return (Data.pest ? "병충해 · 성장 저하 · " : "") + $"성장 중 {1f - Node.RegrowTimeRemaining / Mathf.Max(1f, Node.RegrowSeconds):P0}" + (Data.fertilized ? " · 비료" : "");
                return Data.pest ? "병충해 · 수확 가능" : "수확 가능";
            }
        }

        public void OrderUproot(bool on) { Data.uproot = on && !Data.fallow; if (!on) Data.uprootWork = 0; }
        public void Replant()
        {
            if (!Data.fallow) return;
            if (Data.nextCrop >= 0) { Configure((FarmCrop)Data.nextCrop, Wide); Data.nextCrop = -1; }
            Data.fallow = false; Node.StartSowing();
        }
        // 작물 변경은 다음 수확(빈 밭이면 다시 심을 때) 이후 적용한다.
        public void SetNextCrop(FarmCrop crop) => Data.nextCrop = crop == Crop ? -1 : (int)crop;

        private void OnEnable() { node = GetComponent<ResourceNode>(); node.Harvested += OnHarvested; All.Add(this); }
        private void OnDisable() { if (node != null) node.Harvested -= OnHarvested; All.Remove(this); }

        internal void OnHarvested()
        {
            if (Crop == FarmCrop.AdvancedFungus && Random.value < GameBalance.AdvancedFungusSpecialChance)
                ResourceManager.Instance?.Add(ResourceType.Special, GameBalance.AdvancedFungusSpecial);
            Data.fertilized = false; // 다음 성장 주기에는 다시 비료가 필요하다
            if (Data.nextCrop >= 0) { var next = (FarmCrop)Data.nextCrop; Data.nextCrop = -1; Configure(next, Wide); Node.RestartGrowth(); }
        }

        private void Update() { if (!AntColony.Save.SaveSystem.Busy) Tick(Time.deltaTime); }
        public void Tick(float seconds)
        {
            if (!(seconds > 0) || float.IsInfinity(seconds)) return;
            var labor = Labor(seconds);
            if (Data.uproot)
            {
                Data.uprootWork += seconds * labor;
                if (Data.uprootWork >= UprootSeconds) Uproot();
            }
            else if (NeedsFertilizer && labor > 0)
            {
                Data.fertilizeWork += seconds * labor;
                // ponytail: 비료는 적용 순간 창고에서 차감(실물 운반 없음). 요리 재료와 함께 실제 운반으로 바꿀 때 같이 고친다.
                if (Data.fertilizeWork >= FertilizeSeconds && ResourceManager.Instance.TrySpend(ResourceType.Fertilizer, 1, ResourceReason.Other))
                { Data.fertilized = true; Data.fertilizeWork = 0; }
            }
            if (!Planted) return;
            Data.pestTimer += seconds;
            while (Data.pestTimer >= PestCheckSeconds)
            {
                Data.pestTimer -= PestCheckSeconds;
                if (!Data.pest && Random.value < PestOutbreakChance) Data.pest = true;
                else if (Data.pest) foreach (var n in Neighbors()) if (n.Planted && !n.Data.pest && Random.value < PestSpreadChance) n.Data.pest = true;
            }
        }

        // 손일을 하는 장수의 작업량(농사 속도). 파종과 같은 방식: 이 밭을 대상으로 작업 동작 중인 장수.
        private float Labor(float seconds)
        {
            if (!HasHandWork) return 0;
            float labor = 0;
            foreach (var unit in AntUnitBase.Active)
                if (unit is CommanderAnt c && c.CivilianWorkReady && c.CurrentResourceNode == Node && c.IsGatheringAnimation)
                { labor += c.WorkRate(CommanderActivity.Farming); c.GainExperience(CommanderActivity.Farming, seconds); }
            return labor;
        }

        private void Uproot()
        {
            Data.uproot = false; Data.uprootWork = 0; Data.fallow = true; Data.pest = false; Data.fertilized = false; Data.fertilizeWork = 0;
            Node.ClearCrop();
        }

        // 실제 칸 좌표로 상하좌우에 맞닿은 다른 밭. 대각선만 닿은 밭은 제외한다.
        public IEnumerable<FarmPlot> Neighbors()
        {
            var b = GetComponent<BuildingBase>(); if (b == null) yield break;
            var mine = new HashSet<Vector2Int>(RoomSystem.Footprint(b));
            foreach (var other in All)
            {
                if (other == this || !other.TryGetComponent<BuildingBase>(out var ob)) continue;
                var cells = RoomSystem.Footprint(ob);
                if (cells.Any(c => !mine.Contains(c) && (mine.Contains(c + Vector2Int.up) || mine.Contains(c + Vector2Int.down) || mine.Contains(c + Vector2Int.left) || mine.Contains(c + Vector2Int.right))))
                    yield return other;
            }
        }
    }
}
