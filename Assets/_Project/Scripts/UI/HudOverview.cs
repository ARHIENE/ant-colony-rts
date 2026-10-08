using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Units;
using UnityEngine;
using UnityEngine.UI;

namespace AntColony.UI
{
    // v4의 선택 없음·다중 선택 요약. 시안 숫자 대신 기존 인구·숙소 규칙을 읽는다.
    public sealed class HudOverview : MonoBehaviour
    {
        private Text summary, workforce;
        private RectTransform screen;
        private SelectionManager selection;
        private float refresh;

        public static CommanderAnt[] HomeCommanders => RosterBar.Commanders.Where(c => !c.IsAwayFromHome).ToArray();
        public static int Beds => Dormitory.All.Where(d => d != null && d.isActiveAndEnabled && !d.IsDead).Sum(d => d.Beds);
        public static int BedShortage => Mathf.Max(0, HomeCommanders.Length - Beds);
        public static int AvailableDraft
        {
            get
            {
                var pool = AntPool.Instance; var population = ColonyPopulation.Instance;
                if (pool == null) return 0;
                if (population == null) return pool.Free;
                var available = pool.Free + (population.S.elderlyService ? population.S.old : 0);
                return Mathf.Max(0, Mathf.Min(available, population.MaxSoldiers(EnemyAlert.CrisisActive) - pool.Assigned));
            }
        }
        public static CommanderAnt[] Selected(SelectionManager manager) => manager == null ? new CommanderAnt[0]
            : manager.GetSelectedObjects().Where(s => s != null && s.isActiveAndEnabled && s.IsSelected)
                .Select(s => s.GetComponent<CommanderAnt>()).Where(c => c != null && c.isActiveAndEnabled && !c.IsDead && c.Data != null).ToArray();

        private void Start()
        {
            selection = FindFirstObjectByType<SelectionManager>();
            screen = MenuTheme.Rect("ColonySummaryScreen", HudConsole.Center);
            MenuTheme.Stretch(screen); screen.offsetMin = new Vector2(12, 8); screen.offsetMax = new Vector2(-12, -14);
            MenuTheme.InsetScreen(screen.gameObject).raycastTarget = false;
            summary = MenuTheme.Text(HudConsole.Center, "", 15); summary.name = "ColonySummary";
            MenuTheme.Stretch(summary.rectTransform); summary.rectTransform.offsetMin = new Vector2(26, 14); summary.rectTransform.offsetMax = new Vector2(-26, -20);
            summary.alignment = TextAnchor.UpperLeft; summary.supportRichText = true;
            summary.verticalOverflow = VerticalWrapMode.Truncate;
            workforce = MenuTheme.Text(transform.Find("ResourceBar"), "", 12); workforce.name = "WorkforceSummary";
            workforce.rectTransform.anchorMin = workforce.rectTransform.anchorMax = workforce.rectTransform.pivot = new Vector2(1, 1);
            // 자원 버튼(-13~-41)·이주 수요 막대 아래, 66px 줄 안쪽(v4.4 장수 초상으로 상단이 높아짐).
            workforce.rectTransform.sizeDelta = new Vector2(310, 18); workforce.rectTransform.anchoredPosition = new Vector2(-8, -42);
            workforce.alignment = TextAnchor.UpperRight; workforce.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private void LateUpdate()
        {
            if (summary == null) return;
            var selected = Selected(selection);
            bool anyUnit = selection != null && selection.GetSelectedObjects().Any(s => s != null && s.IsSelected && s.GetComponent<AntUnitBase>() is AntUnitBase u && !u.IsDead);
            bool visible = !BuildScreen.Picking && WorkTargetPanel.Target == null && (!anyUnit || selected.Length > 1);
            summary.gameObject.SetActive(visible); screen.gameObject.SetActive(visible);
            workforce.text = $"대기 {AntPool.Instance?.Free ?? 0}    동원 가능 {AvailableDraft}";
            if (visible)
                summary.text = selected.Length > 1
                    ? $"<size=21><b>장수 {selected.Length}명 선택</b></size>\n공통으로 가능한 명령만 표시합니다.\n\n"
                        + string.Join(" · ", selected.Take(6).Select(c => c.CommanderName + " (" + (c.IsDeployed ? "출전" : CommanderOverhead.Activity(c) is var a && a != "" ? a : "대기") + ")"))
                        + (selected.Length > 6 ? $" 외 {selected.Length - 6}명" : "")
                        + $"\n\n평시 {selected.Count(c => !c.IsDeployed)}명 · 출전 {selected.Count(c => c.IsDeployed)}명"
                    : $"<size=21><b>군체 현황</b></size>\n장수나 건물을 선택하세요. 평시 장수는 작업표에 따라 일합니다.\n\n대기 인력 {AntPool.Instance?.Free ?? 0} · 추가 동원 가능 {AvailableDraft}\n\n숙소 침대 {Beds} / 장수 {HomeCommanders.Length}"
                        + (BedShortage > 0 ? $"   <color=#ef955f>{BedShortage}명 부족</color>" : "   충분");
            if ((refresh -= Time.unscaledDeltaTime) > 0) return;
            refresh = 1;
            bool shortage = GameSession.Exists && GameSession.Instance.GameStarted && BedShortage > 0;
            ToastManager.SetCrisis("beds", shortage ? $"숙소 부족: 침대 {BedShortage}개 부족 — 장수가 노숙할 수 있습니다." : null, FocusBeds, "숙소로 이동");
        }

        public static void FocusBeds()
        {
            var dorm = Dormitory.All.FirstOrDefault(d => d != null && d.isActiveAndEnabled && !d.IsDead);
            if (dorm == null) { BuildScreen.OpenDormitory(); return; }
            WorkTargetPanel.Select(dorm);
            FindFirstObjectByType<AntColony.Camera.IsometricCameraController>()?.FocusOn(dorm.Position);
        }
        private void OnDestroy() => ToastManager.SetCrisis("beds", null);
    }
}
