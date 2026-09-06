using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace AntColony.Buildings
{
    public class BuildingPlacementController : MonoBehaviour
    {
        [SerializeField] private LayerMask groundMask = 1 << 8;
        [SerializeField] private LayerMask obstructionMask = ~0;
        [SerializeField] private Vector3 placementHalfExtents = new Vector3(1.25f, 0.5f, 1.25f);
        [SerializeField] private float maxGroundSlope = 25f;

        private UnityEngine.Camera cam;
        private SelectionManager selectionManager;
        private GameObject barracksTemplate;
        private GameObject researchLabTemplate;
        private BuildingKind pendingKind;
        private WorkerAnt builder;
        private GameObject preview;
        private bool placementValid;

        public bool IsPlacing { get; private set; }

        private void Awake()
        {
            cam = UnityEngine.Camera.main;
            selectionManager = FindFirstObjectByType<SelectionManager>();
            barracksTemplate = FindTemplate<Barracks>("BarracksTemplate");
            researchLabTemplate = FindTemplate<ResearchLab>("MeleeResearchLabTemplate");
        }

        private void Update()
        {
            if (!IsPlacing) return;

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

            var template = GetTemplate(pendingKind);
            var position = GetPlacementPosition(template, hit.point);
            placementValid = Vector3.Angle(hit.normal, Vector3.up) <= maxGroundSlope && !HasObstruction(position);
            UpdatePreview(position, placementValid);

            if (mouse.leftButton.wasPressedThisFrame && !IsPointerOverUi())
            {
                TryPlace(position, hit.point);
            }
        }

        public bool BeginBarracksPlacement() => BeginPlacement(BuildingKind.Barracks);
        public bool BeginResearchLabPlacement() => BeginPlacement(BuildingKind.ResearchLab);

        public string GetBarracksBuildLabel() => GetBuildLabel(BuildingKind.Barracks, "Build Melee Barracks");
        public string GetResearchLabBuildLabel() => GetBuildLabel(BuildingKind.ResearchLab, "Build Melee Lab");

        private bool BeginPlacement(BuildingKind kind)
        {
            var selectedBuilder = GetSelectedBuilder();
            var template = GetTemplate(kind);
            var building = template != null ? template.GetComponent<BuildingBase>() : null;
            if (selectedBuilder == null || !selectedBuilder.CanStartConstruction || building == null || building.Data == null)
                return false;

            CancelPlacement();
            pendingKind = kind;
            builder = selectedBuilder;
            IsPlacing = true;
            CreatePreview(template);
            return true;
        }

        private void TryPlace(Vector3 position, Vector3 groundPosition)
        {
            var template = GetTemplate(pendingKind);
            var building = template != null ? template.GetComponent<BuildingBase>() : null;
            if (!placementValid || builder == null || !builder.CanStartConstruction || building == null || building.Data == null)
                return;

            var cost = building.Data;
            if (ResourceManager.Instance == null || !ResourceManager.Instance.TrySpend(cost.foodCost, cost.soilCost))
                return;

            var completedBuilding = Instantiate(template, position, template.transform.rotation);
            completedBuilding.name = pendingKind == BuildingKind.Barracks ? "MeleeBarracks" : "MeleeResearchLab";
            completedBuilding.SetActive(false);

            var siteObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            siteObject.name = completedBuilding.name + "ConstructionSite";
            siteObject.transform.position = groundPosition + Vector3.up * 0.1f;
            siteObject.transform.localScale = new Vector3(2.5f, 0.2f, 2.5f);
            var collider = siteObject.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            var renderer = siteObject.GetComponent<Renderer>();
            if (renderer != null) renderer.material.color = new Color(0.9f, 0.7f, 0.2f);

            var site = siteObject.AddComponent<BuildingConstructionSite>();
            site.Initialize(completedBuilding, cost.buildTimeSeconds);
            builder.CommandBuild(site);
            FinishPlacementMode();
        }

        public void CancelPlacement()
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

        private string GetBuildLabel(BuildingKind kind, string name)
        {
            var template = GetTemplate(kind);
            var building = template != null ? template.GetComponent<BuildingBase>() : null;
            if (building == null || building.Data == null) return name + " (Unavailable)";
            return $"{name}\n{building.Data.foodCost}F {building.Data.soilCost}S";
        }

        private GameObject GetTemplate(BuildingKind kind)
        {
            return kind == BuildingKind.ResearchLab ? researchLabTemplate : barracksTemplate;
        }

        private static GameObject FindTemplate<T>(string objectName) where T : Component
        {
            foreach (var component in FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (component.gameObject.scene.IsValid() && component.gameObject.name == objectName)
                    return component.gameObject;
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
            foreach (var hit in Physics.OverlapBox(position, placementHalfExtents, Quaternion.identity, obstructionMask, QueryTriggerInteraction.Ignore))
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
            preview.transform.localScale = template != null ? template.transform.localScale : Vector3.one;
            var collider = preview.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
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
