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
            placementValid = Vector3.Angle(hit.normal, Vector3.up) <= maxGroundSlope && !HasObstruction(position)
                && builder != null && builder.CanStartConstruction && builder.CanReach(hit.point);
            UpdatePreview(position, placementValid);

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
            if (AntColony.World.WorldMapManager.Instance != null && AntColony.World.WorldMapManager.Instance.ViewedSite != null)
                return PlacementFailed("Return to the home colony to construct buildings.");
            var selectedBuilder = chosenBuilder != null ? chosenBuilder : GetSelectedBuilder();
            var template = GetTemplate(kind, role);
            var building = template != null ? template.GetComponent<BuildingBase>() : null;
            if (selectedBuilder == null || !selectedBuilder.CanStartConstruction)
                return PlacementFailed("Select an idle commander with troops at home to build.");
            if (building == null || building.Data == null) return PlacementFailed("This building template is unavailable.");

            CancelPlacement();
            pendingKind = kind;
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
            if (!ScienceEffects.BuildingUnlocked(kind)) return "Research the matching science first.";
            if (kind == BuildingKind.MineField && MineField.Count >= GameBalance.MaxMines) return $"Up to {GameBalance.MaxMines} mine fields at once.";
            if (kind == BuildingKind.Infirmary && !Infirmary.Unlocked) return "Research Infirmary first.";
            if (kind == BuildingKind.AirshipYard && (CampaignResearch.Instance == null
                || !CampaignResearch.Instance.Has(ScienceTechnology.MigrationTheory))) return "Research great migration theory first.";
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
            if (!ScienceEffects.BuildingUnlocked(pendingKind)) return;
            if (pendingKind == BuildingKind.MineField && MineField.Count >= GameBalance.MaxMines) return;
            if (pendingKind == BuildingKind.Infirmary && !Infirmary.Unlocked) return;
            if (pendingKind == BuildingKind.ScienceLab && !ScienceLab.PrerequisitesMet) return;
            var template = GetTemplate(pendingKind, pendingRole);
            var building = template != null ? template.GetComponent<BuildingBase>() : null;
            if (!placementValid || builder == null || !builder.CanStartConstruction || building == null || building.Data == null)
            {
                PlacementFailed("Choose a reachable, clear and level construction site.");
                return;
            }

            var cost = building.Data;
            var pool = AntPool.Instance;
            if (ResourceManager.Instance == null || !ResourceManager.Instance.CanAfford(cost.foodCost, cost.soilCost, cost.specialCost))
            {
                PlacementFailed($"Construction needs {cost.foodCost} food, {cost.soilCost} soil and {cost.specialCost} special.");
                return;
            }
            if (pool == null || !pool.TryReserve(cost.constructionAnts))
            {
                PlacementFailed($"Keep {cost.constructionAnts} unassigned ants available for construction.");
                return;
            }
            if (!ResourceManager.Instance.TrySpend(cost.foodCost, cost.soilCost, cost.specialCost, reason: ResourceReason.Construction))
            {
                pool.ReleaseReserved(cost.constructionAnts);
                return;
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
                completedBuilding.AddComponent<FarmPlot>().Configure(SelectedCrop, ScienceEffects.WideFarms);

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
            site.Initialize(completedBuilding, cost.buildTimeSeconds, pool, cost.constructionAnts);
            builder.CommandBuild(site);
            FinishPlacementMode();
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
            return $"{name}\n{building.Data.foodCost}F {building.Data.soilCost}S{(building.Data.specialCost > 0 ? $" {building.Data.specialCost}Sp" : "")} {building.Data.constructionAnts} Ants";
        }

        internal static GameObject GetTemplate(BuildingKind kind, UnitRole role)
        {
            return kind switch
            {
                BuildingKind.ResearchLab => FindTemplate<ResearchLab>(role),
                BuildingKind.Farm => FindFarmTemplate(),
                BuildingKind.Storage => FindTemplate<Storage>(),
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
                BuildingKind.Workshop => FindTemplate<Workshop>() ?? RuntimeBuildingTemplates.Create(kind),
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
            definition.displayName = "Airship Yard";
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
            return groundPoint + Vector3.up * height;
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
