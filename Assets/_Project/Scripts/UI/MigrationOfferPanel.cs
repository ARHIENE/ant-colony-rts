using AntColony.World;
using UnityEngine;
using UnityEngine.UI;
using L = AntColony.UI.MenuLayout;

namespace AntColony.UI
{
    // 이주 개미떼 합류 제안: 제안이 걸려 있는 동안 화면 위쪽에 수락/거절 버튼을 띄운다(게임은 멈추지 않음).
    public sealed class MigrationOfferPanel : MonoBehaviour
    {
        private RectTransform root;
        private Text text;
        private void Start()
        {
            root = MenuTheme.Panel(transform, "MigrationOffer", new Vector2(.5f, 1), new Vector2(420, 74), new Vector2(0, -112));
            text = L.Label(root, "", 13, 12, 6, 396, 30);
            L.Button(root, "AcceptMigration", "수락", 120, 38, 84, 28, () => ColonyEvents.Instance?.AcceptMigration(), "장수 1명과 일반개미가 합류합니다.", true);
            L.Button(root, "DeclineMigration", "거절", 216, 38, 84, 28, () => ColonyEvents.Instance?.DeclineMigration());
            root.gameObject.SetActive(false);
        }
        private void LateUpdate()
        {
            var events = ColonyEvents.Instance;
            var show = events != null && events.MigrationPending;
            if (root.gameObject.activeSelf != show) root.gameObject.SetActive(show);
            if (show) text.text = $"<b>이주 개미떼</b>가 합류를 청합니다 — 장수 1명 + 일반개미 {EventRules.Migrants}마리 · 남은 시간 {events.MigrationOfferRemaining:0}초";
        }
    }
}
