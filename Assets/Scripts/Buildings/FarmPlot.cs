using AntColony.Core;
using AntColony.Data;
using AntColony.World;
using UnityEngine;

namespace AntColony.Buildings
{
    public enum FarmCrop { Fungus, Honeydew, AdvancedFungus }

    // 새로 지은 밭의 작물과 크기. 기본 균류는 밭 템플릿의 재성장 값을 그대로 쓴다.
    [RequireComponent(typeof(ResourceNode))]
    public sealed class FarmPlot : MonoBehaviour
    {
        public const float WideFactor = 2f;
        public FarmCrop Crop { get; private set; }
        public bool Wide { get; private set; }
        private ResourceNode node;
        private float baseSeconds = -1, baseAmount;

        public void Configure(FarmCrop crop, bool wide)
        {
            node = GetComponent<ResourceNode>();
            if (baseSeconds < 0) { baseSeconds = node.RegrowSeconds; baseAmount = node.RegrowAmount; }
            if (wide && !Wide)
            {
                var scale = transform.localScale; scale.x *= WideFactor; transform.localScale = scale;
                Wide = true;
            }
            Crop = crop;
            node.ConfigureRegrowth(
                crop == FarmCrop.Honeydew ? GameBalance.HoneydewSeconds : crop == FarmCrop.AdvancedFungus ? GameBalance.AdvancedFungusSeconds : baseSeconds,
                crop == FarmCrop.Honeydew ? GameBalance.HoneydewFood : crop == FarmCrop.AdvancedFungus ? GameBalance.AdvancedFungusFood : baseAmount);
        }

        // 가뭄 성장 감소(4단계 이벤트가 사용). 감로는 항상 -20%만 받는다.
        public float DroughtPenalty => Crop == FarmCrop.Honeydew ? .2f : ScienceEffects.DroughtGrowthPenalty;

        private void OnEnable() { node = GetComponent<ResourceNode>(); node.Harvested += OnHarvested; }
        private void OnDisable() { if (node != null) node.Harvested -= OnHarvested; }

        internal void OnHarvested()
        {
            if (Crop == FarmCrop.AdvancedFungus && Random.value < GameBalance.AdvancedFungusSpecialChance)
                ResourceManager.Instance?.Add(ResourceType.Special, GameBalance.AdvancedFungusSpecial);
        }
    }
}
