using System.Linq;
using AntColony.Buildings;
using UnityEngine;

namespace AntColony.World
{
    public sealed class CritterVisual : MonoBehaviour
    {
        private Critter critter;
        private Transform model;
        private Animator animator;
        private Vector3 previous, previousScale;
        private string state;
        private float actionUntil;
        private Quaternion facingCorrection;

        private void Awake()
        {
            critter = GetComponent<Critter>();
            var name = critter.Species.ToString();
            var source = Resources.Load<GameObject>("Critters/" + name + "/" + name);
            var controller = Resources.Load<RuntimeAnimatorController>("Critters/" + name + "/" + name + "Animator");
            if (source == null || controller == null) { enabled = false; return; }
            var pivot = new GameObject("CritterModel").transform; pivot.SetParent(transform, false); model = pivot;
            var instance = Instantiate(source, pivot, false);
            animator = instance.GetComponentInChildren<Animator>() ?? instance.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller; animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate; animator.Rebind(); animator.Update(0);
            var bones = instance.GetComponentsInChildren<Transform>();
            var forward = pivot.InverseTransformDirection(bones.First(t => t.name == "Head").position - bones.First(t => t.name == "Body").position); forward.y = 0;
            facingCorrection = Quaternion.Inverse(Quaternion.LookRotation(forward));
            var bounds = Bounds();
            var width = (critter.Info.space >= 2 ? 1.8f : .9f) * (critter.Stage == CritterStage.Baby ? .6f : 1);
            pivot.localScale *= width / Mathf.Max(bounds.size.x, bounds.size.z, .01f);
            GetComponent<Renderer>().forceRenderingOff = true;
            foreach (var c in instance.GetComponentsInChildren<Collider>()) Destroy(c);
            FitGround(); previous = transform.position;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            Play("Idle");
        }

        private Bounds Bounds() => model.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });

        private void FitGround()
        {
            var bounds = Bounds();
            var floor = transform.position.y - .5f;
            if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out var hit, 2, UnityEngine.AI.NavMesh.AllAreas)) floor = hit.position.y;
            model.position += Vector3.up * (floor - bounds.min.y);
            bounds = Bounds();
            var box = GetComponent<BoxCollider>();
            if (box == null) { foreach (var old in GetComponents<Collider>()) Destroy(old); box = gameObject.AddComponent<BoxCollider>(); }
            var scale = transform.lossyScale;
            box.center = transform.InverseTransformPoint(bounds.center);
            box.size = new Vector3(bounds.size.x / Mathf.Abs(scale.x), bounds.size.y / Mathf.Abs(scale.y), bounds.size.z / Mathf.Abs(scale.z));
            previousScale = scale;
        }

        private void Play(string clip)
        {
            if (state == clip || !animator.HasState(0, Animator.StringToHash("Base Layer." + clip))) return;
            animator.CrossFadeInFixedTime(Animator.StringToHash("Base Layer." + clip), .1f, 0); state = clip;
        }

        public void Attack() { if (animator == null) return; state = null; Play("Attack"); actionUntil = Time.time + .6f; }
        public void Hurt() { if (animator == null) return; state = null; Play("Hurt"); actionUntil = Time.time + .3f; }

        private void LateUpdate()
        {
            if (model == null) return;
            if (previousScale != transform.lossyScale || Mathf.Abs(previous.y - transform.position.y) > .01f && critter.Carrier == null && critter.Clinic == null) FitGround();
            var delta = transform.position - previous; previous = transform.position; delta.y = 0;
            if (Time.deltaTime <= 0 || Time.time < actionUntil) return;
            if (delta.sqrMagnitude > .000001f) model.rotation = Quaternion.LookRotation(delta) * facingCorrection;
            Play(critter.Downed ? "Lie" : critter.Bound ? "Bound" : delta.sqrMagnitude > .000001f
                ? critter.Species == Species.Bee ? "Fly" : "Walk" : critter.Eating ? "Eat" : "Idle");
        }
    }
}
