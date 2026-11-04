using System.Linq;
using UnityEngine;

namespace AntColony.Buildings
{
    public static class StartGroundRules
    {
        public static bool OpenFieldAllowed(Vector3 position)
        {
            var cell = new Vector2Int(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.z));
            return OpenFieldAllowed(new[] { cell });
        }
        public static bool OpenFieldAllowed(BuildingBase building) => OpenFieldAllowed(RoomSystem.Footprint(building));
        private static bool OpenFieldAllowed(System.Collections.Generic.IEnumerable<Vector2Int> cells)
        {
            return !AntColony.Core.SceneObjects.All<FloorTile>()
                .Any(f => !f.IsDead && RoomSystem.Footprint(f).Any(cells.Contains));
        }
    }
}
