using AntColony.World;
using UnityEngine;

namespace AntColony.Units
{
    // Visuals follow simulation; animation never changes damage, movement or resource timers.
    public sealed class AntVisual : MonoBehaviour
    {
        private Animator animator;
        private GameObject model;
        private LineRenderer ring;
        private SelectableObject selection;
        private SoldierAnt soldier;
        private WorkerAnt worker;
        private CommanderAnt commander;
        private WildMonster enemy;
        private Vector3 previousPosition;
        private float actionUntil;
        private string state;
        private MaterialPropertyBlock ringColor;
        public string StateName => state;

        public static void Attach(GameObject owner)
        {
            if (owner.GetComponent<AntVisual>() == null && Resources.Load<GameObject>("QuirkyAnt") != null)
                owner.AddComponent<AntVisual>();
        }

        private void Awake()
        {
            ringColor = new MaterialPropertyBlock();
            var prefab = Resources.Load<GameObject>("QuirkyAnt");
            if (prefab == null) { enabled = false; return; }
            foreach (var renderer in GetComponentsInChildren<MeshRenderer>()) renderer.enabled = false;
            model = Instantiate(prefab, transform, false);
            animator = model.GetComponentInChildren<Animator>();
            animator.applyRootMotion = false;
            selection = GetComponent<SelectableObject>(); soldier = GetComponent<SoldierAnt>();
            worker = GetComponent<WorkerAnt>(); commander = GetComponent<CommanderAnt>(); enemy = GetComponent<WildMonster>();
            var ringObject = new GameObject("FactionRing"); ringObject.transform.SetParent(transform, false);
            ring = ringObject.AddComponent<LineRenderer>(); ring.useWorldSpace = false; ring.loop = true;
            ring.positionCount = 40; ring.widthMultiplier = .065f;
            ring.sharedMaterial = Resources.Load<Material>("AntSelection");
            for (var i = 0; i < ring.positionCount; i++)
            {
                var angle = i * Mathf.PI * 2 / ring.positionCount;
                ring.SetPosition(i, new Vector3(Mathf.Cos(angle), .06f, Mathf.Sin(angle)) * 1.1f);
            }
            previousPosition = transform.position;
            Play("Idle_A");
        }

        private void OnEnable() { previousPosition = transform.position; actionUntil = 0; state = null; }

        public void Action(string name, float duration = .35f)
        {
            if (animator == null || !isActiveAndEnabled) return;
            Play(name, true); actionUntil = Time.time + duration;
        }

        public void Attack(Vector3 target)
        {
            Face(target - transform.position);
            Action("Attack", .45f);
        }

        private void Face(Vector3 direction)
        {
            direction.y = 0;
            if (direction.sqrMagnitude > .0001f && model != null)
                model.transform.rotation = Quaternion.LookRotation(direction);
        }

        private void Play(string name, bool restart = false)
        {
            if (animator == null || (!restart && state == name)) return;
            var hash = Animator.StringToHash("Base Layer." + name);
            if (!animator.HasState(0, hash)) return;
            animator.CrossFadeInFixedTime(hash, .1f, 0); state = name;
        }

        private void LateUpdate()
        {
            if (model == null || Time.deltaTime <= 0) return;
            if (enemy == null && soldier == null) enemy = GetComponent<WildMonster>();
            if (commander != null && commander.IsEmbarked) { previousPosition = transform.position; return; }
            var delta = transform.position - previousPosition; previousPosition = transform.position;
            var moving = delta.sqrMagnitude > .000001f;
            var color = enemy != null ? new Color(1, .3f, .22f) : new Color(.3f, .85f, .65f);
            if (selection != null && selection.IsSelected) color = new Color(1, .9f, .35f);
            ring.startColor = ring.endColor = color;
            ringColor.SetColor("_BaseColor", color); ring.SetPropertyBlock(ringColor);
            ring.widthMultiplier = selection != null && selection.IsSelected ? .13f : .055f;
            if (Time.time < actionUntil) return;
            if (moving) Face(delta);
            if (commander != null && (commander.IsCaptive || !commander.HasTroops)) Play("Sit");
            else if (soldier != null && soldier.IsFlying || enemy != null && enemy.IsFlying) Play("Fly");
            else if (moving) Play(delta.magnitude / Time.deltaTime > 2f ? "Run" : "Walk");
            else if (worker != null && worker.IsBuildingAnimation) Play("Attack");
            else if (worker != null && worker.IsGatheringAnimation) Play("Eat");
            else Play("Idle_A");
        }

        public void Death()
        {
            if (model == null) return;
            var body = Instantiate(model, model.transform.position, model.transform.rotation);
            body.transform.localScale = model.transform.lossyScale;
            body.GetComponentInChildren<Animator>().Play("Base Layer.Death", 0, 0);
            Destroy(body, 2.5f);
        }
    }
}
