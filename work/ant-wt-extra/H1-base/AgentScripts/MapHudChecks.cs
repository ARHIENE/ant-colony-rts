using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

public static class MapHudChecks
{
    static int checks;
    static void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
    static void Invoke(object target, string method) => target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, null);
    static Rect Gui(RectTransform rect)
    {
        var corners = new Vector3[4]; rect.GetWorldCorners(corners);
        var a = RectTransformUtility.WorldToScreenPoint(null, corners[0]); var b = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
        return Rect.MinMaxRect(a.x, Screen.height - b.y, b.x, Screen.height - a.y);
    }
    public static async Task<string> Main()
    {
        checks = 0;
        var controller = UnityEngine.Object.FindAnyObjectByType<AntColony.Camera.IsometricCameraController>();
        var camera = controller.GetComponent<UnityEngine.Camera>();
        var focus = controller.FocusPoint; var yaw = controller.Yaw; var zoom = camera.orthographicSize; var aspect = camera.aspect;
        float time = Time.timeScale; bool enabled = controller.enabled;
        var weather = AntColony.Map.WeatherSystem.Instance.CaptureState();
        var session = AntColony.Core.GameSession.Instance; float seconds = session.GameSeconds, playSeconds = session.PlaySeconds;
        var previousMouse = Mouse.current; Mouse mouse = null;
        var restore = controller.GetType().GetMethod("RestoreView", BindingFlags.Instance | BindingFlags.NonPublic);
        try
        {
            AntColony.UI.GameMenuController.Instance.Resume(); controller.enabled = false; Time.timeScale = 0;
            await Task.Delay(100);
            var minimap = UnityEngine.Object.FindAnyObjectByType<AntColony.UI.Minimap>();
            var rect = (RectTransform)minimap.transform;
            var mapCamera = (UnityEngine.Camera)minimap.GetType().GetField("mapCamera", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(minimap);
            Check(rect.rect.width >= 212 && Mathf.Abs(rect.rect.width - rect.rect.height) < .1f, "larger square map");
            Check(rect.Find("Terrain").GetComponent<RawImage>().texture is Texture2D, "height map replaces cluttered scenery");
            var texture = (Texture2D)rect.Find("Terrain").GetComponent<RawImage>().texture;
            Check(texture.GetPixels32().Select(c => c.ToString()).Distinct().Count() >= 4, "map separates heights and cliff edges");
            var bounds = controller.PanBounds;
            foreach (float x in new[] { .05f, .35f, .5f, .65f, .95f })
                foreach (float y in new[] { .05f, .5f, .95f })
                {
                    var point = mapCamera.ViewportPointToRay(new Vector3(x, y, 0)).origin;
                    controller.FocusOn(point); var expected = controller.FocusPoint;
                    controller.FocusOn(new Vector3(bounds.center.x, 0, bounds.center.y));
                    var local = new Vector3(rect.rect.xMin + rect.rect.width * x, rect.rect.yMin + rect.rect.height * y, 0);
                    var screen = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(local));
                    minimap.OnPointerClick(new PointerEventData(EventSystem.current) { position = screen, button = PointerEventData.InputButton.Left });
                    Check(Vector3.Distance(expected, controller.FocusPoint) < .02f, "click maps to world " + x + "," + y);
                }
            var view = (RectTransform)rect.Find("View");
            foreach (var uv in new[] { new Vector2(.4f, .4f), new Vector2(.6f, .7f), new Vector2(1.2f, -.2f) })
            {
                var point = mapCamera.ViewportPointToRay(new Vector3(Mathf.Clamp01(uv.x), Mathf.Clamp01(uv.y), 0)).origin;
                controller.FocusOn(point); var expected = controller.FocusPoint;
                controller.FocusOn(new Vector3(bounds.center.x, 0, bounds.center.y));
                var local = new Vector3(rect.rect.xMin + rect.rect.width * uv.x, rect.rect.yMin + rect.rect.height * uv.y, 0);
                var data = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(local)), button = PointerEventData.InputButton.Left };
                minimap.OnInitializePotentialDrag(data); Check(!data.useDragThreshold, "map drag starts without threshold");
                minimap.OnDrag(data); Check(Vector3.Distance(expected, controller.FocusPoint) < .02f, "held drag follows pointer and clamps outside map");
                var held = controller.FocusPoint; data.button = PointerEventData.InputButton.Right; minimap.OnDrag(data);
                Check(controller.FocusPoint == held, "right drag does not move camera");
            }
            Check(view.GetComponent<Image>() == null && view.GetComponentsInChildren<Image>().Length == 4, "view rectangle is border only");
            foreach (float rotation in new[] { 0f, 45f, 135f })
                foreach (float size in new[] { 8f, 18f, 35f })
                {
                    restore.Invoke(controller, new object[] { focus, rotation, size }); Invoke(minimap, "LateUpdate");
                    var plane = new Plane(Vector3.up, controller.FocusPoint); var corners = new Vector3[4]; view.GetWorldCorners(corners);
                    var uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right };
                    var ground = new Vector2[4];
                    for (int i = 0; i < 4; i++)
                    {
                        var ray = camera.ViewportPointToRay(uv[i]); Check(plane.Raycast(ray, out float d), "view ray hits focus plane");
                        ground[i] = mapCamera.WorldToViewportPoint(ray.GetPoint(d));
                    }
                    float area = 0; for (int i = 0; i < 4; i++) { var p0 = ground[i]; var p1 = ground[(i + 1) % 4]; area += p0.x * p1.y - p1.x * p0.y; }
                    area = Mathf.Abs(area) * .5f;
                    var localMin = rect.InverseTransformPoint(corners[0]); var localMax = rect.InverseTransformPoint(corners[2]);
                    var actualMin = new Vector2((localMin.x - rect.rect.xMin) / rect.rect.width, (localMin.y - rect.rect.yMin) / rect.rect.height);
                    var actualMax = new Vector2((localMax.x - rect.rect.xMin) / rect.rect.width, (localMax.y - rect.rect.yMin) / rect.rect.height);
                    var box = actualMax - actualMin; var focusUv = (Vector2)mapCamera.WorldToViewportPoint(controller.FocusPoint);
                    Check(Vector2.Distance((actualMin + actualMax) * .5f, focusUv) < .002f, "outline centered on camera focus at yaw " + rotation);
                    Check(Mathf.Abs(box.x / box.y - Mathf.Max(1f, camera.aspect)) < .02f, "outline has the screen's horizontal aspect at yaw " + rotation);
                    Check(Mathf.Abs(box.x * box.y - area) < area * .02f, "outline area matches visible ground at size " + size);
                    Check(Quaternion.Angle(view.localRotation, Quaternion.identity) < .001f, "view outline stays horizontal after rotation");
                    Check(Quaternion.Angle(rect.Find("Terrain").localRotation, Quaternion.identity) < .01f, "top-down map stays north-up");
                }
            restore.Invoke(controller, new object[] { new Vector3(bounds.center.x, focus.y, bounds.center.y), yaw, 18f });
            mouse = InputSystem.AddDevice<Mouse>(); mouse.MakeCurrent();
            InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(Screen.width - 1, Screen.height / 2) }); InputSystem.Update();
            controller.ApplySettings(25, 18); var start = controller.FocusPoint;
            Invoke(controller, "HandleEdgePan");
            Check(Mathf.Abs(Vector3.Distance(start, controller.FocusPoint) - 50 * Time.unscaledDeltaTime) < .02f, "edge pan 50m/s while paused");
            controller.ApplySettings(25, 0); start = controller.FocusPoint; Invoke(controller, "HandleEdgePan");
            Check(controller.FocusPoint == start, "zero thickness disables edge pan");
            var hud = GameObject.Find("HUDCanvas").transform;
            foreach (string name in new[] { "ResourceBar", "HudClock", "ConsoleLeft", "V6Dock" })
                Check(AntColony.UI.HudResponsiveLayout.OverlapsHud(Gui((RectTransform)hud.Find(name))), "world labels suppressed under " + name);
            AntColony.UI.ToastManager.Show("HUD 겹침 검사", AntColony.UI.ToastKind.Warning); await Task.Delay(150);
            var toasts = UnityEngine.Object.FindObjectsByType<RectTransform>().First(t => t.name == "Toasts");
            Check(AntColony.UI.HudResponsiveLayout.OverlapsHud(Gui(toasts)), "world labels suppressed under alerts");
            Check(!AntColony.UI.HudResponsiveLayout.OverlapsHud(new Rect(Screen.width / 2 - 5, Screen.height / 2 - 5, 10, 10)), "open terrain labels preserved");
            var offscreen = new Rect(-100, -100, 10, 10);
            Check(AntColony.UI.HudResponsiveLayout.ReserveWorldLabel(offscreen), "first world label reserves its area");
            Check(!AntColony.UI.HudResponsiveLayout.ReserveWorldLabel(offscreen), "overlapping world labels are suppressed");
            AntColony.Map.WeatherSystem.Instance.Set(AntColony.Map.WeatherKind.Rain);
            var fall = UnityEngine.Object.FindObjectsByType<AntColony.Map.Precipitation>().First(p => p.name == "Weather Fall");
            var particles = fall.GetComponent<ParticleSystem>();
            fall.Set(900, new Color(.95f, .5f, .15f, 1), .3f, 3); particles.Emit(20);
            Check(particles.particleCount > 0, "old orange particles reproduced");
            fall.Set(900, Color.white, .35f, 3);
            Check(particles.particleCount == 0 && particles.main.startColor.color == Color.white, "snow clears old leaf/sand colors");
            particles.Emit(20); fall.Set(0, Color.clear, .1f, 5);
            Check(particles.particleCount == 0, "disabled weather clears lingering particles");
            AntColony.Map.WeatherSystem.Instance.Set(AntColony.Map.WeatherKind.Snow);
            Check(particles.main.startColor.color.r == 1 && particles.main.startColor.color.g == 1 && particles.main.startColor.color.b == 1, "actual snow emits white");
            foreach (float a in new[] { 4f / 3, 16f / 9, 2.4f })
                foreach (float size in new[] { 8f, 18f, 35f })
                {
                    camera.aspect = a; camera.orthographicSize = size; Invoke(controller, "ApplyTransform"); Invoke(fall, "LateUpdate");
                    var ps = fall.GetComponent<ParticleSystem>();
                    Check(ps.main.simulationSpace == ParticleSystemSimulationSpace.Local, "rain follows camera without empty trail");
                    var shape = ps.shape.scale;
                    foreach (var corner in new[] { new Vector3(-.5f, -.5f, 0), new Vector3(.5f, .5f, 0) })
                    {
                        var p = camera.WorldToViewportPoint(fall.transform.TransformPoint(Vector3.Scale(corner, shape)));
                        Check(corner.x < 0 ? p.x < 0 && p.y < 0 : p.x > 1 && p.y > 1, "rain covers full viewport " + a + "/" + size);
                    }
                }
            var gen = AntColony.Map.MapGenerator.Active; var material = gen.GetComponent<MeshRenderer>().sharedMaterial;
            var shaderErrors = UnityEditor.ShaderUtil.GetShaderMessages(material.shader).Where(m => m.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error).ToArray();
            Check(shaderErrors.Length == 0, "terrain shader compiles");
            Check(material.GetInt("numTextures") > 1 && material.GetTexture("terrainTextures") != null, "terrain layers remain bound");
            var originalTexture = material.GetTexture("terrainTextures");
            UnityEditor.AssetDatabase.ImportAsset("Assets/_Project/Shaders/TerrainBlend.shader", UnityEditor.ImportAssetOptions.ForceUpdate);
            await Task.Delay(150);
            Check(material.GetInt("numTextures") > 1 && material.GetFloatArray("terrainHeights")?.Length == 32, "shader reimport restores terrain layers");
            Check(material.GetTexture("terrainTextures") == originalTexture, "shader recovery reuses terrain texture");
            session.MarkStarted(playSeconds, AntColony.Core.GameCalendar.SecondsPerDay * 3 + 10); await Task.Delay(150);
            Check(material.GetFloat("_Snow") > .9f && material.GetFloat("terraceStep") > 0, "winter snow retains terrace data");
            return "PASS " + checks + " minimap/camera/HUD/weather checks; terrain appearance still needs SAVE visual review";
        }
        finally
        {
            if (mouse != null) InputSystem.RemoveDevice(mouse); previousMouse?.MakeCurrent();
            session.MarkStarted(playSeconds, seconds);
            AntColony.Map.WeatherSystem.Instance.RestoreState(weather);
            camera.aspect = aspect; restore.Invoke(controller, new object[] { focus, yaw, zoom });
            controller.ApplySettings(AntColony.Core.UserSettings.Current.cameraPanSpeed, AntColony.Core.UserSettings.Current.edgeScrollThickness);
            controller.enabled = enabled; Time.timeScale = time;
        }
    }
}
