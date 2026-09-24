using System;
using System.Linq;
using AntColony.Core;
using AntColony.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class TooltipChecks
{
    public static string Main()
    {
        int checks = 0;
        void Check(bool value, string message)
        { if (!value) throw new Exception("FAIL: " + message); checks++; }

        Check(Application.isPlaying, "Play mode required");
        var menu = GameMenuController.Instance;
        var session = GameSession.Instance;
        Check(menu != null && !session.GameStarted, "start in main menu");
        var data = new PointerEventData(EventSystem.current);
        var tooltip = menu.GetComponentsInChildren<RectTransform>(true).Single(r => r.name == "Tooltip");
        void Hover(MenuTooltip source)
        {
            Check(source != null && !string.IsNullOrWhiteSpace(source.Message), "button has help");
            source.OnPointerEnter(data);
            Check(tooltip.gameObject.activeInHierarchy, "tooltip visible");
            Check(tooltip.GetComponentInChildren<Text>().text == source.Message, "correct help text");
            Check(tooltip.GetComponentsInChildren<Graphic>().All(g => !g.raycastTarget), "help does not block clicks");
            Canvas.ForceUpdateCanvases();
            Check(tooltip.rect.width > 0 && tooltip.rect.height > 0, "visible dimensions");
            source.OnPointerExit(data);
            Check(!tooltip.gameObject.activeSelf, "pointer exit hides help");
        }
        try
        {
            Hover(menu.GetComponentsInChildren<MenuTooltip>().Single(t => t.name == "New Game"));
            session.MarkStarted(); menu.SceneReady(true);
            var scale = Time.timeScale;
            Hover(menu.GetComponentsInChildren<MenuTooltip>().Single(t => t.name == "Menu [Esc]"));
            Check(Time.timeScale == scale, "hover does not pause game");
            var canvas = Object.FindAnyObjectByType<SelectedUnitPanel>().transform;
            var hudButtons = canvas.GetComponentsInChildren<Button>(true).Where(b => b.transform.parent == canvas).ToArray();
            Check(hudButtons.Length >= 13, "HUD controls exist");
            foreach (var button in hudButtons) Hover(button.GetComponent<MenuTooltip>());
            Rect ScreenRect(RectTransform rect)
            {
                var corners = new Vector3[4]; rect.GetWorldCorners(corners);
                return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
            }
            var worldToggle = canvas.Find("WorldMapToggle") as RectTransform;
            var menuButton = menu.GetComponentsInChildren<Button>().Single(b => b.name == "Menu [Esc]");
            Check(!ScreenRect(worldToggle).Overlaps(ScreenRect(menuButton.transform as RectTransform)), "menu does not cover world button");
            foreach (var button in hudButtons)
            {
                var bounds = ScreenRect(button.transform as RectTransform);
                foreach (var other in hudButtons.Where(b => b != button))
                    Check(!bounds.Overlaps(ScreenRect(other.transform as RectTransform)), "HUD buttons do not overlap: " + button.name + " / " + other.name);
            }
            var resourceBar = canvas.Find("ResourceBar");
            foreach (var text in resourceBar.GetComponentsInChildren<Text>())
                Check(text.preferredHeight <= text.rectTransform.rect.height, "resource text fits: " + text.text);
            var unitPanel = canvas.Find("SelectedUnitPanel");
            Check(!ScreenRect(unitPanel as RectTransform).Overlaps(ScreenRect(canvas.Find("COLONY") as RectTransform)), "commander card clears command dock");
            Check(unitPanel.GetComponentsInChildren<Button>(true).All(b => b.GetComponent<MenuTooltip>() != null), "all commander controls have help");
            var source = hudButtons[0].GetComponent<MenuTooltip>();
            source.OnPointerEnter(data); source.gameObject.SetActive(false);
            Check(!tooltip.gameObject.activeSelf, "disabled control clears help");
            source.gameObject.SetActive(true); source.OnPointerEnter(data);
            menu.Pause();
            Check(!tooltip.gameObject.activeSelf, "opening menu clears old help");
            menu.Tooltip("Temporary help"); menu.Resume();
            Check(!tooltip.gameObject.activeSelf, "closing menu clears old help");
            return $"PASS: {checks} tooltip checks";
        }
        finally { session.MarkNotStarted(); menu.Main(); }
    }
}
