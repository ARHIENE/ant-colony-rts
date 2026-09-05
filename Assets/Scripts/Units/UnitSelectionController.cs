using AntColony.Core;
using AntColony.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AntColony.Units
{
    // 선택 자체는 SelectionManager(드래그/클릭/Shift 추가선택)가 담당하고,
    // 이 스크립트는 선택된 SoldierAnt들에게 우클릭으로 이동/공격 명령만 내린다.
    public class UnitSelectionController : MonoBehaviour
    {
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField] private SelectionManager selectionManager;
        [SerializeField] private float formationSpacing = 1.5f;
        [SerializeField] private Color moveMarkerColor = Color.green;

        private UnityEngine.Camera cam;

        private void Awake()
        {
            cam = UnityEngine.Camera.main;
            if (selectionManager == null) selectionManager = FindFirstObjectByType<SelectionManager>();
        }

        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null || selectionManager == null) return;

            if (mouse.rightButton.wasPressedThisFrame)
            {
                IssueCommand(mouse.position.ReadValue());
            }
        }

        private void IssueCommand(Vector2 screenPos)
        {
            if (cam == null) return;

            var selected = selectionManager.GetSelectedObjects();
            if (selected.Count == 0) return;

            var ray = cam.ScreenPointToRay(screenPos);
            if (!Physics.Raycast(ray, out var hit, 500f, groundMask)) return;

            // 적(IDamageable, 야생 몬스터/보스 등)을 직접 클릭하면 전원 그 타겟을 공격.
            var target = hit.collider.GetComponentInParent<IDamageable>();
            // 자원노드를 클릭하면 일개미는 그 자리로 이동해 채집을 시작한다.
            var resourceNode = hit.collider.GetComponentInParent<ResourceNode>();

            var cols = Mathf.CeilToInt(Mathf.Sqrt(selected.Count));
            var index = 0;
            var issuedMove = false;

            foreach (var selectable in selected)
            {
                if (selectable == null) continue;

                var col = index % cols;
                var row = index / cols;
                var offset = new Vector3((col - (cols - 1) / 2f) * formationSpacing, 0f, row * -formationSpacing);

                var soldier = selectable.GetComponent<SoldierAnt>();
                if (soldier != null)
                {
                    if (target != null)
                    {
                        soldier.CommandAttack(target);
                    }
                    else
                    {
                        soldier.CommandMove(hit.point + offset);
                        issuedMove = true;
                    }
                    index++;
                    continue;
                }

                var worker = selectable.GetComponent<WorkerAnt>();
                if (worker != null)
                {
                    // 일개미는 전투 유닛이 아니므로 적을 클릭해도 공격 대신 그 위치로 이동만 한다.
                    if (resourceNode != null && !resourceNode.IsDepleted)
                    {
                        worker.CommandGather(resourceNode);
                    }
                    else
                    {
                        worker.CommandMove(hit.point + offset);
                    }
                    issuedMove = true;
                    index++;
                }
            }

            if (issuedMove)
            {
                MoveMarker.Spawn(hit.point, moveMarkerColor);
            }
        }
    }
}
