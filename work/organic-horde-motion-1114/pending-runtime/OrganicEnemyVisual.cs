using UnityEngine;

namespace AntColony.World
{
    [DisallowMultipleComponent]
    public sealed class OrganicEnemyVisual : MonoBehaviour
    {
        GameObject model;
        Animator animator;
        WildMonster owner;
        Vector3 previous;
        Vector3 modelOrigin;
        float actionLeft, burrowOffset;
        public float BurrowOffset => burrowOffset;
        int action = -1;
        OrganicGroundContact contact;
        OrganicMotionBank motionBank;
        public float GroundCorrection => contact?.LastCorrection ?? 0;
        public GameObject Model => model;
        public Animator Animator => animator;

        public static OrganicEnemyVisual Attach(WildMonster host)
        {
            var visual = host.GetComponent<OrganicEnemyVisual>();
            if (visual == null) visual = host.gameObject.AddComponent<OrganicEnemyVisual>();
            visual.owner = host;
            if (visual.model == null) visual.Build();
            return visual;
        }
        void Build()
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>()) renderer.enabled = false;
            model = Create(owner.OrganicKind, transform);
            animator = model.GetComponentInChildren<Animator>();
            modelOrigin = model.transform.localPosition;
            if (GetComponent<OrganicSupportVisual>() == null) gameObject.AddComponent<OrganicSupportVisual>();
            previous = transform.position;
            contact = new OrganicGroundContact(model);
            motionBank = Resources.Load<OrganicMotionBank>("OrganicEnemies1114/" + owner.OrganicKind + "/HordeMotion");
            SetAction(0);
        }
        public static GameObject Create(OrganicEnemyKind kind, Transform parent = null)
        {
            string path = "OrganicEnemies1114/" + kind + "/";
            var source = Resources.Load<GameObject>(path + "Idle");
            var controller = Resources.Load<RuntimeAnimatorController>(path + kind);
            if (source == null || controller == null) throw new System.InvalidOperationException("생물형 적 에셋 누락: " + kind);
            // FBX의 루트 Transform에도 애니메이션 키가 있다. 크기/발 위치는 별도 부모가 소유한다.
            var root = new GameObject("OrganicModel_" + kind);
            if (kind == OrganicEnemyKind.WallRam) root.transform.rotation = Quaternion.Euler(0, Mathf.Atan2(.56f, .83f) * Mathf.Rad2Deg, 0);
            var animated = Instantiate(source, root.transform, false);
            var animator = animated.GetComponent<Animator>();
            if (animator == null) animator = animated.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.SetInteger("Action", 0); animator.Play("Base Layer.Idle", 0, 0); animator.Update(0);
            foreach (var collider in root.GetComponentsInChildren<Collider>()) { collider.enabled = false; Destroy(collider); }
            var renderers = root.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers) { renderer.enabled = true; bounds.Encapsulate(renderer.bounds); }
            root.transform.localScale *= OrganicEnemyRules.Width(kind) / Mathf.Max(.001f, Mathf.Max(bounds.size.x, bounds.size.z));
            bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            var bottom = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = -bottom;
            return root;
        }
        public void Act(int next, float seconds, float normalized = 0)
        {
            actionLeft = Mathf.Max(0, seconds);
            SetAction(next);
            if (normalized > 0) animator.Play("Base Layer." + StateName(next), 0, normalized);
        }
        static string StateName(int value) => value == 0 ? "Idle" : value == 1 ? "Move" : value == 2 ? "Attack" : value == 3 ? "Hit" : value == 4 ? "Death" : "Heal";
        void SetAction(int next)
        {
            if (animator == null || action == next) return;
            action = next; animator.speed = 1; animator.SetInteger("Action", next);
        }
        void LateUpdate()
        {
            if (model == null || owner == null) return;
            var delta = transform.position - previous; previous = transform.position; delta.y = 0;
            burrowOffset = Mathf.MoveTowards(burrowOffset, owner.OrganicPhase == 6 ? -.5f : 0, Time.deltaTime * 2.5f);
            model.transform.localPosition = modelOrigin + Vector3.up * burrowOffset;
            if (owner.IsDead) { SetAction(4); return; }
            if (Mathf.Abs(burrowOffset) < .001f) contact?.Apply(model, transform.position);
            actionLeft = Mathf.Max(0, actionLeft - Time.deltaTime);
            float speed = Time.deltaTime > 0 ? delta.magnitude / Time.deltaTime : 0;
            if (actionLeft > 0 && !(owner.OrganicPhase == 0 && action == 2 && speed > .05f)) return;
            SetAction(speed > .05f ? 1 : 0);
            if (action == 1) animator.speed = Mathf.Clamp(speed / (motionBank != null && motionBank.strideSpeed > .05f ? motionBank.strideSpeed : 2.5f), .1f, 8);
        }
        public GameObject DeathBody()
        {
            if (model == null) return null;
            var body = Instantiate(model, model.transform.position, model.transform.rotation);
            body.transform.localScale = model.transform.lossyScale;
            var animation = body.GetComponentInChildren<Animator>();
            animation.speed = 1; animation.SetInteger("Action", 4); animation.Play("Base Layer.Death", 0, 0); animation.Update(0);
            body.AddComponent<OrganicDeathPose>();
            return body;
        }
    }

    public sealed class OrganicDeathPose : MonoBehaviour
    {
        float left = 2;
        void Update()
        {
            left -= Time.deltaTime;
            if (left > 0) return;
            var animator = GetComponentInChildren<Animator>();
            if (animator != null) { animator.Play("Base Layer.Death", 0, 1); animator.Update(0); animator.speed = 0; }
            enabled = false;
        }
    }
}
