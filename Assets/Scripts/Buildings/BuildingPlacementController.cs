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
        private BuildingKind pendingKind;
        private UnitRole pendingRole = UnitRole.Melee;
        private WorkerAnt builder;
        private GameObject preview;
        private bool placementValid;

        public bool IsPlacing { get; private set; }

        private void Awake()
        {
            cam = UnityEngine.Camera.main;
            selectionManager = FindFirstObjectByType<SelectionManager>();
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

            var template = GetTemplate(pendingKind, pendingRole);
            var position = GetPlacementPosition(template, hit.point);
            placementValid = Vector3.Angle(hit.normal, Vector3.up) <= maxGroundSlope && !HasObstruction(position);
            UpdatePreview(position, placementValid);

            if (mouse.leftButton.wasPressedThisFrame && !IsPointerOverUi())
            {
                TryPlace(position, hit.point);
            }
        }

        public bool BeginBarracksPlacement() => BeginBarracksPlacement(UnitRole.Melee);
        public bool BeginResearchLabPlacement() => BeginResearchLabPlacement(UnitRole.Melee);
        public bool BeginBarracksPlacement(UnitRole role) => BeginPlacement(BuildingKind.Barracks, role);
        public bool BeginResearchLabPlacement(UnitRole role) => BeginPlacement(BuildingKind.ResearchLab, role);

        public string GetBarracksBuildLabel() => GetBarracksBuildLabel(UnitRole.Melee);
        public string GetResearchLabBuildLabel() => GetResearchLabBuildLabel(UnitRole.Melee);
        public string GetBarracksBuildLabel(UnitRole role) => GetBuildLabel(BuildingKind.Barracks, role, $"Build {role} Barracks");
        public string GetResearchLabBuildLabel(UnitRole role) => GetBuildLabel(BuildingKind.ResearchLab, role, $"Build {role} Lab");

        private bool BeginPlacement(BuildingKind kind, UnitRole role)
        {
            var selectedBuilder = GetSelectedBuilder();
            var template = GetTemplate(kind, role);
            var building = template != null ? template.GetComponent<BuildingBase>() : null;
            if (selectedBuilder == null || !selectedBuilder.CanStartConstruction || building == null || building.Data == null)
                return false;

            CancelPlacement();
            pendingKind = kind;
            pendingRole = role;
            builder = selectedBuilder;
            IsPlacing = true;
            CreatePreview(template);
            return true;
        }

        private void TryPlace(Vector3 position, Vector3 groundPosition)
        {
            var template = GetTemplate(pendingKind, pendingRole);
            var building = template != null ? template.GetComponent<BuildingBase>() : null;
            if (!placementValid || builder == null || !builder.CanStartConstruction || building == null || building.Data == null)
                return;

            var cost = building.Data;
            if (ResourceManager.Instance == null || !ResourceManager.Instance.TrySpend(cost.foodCost, cost.soilCost))
                return;

            var completedBuilding = Instantiate(template, position, template.transform.rotation);
            completedBuilding.name = pendingKind == BuildingKind.Barracks
                ? $"{pendingRole}Barracks"
                : $"{pendingRole}ResearchLab";
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

        private string GetBuildLabel(BuildingKind kind, UnitRole role, string name)
        {
            var template = GetTemplate(kind, role);
            var building = template != null ? template.GetComponent<BuildingBase>() : null;
            if (building == null || building.Data == null) return name + " (Unavailable)";
            return $"{name}\n{building.Data.foodCost}F {building.Data.soilCost}S";
        }

        private static GameObject GetTemplate(BuildingKind kind, UnitRole role)
        {
            return kind == BuildingKind.ResearchLab
                ? FindTemplate<ResearchLab>(role)
                : FindTemplate<Barracks>(role);
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
