namespace AntColony.Regression
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading.Tasks;
    using AntColony.Buildings;
    using AntColony.World;
    using UnityEngine;
    using UnityEngine.AI;
    using Object = UnityEngine.Object;

    public static class EnemyColonyPlacementChecks
    {
        public static async Task<string> Main()
        {
            if (!Application.isPlaying) throw new Exception("Play mode required.");

            var origin = new Vector3(1200f, 0f, 1200f);
            var objects = new List<Object>();
            var randomState = UnityEngine.Random.state;
            var nav = default(NavMeshDataInstance);
            var checks = 0;
            void Check(bool condition, string name)
            {
                if (!condition) throw new Exception("FAIL: " + name);
                checks++;
            }
            GameObject New(string name, Vector3 position)
            {
                var go = new GameObject(name);
                go.transform.position = position;
                objects.Add(go);
                return go;
            }

            try
            {
                var source = new NavMeshBuildSource
                {
                    shape = NavMeshBuildSourceShape.Box,
                    size = new Vector3(60f, .2f, 60f),
                    transform = Matrix4x4.TRS(origin - Vector3.up * .1f, Quaternion.identity, Vector3.one)
                };
                var data = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByIndex(0),
                    new List<NavMeshBuildSource> { source }, new Bounds(origin, new Vector3(60f, 10f, 60f)),
                    Vector3.zero, Quaternion.identity);
                objects.Add(data);
                nav = NavMesh.AddNavMeshData(data);

                var player = New("PlacementCheckPlayerBuilding", origin).AddComponent<BuildingBase>();
                var root = New("PlacementCheckColony", origin + Vector3.right * 5f);
                root.SetActive(false);

                var buildingObject = New("PlacementCheckEnemyBuilding", root.transform.position);
                buildingObject.SetActive(false);
                buildingObject.transform.SetParent(root.transform, true);
                var enemyBuilding = buildingObject.AddComponent<BuildingBase>();
                Set(enemyBuilding, "countsTowardPlayerDefeat", false);
                buildingObject.SetActive(true);

                var marker = New("PlacementCheckMarker", root.transform.position + Vector3.forward * 2f);
                marker.transform.SetParent(root.transform, true);
                var markerLocalPosition = marker.transform.localPosition;

                var colony = root.AddComponent<EnemyColony>();
                Set(colony, "buildings", new[] { enemyBuilding });
                Set(colony, "minPlayerDistance", 15f);
                Set(colony, "maxPlayerDistance", 20f);
                Set(colony, "placementAttempts", 50);
                Set(colony, "navMeshSearchRadius", 2f);

                UnityEngine.Random.InitState(12345);
                var start = root.transform.position;
                root.SetActive(true);
                await Until(() => root.transform.position != start);

                var horizontal = enemyBuilding.Position - player.Position;
                horizontal.y = 0f;
                Check(horizontal.magnitude >= 15f && horizontal.magnitude <= 20f, "colony placed in configured annulus");
                Check(marker.transform.localPosition == markerLocalPosition, "child layout preserved");
                Check((marker.transform.position - enemyBuilding.Position).sqrMagnitude > 0f, "colony children move together");
                Check(NavMesh.SamplePosition(enemyBuilding.Position, out var hit, .1f, NavMesh.AllAreas)
                    && (hit.position - enemyBuilding.Position).sqrMagnitude < .01f, "colony center is on NavMesh");
                Check(colony.RemainingBuildings == 1, "placement preserves colony state");
                return $"PASS: {checks} enemy colony random-placement checks";
            }
            finally
            {
                UnityEngine.Random.state = randomState;
                for (var i = objects.Count - 1; i >= 0; i--)
                    if (objects[i] != null) Object.DestroyImmediate(objects[i]);
                if (nav.valid) nav.Remove();
            }
        }

        private static void Set(object target, string field, object value)
        {
            var info = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            if (info == null) throw new Exception("Missing field: " + field);
            info.SetValue(target, value);
        }

        private static async Task Until(Func<bool> condition)
        {
            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (!condition())
            {
                if (DateTime.UtcNow > deadline) throw new Exception("TIMEOUT: colony did not move");
                await Task.Delay(25);
            }
        }
    }
}
