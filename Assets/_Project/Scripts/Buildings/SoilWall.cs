using AntColony.Core;

namespace AntColony.Buildings
{
    // 흙벽: 공격하지 않는다. NavMesh 장애물(템플릿에서 추가)이 아군 포함 통행을 막고, 산불 확산을 차단한다.
    public sealed class SoilWall : BuildingBase
    {
        public override float Armor => GameBalance.WallArmor;
        protected override bool UsesDefenseDurability => true;
    }
}
