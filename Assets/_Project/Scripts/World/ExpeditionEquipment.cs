using System.Linq;
using AntColony.Units;
using UnityEngine;

namespace AntColony.World
{
    public partial class ExpeditionTransport
    {
        public bool FieldEquipmentReady(CommanderAnt c) => State == ExpeditionState.Deployed && !IsDead && !IsWithdrawing && Route?.IsRunning != true
            && c != null && crew.Contains(c) && c.CanChangeEquipment && !c.IsEmbarked && !c.IsInCombat
            && !c.IsWorking && !c.IsCarrying && Vector3.Distance(c.Position, Position) <= 3;
        // 화물함 앞에서 직접 주고받는다. 소유권은 한 호출에서 이동하므로 이동·취소 중간 상태가 없다.
        public bool EquipFromCargo(CommanderAnt c, EquipmentItem item)
        {
            if (!FieldEquipmentReady(c) || item == null || !item.IsValid || !EquipmentCargo.Contains(item)) return false;
            var old = c.PersonalState.equipment.Find(e => e.slot == item.slot);
            if (!c.CanReplaceEquipment(old, item)) return false;
            EquipmentCargo.Remove(item);
            if (old != null) { c.PersonalState.equipment.Remove(old); EquipmentCargo.Add(old); }
            c.PersonalState.equipment.Add(item);
            if (Walking && CargoLoad + ReservedCargo > CargoCapacity)
            {
                c.PersonalState.equipment.Remove(item); EquipmentCargo.Add(item);
                if (old != null) { EquipmentCargo.Remove(old); c.PersonalState.equipment.Add(old); }
                return false;
            }
            c.RefreshEquipment(); return true;
        }
        public bool ExchangeEquipment(CommanderAnt giver, CommanderAnt receiver, EquipmentItem item)
        {
            if (giver == receiver || !FieldEquipmentReady(giver) || !FieldEquipmentReady(receiver)
                || item == null || !item.IsValid || !giver.PersonalState.equipment.Contains(item)
                || Vector3.Distance(giver.Position, receiver.Position) > 3) return false;
            var old = receiver.PersonalState.equipment.Find(e => e.slot == item.slot);
            if (!giver.CanReplaceEquipment(item, old) || !receiver.CanReplaceEquipment(old, item)) return false;
            giver.PersonalState.equipment.Remove(item);
            if (old != null) { receiver.PersonalState.equipment.Remove(old); giver.PersonalState.equipment.Add(old); }
            receiver.PersonalState.equipment.Add(item);
            if (Walking && CargoLoad + ReservedCargo > CargoCapacity)
            {
                receiver.PersonalState.equipment.Remove(item); giver.PersonalState.equipment.Add(item);
                if (old != null) { giver.PersonalState.equipment.Remove(old); receiver.PersonalState.equipment.Add(old); }
                return false;
            }
            giver.RefreshEquipment(); receiver.RefreshEquipment(); return true;
        }
    }
}
