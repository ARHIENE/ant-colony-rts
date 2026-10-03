namespace AntColony.Buildings
{
    // Phase 4(2026-10-01): 여왕방 삭제. 시작 지점의 비축더미는 자원 반납만 받는다(일반개미는 이주로 늘어남).
    // 기존 씬·저장 호환을 위해 여왕방 스크립트 GUID를 그대로 물려받았다.
    public class Stockpile : BuildingBase
    {
        protected override bool IsDepositPoint => true;
    }
}
