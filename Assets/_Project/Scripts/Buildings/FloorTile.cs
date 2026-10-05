using System.Collections.Generic;
using UnityEngine;

namespace AntColony.Buildings
{
    // 바닥(2026-10-05): 1칸 타일. 위를 지나면 이동이 빨라지고, 방 점수를 올리며, 바닥 깔린 방에서 먹으면 식중독이 줄어든다(청결). 수치 잠정.
    public sealed class FloorTile : BuildingBase
    {
        private static readonly HashSet<Vector2Int> Cells = new HashSet<Vector2Int>();
        private Vector2Int cell;
        public static bool At(Vector3 p) => Cells.Contains(RoomSystem.Cell(p));
        protected override void OnEnable() { base.OnEnable(); cell = RoomSystem.Cell(Position); Cells.Add(cell); }
        protected override void OnDisable() { Cells.Remove(cell); base.OnDisable(); }
    }
}
