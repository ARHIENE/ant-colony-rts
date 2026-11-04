namespace AntColony.Units
{
    // 비행은 이제 별도 클래스가 아니라 현재 보직(UnitRole.Flying)으로 결정되며 SoldierAnt가 직접 처리한다.
    // 이 타입은 기존 씬/에디터 참조를 깨지 않기 위해 남겨둔 껍데기다. 여기에 이동 로직을 다시 넣으면
    // SoldierAnt의 비행 tick과 이중으로 적용되므로 추가 구현을 하지 않는다.
    public class FlyingAnt : SoldierAnt
    {
    }
}
