using AntColony.Core;
using AntColony.Buildings;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace AntColony.Units
{
    // SIMUL-TeaamProject(hyeonyeop 브랜치) AntAttackMoveController.cs 참고 포팅.
    // A 누르고 좌클릭 - 적을 클릭하면 그 대상을 직접 공격, 빈 땅을 클릭하면 어택무브(경로상 적 자동 교전).
    // ESC 또는 우클릭으로 어택 모드 취소.
    [DefaultExecutionOrder(-100)]
    public class AttackMoveController : MonoBehaviour
    {
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField] private SelectionManager selectionManager;
        [SerializeField] private float formationSpacing = 1.5f;

        private UnityEngine.Camera cam;
        private BuildingPlacementController placement;
        private int consumedFrame = -1;

        public bool IsAttackMode { get; private set; }
        public bool ConsumesPointerInput => IsAttackMode || consumedFrame == Time.frameCount;

        private void Awake()
        {
            cam = UnityEngine.Camera.main;
            placement = AntColony.Core.SceneObjects.Any<BuildingPlacementController>();
            if (selectionManager == null) selectionManager = AntColony.Core.SceneObjects.Any<SelectionManager>();
        }

        private void Update()
        {
            if (AntColony.UI.SkillTargeting.ConsumesPointerInput) { IsAttackMode = false; return; }
            if (AntColony.UI.GameMenuController.BlocksInput) { IsAttackMode = false; return; }
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null || mouse == null || selectionManager == null) return;
            if (placement != null && placement.ConsumesPointerInput)
            {
                IsAttackMode = false;
                return;
            }

            // Ctrl+A는 전원 선택(GameFeel)이므로 공격 이동을 켜지 않는다.
            if (keyboard.aKey.wasPressedThisFrame && !keyboard.ctrlKey.isPressed && HasSoldierSelected())
            {
                IsAttackMode = true;
            }

            if (!IsAttackMode) return;
            consumedFrame = Time.frameCount;

            if (keyboard.escapeKey.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame)
            {
                IsAttackMode = false;
                return;
            }

            if (mouse.leftButton.wasPressedThisFrame && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
            {
                IssueAttackCommand(mouse.position.ReadValue());
                IsAttackMode = false;
            }
        }

        // 커맨드 카드의 어택무브 버튼. A 키와 같은 조건으로 공격 지점 선택을 시작한다.
        public void BeginAttackMode() { if (selectionManager != null && HasSoldierSelected()) IsAttackMode = true; }

        // 도착 대형의 방향(2026-11-10): 무리 중심에서 목적지를 바라보는 쪽이 앞줄이다.
        // 이전에는 항상 지도 북쪽이 앞이라, 남쪽·옆으로 보내면 뒷줄이 먼저 적과 닿았다.
        public static Quaternion GroupFacing(System.Collections.Generic.IReadOnlyList<SelectableObject> selected, Vector3 destination)
        {
            var sum = Vector3.zero; var count = 0;
            foreach (var s in selected) if (s != null) { sum += s.transform.position; count++; }
            if (count == 0) return Quaternion.identity;
            var direction = destination - sum / count; direction.y = 0;
            return direction.sqrMagnitude < .25f ? Quaternion.identity : Quaternion.LookRotation(direction.normalized, Vector3.up);
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
            if (target is BuildingBase building && building.CountsTowardPlayerDefeat)
            {
                target = null;
            }
            var selected = selectionManager.GetSelectedObjects();

            var cols = Mathf.CeilToInt(Mathf.Sqrt(selected.Count));
            var facing = GroupFacing(selected, hit.point);
            var index = 0;
            // 지시 확인: 공격 지점에 붉은 마커와 효과음(이전에는 아무 반응이 없었다).
            MoveMarker.Spawn(hit.point, new Color(1f, .3f, .25f), .5f, 1.8f, .4f);
            AntColony.UI.GameFeel.Play(AntColony.UI.GameFeel.Sound.Attack);

            foreach (var selectable in selected)
            {
                if (selectable == null) continue;
                var soldier = selectable.GetComponent<SoldierAnt>();
                if (soldier == null) continue;

                // 대공 불가 역할이 공중 대상을 클릭하면 공격 대신 그 위치로 어택무브한다.
                if (target != null && soldier.CanAttackTarget(target))
                {
                    soldier.CommandAttack(target);
                }
                else
                {
                    var col = index % cols;
                    var row = index / cols;
                    var offset = facing * new Vector3((col - (cols - 1) / 2f) * formationSpacing, 0f, row * -formationSpacing);
                    soldier.CommandAttackMove(hit.point + offset);
                }
                index++;
            }
        }
    }
}
