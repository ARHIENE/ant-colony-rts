using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace AntColony.World
{
    [DefaultExecutionOrder(200)]
    public sealed class OrganicHordeMotion : MonoBehaviour
    {
        const int Batch = 250;
        static readonly int FramesId = Shader.PropertyToID("_MotionFrames");
        static readonly int[] States = { Animator.StringToHash("Idle"), Animator.StringToHash("Move"), Animator.StringToHash("Attack"), Animator.StringToHash("Hit"), Animator.StringToHash("Death"), Animator.StringToHash("Heal") };
        OrganicEnemyVisual visual;
        WildMonster owner;
        OrganicMotionBank bank;
        readonly Matrix4x4[] matrices = new Matrix4x4[Batch];
        readonly Vector4[] frames = new Vector4[Batch];
        readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();
        Vector3[] points = Array.Empty<Vector3>();
        int lastCount;
        float seed, groundTimer;
        Vector3 groundOrigin;
        Quaternion lastFacing;
        bool detached;
        public OrganicMotionBank Bank => bank;
        public int AnimatedBodies => lastCount;
        public Vector3 BodyPosition(int i) => points[i];

        void Awake()
        {
            visual = GetComponent<OrganicEnemyVisual>(); owner = GetComponent<WildMonster>();
            bank = Resources.Load<OrganicMotionBank>("OrganicEnemies1114/" + owner.OrganicKind + "/HordeMotion");
            if (bank == null) throw new InvalidOperationException("무리 동작 데이터 누락: " + owner.OrganicKind);
            seed = (unchecked((uint)GetEntityId().GetHashCode()) % 65521) / 65521f;
        }
        static int Action(AnimatorStateInfo state)
        {
            for (int i = 0; i < States.Length; i++) if (state.shortNameHash == States[i]) return i;
            return 0;
        }
        public void Render(int count)
        {
            if (bank == null || visual == null || visual.Animator == null) return;
            count = Mathf.Max(0, count);
            if (count < lastCount) EmitDeaths(count, lastCount - count);
            if (points.Length < count) { Array.Resize(ref points, Mathf.NextPowerOfTwo(Mathf.Max(64, count))); groundTimer = 0; }
            if (count > lastCount) groundTimer = 0;
            lastCount = count;
            if (count == 0) return;
            var anchor = transform.position;
            bool refresh = groundTimer <= 0 || (anchor - groundOrigin).sqrMagnitude > .01f;
            groundTimer -= Time.deltaTime;
            if (refresh)
            {
                float spacing = Mathf.Max(bank.width, .7f) / .7f;
                for (int i = 0; i < count; i++)
                {
                    float angle = seed * Mathf.PI * 2 + i * 2.39996323f;
                    float radius = Mathf.Min(6, .45f * Mathf.Sqrt(i % 177 + 1)) * spacing;
                    var wanted = anchor + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                    points[i] = Ground(anchor, wanted);
                }
                groundOrigin = anchor; groundTimer = .1f;
            }
            var state = visual.Animator.GetCurrentAnimatorStateInfo(0);
            var next = visual.Animator.GetNextAnimatorStateInfo(0);
            bool transition = visual.Animator.IsInTransition(0);
            float blend = transition ? Mathf.Clamp01(visual.Animator.GetAnimatorTransitionInfo(0).normalizedTime) : 0;
            int action = Action(state), nextAction = transition ? Action(next) : action;
            lastFacing = transform.rotation;
            float burrow = visual.BurrowOffset;
            for (int start = 0; start < count; start += Batch)
            {
                int amount = Mathf.Min(Batch, count - start);
                for (int i = 0; i < amount; i++)
                {
                    int index = start + i;
                    float phase = Mathf.Repeat(seed + index * .381966f, 1);
                    matrices[i] = Matrix4x4.TRS(points[index] + Vector3.up * burrow, lastFacing, Vector3.one);
                    frames[i] = new Vector4(bank.Frame(action, state.normalizedTime, action <= 1 ? phase : 0),
                        bank.Frame(nextAction, transition ? next.normalizedTime : state.normalizedTime, nextAction <= 1 ? phase : 0), blend, 0);
                }
                Draw(bank, matrices, frames, amount, properties, gameObject.layer, anchor);
            }
        }
        public static Vector3 Ground(Vector3 anchor, Vector3 point)
        {
            if (NavMesh.SamplePosition(point, out var hit, 2, NavMesh.AllAreas))
            {
                if (NavMesh.Raycast(anchor, hit.position, out var edge, NavMesh.AllAreas))
                {
                    var inside = Vector3.Lerp(anchor, edge.position, .94f);
                    return NavMesh.SamplePosition(inside, out hit, 1, NavMesh.AllAreas) ? hit.position : anchor;
                }
                return hit.position;
            }
            return anchor;
        }
        internal static void Draw(OrganicMotionBank data, Matrix4x4[] transforms, Vector4[] poses, int amount, MaterialPropertyBlock block, int layer, Vector3 center)
        {
            block.SetVectorArray(FramesId, poses);
            var parameters = new RenderParams(data.material) { matProps = block, layer = layer,
                worldBounds = new Bounds(center, Vector3.one * 80), shadowCastingMode = ShadowCastingMode.Off,
                receiveShadows = true, lightProbeUsage = LightProbeUsage.Off };
            Graphics.RenderMeshInstanced(parameters, data.mesh, 0, transforms, amount);
        }
        void EmitDeaths(int first, int amount)
        {
            if (amount <= 0 || bank == null || points.Length < first + amount) return;
            var positions = new Vector3[amount]; Array.Copy(points, first, positions, 0, amount);
            OrganicHordeRemains.Create(bank, positions, lastFacing, gameObject.layer);
        }
        void OnDisable()
        {
            if (!detached && owner != null && owner.IsDead) { EmitDeaths(0, lastCount); detached = true; }
            lastCount = 0;
        }
    }

    public sealed class OrganicHordeRemains : MonoBehaviour
    {
        OrganicMotionBank bank; Matrix4x4[] matrices; Vector4[] frames;
        readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();
        float age;
        public static void Create(OrganicMotionBank data, Vector3[] positions, Quaternion facing, int layer)
        {
            // Normally one host has at most 50 bodies. Split larger imported groups without dropping bodies.
            for (int start = 0; start < positions.Length; start += 250)
            {
                int count = Mathf.Min(250, positions.Length - start);
                var go = new GameObject("Organic fallen bodies"); go.layer = layer; go.transform.position = positions[start];
                var remains = go.AddComponent<OrganicHordeRemains>(); remains.bank = data;
                remains.matrices = new Matrix4x4[count]; remains.frames = new Vector4[count];
                for (int i = 0; i < count; i++) remains.matrices[i] = Matrix4x4.TRS(positions[start + i], facing, Vector3.one);
            }
        }
        void LateUpdate()
        {
            age += Time.deltaTime;
            if (age > 8) { Destroy(gameObject); return; }
            float frame = bank.Frame(4, age / bank.clips[4].seconds);
            for (int i = 0; i < frames.Length; i++) frames[i] = new Vector4(frame, frame, 0, 0);
            OrganicHordeMotion.Draw(bank, matrices, frames, matrices.Length, properties, gameObject.layer, transform.position);
        }
    }
}

