using System.Collections.Generic;
using AntColony.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AntColony.Units
{
    public class UnitSelectionController : MonoBehaviour
    {
        [SerializeField] private LayerMask groundMask = ~0;

        private readonly List<SoldierAnt> selected = new List<SoldierAnt>();
        private Vector2 dragStart;
        private bool isDragging;

        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            if (mouse.leftButton.wasPressedThisFrame)
            {
                dragStart = mouse.position.ReadValue();
                isDragging = true;
            }
            else if (mouse.leftButton.wasReleasedThisFrame && isDragging)
            {
                isDragging = false;
                SelectWithin(dragStart, mouse.position.ReadValue());
            }

            if (mouse.rightButton.wasPressedThisFrame)
            {
                IssueCommand(mouse.position.ReadValue());
            }
        }

        private void SelectWithin(Vector2 a, Vector2 b)
        {
            selected.Clear();
            var min = Vector2.Min(a, b);
            var max = Vector2.Max(a, b);
            var cam = UnityEngine.Camera.main;
            if (cam == null) return;

            foreach (var soldier in FindObjectsByType<SoldierAnt>(FindObjectsSortMode.None))
            {
                var screenPos = cam.WorldToScreenPoint(soldier.transform.position);
                if (screenPos.x >= min.x && screenPos.x <= max.x && screenPos.y >= min.y && screenPos.y <= max.y)
                {
                    selected.Add(soldier);
                }
            }
        }

        private void IssueCommand(Vector2 screenPos)
        {
            if (selected.Count == 0) return;
            var cam = UnityEngine.Camera.main;
            if (cam == null) return;

            var ray = cam.ScreenPointToRay(screenPos);
            if (!Physics.Raycast(ray, out var hit, 500f, groundMask)) return;

            var target = hit.collider.GetComponentInParent<WildMonster>();
            foreach (var soldier in selected)
            {
                if (soldier == null) continue;
                if (target != null)
                {
                    soldier.CommandAttack(target);
                }
                else
                {
                    soldier.CommandMove(hit.point);
                }
            }
        }
    }
}
