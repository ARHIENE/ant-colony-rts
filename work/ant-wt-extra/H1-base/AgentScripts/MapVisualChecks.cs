using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using AntColony.Core;
using AntColony.Save;
using AntColony.UI;
using AntColony.Map;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

public static class MapVisualChecks
{
    public static async Task<string> Main()
    {
        var settings = UserSettings.Current.Clone();
        var isolated = settings.Clone(); isolated.autoSaveEnabled = false;
        UserSettings.Apply(isolated, false);
        var errors = new System.Collections.Generic.List<string>();
        Application.LogCallback capture = (message, trace, type) => { if (type == LogType.Error || type == LogType.Exception) errors.Add(message); };
        Application.logMessageReceived += capture;
        var checks = 0;
        void Check(bool ok, string name) { if (!ok) throw new Exception(name); checks++; }
        try
        {
            while (SaveSystem.Busy) await Task.Delay(30);
            SaveSystem.NewGame(new NewGameOptions { seed = 261003, mapSize = MapSize.Small, biome = MapBiome.Garden });
            while (SaveSystem.Busy) await Task.Delay(30);
            GameMenuController.Instance.Resume(); Time.timeScale = 0;
            await Task.Yield(); await Task.Yield();
            var terrain = Object.FindAnyObjectByType<MapGenerator>();
            var mat = terrain.GetComponent<MeshRenderer>().sharedMaterial;
            var textures = (Texture2DArray)mat.GetTexture("terrainTextures");
            Check(textures != null, "texture array missing; " + string.Join(";", errors));
            Check(textures.mipmapCount > 1 && textures.filterMode == FilterMode.Trilinear && textures.anisoLevel >= 8, "terrain filtered mipmaps");
            Check(mat.GetFloat("_textureScale") >= 8, "terrain tiling scale");
            Check(!UnityEditor.ShaderUtil.ShaderHasError(mat.shader), "terrain shader compiles");
            foreach (var kind in new[] { WeatherKind.Clear, WeatherKind.Rain, WeatherKind.Snow, WeatherKind.Blizzard, WeatherKind.Sandstorm })
            {
                WeatherSystem.Instance.Set(kind);
                await Task.Yield(); await Task.Yield();
                foreach (var particle in Object.FindObjectsByType<ParticleSystem>())
                {
                    if (particle.GetComponent<Precipitation>() == null) continue;
                    var velocity = particle.velocityOverLifetime;
                    Check(velocity.x.mode == velocity.y.mode && velocity.y.mode == velocity.z.mode, kind + " velocity modes");
                    var material = particle.GetComponent<ParticleSystemRenderer>().sharedMaterial;
                    var texture = (Texture2D)material.GetTexture("_BaseMap");
                    Check(texture.GetPixel(0, 0).a == 0 && texture.GetPixel(16, 16).a > .9f && material.GetFloat("_ZWrite") == 0, "transparent round particle");
                    particle.Simulate(.1f, true, false);
                }
            }
            WeatherSystem.Instance.Set(WeatherKind.Clear);
            var monster = Object.FindObjectsByType<AntColony.World.WildMonster>().First(m => m.name == "WildMonster");
            Check(monster.GetComponent<NavMeshAgent>().baseOffset >= .49f, "cube feet offset");
            var cam = UnityEngine.Camera.main;
            var controller = cam.GetComponent<AntColony.Camera.IsometricCameraController>();
            var bounds = HomeMapBuilder.CurrentWorldBounds;
            var yaw = typeof(AntColony.Camera.IsometricCameraController).GetField("yaw", BindingFlags.NonPublic | BindingFlags.Instance);
            var oldYaw = yaw.GetValue(controller); var size = cam.orthographicSize;
            try
            {
                foreach (var angle in new[] { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f })
                foreach (var zoom in new[] { 8f, 35f })
                foreach (var corner in new[] { bounds.min, bounds.max, new Vector3(bounds.min.x, 0, bounds.max.z), new Vector3(bounds.max.x, 0, bounds.min.z) })
                {
                    yaw.SetValue(controller, angle); cam.orthographicSize = zoom; controller.FocusOn(corner);
                    foreach (var height in new[] { 0f, 15f })
                    foreach (var viewport in new[] { Vector3.zero, Vector3.one, Vector3.right, Vector3.up })
                    {
                        var ray = cam.ViewportPointToRay(viewport);
                        var plane = new Plane(Vector3.up, Vector3.up * height);
                        Check(plane.Raycast(ray, out var distance), "ground in front of camera");
                        var point = ray.GetPoint(distance);
                        Check(point.x >= bounds.min.x && point.x <= bounds.max.x && point.z >= bounds.min.z && point.z <= bounds.max.z, "viewport stays over terrain");
                    }
                }
            }
            finally { yaw.SetValue(controller, oldYaw); cam.orthographicSize = size; controller.FocusOn(bounds.center); }
            Check(errors.Count == 0, "runtime errors: " + string.Join(";", errors.Take(4)));
            return "PASS " + checks + " map visual checks";
        }
        finally { Application.logMessageReceived -= capture; UserSettings.Apply(settings, false); }
    }
    public static string Inspect()
    {
        var report = new StringBuilder();
        var terrain = Object.FindAnyObjectByType<MapGenerator>();
        var ground = terrain.GetComponent<Collider>();
        report.AppendLine("Terrain " + ground.bounds);
        foreach (var r in Object.FindObjectsByType<MeshRenderer>().OrderBy(r => r.name.Contains("Tree")))
        {
            var mat = r.sharedMaterial;
            if (mat == null) continue;
            var color = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : Color.white;
            if (!(color.r > color.g * 2 && color.r > color.b * 2) && !r.name.Contains("Tree")) continue;
            if (!ground.Raycast(new Ray(r.bounds.center + Vector3.up * 200, Vector3.down), out var hit, 400)) continue;
            var a = r.GetComponentInParent<NavMeshAgent>();
            report.AppendLine($"{r.name} parent={r.transform.parent?.name} bottom={r.bounds.min.y:F2} ground={hit.point.y:F2} pivot={r.transform.position.y:F2} agentOffset={a?.baseOffset} components={string.Join(",",r.GetComponents<Component>().Select(c=>c.GetType().Name))}");
            if (report.Length > 5500) break;
        }
        return report.ToString();
    }
}
