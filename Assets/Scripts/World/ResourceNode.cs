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

        // 재성장(밭 등). regrowSeconds가 0이면 기존처럼 소진 시 사라진다.
        [Header("Regrowth")]
        [SerializeField, Min(0f)] private float regrowSeconds = 0f;
        [SerializeField, Min(0f)] private float regrowAmount = 0f;

        private float regrowTimer;

        public ResourceType ResourceType => resourceType;
        public bool IsDepleted => amountRemaining <= 0f;
        public bool IsRegrowing => regrowTimer > 0f;

        private void OnEnable()
        {
            Active.Add(this);
            // 갓 지어진 밭은 비어 있는 상태로 시작해 한 번 성장한 뒤 수확 가능해진다.
            if (regrowSeconds > 0f && IsDepleted) regrowTimer = regrowSeconds;
        }

        private void Update()
        {
            if (regrowTimer <= 0f) return;
            regrowTimer -= Time.deltaTime;
            if (regrowTimer <= 0f) amountRemaining = regrowAmount;
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
                if (regrowSeconds > 0f) regrowTimer = regrowSeconds;
                else gameObject.SetActive(false);
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
