using System.Collections.Generic;
using AntColony.Data;
using UnityEngine;

namespace AntColony.World
{
    public class ResourceNode : MonoBehaviour
    {
        private static readonly List<ResourceNode> Active = new List<ResourceNode>();

        [SerializeField] private ResourceType resourceType = ResourceType.Food;
        [SerializeField] private float amountRemaining = 200f;

        public ResourceType ResourceType => resourceType;
        public bool IsDepleted => amountRemaining <= 0f;

        private void OnEnable()
        {
            Active.Add(this);
        }

        private void OnDisable()
        {
            Active.Remove(this);
        }

        public float Extract(float amount)
        {
            var extracted = Mathf.Min(amount, amountRemaining);
            amountRemaining -= extracted;
            if (amountRemaining <= 0f)
            {
                gameObject.SetActive(false);
            }
            return extracted;
        }

        public static ResourceNode FindNearestActive(Vector3 from)
        {
            ResourceNode nearest = null;
            var nearestDistSqr = float.MaxValue;
            foreach (var node in Active)
            {
                if (node == null || node.IsDepleted) continue;
                var distSqr = (node.transform.position - from).sqrMagnitude;
                if (distSqr < nearestDistSqr)
                {
                    nearestDistSqr = distSqr;
                    nearest = node;
                }
            }
            return nearest;
        }
    }
}
