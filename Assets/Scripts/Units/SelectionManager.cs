using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using AntColony.Buildings;

namespace AntColony.Units
{
    // SIMUL-TeaamProject(hyeonyeop 브랜치) SelectionManager.cs 참고 포팅.
    // 원본은 레거시 Input Manager를 썼지만, 이 프로젝트는 전부 새 Input System을 쓰므로 Mouse/Keyboard.current로 교체함.
    // selectionBoxImage를 비워두면 런타임에 자동 생성함(HUDController와 동일한 컨벤션 — 수작업 UI 없이 동작).
    public class SelectionManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UnityEngine.Camera cam;
        [SerializeField] private Image selectionBoxImage;

        [Header("Raycast")]
        [SerializeField] private LayerMask selectableLayerMask = ~0;

        [Header("Drag")]
        [SerializeField] private float dragStartThreshold = 8f;

        [SerializeField] private AttackMoveController attackMoveController;
        [SerializeField] private BuildingPlacementController buildingPlacementController;

        private readonly List<SelectableObject> selectedObjects = new List<SelectableObject>();

        private bool isMouseDown;
        private bool isDragging;
        private bool additiveSelectionThisDrag;

        private Vector2 dragStartScreen;
        private Vector2 dragEndScreen;

        private void Awake()
        {
            if (cam == null) cam = UnityEngine.Camera.main;
            if (selectionBoxImage == null) selectionBoxImage = CreateSelectionBoxImage();
            if (attackMoveController == null) attackMoveController = FindFirstObjectByType<AttackMoveController>();
            if (buildingPlacementController == null) buildingPlacementController = FindFirstObjectByType<BuildingPlacementController>();

            selectionBoxImage.gameObject.SetActive(false);
        }

        private Image CreateSelectionBoxImage()
        {
            var canvasGO = new GameObject("SelectionBoxCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            var boxGO = new GameObject("SelectionBox");
            boxGO.transform.SetParent(canvasGO.transform, false);
            var rect = boxGO.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;

            var image = boxGO.AddComponent<Image>();
            image.color = new Color(0.4f, 0.9f, 0.4f, 0.25f);
            return image;
        }

        private void Update()
        {
            selectedObjects.RemoveAll(item => item == null || !item.isActiveAndEnabled || !item.IsSelected);
            var mouse = Mouse.current;
            if (mouse == null) return;

            // 어택무브 모드 중 좌클릭은 공격 명령 전용 — 여기서 선택이 바뀌지 않도록 건너뛴다.
            if (attackMoveController != null && attackMoveController.IsAttackMode) return;
            if (buildingPlacementController != null && buildingPlacementController.IsPlacing) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            if (mouse.leftButton.wasPressedThisFrame)
            {
                isMouseDown = true;
                isDragging = false;
                additiveSelectionThisDrag = IsShiftHeld();

                dragStartScreen = mouse.position.ReadValue();
                dragEndScreen = dragStartScreen;
            }

            if (isMouseDown && mouse.leftButton.isPressed)
            {
                dragEndScreen = mouse.position.ReadValue();

                if (!isDragging && Vector2.Distance(dragStartScreen, dragEndScreen) >= dragStartThreshold)
                {
                    isDragging = true;
                    ShowSelectionBox(true);
                }

                if (isDragging)
                    UpdateSelectionBox(dragStartScreen, dragEndScreen);
            }

            if (isMouseDown && mouse.leftButton.wasReleasedThisFrame)
            {
                isMouseDown = false;

                if (isDragging)
                {
                    var rect = GetScreenRect(dragStartScreen, dragEndScreen);
                    SelectObjectsInRect(rect, additiveSelectionThisDrag);
                }
                else
                {
                    ClickSelectOrClear(mouse.position.ReadValue(), IsShiftHeld());
                }

                isDragging = false;
                ShowSelectionBox(false);
            }
        }

        private void ClickSelectOrClear(Vector2 mouseScreen, bool additive)
        {
            if (cam == null) return;

            var ray = cam.ScreenPointToRay(mouseScreen);

            if (Physics.Raycast(ray, out var hit, 1000f, selectableLayerMask))
            {
                var selectable = hit.collider.GetComponentInParent<SelectableObject>();
                if (selectable != null)
                {
                    if (!additive) ClearSelection();
                    AddToSelection(selectable);
                    return;
                }
            }

            if (!additive) ClearSelection();
        }

        private void SelectObjectsInRect(Rect screenRect, bool additive)
        {
            if (!additive) ClearSelection();

            var allSelectables = FindObjectsByType<SelectableObject>(FindObjectsSortMode.None);

            foreach (var selectable in allSelectables)
            {
                if (selectable == null) continue;

                var screenPos = cam.WorldToScreenPoint(selectable.GetSelectionWorldPosition());
                if (screenPos.z < 0f) continue;

                if (screenRect.Contains(new Vector2(screenPos.x, screenPos.y)))
                    AddToSelection(selectable);
            }
        }

        private void AddToSelection(SelectableObject selectable)
        {
            if (selectable == null || selectedObjects.Contains(selectable)) return;

            selectedObjects.Add(selectable);
            selectable.SetSelected(true);
        }

        public void ClearSelection()
        {
            foreach (var selectable in selectedObjects)
            {
                if (selectable != null) selectable.SetSelected(false);
            }
            selectedObjects.Clear();
        }

        public IReadOnlyList<SelectableObject> GetSelectedObjects() => selectedObjects;

        private bool IsShiftHeld()
        {
            var kb = Keyboard.current;
            return kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);
        }

        private void ShowSelectionBox(bool show)
        {
            if (selectionBoxImage != null) selectionBoxImage.gameObject.SetActive(show);
        }

        private void UpdateSelectionBox(Vector2 start, Vector2 end)
        {
            if (selectionBoxImage == null) return;

            var rect = GetScreenRect(start, end);
            var rt = selectionBoxImage.rectTransform;
            rt.anchoredPosition = rect.position;
            rt.sizeDelta = rect.size;
        }

        private Rect GetScreenRect(Vector2 a, Vector2 b)
        {
            var xMin = Mathf.Min(a.x, b.x);
            var yMin = Mathf.Min(a.y, b.y);
            var xMax = Mathf.Max(a.x, b.x);
            var yMax = Mathf.Max(a.y, b.y);
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }
    }
}
