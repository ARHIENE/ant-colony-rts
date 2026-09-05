using UnityEngine;

namespace AntColony.Data
{
    public enum UnitRole
    {
        Worker,
        Melee,
        Ranged,
        Defense,
        Flying,
        Support
    }

    [CreateAssetMenu(fileName = "UnitData", menuName = "AntColony/Unit Data")]
    public class UnitData : ScriptableObject
    {
        public string displayName = "Ant";
        public UnitRole role = UnitRole.Worker;

        [Header("Cost / Production")]
        public int foodCost = 10;
        public float buildTimeSeconds = 5f;
        [Min(1)] public int requiredBarracksTier = 1;

        [Header("Stats")]
        public float maxHealth = 20f;
        public float moveSpeed = 3.5f;

        [Header("Worker Only")]
        public float gatherRate = 5f;
        public int carryCapacity = 10;

        [Header("Soldier Only")]
        public float attackDamage = 5f;
        public float attackRange = 1.5f;
        public float attackInterval = 1f;

        [Header("Upkeep")]
        public int foodUpkeep = 1;
    }
}
