using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using AntColony.UI;

public static class HudV6LayoutChecks
{
    static int checks;
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); checks++; }
    static void Inside(RectTransform rect, RectTransform canvas, string name)
    {
        var corners = new Vector3[4]; rect.GetWorldCorners(corners);
        foreach (var c in corners) { var p = canvas.InverseTransformPoint(c); Check(p.x >= canvas.rect.xMin - 1 && p.x <= canvas.rect.xMax + 1 && p.y >= canvas.rect.yMin - 1 && p.y <= canvas.rect.yMax + 1, name + " outside viewport: " + p); }
    }
    public static async Task<string> Main()
    {
        checks = 0;
        Check(AntColony.Core.GameSession.Instance.GameStarted, "started game");
        var hud = GameObject.Find("HUDCanvas").GetComponent<Canvas>();
        var menus = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).First(c => c.name == "GameMenus");
        var hudMode = hud.renderMode; var menuMode = menus.renderMode;
        var commander = UnityEngine.Object.FindObjectsByType<AntColony.Units.CommanderAnt>(FindObjectsSortMode.None).First(c => !c.IsDead);
        var menu = AntColony.UI.GameMenuController.Instance;
        try
        {
            Check(hud.GetComponent<CanvasScaler>().uiScaleMode == CanvasScaler.ScaleMode.ConstantPixelSize, "HUD pixel scale");
            Check(menus.GetComponent<CanvasScaler>().uiScaleMode == CanvasScaler.ScaleMode.ConstantPixelSize, "menu pixel scale");
            hud.renderMode = menus.renderMode = RenderMode.WorldSpace;
            foreach (var size in new[] { new Vector2(960,720), new Vector2(1280,720), new Vector2(1440,900) })
            {
                var hr = (RectTransform)hud.transform; var mr = (RectTransform)menus.transform;
                hr.sizeDelta = mr.sizeDelta = size;
                Canvas.ForceUpdateCanvases(); await Task.Delay(150);
                var roster = (RectTransform)hud.transform.Find("RosterBar");
                var resources = (RectTransform)hud.transform.Find("ResourceBar/Materials");
                Inside(roster, hr, "roster " + size); Inside(resources, hr, "resources " + size);
                var rc = new Vector3[4]; var tc = new Vector3[4]; roster.GetWorldCorners(rc); resources.GetWorldCorners(tc);
                Check(rc[2].x < tc[0].x, "roster does not overlap resources " + size);
                var toolbar = menus.GetComponentsInChildren<Transform>(true).First(t=>t.name=="MenuToolbar");
                foreach(var button in toolbar.GetComponentsInChildren<Button>(true)) Check(button.GetComponentInChildren<Text>().fontSize == 13, "uniform toolbar font " + button.name);
                foreach (var open in new Action[] { menu.Science, menu.WorkSchedule, menu.Roster, () => menu.ShowEquipment(commander), menu.Population })
                {
                    open(); await Task.Delay(200); Canvas.ForceUpdateCanvases();
                    var fit = menus.GetComponentInChildren<MenuViewport>(); Check(fit != null, "fitted management window");
                    foreach (RectTransform child in fit.transform) if(child.gameObject.activeSelf) Inside(child, mr, menu.ScreenName + " " + size);
                    foreach (var t in fit.GetComponentsInChildren<Text>())
                        if(!string.IsNullOrWhiteSpace(t.text)) Check(t.fontSize * fit.transform.localScale.x >= 11.99f, "readable text " + menu.ScreenName);
                    menu.Resume(); await Task.Delay(100);
                }
                BuildScreen.Open(); await Task.Delay(200);
                var build = hud.GetComponentsInChildren<MenuViewport>().First(v => v.FitSelf);
                Inside((RectTransform)build.transform, hr, "build window " + size);
                BuildScreen.Toggle(); await Task.Delay(100);
            }
            Check(MenuTheme.Plate == MenuTheme.Hex(0x262725), "neutral palette");
            for(int i=0;i<5;i++) ToastManager.Show("V6 알림 검사 " + i + " " + new string('가', 80));
            await Task.Delay(300); Canvas.ForceUpdateCanvases();
            var alerts = UnityEngine.Object.FindObjectsByType<ScrollRect>(FindObjectsSortMode.None).First(r => r.name == "Toasts");
            Check(((RectTransform)alerts.transform).rect.height <= 280.1f, "alerts height capped");
            Check(alerts.viewport.GetComponent<RectMask2D>() != null && alerts.content.rect.height > alerts.viewport.rect.height, "long alerts remain scrollable");
            return "PASS " + checks + " V6 layout/type checks; 960/1280/1440 viewport simulations, 5 real management screens and build";
        }
        finally { hud.renderMode = hudMode; menus.renderMode = menuMode; menu.Resume(); }
    }
}
