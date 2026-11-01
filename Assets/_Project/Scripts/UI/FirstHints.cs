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
            ("invasion", "첫 침입", "적이 본거지에 들어왔습니다. 징집소 건물을 선택해 장수·병력을 편성하고 출전하세요. 평시 장수는 병력이 없습니다."),
            ("collapse", "첫 붕괴 경고", "기분 35 이하는 정신 붕괴 위험 구간입니다. 장수 관리의 기분 경고 필터에서 원인을 확인하고 식사·수면·오락·포상으로 개선하세요."),
            ("prisoner", "첫 포로", "포로 수용소에서 건강·배고픔·다음 회유 확률을 확인하세요. 회유는 식량을 소비하며 합류가 보장되지 않습니다. 처형은 되돌릴 수 없습니다."),
            ("injury", "첫 부상", "부상 장수를 선택해 휴식·치료를 지시하세요. 의무실의 빈 침상과 의료 허용 장수가 필요합니다. 건강 화면에서 출혈·처치 대기 이유를 확인하세요."),
            ("breeding", "첫 번식", "양육실에서 새 장수가 태어났습니다. 부모의 특성·재능 일부를 물려받습니다. 장수 관리에서 기술과 작업표를 확인하고 잠자리·식량을 준비하세요."),
            ("worldmap", "월드맵 해금", "월드맵에서 거점을 고르고 위험도·거리·보급을 확인하세요. 수송 수단에 장수를 태운 뒤 직접 출정합니다. 행성은 드래그해 돌릴 수 있습니다."),
            ("contact", "첫 문명 접촉", "다른 문명을 만났습니다. 외교에서 호감도 사유·협정 조건·거래 물품을 확인하세요. 선전포고는 직접 결정하며 다른 세력과의 관계에도 영향을 줍니다."),
            ("loyalty", "첫 탈주 경고", "기분이 특성별 이탈 기준 이하로 계속되면 이탈·반란 위험이 있습니다. 장수 상세의 남은 시간과 부정 요인을 확인하고 포상·생활 여건을 개선하세요."),
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
                if (c.Health.Injured) Trigger("injury");
                if (c.Mood <= c.Traits.DepartureMood) Trigger("loyalty");
            }
        }
    }
}
