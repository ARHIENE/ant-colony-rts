using AntColony.Core;
using AntColony.Save;
using AntColony.Units;
using UnityEngine;

namespace AntColony.UI
{
    [DefaultExecutionOrder(700)]
    public sealed class SpeechBubbles : MonoBehaviour
    {
        public const int Limit = 3;
        public const float Duration = 2.8f;
        private struct Bubble { public CommanderAnt speaker; public string text; public float until; public int priority; }
        private readonly Bubble[] bubbles = new Bubble[Limit];
        private static SpeechBubbles instance;
        private GUIStyle style;
        private void OnEnable() { instance = this; CommanderPersonalState.MoodChanged += OnMood; }
        private void OnDisable() { CommanderPersonalState.MoodChanged -= OnMood; if (instance == this) instance = null; }
        public static void Clear() { if (instance != null) System.Array.Clear(instance.bubbles, 0, Limit); }

        public static string MoodReason(CommanderAnt c, string reason)
        {
            if (reason.StartsWith("DeathEvent:", System.StringComparison.Ordinal))
            {
                var id = reason.Substring(11);
                if (CommanderRoster.Instance != null)
                    foreach (var other in CommanderRoster.Instance.Commanders)
                        if (other != null && other.PersonalState.id == id) return other.CommanderName + "의 죽음";
                return "동료의 죽음";
            }
            return PersonalDisplayNames.Mood(reason);
        }

        private void OnMood(CommanderPersonalState state, string reason, float value)
        {
            if (!CampaignHistory.Recording || Mathf.Abs(value) < 3 || reason.StartsWith("DeathEvent:") || reason.StartsWith("Downed: ")) return;
            var roster = CommanderRoster.Instance;
            if (roster == null) return;
            foreach (var c in roster.Commanders)
                if (c != null && c.PersonalState == state)
                { Say(c, MoodReason(c, reason) + (value < 0 ? "… 힘들어" : " · 기분이 나아졌어")); return; }
        }

        public static void Say(CommanderAnt c, string text, int priority = 0)
        {
            if (instance == null || !CampaignHistory.Recording || c == null || !c.isActiveAndEnabled || !c.IsColonyMember
                || c.IsCaptive || c.IsEmbarked || c.IsAwayFromHome || c.PersonalHealth <= 0 || c.Health.Downed || c.IsAsleep) return;
            var camera = UnityEngine.Camera.main;
            if (camera == null) return;
            var p = camera.WorldToViewportPoint(c.Position);
            if (p.z <= 0 || p.x < 0 || p.x > 1 || p.y < 0 || p.y > 1) return;
            float now = Time.unscaledTime;
            if (!c.TryStorySpeech(now, priority)) return;
            int slot = -1;
            for (var i = 0; i < Limit; i++)
            {
                var b = instance.bubbles[i];
                if (b.speaker == c) { slot = i; break; }
                if (slot < 0 && (b.speaker == null || b.until <= now)) slot = i;
            }
            if (slot < 0)
                for (var i = 0; i < Limit; i++)
                    if (instance.bubbles[i].priority < priority) { slot = i; break; }
            if (slot < 0) return;
            c.StorySpeechAccepted(now, priority);
            instance.bubbles[slot] = new Bubble { speaker = c, text = text, until = now + Duration, priority = priority };
        }

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint || SaveSystem.Busy || GameMenuController.BlocksInput || BuildScreen.IsOpen || WorldMapPanel.AnyOpen) return;
            var camera = UnityEngine.Camera.main;
            if (camera == null) return;
            if (style == null) style = new GUIStyle(GUI.skin.box) { fontSize = 12, alignment = TextAnchor.MiddleCenter, wordWrap = true,
                padding = new RectOffset(5, 5, 3, 3), normal = { textColor = new Color(.93f, .93f, .9f) } };
            for (var i = 0; i < Limit; i++)
            {
                var b = bubbles[i]; var c = b.speaker;
                if (b.until <= Time.unscaledTime || c == null || !c.isActiveAndEnabled || !c.IsColonyMember || c.IsEmbarked || c.IsAwayFromHome) continue;
                var p = camera.WorldToScreenPoint(c.Position + Vector3.up * 2.2f);
                if (p.z <= 0) continue;
                for (var row = 0; row < 4; row++)
                {
                    var rect = new Rect(p.x - 110, Screen.height - p.y - 82 - row * 38, 220, 36);
                    if (rect.x < 0 || rect.xMax > Screen.width || rect.y < HudResponsiveLayout.TopHeight || rect.yMax > Screen.height) continue;
                    if (!HudResponsiveLayout.ReserveWorldLabel(rect)) continue;
                    GUI.Box(rect, b.text, style); break;
                }
            }
        }
    }
}
