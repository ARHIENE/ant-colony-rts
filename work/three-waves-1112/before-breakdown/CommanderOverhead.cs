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
        private readonly System.Collections.Generic.Dictionary<CommanderAnt, SkinnedMeshRenderer[]> bodies = new();
        private Vector3 LabelAnchor(CommanderAnt c, UnityEngine.Camera camera)
        {
            if (!bodies.TryGetValue(c, out var renderers) || Time.frameCount % 120 == 0)
                bodies[c] = renderers = c.GetComponentsInChildren<SkinnedMeshRenderer>();
            var point = camera.WorldToScreenPoint(c.Position + Vector3.up * .8f);
            bool found = false;
            foreach (var renderer in renderers)
            {
                if (renderer == null || !renderer.enabled || renderer.forceRenderingOff || !renderer.gameObject.activeInHierarchy) continue;
                var bounds = renderer.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var projected = camera.WorldToScreenPoint(corner);
                    if (projected.z > 0 && (!found || projected.y > point.y)) { point.y = projected.y; found = true; }
                }
            }
            return point;
        }

        public static bool MoodAlert(CommanderAnt c) => c.PersonalState.mentalBreak != MentalBreak.None || c.Mood <= MoodWarning;

        public static string Activity(CommanderAnt c)
        {
            if (c.PersonalHealth <= 0 || c.Health.Downed) return "쓰러짐";
            if (c.IsInCombat) return "교전";
            if (c.IsFollowingCombatOrder) return "공격 이동";
            if (c.IsFollowingMoveOrder && AntColony.World.WaveDefense.Instance?.InWave == true) return "전투 이동";
            if (c.IsReturning) return "귀환";
            if (c.IsDeployed) return "출전";
            if (c.PersonalState.treating) return "치료";
            if (c.IsAsleep) return "수면";
            if (c.WorkState.resting) return "휴식";
            if (c.IsFoodPoisoned) return "식중독";
            if (c.IsEating) return "식사";
            if (c.IsPlaying) return c.PersonalState.joy.freeWalk ? "산책" : c.PlaySpot != null && !c.PersonalState.joy.groupStarted && !c.PlaySpot.HasMinimumUsers ? "오락 인원 대기" : "오락";
            if (c.CorpseTarget != null) return c.EatingCorpse ? "동족 포식" : "치우기";
            if (c.PersonalState.mentalBreak != MentalBreak.None) return "붕괴";
            if (c.ServiceTarget != null) return c.ServiceJob == CommanderJobs.Firefighting ? "소화" : c.ServiceJob == CommanderJobs.Nursing ? "의료" : c.ServiceJob == CommanderJobs.Repair ? "수리" : c.ServiceJob == CommanderJobs.Administration ? "정치" : c.ServiceJob == CommanderJobs.Crafting ? "제작" : c.ServiceJob == CommanderJobs.Husbandry ? "사육" : c.ServiceJob == CommanderJobs.Hunting ? "훈련" : "요리";
            if (c.HuntTarget != null) return "사냥";
            if (c.CurrentResourceNode != null && c.CurrentResourceNode.IsLooseCargo) return "운반";
            if (c.ConstructionTarget != null && c.ConstructionTarget.IsArt) return "예술";
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
            if (Event.current.type != EventType.Repaint || CommanderRoster.Instance == null || GameMenuController.BlocksInput || BuildScreen.IsOpen || WorldMapPanel.AnyOpen) return;
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
                bool selected = c.GetComponent<SelectableObject>()?.IsSelected == true;
                bool hurt = c.PersonalHealth < GameBalance.CommanderHealth;
                // A healthy, unselected commander still needs a readable identity in the world.
                var p = LabelAnchor(c, camera);
                if (p.z <= 0 || p.x < 0 || p.x > Screen.width || p.y < 0 || p.y > Screen.height) continue;
                float scale = Mathf.Max(.85f, MenuTheme.UiScale), line = 20 * scale;
                nameStyle.font = MenuTheme.Font; nameStyle.fontStyle = FontStyle.Normal;
                nameStyle.fontSize = Mathf.Max(12, Mathf.RoundToInt(13 * scale));
                float width = Mathf.Max(64 * scale, nameStyle.CalcSize(new GUIContent(c.CommanderName)).x + 12 * scale);
                string activity = Activity(c);
                if (activity.Length == 0) activity = "대기";
                float detail = selected ? 17 * scale : 0;
                float height = line + detail + (hurt ? 5 * scale : 0);
                float x = p.x - width * .5f, y = Screen.height - p.y - height - 4 * scale;
                // Do not push labels far above their owner; details remain in the bottom selection dock.
                bool reserved = false;
                float originalY = y;
                for (int lane = 0; lane < 3; lane++)
                {
                    y = originalY - lane * (height + 3 * scale);
                    if (y < 4) break;
                    if (HudResponsiveLayout.ReserveWorldLabel(new Rect(x, y, width, height))) { reserved = true; break; }
                }
                if (!reserved) continue;
                if (y != originalY)
                {
                    var tint = GUI.color; GUI.color = MenuTheme.Muted;
                    GUI.DrawTexture(new Rect(p.x, y + height, 1, originalY - y), Texture2D.whiteTexture);
                    GUI.color = tint;
                }
                var old = GUI.color; GUI.color = new Color(.16f, .15f, .14f, .92f);
                GUI.DrawTexture(new Rect(x, y, width, line + detail), Texture2D.whiteTexture); GUI.color = old;
                nameStyle.normal.textColor = MoodAlert(c) ? new Color(1f, .7f, .5f) : MenuTheme.TextColor;
                GUI.Label(new Rect(x, y, width, line), c.CommanderName, nameStyle);
                GUI.color = selected ? MenuTheme.Accent : MenuTheme.Muted;
                GUI.DrawTexture(new Rect(x, y + line + detail - 2 * scale, width, 2 * scale), Texture2D.whiteTexture);
                GUI.color = old;
                if (selected)
                {
                    tagStyle.font = MenuTheme.Font; tagStyle.fontSize = Mathf.RoundToInt(11 * scale);
                    tagStyle.normal.background = null; tagStyle.normal.textColor = MenuTheme.Muted;
                    GUI.Label(new Rect(x, y + line - 2 * scale, width, detail), activity, tagStyle);
                }
                if (hurt) Bar(new Rect(x + 2 * scale, y + line + detail + scale, width - 4 * scale, 3 * scale), c.PersonalHealth / GameBalance.CommanderHealth, MenuTheme.Hp);


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
