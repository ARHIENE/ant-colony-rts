using System.Linq;
using UnityEngine;

namespace AntColony.Units
{
    public static class RigAnimator
    {
        // 메쉬 FBX에는 포장 노드가 하나 더 있다. 클립의 Root/... 경로는 실제 리그를 기준으로 연결한다.
        public static Animator Bind(GameObject model, string rigName, RuntimeAnimatorController controller)
        {
            var rig = model.transform.Find(rigName);
            var imported = model.GetComponentInChildren<Animator>();
            var avatar = imported.avatar;
            if (imported.transform != rig) { imported.runtimeAnimatorController = null; imported.enabled = false; }
            var animator = rig.GetComponent<Animator>();
            if (animator == null) animator = rig.gameObject.AddComponent<Animator>();
            animator.avatar = avatar; animator.runtimeAnimatorController = controller; animator.applyRootMotion = false;
            // FBX의 스킨 메시가 리그의 형제 노드라 Animator의 가시성 판정에 잡히지 않는다.
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            foreach (var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>(true)) skin.updateWhenOffscreen = true;
            return animator;
        }

        public static Animator Find(GameObject model) => model.GetComponentsInChildren<Animator>(true).FirstOrDefault(a => a.runtimeAnimatorController != null);
    }
}
