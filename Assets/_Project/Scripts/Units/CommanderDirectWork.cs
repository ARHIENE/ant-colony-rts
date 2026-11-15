using AntColony.Buildings;
using AntColony.World;

namespace AntColony.Units
{
    public partial class CommanderAnt
    {
        private ExpeditionTransport FieldWorkTransport => Transport != null ? Transport : Garrison?.DockedTransport;
        private string DirectWorkBlock(CommanderJobs job, bool fieldGather = false)
        {
            // 일반 연구·탐사는 수락한 직접 명령으로 해제할 수 있다.
            // 실제 장수 강화 작업만 완료/취소 전 다른 작업을 차단한다.
            if (!CanReceiveOrders || LabUpgradeLab != null) return "현재 부상·치료·제작 상태에서는 작업을 바꿀 수 없습니다.";
            if (IsDeployed) return "본거지 평시 작업으로 복귀한 뒤 지시하세요.";
            if (IsAwayFromHome && (!fieldGather || IsEmbarked || FieldWorkTransport == null
                || FieldWorkTransport.IsDead || FieldWorkTransport.State != ExpeditionState.Deployed
                || FieldWorkTransport.Site == null || FieldWorkTransport.IsWithdrawing))
                return "현지 도착 후 철수 중이 아닌 원정대에 채집을 지시하세요.";
            if (!AllowsJob(job)) return "작업표에서 해당 작업이 금지됐거나 특성상 수행할 수 없습니다.";
            if (IsAsleep || !fieldGather && IsSleepTime || IsEating) return "식사·수면을 마친 뒤 작업할 수 있습니다.";
            if (MedicalJob == MedJob.Surgery || MedicalJob == MedJob.Rescue) return "수술·구조 운반을 마친 뒤 작업을 바꿀 수 있습니다.";
            return "";
        }

        // 우클릭만 사용한다. 자동 후보 검색은 기존 CommandGather/CommandBuild를 유지한다.
        // 거절된 명령은 진행 중 연구·배송 예약이나 들고 있는 자원을 바꾸지 않는다.
        public bool TryDirectGather(ResourceNode node, out string reason)
        {
            reason = node == null || !node.isActiveAndEnabled ? "대상이 사라졌습니다." : DirectWorkBlock(JobFor(node), IsAwayFromHome);
            if (reason.Length > 0) return false;
            if (IsAwayFromHome)
            {
                var ship = FieldWorkTransport;
                if ((node.transform.position - ship.Site.transform.position).sqrMagnitude > 70 * 70)
                { reason = "현재 원정 지역의 자원을 선택하세요."; return false; }
                if (ship.CargoFull || !TryWorkApproach(ship.Position, out _))
                { reason = "원정 화물 공간과 수송 수단까지의 경로를 확인하세요."; return false; }
            }
            if (!node.CanGatherWith(this, false))
            {
                reason = node.GatheringForbidden ? "채집 금지 대상입니다."
                    : node.IsRegrowing ? "아직 성장 중입니다."
                    : node.RequiresMiningTool && PersonalState.toolTier < ResourceNode.MiningToolTier ? "돌 도구가 필요합니다."
                    : "자원·연구·지역 해금 또는 침수 조건을 확인하세요.";
                return false;
            }
            if (node.IsLooseCargo && !IsAwayFromHome && !HasCargoDestination(node.ResourceType))
            { reason = "해당 자원을 받을 수 있는 창고와 빈 공간이 필요합니다."; return false; }
            if (!TryGatherApproach(node, out _, true))
            { reason = "작업 지점까지 이동할 수 없습니다."; return false; }
            if (CurrentResourceNode == node && IsWorking) return true;
            if (node.NeedsDesignation && !node.RequestWork())
            { reason = "이 대상에는 작업을 지정할 수 없습니다."; return false; }
            LeavePlay(); CommandStop(); DropCargo();
            CommandGather(node);
            if (CurrentResourceNode == node && IsWorking) return true;
            reason = "작업 조건이 바뀌어 시작하지 못했습니다.";
            return false;
        }

        public bool TryDirectBuild(BuildingConstructionSite site, out string reason)
        {
            reason = site == null || !site.isActiveAndEnabled ? "건설 예정지가 사라졌습니다." : DirectWorkBlock(site.Job);
            if (reason.Length > 0) return false;
            if (site.Paused || site.WaitingForUser || !site.CanWork && site.PendingPickup == null)
            { reason = BuildingConstructionSite.Describe(site.WaitReason); return false; }
            if (!TryWorkApproach(site.Position, out _))
            { reason = "건설 지점까지 이동할 수 없습니다."; return false; }
            if (ConstructionTarget == site || DeliverySite == site) return true;
            LeavePlay(); CommandStop(); DropCargo();
            CommandBuild(site);
            if (ConstructionTarget == site || DeliverySite == site) return true;
            reason = "재료 반입 경로 또는 건설 도구를 확인하세요.";
            return false;
        }
    }
}
