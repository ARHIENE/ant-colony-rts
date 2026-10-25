using System.Linq;
using UnityEngine;

namespace AntColony.Units
{
    public sealed class CivilianVisual : MonoBehaviour
    {
        private Transform model;
        private Animator animator;
        private Vector3 previous;
        private bool walking;

        private void Awake()
        {
            var source = Resources.Load<GameObject>("Residents/Resident_Combo_A");
            var controller = Resources.Load<RuntimeAnimatorController>("Residents/ResidentAnimator");
            if (source == null || controller == null) { enabled = false; return; }
            model = new GameObject("ResidentModel").transform; model.SetParent(transform, false);
            var instance = Instantiate(source, model, false);
            animator = RigAnimator.Bind(instance, "Combo_A", controller);
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate; animator.Rebind(); animator.Update(0);
            Bounds Bounds() => instance.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
            model.localScale *= 1.4f / Mathf.Max(Bounds().size.y, .01f);
            model.position += Vector3.up * (transform.position.y - .6f - Bounds().min.y);
            model.localRotation = Quaternion.Euler(0, 180, 0);
            GetComponent<Renderer>().forceRenderingOff = true;
            animator.Play("Base Layer.Idle"); previous = transform.position;
        }

        private void LateUpdate()
        {
            if (model == null || Time.deltaTime <= 0) return;
            var delta = transform.position - previous; previous = transform.position; delta.y = 0;
            var moving = delta.sqrMagnitude > .000001f;
            if (moving) model.rotation = Quaternion.LookRotation(delta) * Quaternion.Euler(0, 180, 0);
            if (moving == walking) return;
            walking = moving; animator.CrossFadeInFixedTime("Base Layer." + (walking ? "Walk" : "Idle"), .1f);
        }
    }
}
