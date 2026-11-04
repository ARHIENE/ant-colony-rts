using UnityEngine;

namespace AntColony.Units
{
    // Animator's built-in culling cannot see these FBX sibling meshes. Use their actual bounds instead.
    public sealed class RigVisibility : MonoBehaviour
    {
        private Animator animator;
        private SkinnedMeshRenderer[] renderers;
        private float nextCheck;
        private static UnityEngine.Camera camera;
        private static readonly Plane[] Planes = new Plane[6];
        private static int planeFrame = -1;

        public void Initialize(Animator value, GameObject model)
        {
            animator = value;
            renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        }

        private void LateUpdate()
        {
            if (animator == null || renderers == null || Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + .1f;
            if (camera == null) camera = UnityEngine.Camera.main;
            if (camera == null) return;
            if (planeFrame != Time.frameCount)
            {
                GeometryUtility.CalculateFrustumPlanes(camera, Planes);
                planeFrame = Time.frameCount;
            }
            var visible = false;
            foreach (var renderer in renderers)
                if (renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy && GeometryUtility.TestPlanesAABB(Planes, renderer.bounds))
                { visible = true; break; }
            if (animator.enabled != visible)
            {
                animator.enabled = visible;
                // Sibling skins need forced updates while visible; stop that work together with offscreen animation.
                foreach (var skin in renderers) if (skin != null) skin.updateWhenOffscreen = visible;
            }
        }

        private void OnDisable()
        {
            if (animator == null) return;
            animator.enabled = true;
            foreach (var skin in renderers) if (skin != null) skin.updateWhenOffscreen = true;
        }
    }
}
