using System.Collections.Generic;
using AntColony.Core;
using AntColony.Save;
using AntColony.World;
using UnityEngine;

namespace AntColony.UI
{
    // 첫 등장 힌트 토스트(1회, 민트). 이미 본 힌트는 사용자 설정에 남아 다시 뜨지 않고, 설정에서 끌 수 있다.
    // 사건 훅(Trigger)과, 장수 상태처럼 훅이 흩어진 항목을 1초마다 훑는 스캔을 함께 둔다.
    public sealed class FirstHints : MonoBehaviour
    {
        public static readonly (string key, string title, string body)[] All =
        {
            ("invasion", "첫 침입", "적이 본거지에 들어왔습니다. 징집소(E)에서 장수와 병력을 편성해 출전시키세요."),
            ("collapse", "첫 붕괴 경고", "기분이 35 이하인 장수는 정신 붕괴 위험이 있습니다. 휴식·포상으로 기분을 올리세요. 장수 관리(G)의 '기분 경고' 필터로 모아 볼 수 있습니다."),
            ("prisoner", "첫 포로", "포로 수용소의 포로는 회유해 합류시키거나 처형할 수 있습니다."),
            ("injury", "첫 부상", "부상 장수는 커맨드 카드의 '치료'로 빈 침상이 있는 의무실에 보내세요."),
            ("breeding", "첫 번식", "보육실에서 새 장수가 태어났습니다. 부모의 특성과 재능 일부를 물려받습니다."),
            ("worldmap", "월드맵 해금", "M으로 월드맵을 열어 거점에 원정을 보내세요. 행성을 드래그해 돌릴 수 있습니다."),
            ("contact", "첫 문명 접촉", "다른 문명을 만났습니다. 외교 화면에서 협정·거래·선전포고를 할 수 있습니다."),
            ("loyalty", "첫 충성심 경고", "충성심이 20 이하인 장수는 떠나거나 반란을 일으킬 수 있습니다. 포상과 좋은 대우로 충성심을 올리세요."),
        };
        private float scan;

        public static bool Seen(string key) => UserSettings.Current.shownHints.Contains(key);
        public static void Trigger(string key)
        {
            var s = UserSettings.Current;
            if (!s.firstHints || s.shownHints.Contains(key) || SaveSystem.Busy || !GameSession.Exists || !GameSession.Instance.GameStarted) return;
            var hint = System.Array.Find(All, h => h.key == key);
            if (hint.key == null) return;
            s.shownHints.Add(key); UserSettings.Save();
            ToastManager.Show($"[힌트] {hint.title}: {hint.body} (F2 설명서 → {hint.title})", ToastKind.Hint);
        }
        // CampaignHistory 기록 종류와 힌트를 잇는다.
        public static void OnRecord(string kind, string result)
        {
            if (kind == "포로") Trigger("prisoner");
            else if (kind == "접촉") Trigger("contact");
            else if (kind == "합류" && result == "출생") Trigger("breeding");
        }

        private void Update()
        {
            if (!UserSettings.Current.firstHints || SaveSystem.Busy || !GameSession.Exists || !GameSession.Instance.GameStarted) return;
            scan -= Time.unscaledDeltaTime; if (scan > 0) return; scan = 1;
            if (WorldMapManager.Instance != null && WorldMapManager.Instance.Unlocked) Trigger("worldmap");
            if (CommanderRoster.Instance == null) return;
            foreach (var c in CommanderRoster.Instance.Commanders)
            {
                if (c == null || !c.IsColonyMember || c.IsHostile) continue;
                if (c.PersonalState.injuries.Count > 0) Trigger("injury");
                if (c.Traits.Loyalty <= 20) Trigger("loyalty");
            }
        }
    }
}
