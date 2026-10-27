using System.Collections.Generic;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using AntColony.World;
using UnityEngine;

namespace AntColony.Buildings
{
    public sealed class UndergroundScanner : BuildingBase, PowerGrid.IUser
    {
        public const float Radius = 35, CandidateSeconds = 300, Watts = 200, BuildSeconds = 45;
        public static (ResourceType type, int amount)[] Cost => new[] {
            (ResourceType.Steel, 60), (ResourceType.Copper, 20), (ResourceType.Component, 6) };
        public CommanderAnt Target { get; private set; }
        public int CandidateId { get; internal set; } = -1;
        public bool Powered { get; set; }
        public float Demand => isActiveAndEnabled && !IsDead ? Watts : 0;
        public IEnumerable<Vector2Int> Cells => RoomSystem.Footprint(this);
        public float Progress => UndergroundSurvey.Instance?.Find(CandidateId)?.work ?? 0;
        public string Status => !ScienceEffects.BuildingUnlocked(BuildingKind.UndergroundScanner) ? "지하 탐사 연구 필요"
            : Target == null ? "담당 장수 없음" : !Powered ? "정전 · 진행 보존"
            : !Target.SurveyWorkReady ? "담당 장수 생활·이동·전투 중 · 진행 보존"
            : CandidateId < 0 ? "범위 안 미조사 후보 없음 · 대기" : $"탐사 {Progress:0}/{CandidateSeconds:0}초";
        protected override void OnEnable() { base.OnEnable(); PowerGrid.Register(this); }
        protected override void OnDisable() { Release(); PowerGrid.Unregister(this); Powered = false; base.OnDisable(); }
        public bool RequestAssignment(CommanderAnt commander)
        {
            if (Target != null || !isActiveAndEnabled || IsDead || commander == null || !commander.CivilianWorkReady
                || !commander.CanChangeAllocation || !commander.AllowsJob(CommanderJobs.Research)
                || !ScienceEffects.BuildingUnlocked(BuildingKind.UndergroundScanner) || !commander.TryWorkApproach(Position, out _)) return false;
            commander.CommandStop(); RestoreAssignment(commander, -1); return true;
        }
        public bool TryAssign(CommanderAnt commander)
        {
            if (!isActiveAndEnabled || IsDead || Target != null || commander == null || !commander.isActiveAndEnabled
                || !commander.SurveyWorkReady || !commander.CanChangeAllocation || commander.IsDeployed || commander.IsAwayFromHome
                || commander.SurveyAssignment != null || !commander.AllowsJob(CommanderJobs.Research) || commander.IsWorking || Vector3.Distance(Position, commander.Position) > 8
                || !ScienceEffects.BuildingUnlocked(BuildingKind.UndergroundScanner)) return false;
            commander.CommandStop(); RestoreAssignment(commander, -1); return true;
        }
        public void Release()
        {
            var c = Target; Target = null;
            if (c != null && c.SurveyAssignment == this) { c.SurveyAssignment = null; c.SetWorkTarget(null); }
            CandidateId = -1;
        }
        internal void RestoreAssignment(CommanderAnt c, int candidate)
        {
            Release(); Target = c; CandidateId = candidate;
            if (c != null) { c.SurveyAssignment = this; c.SetWorkTarget(this); }
        }
        private Vector3 gridPosition;
        private void Update() => Tick(Time.deltaTime);
        public void Tick(float seconds)
        {
            if (!(seconds > 0) || float.IsInfinity(seconds) || Time.timeScale <= 0 || AntColony.Save.SaveSystem.Busy
                || !GameSession.Exists || !GameSession.Instance.GameStarted || !isActiveAndEnabled || IsDead) return;
            if (gridPosition != Position) { gridPosition = Position; Powered = false; PowerGrid.MarkDirty(); }
            if (Target != null && (!Target.isActiveAndEnabled || Target.IsDead || Target.SurveyAssignment != this
                || Target.IsAwayFromHome || Target.IsDeployed || !Target.AllowsJob(CommanderJobs.Research))) Release();
            var survey = UndergroundSurvey.Instance;
            if (survey == null || Target == null) return;
            // 위치 변경·다른 맵 이전 시 예약만 풀고 후보에 누적한 작업은 남긴다.
            if (!survey.InRange(CandidateId, Position)) CandidateId = -1;
            if (CandidateId < 0) CandidateId = survey.Reserve(this);
            if (!Powered || !Target.SurveyWorkReady || Vector3.Distance(Target.Position, Position) > 8
                || !ScienceEffects.BuildingUnlocked(BuildingKind.UndergroundScanner)) return;
            if (CandidateId >= 0)
            {
                survey.Work(CandidateId, seconds * Target.Talents.Multiplier(CommanderActivity.Research));
                Target.GainExperience(CommanderActivity.Research, seconds);
                if (survey.Find(CandidateId).discovered) CandidateId = -1;
            }
        }
        internal static GameObject Template()
        {
            foreach (var b in Resources.FindObjectsOfTypeAll<UndergroundScanner>())
                if (b.gameObject.scene.IsValid() && b.name.EndsWith("Template")) return b.gameObject;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.SetActive(false); go.name = "UndergroundScannerTemplate";
            go.transform.localScale = new Vector3(2, 1, 2); go.GetComponent<Collider>().isTrigger = true;
            var data = ScriptableObject.CreateInstance<BuildingData>(); data.kind = BuildingKind.UndergroundScanner;
            data.displayName = "지하 탐사기"; data.foodCost = data.specialCost = data.constructionAnts = 0;
            data.soilCost = 86; data.buildTimeSeconds = BuildSeconds;
            go.AddComponent<UndergroundScanner>().ConfigureRuntime(data); return go;
        }
    }
}
