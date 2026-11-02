using AntColony.Core;
using AntColony.World;
using UnityEngine;
using UnityEngine.UI;

namespace AntColony.UI
{
    // 우상단 시계 아래: 평화(다음 웨이브까지 남은 시간·지금 시작) / 웨이브(남은 적·남은 적 보기).
    public sealed class HudWave : MonoBehaviour
    {
        private RectTransform root;
        private Text title, detail;
        private Button action;
        private Text actionText;
        private int focusIndex;

        public static HudWave Create(Transform canvas)
        {
            var root = MenuTheme.Panel(canvas, "HudWave", new Vector2(1, 1), new Vector2(244, 52), new Vector2(-8, -146));
            var hud = root.gameObject.AddComponent<HudWave>(); hud.root = root;
            root.gameObject.AddComponent<CanvasGroup>();
            hud.title = Label(root, 12, new Vector2(8, -5), MenuTheme.TextColor); hud.title.fontStyle = FontStyle.Bold;
            hud.detail = Label(root, 12, new Vector2(8, -27), MenuTheme.Dim);
            var rect = MenuTheme.Rect("WaveAction", root);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-6, -6); rect.sizeDelta = new Vector2(84, 40);
            rect.gameObject.AddComponent<Image>().color = Color.white;
            hud.action = rect.gameObject.AddComponent<Button>(); MenuTheme.StyleButton(hud.action);
            hud.action.onClick.AddListener(hud.OnAction);
            hud.actionText = MenuTheme.Text(rect, "", 12); MenuTheme.Stretch(hud.actionText.rectTransform);
            hud.actionText.alignment = TextAnchor.MiddleCenter;
            return hud;
        }

        private void OnAction()
        {
            var wave = WaveDefense.Instance; if (wave == null) return;
            if (!wave.InWave) { wave.StartNow(); return; }
            // 남은 적을 차례로 비춘다(전멸해야 끝나므로 숨은 적 찾기용).
            var local = SceneObjects.Any<LocalIncursions>(); if (local == null) return;
            var alive = new System.Collections.Generic.List<WildMonster>();
            foreach (var v in local.Visitors) if (v != null && !v.IsDead && v.isActiveAndEnabled) alive.Add(v);
            if (alive.Count == 0) return;
            focusIndex = (focusIndex + 1) % alive.Count;
            SceneObjects.Any<AntColony.Camera.IsometricCameraController>()?.FocusOn(alive[focusIndex].transform.position);
        }

        private void LateUpdate()
        {
            var wave = WaveDefense.Instance;
            var visible = wave != null && GameSession.Exists && GameSession.Instance.GameStarted && !GameMenuController.BlocksInput;
            var group = root.GetComponent<CanvasGroup>();
            group.alpha = visible ? 1 : 0; group.blocksRaycasts = group.interactable = visible;
            if (!visible) return;
            if (wave.InWave)
            {
                title.text = $"웨이브 {wave.Wave} 방어 중";
                detail.text = $"남은 적 {wave.RemainingEnemies}";
                actionText.text = "남은 적 보기";
                action.interactable = wave.AliveEnemies > 0;
                return;
            }
            var seconds = Mathf.CeilToInt(wave.PeaceLeft);
            title.text = $"평화 · 웨이브 {wave.Wave + 1}까지";
            detail.text = !WaveDefense.CanStart && seconds <= 0
                ? (RocketCampaign.SuppressNewCrises ? "로켓 가동 중 보류" : "본거지 장수 부재로 대기")
                : $"{seconds / 60}:{seconds % 60:00} · 적 {WaveDefense.Count(wave.Wave + 1)}마리 예상";
            actionText.text = "지금 시작";
            action.interactable = WaveDefense.CanStart;
        }

        private static Text Label(Transform parent, int size, Vector2 position, Color color)
        {
            var text = MenuTheme.Text(parent, "", size);
            var rect = text.rectTransform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position; rect.sizeDelta = new Vector2(150, 20); text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            return text;
        }
    }
}
