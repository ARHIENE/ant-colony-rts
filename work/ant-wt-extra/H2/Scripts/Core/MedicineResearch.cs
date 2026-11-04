using System.Linq;
using AntColony.Data;

namespace AntColony.Core
{
    public static class MedicineResearch
    {
        // 연구 enum·목록은 공동 작업 담당자가 등록한다. 미등록 상태에서는 상위 약품을 열지 않는다.
        public static ScienceDefinition[] Definitions(ScienceTechnology normal, ScienceTechnology advanced) => new[] {
            new ScienceDefinition(normal, "약제", 2), new ScienceDefinition(advanced, "정밀 약제", 4, normal) };
        public static bool Unlocked(ResourceType type)
        {
            var name = type == ResourceType.MedicineNormal ? "약제" : type == ResourceType.MedicineAdvanced ? "정밀 약제" : null;
            if (name == null) return true;
            var definition = CampaignResearch.Technologies.FirstOrDefault(d => d.Name == name);
            return definition != null && CampaignResearch.Instance?.Has(definition.Technology) == true;
        }
    }
}
