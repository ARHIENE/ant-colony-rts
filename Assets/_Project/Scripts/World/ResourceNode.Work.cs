using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;

namespace AntColony.World
{
    public enum NaturalWorkKind { None, Logging, Mining, Harvesting }

    public partial class ResourceNode
    {
        public bool NeedsDesignation => !IsUndergroundLayer && !IsLooseCargo && !requiresFishing && !IsFarm && !IsRaidLoot && GetComponent<Corpse>() == null;
        public bool WorkRequested { get; internal set; }
        public bool WorkPaused { get; internal set; }
        public long WorkRequestOrder { get; internal set; }
        public NaturalWorkKind WorkKind => !NeedsDesignation ? NaturalWorkKind.None
            : resourceType == ResourceType.Wood || resourceType == ResourceType.Resin ? NaturalWorkKind.Logging
            : RequiresMiningTool || resourceType == ResourceType.Soil || resourceType == ResourceType.Sand || resourceType == ResourceType.Clay
                ? NaturalWorkKind.Mining : NaturalWorkKind.Harvesting;
        public string WorkName => WorkKind == NaturalWorkKind.Logging ? "벌목" : WorkKind == NaturalWorkKind.Mining ? "채굴" : "야생 채집";

        public bool RequestWork()
        {
            if (!NeedsDesignation || !isActiveAndEnabled || GatheringForbidden || IsDepleted && !IsRegrowing) return false;
            WorkRequested = true; WorkPaused = false;
            WorkPriorities.RequestedAt(this);
            if (GetComponent<ResourceNodeStatus>() == null) gameObject.AddComponent<ResourceNodeStatus>();
            return true;
        }
        public void PauseWork(bool paused)
        {
            WorkPaused = paused && WorkRequested;
            if (WorkPaused) ReleaseWorkers();
        }
        public void CancelWork()
        {
            WorkRequested = WorkPaused = false;
            WorkRequestOrder = 0;
            ReleaseWorkers();
        }
        private void ReleaseWorkers()
        {
            foreach (var worker in AntUnitBase.Active.OfType<WorkerAnt>().Where(w => w.CurrentResourceNode == this).ToArray())
            {
                worker.CommandStop();
                worker.DropCargo(); // 이미 채취한 물품은 남기고 운반 작업이 회수한다.
            }
        }
        public string WorkWaitReason
        {
            get
            {
                if (!WorkRequested) return "작업 미지정";
                if (WorkPaused) return "일시중지";
                if (GatheringForbidden) return "채집 금지";
                if (!IsUnlocked) return "지역 잠김";
                if (ColonyEvents.Flooded(this)) return "침수";
                if (IsRegrowing) return "성장 대기";
                if (IsDepleted) return "완료";
                var workers = AntUnitBase.Active.OfType<CommanderAnt>().Where(c => c.IsColonyMember && !c.IsAwayFromHome && !c.IsDeployed && c.AllowsJob(CommanderJobs.Gathering)).ToArray();
                if (workers.Length == 0) return "채집 허용 장수 없음";
                if (!workers.Any(CanGatherWith)) return "필요 도구·자원 연구/표본 조건 확인";
                if (!workers.Any(c => CanGatherWith(c) && c.TryWorkApproach(transform.position, out _))) return "경로 차단";
                var assigned = workers.FirstOrDefault(c => c.CurrentResourceNode == this && c.IsWorking);
                return assigned == null ? "담당 장수 대기" : assigned.IsGatheringAnimation ? WorkName + " 중" : "작업 지점으로 이동";
            }
        }
    }
}
