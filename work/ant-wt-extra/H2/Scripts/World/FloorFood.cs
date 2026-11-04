using AntColony.Core;
using AntColony.Data;
using UnityEngine;

namespace AntColony.World
{
    // 바닥에 놓인 부패 식품 더미의 신선도(2026-10-12). 바닥은 상온이라 그대로 부패하고, 다 된 몫은 옆에 썩은 식품 더미로 나온다.
    // ResourceNode.ConfigureLoot가 부패 식품 더미에 붙인다(활성화 전에도 Lots를 채울 수 있게 노드는 필요할 때 찾는다).
    [RequireComponent(typeof(ResourceNode))]
    public sealed class FloorFood : MonoBehaviour
    {
        public FoodLots Lots = new FoodLots();
        private ResourceNode node; private float timer;
        private ResourceNode Node => node != null ? node : node = GetComponent<ResourceNode>();
        public FoodLots Sync() { Lots.Match(Mathf.RoundToInt(Node.AmountRemaining), Node.ResourceType); return Lots; }
        private void Update()
        {
            timer += Time.deltaTime; if (timer < 1f) return;
            Age(timer); timer = 0;
        }
        public void Age(float seconds)
        {
            var rotten = Sync().Age(seconds);
            rotten = Mathf.Min(rotten, Mathf.FloorToInt(Node.AmountRemaining + .001f));
            if (rotten <= 0 || !Node.TryConsumeStock(rotten)) return;
            SpoilNotice.Add(Node.ResourceType, rotten);
            DiplomacyManager.DropFloor(ResourceType.RottenFood, rotten, transform.position + Vector3.right * .8f);
            if (Node.IsDepleted) Destroy(gameObject);
        }
    }
}
