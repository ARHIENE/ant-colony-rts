using System.Collections.Generic;
using AntColony.Boss;
using AntColony.Buildings;
using AntColony.Data;
using UnityEngine;
using UnityEngine.AI;

namespace AntColony.World
{
    public enum ExpeditionSiteKind { Settlement, BossNest, ResourceSite }
    public enum ConquestDisposition { Undecided, Annexed, Abandoned, Lost }

    // ponytail: 원정 전장은 서로 끊어진 70m NavMesh 섬이다. 아트가 준비되면 전용 씬으로 교체한다.
    public class ExpeditionSite : MonoBehaviour
    {
        public string Title { get; private set; }
        public string Faction { get; private set; }
        public ExpeditionSiteKind Kind { get; private set; }
        // 거리와 무관한 고정 난이도: 정착지 수비병·재고·생산, 보스 체력·전리품, 중립 자원량에 적용.
        public int Difficulty { get; private set; }
        public Color Color { get; private set; }
        public Vector2 MapPosition { get; private set; }
        public Vector3 Landing => transform.position + new Vector3(0, 0, -22);
        public EnemyColony Colony { get; private set; }
        public BossHealth Boss { get; private set; }
        public ExpeditionTransport Visitor { get; internal set; }
        public bool Cleared { get; private set; }
        public ConquestDisposition Disposition { get; private set; }
        public AnnexedSettlement Settlement { get; private set; }
        public SettlementDefense Defense { get; private set; }
        internal EnemyCommander GuardTemplate { get; private set; }
        public bool CanResolveConquest
        {
            get
            {
                if (!isActiveAndEnabled || Kind != ExpeditionSiteKind.Settlement || !Cleared
                    || (Disposition != ConquestDisposition.Undecided && Disposition != ConquestDisposition.Lost) || Visitor == null
                    || !Visitor.isActiveAndEnabled || Visitor.State != ExpeditionState.Deployed) return false;
                foreach (var guard in GetComponentsInChildren<WildMonster>(true))
                    if (!guard.IsDead) return false;
                foreach (var commander in Visitor.Crew)
                    if (commander != null && commander.isActiveAndEnabled && commander.HasTroops) return true;
                return false;
            }
        }
        private ResourceNode[] resourceNodes;
        private NavMeshData navData;
        private NavMeshDataInstance navInstance;
        private float growthTimer;

        public void Initialize(string title, string faction, Color color, Vector2 mapPosition, ExpeditionSiteKind kind, int difficulty,
            EnemyColony colonyTemplate, BossHealth bossTemplate, EnemyCommander guardTemplate)
        {
            Title = title;
            Faction = faction;
            Kind = kind;
            Difficulty = Mathf.Max(1, difficulty);
            GuardTemplate = guardTemplate;
            Color = color;
            MapPosition = mapPosition;
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

            if (kind == ExpeditionSiteKind.ResourceSite)
            {
                resourceNodes = new ResourceNode[2];
                for (var i = 0; i < resourceNodes.Length; i++)
                {
                    var node = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    node.SetActive(false);
                    var type = i == 0 ? ResourceType.Food : ResourceType.Soil;
                    node.name = title + " " + type;
                    node.transform.SetParent(transform, false);
                    node.transform.localPosition = new Vector3(i == 0 ? -4 : 4, .6f, 0);
                    node.transform.localScale = Vector3.one * 1.2f;
                    var block = new MaterialPropertyBlock();
                    var nodeColor = i == 0 ? BossLoot.FoodColor : new Color(.6f, .4f, .2f);
                    block.SetColor(BossLoot.BaseColorId, nodeColor);
                    block.SetColor(BossLoot.ColorId, nodeColor);
                    node.GetComponent<Renderer>().SetPropertyBlock(block);
                    resourceNodes[i] = node.AddComponent<ResourceNode>();
                    // 자원량은 임시값. 채집/화물/귀환은 기존 원정 경로를 그대로 사용한다.
                    resourceNodes[i].ConfigureLoot(type, 100 * Difficulty);
                    node.AddComponent<ResourceNodeStatus>();
                    node.SetActive(true);
                }
            }
            else if (kind == ExpeditionSiteKind.BossNest)
            {
                Boss = Instantiate(bossTemplate, transform);
                Boss.ConfigureExpedition(Difficulty);
                Boss.transform.position = transform.position + new Vector3(0, 1, 12);
                Boss.gameObject.SetActive(true);
            }
            else
            {
                Colony = Instantiate(colonyTemplate, transform);
                Colony.ConfigureExpedition(Difficulty);
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
                // 고정 난이도만큼 수비 장수를 세운다. 거리에 따라 늘어나지 않는다.
                for (var i = 0; guardTemplate != null && i < Difficulty; i++)
                {
                    var guard = Instantiate(guardTemplate, transform.position + new Vector3((i - (Difficulty - 1) * .5f) * 3, 0, 3),
                        Quaternion.identity, transform);
                    // 관직은 전투력이 아니라 포로 정보에만 쓰이므로 난이도와 엮지 않는다.
                    guard.ConfigureCommander(title + (Difficulty > 1 ? $" Commander {i + 1}" : " Commander"),
                        CommanderRank.Sergeant, null, AntColony.Units.CommanderTraits.Random());
                    // MakeRaider를 사용하지 않는다. 원정 수비병은 본거지로 이동하지 않는다.
                    guard.gameObject.SetActive(true);
                }
            }
        }

        public bool TryResolveConquest(ConquestDisposition disposition)
        {
            if ((disposition != ConquestDisposition.Annexed && disposition != ConquestDisposition.Abandoned)
                || !CanResolveConquest) return false;
            Disposition = disposition;
            if (disposition == ConquestDisposition.Annexed)
            {
                if (Settlement == null) Settlement = gameObject.AddComponent<AnnexedSettlement>();
                if (Defense == null) Defense = gameObject.AddComponent<SettlementDefense>();
                Defense.ResetAfterConquest();
            }
            Defense?.RescuePrisoners();
            return true;
        }

        internal void LoseSettlement()
        {
            Disposition = ConquestDisposition.Lost;
        }

        private void Update()
        {
            if (Cleared) return;
            if (Kind == ExpeditionSiteKind.ResourceSite)
            {
                Cleared = true;
                foreach (var node in resourceNodes)
                    if (node != null && !node.IsDepleted) Cleared = false;
            }
            else Cleared = Kind == ExpeditionSiteKind.BossNest ? Boss == null || Boss.IsDead : Colony != null && Colony.IsDefeated;
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
