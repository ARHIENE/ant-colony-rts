using AntColony.Core;
using AntColony.Units;
using UnityEngine;

namespace AntColony.UI
{
    // 장수 머리 위: 이름 · 개인 체력 바(출전 중 병력 바 추가) · 하는 일 · 기분 경고(위험할 때만).
    // 하는 일은 ActivityIcons의 픽셀 아이콘으로 그린다(Activity()의 이름이 아이콘 키).
    public sealed class CommanderOverhead : MonoBehaviour
    {
        public const float MoodWarning = 35f; // 정신 붕괴 확률이 생기는 기분 구간(CommanderPersonal)과 같다.
        private GUIStyle nameStyle, tagStyle;

        public static bool MoodAlert(CommanderAnt c) => c.PersonalState.mentalBreak != MentalBreak.None || c.Mood <= MoodWarning;

        public static string Activity(CommanderAnt c)
        {
            if (c.IsReturning) return "귀환";
            if (c.IsDeployed) return "출전";
            if (c.PersonalState.treating) return "치료";
            if (c.WorkState.resting) return "휴식";
            if (c.PersonalState.mentalBreak != MentalBreak.None) return "붕괴";
            if (c.ScienceAssignment != null) return "연구";
            if (!c.IsWorking && !c.LabUpgradeBusy && c.CraftingWorkshop == null) return "";
            return c.CurrentActivity switch
            {
                CommanderActivity.Gathering => "채집", CommanderActivity.Building => "건설", CommanderActivity.Farming => "농사",
                CommanderActivity.Fishing => "낚시", CommanderActivity.Crafting => "제작", CommanderActivity.Research => "연구", _ => ""
            };
        }

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint || CommanderRoster.Instance == null || GameMenuController.BlocksInput || WorldMapPanel.AnyOpen) return;
            var camera = UnityEngine.Camera.main;
            if (camera == null) return;
            if (nameStyle == null)
            {
                nameStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 12, fontStyle = FontStyle.Bold };
                tagStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontSize = 11, padding = new RectOffset(2, 2, 0, 0) };
            }
            foreach (var c in CommanderRoster.Instance.Commanders)
            {
                if (c == null || !c.isActiveAndEnabled || c.IsDead || c.IsEmbarked || c.IsAwayFromHome) continue;
                var p = camera.WorldToScreenPoint(c.Position + Vector3.up * 2.2f);
                if (p.z <= 0 || p.x < 0 || p.x > Screen.width || p.y < 0 || p.y > Screen.height) continue;
                float x = p.x - 40, y = Screen.height - p.y - 34;
                nameStyle.normal.textColor = Color.white;
                GUI.Label(new Rect(x - 20, y, 120, 16), c.CommanderName, nameStyle);
                Bar(new Rect(x, y + 16, 80, 5), c.PersonalHealth / GameBalance.CommanderHealth, new Color(.35f, .85f, .4f));
                if (c.IsDeployed) Bar(new Rect(x, y + 22, 80, 5), c.CommandLimit > 0 ? c.TroopHealth / c.CommandLimit : 0, new Color(.95f, .7f, .25f));
                var activity = Activity(c);
                var icon = ActivityIcons.Get(activity);
                if (icon != null) { GUI.Box(new Rect(x + 82, y + 10, 20, 20), GUIContent.none, tagStyle); GUI.DrawTexture(new Rect(x + 84, y + 12, 16, 16), icon); }
                if (MoodAlert(c)) { tagStyle.normal.textColor = new Color(1f, .3f, .25f); GUI.Box(new Rect(x - 20, y + 12, 18, 16), "!", tagStyle); }
            }
        }

        private static void Bar(Rect rect, float fill, Color color)
        {
            var old = GUI.color;
            GUI.color = new Color(0, 0, 0, .6f); GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = color; GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(fill), rect.height), Texture2D.whiteTexture);
            GUI.color = old;
        }
    }
}
