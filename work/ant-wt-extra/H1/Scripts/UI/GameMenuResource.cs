using AntColony.Data;
using AntColony.World;

namespace AntColony.UI
{
    public sealed partial class GameMenuController
    {
        public void ShowResourceNode(ResourceNode node)
        {
            if (node == null) return;
            Screen("채집 대상");
            MenuTheme.Text(content, $"{node.name} · {node.ResourceType.DisplayName()} {node.AmountRemaining:0.#}", 22, 55);
            MenuTheme.Text(content, node.GatheringForbidden ? "채집 금지 — 수동 지시와 자율 작업에서 제외됩니다." : "채집 허용 — 작업표에 따라 장수가 채집합니다.", 18, 60);
            MenuTheme.Text(content, "금지해도 이미 얻은 자원은 보존해 반납합니다. 금지 상태는 저장됩니다.", 16, 50);
            var button = MenuTheme.Button(content, "Toggle Gathering", () => {
                if (node == null) { Resume(); return; }
                node.GatheringForbidden = !node.GatheringForbidden;
                ShowResourceNode(node);
            });
            button.GetComponentInChildren<UnityEngine.UI.Text>().text = node.GatheringForbidden ? "채집 허용" : "채집 금지";
            MenuTheme.Button(content, "Close Resource", Resume).GetComponentInChildren<UnityEngine.UI.Text>().text = "게임 재개";
        }
    }
}
