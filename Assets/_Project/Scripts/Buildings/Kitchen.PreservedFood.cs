using AntColony.Data;

namespace AntColony.Buildings
{
    public sealed partial class Kitchen
    {
        // 통합 Food에는 어육 원산지가 없으므로 범용 훈연식으로 명시한다. 목재/노동을 들여 원식량1→1끼, 추가 보존 보너스 없음.
        public static Recipe SmokedFood => new Recipe("훈연식", ResourceType.SmokedFood, 16f, (ResourceType.Food, 1), (ResourceType.Wood, 1));
    }
}
