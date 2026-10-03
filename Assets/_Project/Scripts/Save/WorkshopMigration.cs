using System.Collections.Generic;
using AntColony.Units;

namespace AntColony.Save
{
    internal static class WorkshopMigration
    {
        internal static bool Upgrade(SaveFileV1 file, out string error)
        {
            error = "Invalid legacy equipment inventory.";
            if (file.equipmentInventory == null || file.equipmentInventory.Count > 10000 || file.buildings == null) return false;
            file.equipmentLoot = new List<EquipmentLootDto>();
            foreach (var b in file.buildings)
            {
                if (b == null) return false;
                b.workshop = new Buildings.Workshop.State();
            }
            if (file.equipmentInventory.Count > EquipmentInventory.Capacity)
            {
                // 이전 무제한 보관함의 초과 장비는 본거지 여왕방 옆에 보존한다.
                var home = file.buildings.Find(b => b.kind == "QueenChamber");
                if (home?.position == null || !home.position.IsFinite()) return false;
                file.equipmentLoot.Add(new EquipmentLootDto { position = new Vec3Dto(home.position.ToVector3() + UnityEngine.Vector3.right * 4),
                    items = file.equipmentInventory.GetRange(EquipmentInventory.Capacity, file.equipmentInventory.Count - EquipmentInventory.Capacity) });
                file.equipmentInventory.RemoveRange(EquipmentInventory.Capacity, file.equipmentInventory.Count - EquipmentInventory.Capacity);
            }
            file.version = 4; error = null; return true;
        }
    }
}
