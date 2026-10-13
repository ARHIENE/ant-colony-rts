namespace AntColony.Buildings
{
    // Phase 4(2026-10-01): 여왕방 삭제. 시작 지점의 비축더미는 자원 반납만 받는다(일반개미는 이주로 늘어남).
    // 기존 씬·저장 호환을 위해 여왕방 스크립트 GUID를 그대로 물려받았다.
    public class Stockpile : BuildingBase
    {
        protected override bool IsDepositPoint => true;
        // 비축더미는 기본 창고 칸(BaseBin)이다. 못 들어간 몫은 다른 창고, 그래도 넘치면 바닥.
        public override void DepositResources(AntColony.Data.ResourceType type, int amount)
        {
            var rm = AntColony.Core.ResourceManager.Instance; if (rm == null) return;
            var left = amount - rm.AddTo(rm.BaseBin, type, amount);
            if (left > 0) rm.Add(type, left, near: Position);
        }
    }
}
