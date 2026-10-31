using System.Collections.Generic;
using System.Linq;
using AntColony.Data;

namespace AntColony.World
{
    public static class StellarSupply
    {
        // 승급 합금80 = 금속80 = 광석160, 표본1. 다른 로켓 필수 비용에는 성휘가 없다.
        public const int RocketOreRequired = 161;
        public static void EnsureHome(List<UndergroundSurvey.Layer> layers)
        {
            int total = layers.Where(l => l.type == (int)ResourceType.StellarOre).Sum(l => l.total);
            // 빈 후보4개·좌표·ID·난수 소비·층당80은 유지하고 유자원 후보만 치환한다.
            // 같은 종류가 2개 이상인 광층부터 바꿔 다른 자원의 유일한 광층 소실을 최소화한다.
            var candidates = layers.AsEnumerable().Reverse()
                .OrderBy(l => layers.Count(o => o.type == l.type) > 1 ? 0 : 1).ToList();
            foreach (var layer in candidates)
            {
                if (total >= RocketOreRequired) break;
                if (layer.type < 0 || layer.type == (int)ResourceType.StellarOre) continue;
                layer.type = (int)ResourceType.StellarOre;
                layer.total = UndergroundSurvey.Amount(ResourceType.StellarOre);
                layer.remaining = layer.total;
                total += layer.total;
            }
        }
        public static string Notice(IEnumerable<UndergroundSurvey.Layer> layers) =>
            layers.Sum(l => l.type == (int)ResourceType.StellarOre ? l.total : 0) < RocketOreRequired
                ? "성휘 공급 주의: 이 저장의 최초 매장 총량은 로켓 경로 최소 광석161(승급160+표본1)보다 적습니다. 기존 광층은 보충하지 않습니다. 진행 불가 가능성이 있으므로 새 게임을 권장합니다."
                : null;
    }
}
