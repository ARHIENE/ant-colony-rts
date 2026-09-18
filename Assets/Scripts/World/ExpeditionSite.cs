using System.Collections.Generic;
using AntColony.Boss;
using AntColony.Buildings;
using UnityEngine;
using UnityEngine.AI;

namespace AntColony.World
{
    // ponytail: 원정 전장은 서로 끊어진 70m NavMesh 섬이다. 아트가 준비되면 전용 씬으로 교체한다.
    public class ExpeditionSite : MonoBehaviour
    {
        public string Title { get; private set; }
        public Color Color { get; private set; }
        public Vector2 MapPosition { get; private set; }
        public Vector3 Landing => transform.position + new Vector3(0, 0, -22);
        public EnemyColony Colony { get; private set; }
        public BossHealth Boss { get; private set; }
        public ExpeditionTransport Visitor { get; internal set; }
        public bool Cleared { get; private set; }
        private bool bossSite;
        private NavMeshData navData;
        private NavMeshDataInstance navInstance;
        private float growthTimer;

        public void Initialize(string title, Color color, Vector2 mapPosition, EnemyColony colonyTemplate,
            BossHealth bossTemplate, EnemyCommander guardTemplate)
        {
            Title = title;
            Color = color;
            MapPosition = mapPosition;
            bossSite = bossTemplate != null;
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = title + " Ground";
            ground.transform.SetParent(transform, false);
            ground.transform.localPosition = new Vector3(0, -.25f, 0);
            ground.transform.localScale = new Vector3(70, .5f, 70);
            ground.layer = 8;
            ground.GetComponent<Renderer>().material.color = new Color(.25f, .32f, .2f);
            var sources = new List<NavMeshBuildSource> { new NavMeshBuildSource {
                shape = NavMeshBuildSourceShape.Box, size = new Vector3(70, .5f, 70),
                transform = Matrix4x4.TRS(ground.transform.position, Quaternion.identity, Vector3.one)
            }};
            navData = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByIndex(0), sources,
                new Bounds(transform.position, new Vector3(72, 10, 72)), Vector3.zero, Quaternion.identity);
            navInstance = NavMesh.AddNavMeshData(navData);

            if (bossSite)
            {
                Boss = Instantiate(bossTemplate, transform);
                Boss.transform.position = transform.position + new Vector3(0, 1, 12);
                Boss.gameObject.SetActive(true);
            }
            else
            {
                Colony = Instantiate(colonyTemplate, transform);
                Colony.ConfigureExpedition();
                // 기존 소굴의 건물/재고 상대 배치를 유지하되 평평한 원정 지면에 맞춘다.
                var buildings = Colony.GetComponentsInChildren<BuildingBase>(true);
                var center = Vector3.zero;
                foreach (var b in buildings) center += b.Position;
                center /= Mathf.Max(1, buildings.Length);
                Colony.transform.position += transform.position + Vector3.forward * 10 - center;
                foreach (var b in buildings)
                    b.transform.position = new Vector3(b.Position.x, .4f, b.Position.z);
                foreach (var n in Colony.GetComponentsInChildren<ResourceNode>(true))
                    n.transform.position = new Vector3(n.transform.position.x, .3f, n.transform.position.z);
                Colony.gameObject.SetActive(true);
                if (guardTemplate != null)
                {
                    var guard = Instantiate(guardTemplate, transform.position + Vector3.forward * 3, Quaternion.identity, transform);
                    guard.ConfigureCommander(title + " Commander", AntColony.Data.CommanderRank.Sergeant, null,
                        AntColony.Units.CommanderTraits.Random());
                    // MakeRaider를 사용하지 않는다. 원정 수비병은 본거지로 이동하지 않는다.
                    guard.gameObject.SetActive(true);
                }
            }
        }

        private void Update()
        {
            if (Cleared) return;
            Cleared = bossSite ? Boss == null || Boss.IsDead : Colony != null && Colony.IsDefeated;
            if (Cleared || Colony == null) return;
            growthTimer += Time.deltaTime;
            if (growthTimer < 60f) return;
            growthTimer = 0;
            Colony.AddResources(Colony.RemainingBuildings * 2, Colony.RemainingBuildings);
            Colony.TryExpand(0, 30, 5, 5);
        }

        private void OnDestroy()
        {
            if (navInstance.valid) navInstance.Remove();
            if (navData != null) Destroy(navData);
        }
    }
}
