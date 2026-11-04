using AntColony.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AntColony.UI
{
    // 상시 경고는 닫기 가능한 위기 알림과 별도로 표시한다.
    public sealed class RocketSafetyHud : MonoBehaviour
    {
        private RectTransform strip;
        private Text label;
        private void Update()
        {
            var warning = GameSession.Exists && GameSession.Instance.GameStarted ? CampaignOutcome.SingleHarshWarning : null;
            if (strip == null && warning != null)
            {
                var canvas = new GameObject("SurvivalWarning", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
                canvas.transform.SetParent(transform, false);
                var surface = canvas.GetComponent<Canvas>(); surface.renderMode = RenderMode.ScreenSpaceOverlay; surface.sortingOrder = 90;
                var scale = canvas.GetComponent<CanvasScaler>(); scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scale.referenceResolution = new Vector2(1920, 1080); scale.matchWidthOrHeight = .5f;
                strip = MenuTheme.Rect("SingleHarshWarning", canvas.transform);
                strip.anchorMin = new Vector2(.15f, 1); strip.anchorMax = new Vector2(.85f, 1); strip.pivot = new Vector2(.5f, 1);
                strip.sizeDelta = new Vector2(0, 32); strip.anchoredPosition = new Vector2(0, -70);
                MenuTheme.InsetScreen(strip.gameObject).raycastTarget = false;
                label = MenuTheme.Text(strip, "", 13); MenuTheme.Stretch(label.rectTransform);
                label.color = MenuTheme.Warning; label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false;
            }
            if (strip == null) return;
            strip.gameObject.SetActive(warning != null);
            if (warning != null) label.text = warning;
        }
    }
}
