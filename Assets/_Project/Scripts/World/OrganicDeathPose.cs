using System.Collections.Generic;
using UnityEngine;

namespace AntColony.World
{
    // Meshy rigs keep their proportions. A staggered collapse replaces the old whole-body flip.
    public sealed class OrganicDeathPose : MonoBehaviour
    {
        public const float Duration = 2.6f;
        struct Joint { public Transform bone; public Vector3 axis; public float degrees, delay; }
        readonly List<Joint> joints = new List<Joint>();
        Transform[] bones;
        Quaternion[] initial, rest;
        Vector3[] initialPosition, restPosition;
        Transform root;
        Quaternion rootRest;
        Vector3 rollAxis, pitchAxis, modelPosition;
        OrganicGroundContact contact;
        float elapsed, roll, side;
        public float Elapsed => elapsed;
        public static OrganicDeathPose Attach(GameObject model, OrganicEnemyKind kind, float age)
        {
            var pose = model.GetComponent<OrganicDeathPose>() ?? model.AddComponent<OrganicDeathPose>();
            pose.Initialize(kind, age); return pose;
        }
        void Initialize(OrganicEnemyKind kind, float age)
        {
            contact?.Dispose(); joints.Clear();
            var skin = GetComponentInChildren<SkinnedMeshRenderer>();
            if (skin == null) { enabled = false; return; }
            bones = skin.bones;
            initial = new Quaternion[bones.Length]; rest = new Quaternion[bones.Length];
            initialPosition = new Vector3[bones.Length]; restPosition = new Vector3[bones.Length];
            for (int i = 0; i < bones.Length; i++) { initial[i] = bones[i].localRotation; initialPosition[i] = bones[i].localPosition; }
            var animator = GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.enabled = true; animator.SetInteger("Action", 0); animator.Play("Base Layer.Idle", 0, 0); animator.Update(0);
                animator.enabled = false;
            }
            for (int i = 0; i < bones.Length; i++) { rest[i] = bones[i].localRotation; restPosition[i] = bones[i].localPosition; }
            root = Find(0); if (root == null) { enabled = false; return; }
            rootRest = root.localRotation;
            rollAxis = root.InverseTransformDirection(transform.forward).normalized;
            pitchAxis = root.InverseTransformDirection(transform.right).normalized;
            side = Mathf.Sin(transform.position.x * 13.17f + transform.position.z * 7.31f) >= 0 ? 1 : -1;
            roll = kind is OrganicEnemyKind.ClawHunter or OrganicEnemyKind.BreachBeast or OrganicEnemyKind.BroodAlien or OrganicEnemyKind.NeedleStalker or OrganicEnemyKind.HookMaw ? 76 : kind is OrganicEnemyKind.BurrowLurker or OrganicEnemyKind.WallMelter or OrganicEnemyKind.VeilWeaver ? 26 : 48;
            var hips = Hips(kind);
            for (int i = 0; i < hips.Length; i++)
            {
                var hip = Find(hips[i]); if (hip == null) continue;
                Add(hip, transform.right, 32 + i % 3 * 8, .06f * (i % 4));
                // The next two joints fold after the leg loses support, rather than rotating as one rigid limb.
                var lower = hip.childCount > 0 ? hip.GetChild(0) : null;
                if (lower != null) { Add(lower, transform.right, -54, .18f + i % 3 * .06f); if (lower.childCount > 0) Add(lower.GetChild(0), transform.right, 30, .35f + i % 2 * .08f); }
            }
            Add(Find(Head(kind)), transform.right, 28, .4f);
            if (kind == OrganicEnemyKind.BurrowLurker)
                for (int i = 8; i <= 15; i++) Add(Find(i), transform.up, (i % 2 == 0 ? 1 : -1) * 14, .4f + (15 - i) * .07f);
            if (kind == OrganicEnemyKind.HookMaw)
                for (int i = 58; i <= 63; i++) Add(Find(i), transform.right, 18, .5f + (63 - i) * .08f);
            modelPosition = transform.localPosition;
            contact = new OrganicGroundContact(gameObject); elapsed = Mathf.Clamp(age, 0, Duration); enabled = true;
            Sample(elapsed);
            if (elapsed >= Duration) Finish();
        }
        Transform Find(int id) { string name = "Bone_" + id.ToString("000"); foreach (var b in bones) if (b.name == name) return b; return null; }
        void Add(Transform bone, Vector3 worldAxis, float degrees, float delay)
        { if (bone != null && bone != root) joints.Add(new Joint { bone = bone, axis = bone.InverseTransformDirection(worldAxis).normalized, degrees = degrees, delay = delay }); }
        static float Ease(float start, float end, float t) => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(start, end, t));
        public void Sample(float seconds)
        {
            if (root == null) return;
            float t = Mathf.Clamp(seconds, 0, Duration), settle = Ease(.15f, 1.25f, t), blend = Ease(0, .5f, t);
            elapsed = t;
            transform.localPosition = modelPosition;
            for (int i = 0; i < bones.Length; i++) { bones[i].localRotation = Quaternion.Slerp(initial[i], rest[i], blend); bones[i].localPosition = Vector3.Lerp(initialPosition[i], restPosition[i], blend); }
            float recoil = Mathf.Sin(Mathf.Clamp01(t / .3f) * Mathf.PI) * -9;
            float bounce = t > 1 ? Mathf.Sin((t - 1) * 15) * Mathf.Exp(-(t - 1) * 5) * 7 * (1 - Ease(2, Duration, t)) : 0;
            root.localRotation = Quaternion.Slerp(initial[System.Array.IndexOf(bones, root)], rootRest, blend)
                * Quaternion.AngleAxis(side * (roll * settle + bounce), rollAxis) * Quaternion.AngleAxis(recoil + 8 * settle, pitchAxis);
            foreach (var joint in joints)
            {
                float fold = Ease(.1f + joint.delay, .9f + joint.delay, t);
                float twitch = t > 1.15f + joint.delay ? Mathf.Sin((t - joint.delay) * 23) * 3 * (1 - Ease(1.3f, 2.35f, t)) : 0;
                joint.bone.localRotation *= Quaternion.AngleAxis(joint.degrees * fold + twitch, joint.axis);
            }
            contact?.Apply(gameObject, transform.parent != null ? transform.parent.position : transform.position);
        }
        void LateUpdate()
        {
            if (AntColony.Save.SaveSystem.Busy) return;
            elapsed = Mathf.Min(Duration, elapsed + Time.deltaTime); Sample(elapsed); if (elapsed >= Duration) Finish();
        }
        void Finish() { contact?.Dispose(); contact = null; enabled = false; }
        void OnDestroy() { contact?.Dispose(); }
        static int[] Hips(OrganicEnemyKind kind) => kind switch
        {
            OrganicEnemyKind.WallRam => new[]{11,15,32,36}, OrganicEnemyKind.NeedleStalker => new[]{12,17}, OrganicEnemyKind.BloomMender => new[]{9,14,19,24,29},
            OrganicEnemyKind.BreachBeast or OrganicEnemyKind.ClawHunter => new[]{12,17}, OrganicEnemyKind.Bulwark => new[]{27,33,9,14},
            OrganicEnemyKind.CarrionReclaimer => new[]{34,39,46,51,11,15,19,23,27}, OrganicEnemyKind.VeilWeaver => new[]{5,9,13,17},
            OrganicEnemyKind.BroodAlien => new[]{10,15}, OrganicEnemyKind.BroodCarrier => new[]{6,11,16,21,26,31,36,41},
            OrganicEnemyKind.HuntRider => new[]{44,48,9,14}, OrganicEnemyKind.SiegeBrooder => new[]{8,12,16,20,24},
            OrganicEnemyKind.SiegeRider => new[]{10,14,20,24,33,37,41}, OrganicEnemyKind.WallMelter => new[]{7,12,17,22,27,32,37,42},
            OrganicEnemyKind.AcidSpitter => new[]{13,17,20,24,28}, OrganicEnemyKind.BurrowLurker => new[]{22,27}, OrganicEnemyKind.HookMaw => new[]{10,15}, _ => new int[0]
        };
        static int Head(OrganicEnemyKind kind) => kind switch
        {
            OrganicEnemyKind.WallRam => 5, OrganicEnemyKind.NeedleStalker => 4, OrganicEnemyKind.BloomMender => 3,
            OrganicEnemyKind.BreachBeast => 22, OrganicEnemyKind.Bulwark => 19, OrganicEnemyKind.CarrionReclaimer => 7,
            OrganicEnemyKind.ClawHunter => 6, OrganicEnemyKind.BroodAlien => 18, OrganicEnemyKind.HuntRider => 23,
            OrganicEnemyKind.SiegeBrooder => 4, OrganicEnemyKind.SiegeRider => 29, OrganicEnemyKind.WallMelter => 3,
            OrganicEnemyKind.AcidSpitter => 9, OrganicEnemyKind.BurrowLurker => 2, OrganicEnemyKind.HookMaw => 28, _ => 1
        };
    }
}
