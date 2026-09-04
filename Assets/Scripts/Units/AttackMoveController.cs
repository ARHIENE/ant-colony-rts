using AntColony.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AntColony.Units
{
    // SIMUL-TeaamProject(hyeonyeop 브랜치) AntAttackMoveController.cs 참고 포팅.
    // A 누르고 좌클릭 - 적을 클릭하면 그 대상을 직접 공격, 빈 땅을 클릭하면 어택무브(경로상 적 자동 교전).
    // ESC 또는 우클릭으로 어택 모드 취소.
    public class AttackMoveController : MonoBehaviour
    {
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField] private SelectionManager selectionManager;
        [SerializeField] private float formationSpacing = 1.5f;

        private UnityEngine.Camera cam;

        public bool IsAttackMode { get; private set; }

        private void Awake()
        {
            cam = UnityEngine.Camera.main;
            if (selectionManager == null) selectionManager = FindFirstObjectByType<SelectionManager>();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null || mouse == null || selectionManager == null) return;

            if (keyboard.aKey.wasPressedThisFrame && HasSoldierSelected())
            {
                IsAttackMode = true;
            }

            if (!IsAttackMode) return;

            if (keyboard.escapeKey.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame)
            {
                IsAttackMode = false;
                return;
            }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                IssueAttackCommand(mouse.position.ReadValue());
                IsAttackMode = false;
            }
        }

        private bool HasSoldierSelected()
        {
            foreach (var selectable in selectionManager.GetSelectedObjects())
            {
                if (selectable != null && selectable.GetComponent<SoldierAnt>() != null) return true;
            }
            return false;
        }

        private void IssueAttackCommand(Vector2 screenPos)
        {
            if (cam == null) return;

            var ray = cam.ScreenPointToRay(screenPos);
            if (!Physics.Raycast(ray, out var hit, 1000f, groundMask)) return;

            var target = hit.collider.GetComponentInParent<IDamageable>();
            var selected = selectionManager.GetSelectedObjects();

            var cols = Mathf.CeilToInt(Mathf.Sqrt(selected.Count));
            var index = 0;

            foreach (var selectable in selected)
            {
                if (selectable == null) continue;
                var soldier = selectable.GetComponent<SoldierAnt>();
                if (soldier == null) continue;

                if (target != null)
                {
                    soldier.CommandAttack(target);
                }
                else
                {
                    var col = index % cols;
                    var row = index / cols;
                    var offset = new Vector3((col - (cols - 1) / 2f) * formationSpacing, 0f, row * -formationSpacing);
                    soldier.CommandAttackMove(hit.point + offset);
                }
                index++;
            }
        }
    }
}
