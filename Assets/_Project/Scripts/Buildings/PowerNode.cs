using System.Collections.Generic;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;

namespace AntColony.Buildings
{
    // 전력 1차(2026-10-05, 산소미포함식 전선 잇기): 발전기 → 전선 → 가구. 이웃 칸으로 이어진 전력 가구가 한 전력망(PowerGrid).
    // 쳇바퀴 = 장수가 뛰어야 발전(운반 작업), 장작 발전기 = 재료를 태움, 배터리 = 남는 전력 저장, 전등 = 소비.
    public sealed class PowerNode : BuildingBase
    {
        private static readonly List<PowerNode> Active = new List<PowerNode>();
        public static IReadOnlyList<PowerNode> All => Active;
        public BuildingKind Kind => Data.kind;
        public bool IsGenerator => Kind == BuildingKind.Treadmill || Kind == BuildingKind.WoodGenerator;
        public bool IsConsumer => Kind == BuildingKind.ElectricLamp;
        public bool IsBattery => Kind == BuildingKind.Battery;
        public float Charge { get; internal set; }
        public bool Powered { get; internal set; }
        public CommanderAnt Runner { get; private set; }
        private float runnerSeconds, burnSeconds;
        private Light lamp;

        // 지금 내는 전력(W). 쳇바퀴는 최근 0.5초 안에 장수가 뛰었을 때만, 장작 발전기는 태울 재료가 있을 때만.
        public float Output => Kind == BuildingKind.Treadmill ? (runnerSeconds > 0 ? GameBalance.TreadmillWatts : 0)
            : Kind == BuildingKind.WoodGenerator ? (burnSeconds > 0 ? GameBalance.WoodGeneratorWatts : 0) : 0;
        public float Demand => IsConsumer ? GameBalance.LampWatts : 0;
        // 쳇바퀴를 돌릴 필요(PowerGrid.WantsPower).
        public bool NeedsRunner => Kind == BuildingKind.Treadmill && isActiveAndEnabled && !IsDead && PowerGrid.WantsPower(this);

        protected override void OnEnable()
        {
            base.OnEnable(); Active.Add(this); PowerGrid.MarkDirty();
            if (Kind == BuildingKind.ElectricLamp)
            {
                lamp = GetComponent<Light>() ?? gameObject.AddComponent<Light>();
                lamp.type = LightType.Point; lamp.range = 8; lamp.color = new Color(1, .95f, .8f); lamp.intensity = 2; lamp.enabled = false;
            }
        }
        protected override void OnDisable() { Active.Remove(this); Runner = null; Powered = false; PowerGrid.MarkDirty(); base.OnDisable(); }

        // 장수가 쳇바퀴에서 뛰는 한 틱. 필요 없어지면 false(작업 끝).
        public bool Run(CommanderAnt c, float seconds)
        {
            if (Kind != BuildingKind.Treadmill || Runner != null && Runner != c && !Runner.IsDead && Runner.ServiceTarget == this) return false;
            if (!NeedsRunner) { Runner = null; return false; }
            Runner = c; runnerSeconds = .5f;
            c.GainExperience(CommanderActivity.Strength, seconds);
            c.AddFatigue(GameBalance.FatiguePerWorkSecond * GameBalance.TreadmillExtraFatigue * seconds);
            return true;
        }

        private void Update()
        {
            var dt = Time.deltaTime;
            runnerSeconds = Mathf.Max(0, runnerSeconds - dt);
            if (Kind == BuildingKind.WoodGenerator)
            {
                burnSeconds = Mathf.Max(0, burnSeconds - dt);
                // 전력이 필요할 때만 장작(재료 1)을 새로 넣는다.
                if (burnSeconds == 0 && PowerGrid.WantsPower(this) && ResourceManager.Instance != null
                    && ResourceManager.Instance.TrySpend(0, 1, reason: ResourceReason.Production)) burnSeconds = GameBalance.WoodBurnSeconds;
            }
            if (lamp != null) lamp.enabled = Powered && GameCalendar.IsNight;
        }
    }
}
