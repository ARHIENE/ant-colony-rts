using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;
using Resource = AntColony.Data.ResourceType;
using ColonySelection = AntColony.Units.SelectionManager;

public static class AcidTowerChecks
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static async Task Ready()
    {
        var deadline = DateTime.UtcNow.AddSeconds(60);
        while (SaveSystem.Busy && DateTime.UtcNow < deadline) await Task.Delay(100);
        if (SaveSystem.Busy) throw new Exception("Scene load timed out.");
        GameMenuController.Instance.Pause();
    }

    public static async Task<string> Main()
    {
        if (!Application.isPlaying || GameSession.Instance.GameStarted) throw new Exception("Use a fresh Play session.");
        var previousRoot = SaveStorage.RootOverride;
        var root = Path.Combine(Application.temporaryCachePath, "AcidTowerChecks-" + Guid.NewGuid().ToString("N"));
        SaveStorage.RootOverride = root;
        Encyclopedia.ResetCache();
        int passed = 0;
        void Check(bool ok, string message) { if (!ok) throw new Exception("FAIL: " + message); passed++; }
        try
        {
            SaveSystem.NewGame(new NewGameOptions { mapSize = MapSize.Small, seed = 23456 });
            await Ready();
            var template = Object.FindObjectsByType<AcidTower>(FindObjectsInactive.Include).Single(t => t.name == "AcidTowerTemplate");
            Check(!template.gameObject.activeSelf && template.Data.kind == BuildingKind.AcidTower, "inactive building template");
            var placement = Object.FindAnyObjectByType<BuildingPlacementController>();
            var selection = Object.FindAnyObjectByType<ColonySelection>();
            selection.ClearSelection();
            Check(!placement.BeginAcidTowerPlacement(), "requires a selected builder");
            var builder = CommanderRoster.Instance.Commanders.First(c => c.HasTroops && c.CanStartConstruction);
            typeof(ColonySelection).GetMethod("AddToSelection", Private).Invoke(selection, new object[] { builder.GetComponent<AntColony.Units.SelectableObject>() });
            var resources = ResourceManager.Instance; var pool = AntPool.Instance;
            resources.Add(Resource.Food, 200); resources.Add(Resource.Soil, 200);
            var food = resources.GetAmount(Resource.Food); var soil = resources.GetAmount(Resource.Soil); var free = pool.Free;
            var button = Object.FindObjectsByType<UnityEngine.UI.Button>().Single(b => b.name == "Acid TowerButton");
            button.onClick.Invoke();
            Check(placement.IsPlacing, "HUD button starts real construction placement");
            typeof(BuildingPlacementController).GetField("placementValid", Private).SetValue(placement, true);
            var ground = builder.Position + Vector3.forward * 4;
            typeof(BuildingPlacementController).GetMethod("TryPlace", Private).Invoke(placement, new object[] { ground + Vector3.up, ground });
            var site = Object.FindAnyObjectByType<BuildingConstructionSite>();
            Check(site != null && !placement.IsPlacing, "placement creates construction site");
            Check(resources.GetAmount(Resource.Food) == food - template.Data.foodCost && resources.GetAmount(Resource.Soil) == soil - template.Data.soilCost, "construction pays resource costs");
            Check(pool.Free == free - template.Data.constructionAnts && pool.Reserved == template.Data.constructionAnts, "construction reserves ants");
            Check(!Object.FindObjectsByType<AcidTower>().Any(), "unfinished tower cannot attack");
            site.Complete(); builder.CommandStop();
            Check(pool.Free == free && pool.Reserved == 0, "completion returns workforce");
            var tower = Object.FindObjectsByType<AcidTower>().Single();
            Check(tower.CurrentHealth == tower.Data.maxHealth, "completed tower has correct HP");
            tower.transform.position = new Vector3(1400, 0, 1400);
            WildMonster Enemy(string name, float distance)
            {
                var go = new GameObject(name); go.SetActive(false); go.transform.position = builder.Position;
                go.AddComponent<NavMeshAgent>(); var enemy = go.AddComponent<WildMonster>(); enemy.MakeRaider();
                go.SetActive(true); go.GetComponent<NavMeshAgent>().enabled = false;
                go.transform.position = tower.Position + Vector3.right * distance; return enemy;
            }
            var near = Enemy("Tower check near", 2); var far = Enemy("Tower check far", 4);
            var friend = new GameObject("Tower check friendly").AddComponent<BuildingBase>(); friend.transform.position = tower.Position + Vector3.left;
            var friendlyHp = friend.CurrentHealth; var nearHp = near.CurrentHealth; var farHp = far.CurrentHealth;
            tower.Tick(.01f);
            Check(near.CurrentHealth == nearHp - tower.Damage && far.CurrentHealth == farHp, "shoots nearest enemy only");
            Check(friend.CurrentHealth == friendlyHp, "does not attack friendly buildings");
            Check(tower.GetComponentInChildren<LineRenderer>().enabled, "acid spray feedback");
            tower.Tick(.1f); tower.Tick(0); tower.Tick(float.NaN);
            Check(near.CurrentHealth == nearHp - tower.Damage, "cooldown and paused ticks prevent extra damage");
            near.TakeDamage(10000); near.gameObject.SetActive(false);
            tower.Tick(tower.AttackInterval);
            Check(far.CurrentHealth == farHp - tower.Damage, "retargets when enemy dies");
            far.transform.position = tower.Position + Vector3.right * (tower.Range + 2);
            tower.Tick(tower.AttackInterval);
            Check(far.CurrentHealth == farHp - tower.Damage, "does not attack outside range");
            far.transform.position = tower.Position + Vector3.right * 3;
            tower.gameObject.SetActive(false); tower.Tick(10);
            Check(far.CurrentHealth == farHp - tower.Damage, "disabled tower cannot attack");
            tower.gameObject.SetActive(true); tower.Tick(1);
            Check(far.CurrentHealth == farHp - tower.Damage * 2, "reactivated tower resumes attacking");
            Check(resources.GetAmount(Resource.Food) == food - template.Data.foodCost && resources.GetAmount(Resource.Soil) == soil - template.Data.soilCost, "firing has no resource upkeep");
            far.gameObject.SetActive(false); friend.gameObject.SetActive(false);
            Object.Destroy(near.gameObject); Object.Destroy(far.gameObject); Object.Destroy(friend.gameObject);
            tower.TakeDamage(37);
            await Task.Yield();
            foreach (var c in CommanderRoster.Instance.Commanders) c.CommandStop();
            var expectedHp = tower.CurrentHealth; var expectedCooldown = tower.Cooldown;
            var snapshot = SaveSnapshot.Capture();
            var saved = snapshot.buildings.Single(b => b.kind == "AcidTower");
            Check(saved.runtimeBuilt && saved.health == expectedHp && saved.towerCooldown == expectedCooldown, "snapshot includes tower health and cooldown");
            saved.towerCooldown = -1;
            Check(!SaveValidator.Validate(snapshot, out _), "invalid cooldown rejected");
            saved.towerCooldown = expectedCooldown;
            Check(SaveSystem.TrySave(false, 0, out var error), "save succeeds: " + error);
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "load accepted: " + error);
            await Ready();
            var restored = Object.FindObjectsByType<AcidTower>().Single();
            Check(GameSession.Instance.GameStarted && Mathf.Abs(restored.CurrentHealth - expectedHp) < .01f, "tower health restored after scene reload");
            Check(Vector3.Distance(restored.Position, new Vector3(1400, 0, 1400)) < .01f, "tower position restored");
            Check(restored.Cooldown <= expectedCooldown && restored.Cooldown > expectedCooldown - .5f, "cooldown survives reload");
            Check(restored.GetComponent<NavMeshObstacle>().carving, "completed tower obstructs navigation");
            return $"PASS: {passed} acid tower checks (construction, combat, real save/load)";
        }
        finally { SaveStorage.RootOverride = previousRoot; Encyclopedia.ResetCache(); }
    }
}
