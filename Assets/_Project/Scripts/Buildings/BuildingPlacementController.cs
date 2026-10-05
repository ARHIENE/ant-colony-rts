using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace AntColony.Buildings
{
    [DefaultExecutionOrder(-200)]
    public class BuildingPlacementController : MonoBehaviour
    {
        [SerializeField] private LayerMask groundMask = 1 << 8;
        [SerializeField] private LayerMask obstructionMask = ~0;
        [SerializeField] private Vector3 placementHalfExtents = new Vector3(1.25f, 0.5f, 1.25f);
        [SerializeField] private float maxGroundSlope = 25f;

        private UnityEngine.Camera cam;
        private SelectionManager selectionManager;
        private BuildingKind pendingKind;
        private FarmCrop? pendingCrop;
        private UnitRole pendingRole = UnitRole.Melee;
        private WorkerAnt builder;
        private GameObject preview;
        private bool placementValid;
        private int consumedFrame = -1;

        public bool IsPlacing { get; private set; }
        // 새로 짓는 밭의 작물. 해금되지 않은 작물은 고를 수 없다.
        public static FarmCrop SelectedCrop { get; private set; }
        public static void CycleCrop()
        {
            do SelectedCrop = (FarmCrop)(((int)SelectedCrop + 1) % 3);
            while (!ScienceEffects.CropUnlocked(SelectedCrop));
        }
        public bool BeginSciencePlacement(BuildingKind kind) => BeginPlacement(kind, UnitRole.Worker);
        public bool ConsumesPointerInput => IsPlacing || consumedFrame == Time.frameCount;

        private void Awake()
        {
            cam = UnityEngine.Camera.main;
            selectionManager = FindFirstObjectByType<SelectionManager>();
        }

        private void Update()
        {
            if (AntColony.UI.SkillTargeting.ConsumesPointerInput) return;
            if (AntColony.UI.GameMenuController.BlocksInput) return;
            if (!IsPlacing) return;
            consumedFrame = Time.frameCount;

            var mouse = Mouse.current;
            var keyboard = Keyboard.current;
            if (mouse == null || cam == null)
            {
                CancelPlacement();
                return;
            }

            if ((keyboard != null && keyboard.escapeKey.wasPressedThisFrame) || mouse.rightButton.wasPressedThisFrame)
            {
                CancelPlacement();
                return;
            }

            var ray = cam.ScreenPointToRay(mouse.position.ReadValue());
            if (!Physics.Raycast(ray, out var hit, 1000f, groundMask, QueryTriggerInteraction.Ignore))
            {
                SetPreviewVisible(false);
                placementValid = false;
                return;
            }

            var template = GetTemplate(pendingKind, pendingRole);
            var position = GetPlacementPosition(template, hit.point);
            // Phase 5: 주거 건물은 방 밖에만 짓는다.
            placementValid = Vector3.Angle(hit.normal, Vector3.up) <= maxGroundSlope && !HasObstruction(position)
                && !(Housing.IsKind(pendingKind) && RoomSystem.IsIndoors(position))
                && builder != null && builder.CanStartConstruction && builder.CanReach(hit.point);
            UpdatePreview(position, placementValid);

            if (IsLineKind(pendingKind)) { UpdateLine(mouse, position); return; }
            if (mouse.leftButton.wasPressedThisFrame && !IsPointerOverUi())
            {
                TryPlace(position, hit.point);
            }
        }

        public bool BeginFarmPlacement() => BeginPlacement(BuildingKind.Farm, UnitRole.Worker);
        public bool BeginStoragePlacement() => BeginPlacement(BuildingKind.Storage, UnitRole.Worker);
        public string GetStorageBuildLabel() => GetBuildLabel(BuildingKind.Storage, UnitRole.Worker, "Build Storage");
        public bool BeginAcidTowerPlacement() => BeginPlacement(BuildingKind.AcidTower, UnitRole.Worker);
        public string GetAcidTowerBuildLabel() => GetBuildLabel(BuildingKind.AcidTower, UnitRole.Worker, "Acid Tower");
        public bool BeginScienceLabPlacement() => BeginPlacement(BuildingKind.ScienceLab, UnitRole.Worker);
        public bool BeginAirshipYardPlacement() => BeginPlacement(BuildingKind.AirshipYard, UnitRole.Worker);
        public bool BeginInfirmaryPlacement() => BeginPlacement(BuildingKind.Infirmary, UnitRole.Worker);
        public string GetScienceLabBuildLabel() => ScienceLab.PrerequisitesMet
            ? GetBuildLabel(BuildingKind.ScienceLab, UnitRole.Worker, "Build Science Lab")
            : "Science: 60 Ants\nFishing + Barracks T2";
        public string GetFarmBuildLabel() => GetBuildLabel(BuildingKind.Farm, UnitRole.Worker, "Build Farm");

        // 장수 획득 건물 3종. 전투 보직과 무관하므로 역할은 Worker로 고정한다.
        public bool BeginNurseryPlacement() => BeginPlacement(BuildingKind.Nursery, UnitRole.Worker);
        public bool BeginScoutPostPlacement() => BeginPlacement(BuildingKind.ScoutPost, UnitRole.Worker);
        public bool BeginPrisonerCampPlacement() => BeginPlacement(BuildingKind.PrisonerCamp, UnitRole.Worker);
        public string GetNurseryBuildLabel() => GetBuildLabel(BuildingKind.Nursery, UnitRole.Worker, "Build Nursery");
        public string GetScoutPostBuildLabel() => GetBuildLabel(BuildingKind.ScoutPost, UnitRole.Worker, "Build Scout Post");
        public string GetPrisonerCampBuildLabel() => GetBuildLabel(BuildingKind.PrisonerCamp, UnitRole.Worker, "Build Prison");

        public bool BeginBarracksPlacement() => BeginBarracksPlacement(UnitRole.Melee);
        public bool BeginResearchLabPlacement() => BeginResearchLabPlacement(UnitRole.Melee);
        public bool BeginBarracksPlacement(UnitRole role) => BeginPlacement(BuildingKind.Barracks, role);
        public bool BeginResearchLabPlacement(UnitRole role) => BeginPlacement(BuildingKind.ResearchLab, role);

        public string GetBarracksBuildLabel() => GetBarracksBuildLabel(UnitRole.Melee);
        public string GetResearchLabBuildLabel() => GetResearchLabBuildLabel(UnitRole.Melee);
        public string GetBarracksBuildLabel(UnitRole role) => GetBuildLabel(BuildingKind.Barracks, role, $"Build {role} Barracks");
        public string GetResearchLabBuildLabel(UnitRole role) => GetBuildLabel(BuildingKind.ResearchLab, role, $"Build {role} Lab");

        // 건설 화면에서 고른 장수에게 맡긴다. null이면 현재 선택된 장수를 쓴다.
        public bool BeginPlacement(BuildingKind kind, UnitRole role, WorkerAnt chosenBuilder)
        {
            var locked = LockReason(kind);
            if (locked != null) return PlacementFailed(locked);
            // 버섯밭·축사 = 작물을 고정한 밭(2026-10-05). 배치·저장은 밭과 같다.
            var crop = kind == BuildingKind.MushroomFarm ? FarmCrop.Fungus : kind == BuildingKind.AphidPen ? FarmCrop.Honeydew : (FarmCrop?)null;
            if (crop != null) kind = BuildingKind.Farm;
            if (AntColony.World.WorldMapManager.Instance != null && AntColony.World.WorldMapManager.Instance.ViewedSite != null)
                return PlacementFailed("Return to the home colony to construct buildings.");
            var selectedBuilder = chosenBuilder != null ? chosenBuilder : GetSelectedBuilder();
            var template = GetTemplate(kind, role);
            var building = template != null ? template.GetComponent<BuildingBase>() : null;
            if (selectedBuilder == null || !selectedBuilder.CanStartConstruction)
                return PlacementFailed("Select an idle civilian commander at home to build.");
            if (building == null || building.Data == null) return PlacementFailed("This building template is unavailable.");

            CancelPlacement();
            pendingKind = kind;
            pendingCrop = crop;
            pendingRole = role;
            builder = selectedBuilder;
            IsPlacing = true;
            CreatePreview(template);
            return true;
        }

        private bool BeginPlacement(BuildingKind kind, UnitRole role) => BeginPlacement(kind, role, null);
        public BuildingKind PendingKind => pendingKind;
        public UnitRole PendingRole => pendingRole;
        public WorkerAnt Builder => builder;

        // 연구·수량 조건으로 지금 지을 수 없으면 이유를, 가능하면 null을 돌려준다.
        public static string LockReason(BuildingKind kind)
        {
            if (kind == BuildingKind.ConscriptionPost && (FindFirstObjectByType<ConscriptionPost>() != null || System.Array.Exists(FindObjectsByType<BuildingConstructionSite>(FindObjectsSortMode.None), s => s.BuildingKind == kind))) return "본거지 징집소는 한 곳만 건설할 수 있습니다.";
            if ((kind == BuildingKind.Farm || kind == BuildingKind.MushroomFarm || kind == BuildingKind.AphidPen) && !BiomeRules.FarmAllowed) return "도시 구석에는 밭을 지을 수 없습니다.";
            if (kind == BuildingKind.AphidPen && !ScienceEffects.CropUnlocked(FarmCrop.Honeydew)) return "감로 목장 연구가 필요합니다.";
            if (!ScienceEffects.BuildingUnlocked(kind)) return "Research the matching science first.";
            if (kind == BuildingKind.MineField && MineField.Count >= GameBalance.MaxMines) return $"Up to {GameBalance.MaxMines} mine fields at once.";
            if (kind == BuildingKind.Infirmary && !Infirmary.Unlocked) return "Research Infirmary first.";
            if (kind == BuildingKind.AirshipYard && (CampaignResearch.Instance == null
                || !CampaignResearch.Instance.Has(ScienceTechnology.MigrationTheory))) return "로켓 이론을 먼저 연구하세요.";
            if (kind == BuildingKind.ScienceLab && !ScienceLab.PrerequisitesMet) return "Science Lab requires 60 ants, Fishing and a Tier 2 barracks.";
            return null;
        }

        private static bool PlacementFailed(string reason)
        {
            AntColony.UI.ToastManager.Show(reason);
            return false;
        }

        private void TryPlace(Vector3 position, Vector3 groundPosition)
        {
            if (PlaceOne(position, groundPosition, true) != null) FinishPlacementMode();
        }

        // 한 칸 배치. commandBuilder = 고른 장수에게 바로 맡김(줄 배치의 나머지 칸은 건설 작업이 켜진 장수가 자율로 짓는다).
        private BuildingConstructionSite PlaceOne(Vector3 position, Vector3 groundPosition, bool commandBuilder)
        {
            if (LockReason(pendingKind) != null) return null;
            if (pendingKind == BuildingKind.MineField && MineField.Count >= GameBalance.MaxMines) return null;
            if (pendingKind == BuildingKind.Infirmary && !Infirmary.Unlocked) return null;
            if (pendingKind == BuildingKind.ScienceLab && !ScienceLab.PrerequisitesMet) return null;
            var template = GetTemplate(pendingKind, pendingRole);
            var building = template != null ? template.GetComponent<BuildingBase>() : null;
            if (commandBuilder && (!placementValid || builder == null || !builder.CanStartConstruction) || builder == null || building == null || building.Data == null)
            {
                PlacementFailed("Choose a reachable, clear and level construction site.");
                return null;
            }

            var cost = building.Data;
            var pool = AntPool.Instance;
            if (ResourceManager.Instance == null || !ResourceManager.Instance.CanAfford(cost.foodCost, cost.soilCost, cost.specialCost))
            {
                PlacementFailed($"Construction needs {cost.foodCost} food, {cost.soilCost} soil and {cost.specialCost} special.");
                return null;
            }
            if (builder is CommanderAnt commander && !commander.CanDoJob(Decoration.IsKind(pendingKind) ? CommanderJobs.Art : CommanderJobs.Building))
            { PlacementFailed("이 장수는 해당 작업을 할 수 없습니다."); return null; }
            if (pool == null)
            {
                PlacementFailed($"Keep {cost.constructionAnts} unassigned ants available for construction.");
                return null;
            }
            if (!ResourceManager.Instance.TrySpend(cost.foodCost, cost.soilCost, cost.specialCost, reason: ResourceReason.Construction))
            {
                // 인력은 작업이 시작되면 대상의 슬라이더 요청 수만큼 빌린다.
                return null;
            }

            var completedBuilding = Instantiate(template, position, template.transform.rotation);
            completedBuilding.name = pendingKind switch
            {
                BuildingKind.Barracks => $"{pendingRole}Barracks",
                BuildingKind.ResearchLab => $"{pendingRole}ResearchLab",
                _ => pendingKind.ToString()
            };
            completedBuilding.SetActive(false);
            if (pendingKind == BuildingKind.Farm)
                completedBuilding.AddComponent<FarmPlot>().Configure(pendingCrop ?? SelectedCrop, ScienceEffects.WideFarms);

            var siteObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            siteObject.name = completedBuilding.name + "ConstructionSite";
            siteObject.transform.position = groundPosition + Vector3.up * 0.1f;
            siteObject.transform.localScale = new Vector3(2.5f, 0.2f, 2.5f);
            var collider = siteObject.GetComponent<Collider>();
            if (collider != null) collider.isTrigger = true;
            var footprint = template.GetComponent<Renderer>();
            if (footprint != null)
                siteObject.transform.localScale = new Vector3(footprint.bounds.size.x * WidthFactor(pendingKind), 0.2f, footprint.bounds.size.z);
            var renderer = siteObject.GetComponent<Renderer>();
            if (renderer != null) renderer.material.color = new Color(0.9f, 0.7f, 0.2f);

            var site = siteObject.AddComponent<BuildingConstructionSite>();
            site.Initialize(completedBuilding, cost.buildTimeSeconds);
            Workforce.For(site).Request(cost.constructionAnts);
            if (commandBuilder) builder.CommandBuild(site);
            return site;
        }

        // 벽 줄 드래그(2026-10-03): 누른 칸에서 끈 방향(가로·세로 중 긴 쪽)으로 한 줄, 놓으면 칸마다 건설 예정지.
        public const int MaxLineCells = 40;
        private Vector3? dragStart;
        private readonly System.Collections.Generic.List<GameObject> linePreviews = new System.Collections.Generic.List<GameObject>();
        public static bool IsLineKind(BuildingKind kind) => kind == BuildingKind.SoilWall || kind == BuildingKind.LeafWall
            || kind == BuildingKind.CapWall || kind == BuildingKind.CastleWall || kind == BuildingKind.PowerWire || kind == BuildingKind.Floor; // 전선·바닥도 한 줄 드래그

        public System.Collections.Generic.List<Vector3> LineCells(Vector3 start, Vector3 end)
        {
            var renderer = GetTemplate(pendingKind, pendingRole)?.GetComponent<Renderer>();
            var size = renderer != null ? renderer.bounds.size : Vector3.one;
            var delta = end - start; bool alongX = Mathf.Abs(delta.x) >= Mathf.Abs(delta.z);
            var step = Mathf.Max(1, Mathf.Round(alongX ? size.x : size.z));
            var count = Mathf.Min(MaxLineCells, Mathf.RoundToInt(Mathf.Abs(alongX ? delta.x : delta.z) / step) + 1);
            var dir = alongX ? new Vector3(Mathf.Sign(delta.x), 0, 0) : new Vector3(0, 0, Mathf.Sign(delta.z));
            var cells = new System.Collections.Generic.List<Vector3>();
            for (var i = 0; i < count; i++) cells.Add(start + dir * step * i);
            return cells;
        }

        private bool CellValid(Vector3 cell) => !HasObstruction(cell) && builder != null && builder.CanReach(cell);

        private void UpdateLine(Mouse mouse, Vector3 position)
        {
            if (mouse.leftButton.wasPressedThisFrame && !IsPointerOverUi() && placementValid) dragStart = position;
            if (dragStart == null) return;
            var cells = LineCells(dragStart.Value, position);
            var valid = cells.ConvertAll(CellValid);
            SetPreviewVisible(false);
            while (linePreviews.Count < cells.Count) { var p = Instantiate(preview); p.SetActive(true); linePreviews.Add(p); }
            for (var i = 0; i < linePreviews.Count; i++)
            {
                linePreviews[i].SetActive(i < cells.Count);
                if (i >= cells.Count) continue;
                linePreviews[i].transform.position = cells[i];
                linePreviews[i].GetComponent<Renderer>().material.color = valid[i] ? new Color(0.2f, 0.9f, 0.3f, 0.65f) : new Color(0.9f, 0.2f, 0.2f, 0.65f);
            }
            if (mouse.leftButton.wasReleasedThisFrame) PlaceLine(cells, valid);
        }

        // 칸마다 비용을 따로 낸다. 막힌 칸은 건너뛰고, 비용이 모자라면 거기서 멈춘다. 첫 칸은 고른 장수에게 맡긴다.
        public int PlaceLine(System.Collections.Generic.List<Vector3> cells, System.Collections.Generic.List<bool> valid)
        {
            var renderer = GetTemplate(pendingKind, pendingRole)?.GetComponent<Renderer>();
            var height = renderer != null ? renderer.bounds.extents.y : .5f;
            BuildingConstructionSite first = null; int placed = 0, blocked = 0;
            for (var i = 0; i < cells.Count; i++)
            {
                if (!valid[i]) { blocked++; continue; }
                var site = PlaceOne(cells[i], cells[i] - Vector3.up * height, false);
                if (site == null) break;
                if (first == null) first = site;
                placed++;
            }
            if (first != null && builder != null && builder.CanStartConstruction) builder.CommandBuild(first);
            if (placed > 0) AntColony.UI.ToastManager.Show($"벽 {placed}칸 배치" + (blocked > 0 ? $" · 막힌 칸 {blocked}개 제외" : "") + (placed + blocked < cells.Count ? " · 자원 부족으로 중단" : ""));
            else if (blocked > 0) PlacementFailed("막히지 않은 칸이 없습니다.");
            FinishPlacementMode();
            return placed;
        }

        private static GameObject FindDecorationTemplate(BuildingKind kind)
        {
            foreach (var d in Resources.FindObjectsOfTypeAll<BuildingBase>())
                if (d.Data != null && d.Data.kind == kind && d.name.EndsWith("Template")) return d.gameObject;
            return RuntimeBuildingTemplates.Create(kind);
        }

        public void CancelPlacement()
        {
            FinishPlacementMode();
        }

        private void OnDisable()
        {
            FinishPlacementMode();
        }

        private void FinishPlacementMode()
        {
            IsPlacing = false;
            builder = null;
            if (preview != null) Destroy(preview);
            preview = null;
            dragStart = null;
            foreach (var p in linePreviews) if (p != null) Destroy(p);
            linePreviews.Clear();
        }

        private WorkerAnt GetSelectedBuilder()
        {
            if (selectionManager == null) return null;
            foreach (var selectable in selectionManager.GetSelectedObjects())
            {
                if (selectable == null) continue;
                var worker = selectable.GetComponent<WorkerAnt>();
                if (worker != null && worker.CanStartConstruction) return worker;
            }
            return null;
        }

        private string GetBuildLabel(BuildingKind kind, UnitRole role, string name)
        {
            var template = GetTemplate(kind, role);
            var building = template != null ? template.GetComponent<BuildingBase>() : null;
            if (building == null || building.Data == null) return name + " (Unavailable)";
            return $"{name}\n{building.Data.foodCost}F {building.Data.soilCost} 재료{(building.Data.specialCost > 0 ? $" {building.Data.specialCost}Sp" : "")} {building.Data.constructionAnts} Ants";
        }

        internal static GameObject GetTemplate(BuildingKind kind, UnitRole role)
        {
            return kind switch
            {
                BuildingKind.ResearchLab => FindTemplate<ResearchLab>(role),
                BuildingKind.Farm or BuildingKind.MushroomFarm or BuildingKind.AphidPen => FindFarmTemplate(),
                BuildingKind.Storage => FindTemplate<Storage>(kind),
                BuildingKind.Nursery => FindTemplate<NurseryChamber>(),
                BuildingKind.ScoutPost => FindTemplate<ScoutPost>(),
                BuildingKind.PrisonerCamp => FindTemplate<PrisonerCamp>(),
                BuildingKind.ScienceLab => FindTemplate<ScienceLab>(),
                BuildingKind.AcidTower => FindTemplate<AcidTower>(),
                BuildingKind.AirshipYard => FindTemplate<AirshipYard>() ?? CreateAirshipTemplate(),
                BuildingKind.Infirmary => FindTemplate<Infirmary>() ?? CreateInfirmaryTemplate(),
                BuildingKind.Barracks => FindTemplate<Barracks>(role),
                BuildingKind.SoilWall => FindTemplate<SoilWall>() ?? RuntimeBuildingTemplates.Create(kind),
                BuildingKind.TrapPit => FindTemplate<TrapPit>() ?? RuntimeBuildingTemplates.Create(kind),
                BuildingKind.AreaAcidTower => FindTemplate<AreaAcidTower>() ?? RuntimeBuildingTemplates.Create(kind),
                BuildingKind.Watchtower => FindTemplate<Watchtower>() ?? RuntimeBuildingTemplates.Create(kind),
                BuildingKind.MineField => FindTemplate<MineField>() ?? RuntimeBuildingTemplates.Create(kind),
                BuildingKind.DefenseLab => FindTemplate<DefenseLab>() ?? RuntimeBuildingTemplates.Create(kind),
                BuildingKind.RestRoom => FindTemplate<RestRoom>() ?? RuntimeBuildingTemplates.Create(kind),
                BuildingKind.ConscriptionPost => FindTemplate<ConscriptionPost>() ?? RuntimeBuildingTemplates.Create(kind),
                BuildingKind.Workshop => FindTemplate<Workshop>() ?? RuntimeBuildingTemplates.Create(kind),
                BuildingKind.Dormitory => FindTemplate<Dormitory>(kind) ?? RuntimeBuildingTemplates.Create(kind),
                BuildingKind.Kitchen => FindTemplate<Kitchen>(kind) ?? RuntimeBuildingTemplates.Create(kind),
                BuildingKind.FlowerPot or BuildingKind.ShellDecoration or BuildingKind.MarbleMosaic or BuildingKind.BottleMobile or BuildingKind.FireflyLamp
                    or BuildingKind.Campfire or BuildingKind.GamblingDen
                    or BuildingKind.Hut or BuildingKind.House or BuildingKind.Apartment
                    or BuildingKind.LeafWall or BuildingKind.CapWall or BuildingKind.Door or BuildingKind.CastleWall or BuildingKind.Gate
                    or BuildingKind.Toilet or BuildingKind.Washbasin or BuildingKind.Shower
                    or BuildingKind.Treadmill or BuildingKind.WoodGenerator or BuildingKind.PowerWire or BuildingKind.Battery or BuildingKind.ElectricLamp
                    or BuildingKind.Hearth or BuildingKind.SleepingMat or BuildingKind.DoubleBed or BuildingKind.Floor or BuildingKind.LockedDoor or BuildingKind.BarredDoor
                    or BuildingKind.SingleBed or BuildingKind.Bookshelf or BuildingKind.Bathtub
                    or BuildingKind.FoodStore or BuildingKind.Jar or BuildingKind.Armory => FindDecorationTemplate(kind),
                _ => null
            };
        }

        // 균류 재배 이후 새 밭은 2칸 폭이다. 배치 검사·미리보기·공사장 크기에 같이 반영한다.
        private static float WidthFactor(BuildingKind kind) => kind == BuildingKind.Farm && ScienceEffects.WideFarms ? FarmPlot.WideFactor : 1f;

        private static GameObject CreateInfirmaryTemplate()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.SetActive(false);
            go.name = "InfirmaryTemplate";
            go.transform.localScale = new Vector3(3, 2, 3);
            go.GetComponent<Renderer>().material.color = new Color(.65f, .85f, .8f);
            var definition = ScriptableObject.CreateInstance<BuildingData>();
            definition.kind = BuildingKind.Infirmary;
            definition.displayName = "Infirmary";
            definition.foodCost = 40; definition.soilCost = 40;
            definition.constructionAnts = 4; definition.buildTimeSeconds = 8;
            go.AddComponent<Infirmary>().ConfigureRuntime(definition);
            go.AddComponent<UnityEngine.AI.NavMeshObstacle>().carving = true;
            return go;
        }

        private static GameObject CreateAirshipTemplate()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.SetActive(false);
            go.name = "AirshipYardTemplate";
            go.transform.localScale = new Vector3(6, 2, 4);
            var definition = ScriptableObject.CreateInstance<BuildingData>();
            definition.kind = BuildingKind.AirshipYard;
            definition.displayName = "Rocket Launch Pad";
            definition.foodCost = 100; definition.soilCost = 150;
            definition.constructionAnts = 10; definition.buildTimeSeconds = 30;
            definition.maxHealth = 600;
            go.AddComponent<AirshipYard>().ConfigureRuntime(definition);
            var obstacle = go.AddComponent<UnityEngine.AI.NavMeshObstacle>();
            obstacle.carving = true;
            return go;
        }

        // 밭은 역할 구분이 없으므로 씬의 FarmTemplate 오브젝트를 그대로 쓴다.
        private static GameObject FindFarmTemplate()
        {
            foreach (var node in FindObjectsByType<AntColony.World.ResourceNode>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (node.gameObject.scene.IsValid() && node.gameObject.name == "FarmTemplate") return node.gameObject;
            }
            return null;
        }

        // 역할 구분이 없는 건물의 템플릿. 이름이 Template으로 끝나는 씬 오브젝트 하나를 쓴다.
        private static GameObject FindTemplate<T>() where T : Component
        {
            foreach (var component in FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (component.gameObject.scene.IsValid() && component.gameObject.name.EndsWith("Template"))
                    return component.gameObject;
            }
            return null;
        }

        // 같은 컴포넌트를 쓰는 변형(식당/화덕, 숙소/자리/큰침대)이 섞이지 않게 종류까지 맞춘다.
        private static GameObject FindTemplate<T>(BuildingKind kind) where T : BuildingBase
        {
            foreach (var component in FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (component.gameObject.scene.IsValid() && component.gameObject.name.EndsWith("Template") && (component.Data == null || component.Data.kind == kind))
                    return component.gameObject;
            return null;
        }

        private static GameObject FindTemplate<T>(UnitRole role) where T : Component
        {
            foreach (var component in FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!component.gameObject.scene.IsValid() || !component.gameObject.name.EndsWith("Template")) continue;
                if (component is Barracks barracks && barracks.Role == role) return component.gameObject;
                if (component is ResearchLab lab && lab.Role == role) return component.gameObject;
            }
            return null;
        }

        private Vector3 GetPlacementPosition(GameObject template, Vector3 groundPoint)
        {
            var renderer = template != null ? template.GetComponent<Renderer>() : null;
            var height = renderer != null ? renderer.bounds.extents.y : 0.5f;
            // Phase 5: 1m 칸 격자에 맞춘다(크기가 홀수 칸이면 칸 가운데, 짝수 칸이면 칸 경계).
            var size = renderer != null ? renderer.bounds.size : Vector3.one;
            float Snap(float v, float s) { var cells = Mathf.Max(1, Mathf.RoundToInt(s)); return cells % 2 == 1 ? Mathf.Floor(v) + .5f : Mathf.Round(v); }
            return new Vector3(Snap(groundPoint.x, size.x * WidthFactor(pendingKind)), groundPoint.y + height, Snap(groundPoint.z, size.z));
        }

        private bool HasObstruction(Vector3 position)
        {
            var template = GetTemplate(pendingKind, pendingRole);
            var renderer = template != null ? template.GetComponent<Renderer>() : null;
            var extents = renderer != null ? renderer.bounds.extents : placementHalfExtents;
            extents.x *= WidthFactor(pendingKind);
            foreach (var hit in Physics.OverlapBox(position, extents, Quaternion.identity, obstructionMask, QueryTriggerInteraction.Collide))
            {
                if ((groundMask.value & (1 << hit.gameObject.layer)) != 0) continue;
                return true;
            }
            return false;
        }

        private void CreatePreview(GameObject template)
        {
            preview = GameObject.CreatePrimitive(PrimitiveType.Cube);
            preview.name = "BuildingPlacementPreview";
            preview.transform.localScale = template != null ? Vector3.Scale(template.transform.localScale, new Vector3(WidthFactor(pendingKind), 1, 1)) : Vector3.one;
            var collider = preview.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
                Destroy(collider);
            }
        }

        private void UpdatePreview(Vector3 position, bool valid)
        {
            SetPreviewVisible(true);
            preview.transform.position = position;
            var renderer = preview.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = valid ? new Color(0.2f, 0.9f, 0.3f, 0.65f) : new Color(0.9f, 0.2f, 0.2f, 0.65f);
        }

        private void SetPreviewVisible(bool visible)
        {
            if (preview != null && preview.activeSelf != visible) preview.SetActive(visible);
        }

        private static bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }
}
