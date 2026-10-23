using AntColony.Core;
using AntColony.Data;
using AntColony.World;
using UnityEngine;

namespace AntColony.Buildings
{
    // 수량은 인원과 무관한 작은 보너스(기획 미정 수치의 잠정값).
    public sealed class CrashWreck : BuildingBase
    {
        public void Salvage()
        {
            DiplomacyManager.DropFloor(ResourceType.Wood, 8, Position + Vector3.left);
            DiplomacyManager.DropFloor(ResourceType.Stone, 6, Position + Vector3.right);
            DiplomacyManager.DropFloor(ResourceType.Component, 1, Position + Vector3.forward);
        }
    }
}
