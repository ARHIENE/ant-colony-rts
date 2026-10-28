using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Map;
using AntColony.Save;
using AntColony.World;
using AntColony.Units;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

public static class ExpeditionTerrain1028Checks
{
    public static async Task<string> Gather()
    {
        if (!Application.isPlaying) throw new Exception("Run in Play");
        checks = 0; var root = SaveStorage.RootOverride; var speed = Time.timeScale;
        var autoPause = UserSettings.Current.autoPauseThreat;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "ExpeditionGather1028-" + Guid.NewGuid().ToString("N"));
        try
        {
            await Ready();
            var candidate = StartingCandidate.Roll(0); candidate.traits.values.Clear();
            SaveSystem.NewGame(new NewGameOptions { seed = 281028, startingCount = 1, startingTeam = new[] { candidate } });
            await Ready();
            AntColony.UI.GameMenuController.Instance.Resume(); Time.timeScale = 1;
            UserSettings.Current.autoPauseThreat = false;
            var c = CommanderRoster.Instance.Commanders[0];
            c.SetJobEnabled(CommanderJobs.All, false); c.SetJobEnabled(CommanderJobs.Hauling, true); c.CommandStop();
            var world = WorldMapManager.Instance;
            var ship = world.CreateTransport(false, c.Position + Vector3.right * 2);
            Check(ship.TryBoard(new[] { c }), "boarding accepted");
            Check(ship.Crew.Contains(c) && ship.PendingCrewOrderCount == 0, "boarding completed");
            var field = world.Sites.First(s => s.Kind == ExpeditionSiteKind.ResourceSite && WorldMapManager.SiteRevealed(s));
            Check(ship.TryDepart(field), "depart to resource site"); ship.Tick(ship.TravelSeconds);
            await Task.Delay(200); // LandCrew re-enables its NavMeshAgent; allow registration before the next order.
            Check(ship.State == ExpeditionState.Deployed && !c.IsEmbarked && c.IsAwayFromHome, "land on rebuilt terrain");
            foreach (var node in field.GetComponentsInChildren<ResourceNode>())
            {
                typeof(ResourceNode).GetField("amountRemaining", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(node, 1f);
                var routeReady = DateTime.UtcNow.AddSeconds(10);
                while (!c.TryWorkApproach(node.transform.position, out _) && DateTime.UtcNow < routeReady) await Task.Delay(30);
                Check(c.TryWorkApproach(node.transform.position, out _), "landed agent has resource route");
                c.CommandGather(node);
                Check(c.CurrentResourceNode == node, "resource order accepted " + node.ResourceType
                    + " ready=" + c.CanReceiveOrders + " can=" + node.CanGatherWith(c) + " allowed=" + c.AllowsJob(CommanderAnt.JobFor(node)));
                var end = DateTime.UtcNow.AddSeconds(60);
                while ((!node.IsDepleted || c.IsCarrying || ship.GetCargo(node.ResourceType) != 1) && DateTime.UtcNow < end)
                {
                    c.PersonalState.meal.satiety = 100; c.PersonalState.hygiene.hygiene = 100; c.PersonalState.sleep.fatigue = 0;
                    await Task.Delay(30);
                }
                Check(node.IsDepleted && !c.IsCarrying && ship.GetCargo(node.ResourceType) == 1,
                    "real walk/pickup/return/load " + node.ResourceType + " remaining=" + node.AmountRemaining + " cargo=" + c.CarriedAmount);
            }
            var result = "PASS " + checks + " real transport boarding, landing, two resource pickups and cargo return on rebuilt expedition terrain";
            File.WriteAllText("Temp/expedition-terrain-gather1028-result.txt", result); return result;
        }
        catch (Exception e) { File.WriteAllText("Temp/expedition-terrain-gather1028-result.txt", e.ToString()); throw; }
        finally { SaveStorage.RootOverride = root; Time.timeScale = speed; UserSettings.Current.autoPauseThreat = autoPause; }
    }
    static int checks;
    static void Check(bool ok, string label) { if (!ok) throw new Exception("Expedition terrain FAIL " + checks + " " + label); checks++; }
    static async Task Ready()
    {
        var until = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < until) await Task.Delay(80);
        Check(!SaveSystem.Busy, "scene ready"); await Task.Delay(200);
    }
    static void Terrain(ExpeditionSite site)
    {
        var ground = site.GetComponentInChildren<ExpeditionTerrain>();
        Check(ground != null, "terrain " + site.Title);
        var mesh = ground.GetComponent<MeshFilter>().sharedMesh;
        Check(mesh.vertexCount == 289 && mesh.triangles.Length == 1536, "terrain mesh " + site.Title);
        Check(mesh.vertices.Where(v => Mathf.Abs(v.x) <= 35 && Mathf.Abs(v.z) <= 35).All(v => v.y == 0), "saved walk heights unchanged");
        Check(mesh.vertices.Any(v => v.y < -1) && mesh.bounds.size.x == 78, "sloping outer skirt");
        var box = ground.GetComponent<BoxCollider>();
        Check(box.size == new Vector3(70, .5f, 70) && box.center.y == -.25f && ground.gameObject.layer == 8, "same ground collision");
        Check(box.Raycast(new Ray(site.Landing + Vector3.up * 10, Vector3.down), out var hit, 20) && Mathf.Abs(hit.point.y - site.transform.position.y) < .01f, "landing ground ray");
        Check(NavMesh.SamplePosition(site.Landing, out var landing, 1, NavMesh.AllAreas), "landing navmesh");
        var path = new NavMeshPath();
        Check(NavMesh.CalculatePath(landing.position, site.transform.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete, "centre reachable");
        var material = ground.GetComponent<Renderer>().sharedMaterial;
        Check(material.shader.name == "AntColony/TerrainBlend" && material.GetTexture("terrainTextures") is Texture2DArray array && array.depth == 3, "biome texture layers");
        var scenery = ground.transform.Find("Expedition Scenery");
        Check(scenery != null && scenery.childCount == 16, "sixteen display props");
        Check(scenery.GetComponentsInChildren<ResourceNode>().Length == 0 && scenery.GetComponentsInChildren<Collider>().Length == 0, "scenery has no resources or collision");
        foreach (Transform prop in scenery)
        {
            var bounds = prop.GetComponentsInChildren<Renderer>()[0].bounds;
            var local = ground.transform.InverseTransformPoint(bounds.center);
            Check(Mathf.Max(Mathf.Abs(local.x), Mathf.Abs(local.z)) > 35, "prop outside walkable area");
        }
    }
    public static async Task<string> Main()
    {
        if (!Application.isPlaying) throw new Exception("Run in Play");
        checks = 0; var root = SaveStorage.RootOverride; var speed = Time.timeScale;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "ExpeditionTerrain1028-" + Guid.NewGuid().ToString("N"));
        try
        {
            await Ready();
            var candidate = StartingCandidate.Roll(0); candidate.traits.values.Clear();
            SaveSystem.NewGame(new NewGameOptions { seed = 281028, startingCount = 1, startingTeam = new[] { candidate } });
            await Ready(); Time.timeScale = 0;
            var world = WorldMapManager.Instance;
            Check(world.Sites.Count == 33, "all three-ring sites generated");
            foreach (var site in world.Sites) Terrain(site);
            Check(BiomeRules.All.All(b => world.Sites.Any(s => s.Biome == b)), "all six biomes covered");
            // Reimport/reset recovery: each site's frame update must restore shader bindings.
            var first = world.Sites[0].GetComponentInChildren<ExpeditionTerrain>();
            var mat = first.GetComponent<Renderer>().sharedMaterial;
            mat.SetInt("numTextures", 0); mat.SetTexture("terrainTextures", null);
            await Task.Delay(100);
            Check(mat.GetInt("numTextures") == 3 && mat.GetTexture("terrainTextures") != null, "shader binding recovery");
            var savedGame = GameSession.Instance.GameSeconds;
            var savedClimate = GameSession.Instance.Options.climate;
            try
            {
                foreach (MapClimate climate in Enum.GetValues(typeof(MapClimate)))
                {
                    GameSession.Instance.Options.climate = climate;
                    for (var season = 0; season < 4; season++)
                    {
                        GameSession.Instance.MarkStarted(GameSession.Instance.PlaySeconds, season * GameCalendar.SecondsPerSeason);
                        await Task.Delay(60);
                        foreach (var site in world.Sites.GroupBy(s => s.Biome).Select(g => g.First()))
                        {
                            var m = site.GetComponentInChildren<ExpeditionTerrain>().GetComponent<Renderer>().sharedMaterial;
                            var style = BiomeMapStyle.For(site.Biome);
                            Check(m.GetColor("_Tint") == BiomeRules.GroundTint(site.Biome) * SeasonVisuals.GroundSeasonTint(site.Biome, climate), "site season tint");
                            Check(m.GetFloat("_Snow") == (style.snowGround != null && season == 3 && SeasonVisuals.HasSeasonalSnow(site.Biome, climate) ? 1 : 0), "site snow climate");
                            Check(m.GetFloat("_Leaves") == (style.leafGround != null && season == 2 && SeasonVisuals.HasSeasonalLeaves(site.Biome, climate) ? 1 : 0), "site leaves climate");
                        }
                    }
                }
            }
            finally { GameSession.Instance.Options.climate = savedClimate; GameSession.Instance.MarkStarted(GameSession.Instance.PlaySeconds, savedGame); }
            var food = world.Sites.First(s => s.Kind == ExpeditionSiteKind.ResourceSite).GetComponentsInChildren<ResourceNode>();
            Check(food.Length == 2 && food.All(n => n.GetComponent<Renderer>().forceRenderingOff
                && n.GetComponentsInChildren<Renderer>().Any(r => r.gameObject != n.gameObject && r.enabled && !r.forceRenderingOff)), "resource site cargo models");
            var stocks = food.Select(n => n.AmountRemaining).ToArray();
            Check(SaveSystem.TrySave(false, 0, out var error), "save " + error);
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "load " + error);
            await Ready(); Time.timeScale = 0;
            foreach (var site in WorldMapManager.Instance.Sites) Terrain(site);
            var restored = WorldMapManager.Instance.Sites.First(s => s.Kind == ExpeditionSiteKind.ResourceSite).GetComponentsInChildren<ResourceNode>();
            Check(restored.Select(n => n.AmountRemaining).SequenceEqual(stocks), "resource stocks unchanged by restore");
            var result = "PASS " + checks + " all 33 sites/six biomes, mesh/collision/navmesh/scenery, season-climate matrix and real save-load checks";
            File.WriteAllText("Temp/expedition-terrain1028-result.txt", result); return result;
        }
        catch (Exception e) { File.WriteAllText("Temp/expedition-terrain1028-result.txt", e.ToString()); throw; }
        finally { SaveStorage.RootOverride = root; Time.timeScale = speed; }
    }
}
