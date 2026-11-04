namespace AntColony.Regression
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading.Tasks;
    using AntColony.Buildings;
    using AntColony.Data;
    using AntColony.World;
    using UnityEngine;
    using UnityEngine.AI;
    using Object = UnityEngine.Object;

    public static class EnemyColonyEconomyChecks
    {
        public static async Task<string> Main()
        {
            if (!Application.isPlaying) throw new Exception("Play mode required.");
            // 새 Play 세션은 메인 메뉴(일시정지)로 시작하므로 필요하면 게임을 직접 시작한다.
            if (!Core.GameSession.Instance.GameStarted)
            {
                while (Save.SaveSystem.Busy) await Task.Delay(50);
                Save.SaveSystem.NewGame(new Core.NewGameOptions());
                while (Save.SaveSystem.Busy) await Task.Delay(50);
                UI.GameMenuController.Instance.Resume(); Time.timeScale = 1;
            }
            var origin = new Vector3(1100f, 0f, 1100f);
            var objects = new List<Object>();
            var nav = default(NavMeshDataInstance);
            GameObject New(string name, Vector3 position)
            {
                var go = new GameObject(name);
                go.transform.position = position;
                objects.Add(go);
                return go;
            }

            try
            {
                var navData = NavMeshBuilder.BuildNavMeshData(
                    NavMesh.GetSettingsByIndex(0),
                    new List<NavMeshBuildSource> { new NavMeshBuildSource {
                        shape = NavMeshBuildSourceShape.Box,
                        size = new Vector3(30f, .2f, 30f),
                        transform = Matrix4x4.TRS(origin - Vector3.up * .1f, Quaternion.identity, Vector3.one)
                    } },
                    new Bounds(origin, new Vector3(30f, 10f, 30f)), Vector3.zero, Quaternion.identity);
                objects.Add(navData);
                nav = NavMesh.AddNavMeshData(navData);

                var root = New("EconomyTestColony", origin);
                root.SetActive(false);
                var colony = root.AddComponent<EnemyColony>();
                Set(colony, "randomizeAtStart", false);

                var buildingObject = New("EconomyTestBuilding", origin);
                buildingObject.transform.SetParent(root.transform);
                var building = buildingObject.AddComponent<BuildingBase>();
                Set(building, "countsTowardPlayerDefeat", false);
                Set(colony, "buildings", new[] { building });

                var foodObject = New("EconomyTestFood", origin);
                foodObject.transform.SetParent(root.transform);
                var food = foodObject.AddComponent<ResourceNode>();
                Set(food, "resourceType", ResourceType.Food);
                Set(food, "amountRemaining", 0f);
                Set(food, "ownerColony", colony);

                var soilObject = New("EconomyTestSoil", origin);
                soilObject.transform.SetParent(root.transform);
                var soil = soilObject.AddComponent<ResourceNode>();
                Set(soil, "resourceType", ResourceType.Soil);
                Set(soil, "amountRemaining", 0f);
                Set(soil, "ownerColony", colony);

                var templateObject = New("EconomyTestRaider", origin);
                templateObject.SetActive(false);
                var invasion = root.AddComponent<ColonyInvasion>();
                Set(invasion, "raiderTemplate", templateObject.AddComponent<WildMonster>());
                Set(invasion, "spawnPoint", root.transform);
                Set(invasion, "firstWaveDelay", 100f);
                Set(invasion, "economyTickSeconds", .05f);
                Set(invasion, "foodIncomePerBuilding", 2);
                Set(invasion, "soilIncomePerBuilding", 1);
                Set(invasion, "expansionFoodCost", 4);
                Set(invasion, "expansionSoilCost", 2);
                Set(invasion, "maxBuildings", 2);
                Set(invasion, "expansionRadius", 4f);

                root.SetActive(true);
                await Until(() => colony.RemainingBuildings == 2, "resource income funds building expansion");
                invasion.enabled = false;

                // 확장·생산은 실제 재고에서만 지불된다. 남은 수치는 틱 타이밍에 따라 달라지므로 비워 놓고 판정한다.
                while (colony.TrySpendResources(1, 0)) { }
                while (colony.TrySpendResources(0, 1)) { }
                Check(!colony.TrySpendResources(1, 0) && !colony.TrySpendResources(0, 1), "enemy cannot overspend stock");
                colony.AddResources(3, 2);
                Check(colony.TrySpendResources(1, 1), "enemy stock can fund production");

                Set(colony, "maxFoodStock", 6f);
                colony.AddResources(1000, 0);
                Check(Mathf.Approximately(colony.GetStock(ResourceType.Food), 6f), "enemy food stock stops at the cap");

                foreach (var target in root.GetComponentsInChildren<BuildingBase>(true))
                    target.TakeDamage(target.MaxHealth);
                Check(colony.IsDefeated, "colony is defeated when every building falls");
                var defeatedStock = colony.GetStock(ResourceType.Food);
                invasion.enabled = true;
                await Task.Delay(300);
                Check(Mathf.Approximately(colony.GetStock(ResourceType.Food), defeatedStock), "defeated colony earns no income");

                return "PASS: enemy income, stock cap, expansion, production budget, and income stop after defeat.";
            }
            finally
            {
                if (nav.valid) nav.Remove();
                for (var i = objects.Count - 1; i >= 0; i--)
                    if (objects[i] != null) Object.DestroyImmediate(objects[i]);
            }
        }

        private static FieldInfo Field(object target, string name)
        {
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (field != null) return field;
            }
            throw new MissingFieldException(target.GetType().Name, name);
        }

        private static void Set(object target, string name, object value) => Field(target, name).SetValue(target, value);
        private static void Check(bool condition, string name)
        {
            if (!condition) throw new Exception("FAIL: " + name);
        }

        private static async Task Until(Func<bool> condition, string name)
        {
            var end = DateTime.UtcNow.AddSeconds(5);
            while (!condition())
            {
                if (DateTime.UtcNow > end) throw new Exception("TIMEOUT: " + name);
                await Task.Delay(25);
            }
        }
    }
}
