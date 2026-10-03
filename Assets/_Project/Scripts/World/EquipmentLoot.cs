using System.Collections.Generic;
using AntColony.Units;
using UnityEngine;

namespace AntColony.World
{
    public sealed class EquipmentLoot : MonoBehaviour
    {
        public List<EquipmentItem> Items { get; private set; } = new List<EquipmentItem>();
        private CommanderAnt collector;
        public static EquipmentLoot Drop(Vector3 position, IEnumerable<EquipmentItem> items)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "장비 전리품 (우클릭 회수)";
            go.transform.position = position + Vector3.up * .35f; go.transform.localScale = Vector3.one * .7f;
            go.GetComponent<Renderer>().material.color = new Color(.9f, .65f, .2f);
            var loot = go.AddComponent<EquipmentLoot>(); loot.Items.AddRange(items); return loot;
        }
        public bool TryCollect(CommanderAnt c)
        {
            if (c == null || !c.CanChangeEquipment || c.IsCarrying || EquipmentInventory.Instance == null || EquipmentInventory.Instance.Full) return false;
            if (Vector3.Distance(c.Position, transform.position) > 3)
            {
                if (!c.CanReach(transform.position)) return false;
                collector = c; c.CommandMove(transform.position); return true;
            }
            for (int i = Items.Count - 1; i >= 0; i--)
                if (EquipmentInventory.Instance.Add(Items[i])) Items.RemoveAt(i);
            collector = null;
            if (Items.Count == 0) { gameObject.SetActive(false); Destroy(gameObject); }
            return true;
        }
        private void Update()
        {
            if (collector == null || !collector.CanChangeEquipment) { collector = null; return; }
            if (Vector3.Distance(collector.Position, transform.position) <= 3) TryCollect(collector);
        }
    }
}
