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
        private Text title, detail, countdown;
        private Image timerBackground;
        private Button action;
        private Text actionText;
        private int focusIndex;

        public static HudWave Create(Transform canvas)
        {
            var root = MenuTheme.Panel(canvas, "HudWave", new Vector2(1, 1), new Vector2(220, 60), new Vector2(-14, -20));
            root.gameObject.AddComponent<MenuTooltip>();
            var hud = root.gameObject.AddComponent<HudWave>(); hud.root = root;
            root.gameObject.AddComponent<CanvasGroup>();
            hud.title = Label(root, 12, new Vector2(12, -6), MenuTheme.TextColor); hud.title.fontStyle = FontStyle.Normal; hud.title.font = MenuTheme.NumberFont;
            hud.detail = Label(root, 12, new Vector2(12, -27), MenuTheme.Dim);
            hud.detail.gameObject.SetActive(true);
            hud.title.rectTransform.sizeDelta = new Vector2(114,24);
            var rect = MenuTheme.Rect("WaveAction", root);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-8, -8); rect.sizeDelta = new Vector2(80, 44);
            rect.gameObject.AddComponent<Image>().color = Color.white;
            hud.action = rect.gameObject.AddComponent<Button>(); MenuTheme.StyleButton(hud.action);
            hud.action.onClick.AddListener(hud.OnAction);
            hud.timerBackground = rect.GetComponent<Image>();
            hud.action.transition = Selectable.Transition.None;
            hud.timerBackground.canvasRenderer.SetColor(Color.white);
            hud.countdown = Label(rect, 24, new Vector2(0, 0), MenuTheme.Accent);
            hud.countdown.font = MenuTheme.NumberFont; hud.countdown.fontSize = 24;
            hud.countdown.rectTransform.sizeDelta = new Vector2(80, 42); hud.countdown.alignment = TextAnchor.MiddleCenter;
            hud.actionText = Label(rect, 11, new Vector2(0, -27), MenuTheme.Dim);
            hud.actionText.fontSize = 11; hud.actionText.rectTransform.sizeDelta = new Vector2(80, 16);
            hud.actionText.alignment = TextAnchor.MiddleCenter; hud.actionText.gameObject.SetActive(false);
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
            float duration = wave.Wave == 0 ? WaveDefense.FirstPeace : WaveDefense.Peace;
            float urgency = wave.InWave ? 1 : 1 - Mathf.Clamp01(wave.PeaceLeft / duration);
            root.GetComponent<Image>().color = Color.Lerp(MenuTheme.Hex(0x353e30), MenuTheme.Hex(0x632820), urgency);
            timerBackground.color = Color.Lerp(MenuTheme.Hex(0x242e21), MenuTheme.Hex(0x3e1816), urgency);
            countdown.color = Color.Lerp(MenuTheme.Hex(0xe6ce96), MenuTheme.Hex(0xffb19a), urgency);
            root.Find("Accent").GetComponent<Image>().color = Color.Lerp(MenuTheme.Hex(0x727b5b), MenuTheme.Hex(0xcc674e), urgency);
            if (wave.InWave)
            {
                title.text = $"{wave.Wave}차 습격 진행";
                countdown.text = wave.RemainingEnemies.ToString();
                detail.text = "남은 적";
                actionText.text = "적 위치";
                root.GetComponent<MenuTooltip>().Message = title.text + " · 버튼을 눌러 남은 적 위치를 차례로 확인합니다.";
                action.interactable = wave.AliveEnemies > 0;
                return;
            }
            var seconds = Mathf.CeilToInt(wave.PeaceLeft);
            title.text = wave.Wave == 0 ? "첫 습격까지" : $"{wave.Wave + 1}차 습격까지";
            countdown.text = $"{seconds / 60:00}:{seconds % 60:00}";
            detail.text = !WaveDefense.CanStart && seconds <= 0
                ? (RocketCampaign.SuppressNewCrises ? "로켓 가동 중 보류" : "본거지 장수 부재로 대기")
                : seconds <= 60 ? "습격 임박" : "평화 시간";
            actionText.text = "앞당기기";
            root.GetComponent<MenuTooltip>().Message = detail.text + $" · 적 {WaveDefense.Count(wave.Wave + 1)}마리 예상 · 버튼: 지금 시작. 평화 시간이 끝나면 자동으로 시작합니다.";
            action.interactable = WaveDefense.CanStart;
        }

        private static Text Label(Transform parent, int size, Vector2 position, Color color)
        {
            var text = MenuTheme.Text(parent, "", size);
            var rect = text.rectTransform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position; rect.sizeDelta = new Vector2(142, 20); text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.resizeTextForBestFit = false;
            text.resizeTextMinSize = 10;
            text.resizeTextMaxSize = size;
            return text;
        }
    }
}
