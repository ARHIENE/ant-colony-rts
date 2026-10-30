using AntColony.Data;

namespace AntColony.Core
{
    // 실제 지표 채집·지하 채굴에 연구 관문이 있는 자원만 연구 대상으로 보인다.
    public static class ResourceStudyRules
    {
        public static bool RequiresResearch(ResourceType type) => type is ResourceType.IronOre or ResourceType.CopperOre
            or ResourceType.TinOre or ResourceType.Coal or ResourceType.Sulfur or ResourceType.TitaniumOre
            or ResourceType.StellarOre or ResourceType.CrudeOil or ResourceType.NaturalGas;
    }
}
