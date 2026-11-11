using System;
using System.Collections.Generic;
using AntColony.World;
using UnityEngine;

namespace AntColony.Units
{
    // 곡괭이 타격 시점(2026-11-11): 원본 Heavy_Hammer_Swing은 지면을 치는 모션이라 바위에서는 날이 닿은 뒤에도 머리가 계속 내려간다.
    // 날 끝이 실제로 보이는 바위 표면에 닿는 위상을 구해 그 위상에서 멈추고, 같은 원본 프레임을 되감아 다시 들어 올린다.
    // 관절 회전·팔 궤적은 원본 프레임 그대로이며 재생 시간만 다시 배치한다(IK·임의 곡선 없음).
    public sealed class MeshyNativeStrike
    {
        // 원본 클립에서 날 끝이 가장 높은 위상과, 타격으로 볼 수 있는 가장 늦은 위상(이후는 몸 쪽으로 끌어당기는 구간).
        public const float Apex = .50f, Latest = .82f;
        const float HoldSeconds = .10f, LiftRatio = 1.6f; // 접촉 뒤 잠깐 멈춤, 들어 올림은 내려침보다 느리게
        const int Samples = 32, Refine = 6, SurfaceLayer = 2; // Ignore Raycast: 다른 시스템의 기본 레이캐스트에 잡히지 않는다

        // 접촉 위상(클립 정규화). 음수면 닿는 위상이 없어 원본 재생을 그대로 쓴다.
        public float Contact { get; private set; } = -1;
        // 표면을 실제 메시로 계산했는지(false면 경계 상자 근사).
        public bool Exact { get; private set; }
        ResourceNode node; Vector3 at; int motion = -1; float amount;

        public static bool Applies(string motionName, string heldTool)
            => motionName == "Tool_Mine" && !string.IsNullOrEmpty(heldTool) && heldTool.StartsWith("Tool_Pickaxe_");

        // 같은 대상·같은 자리·같은 모션이면 다시 풀지 않는다. 캐서 모양(남은 양)이 바뀌면 다시 푼다.
        public bool Matches(ResourceNode target, Vector3 position, int motionIndex)
            => node == target && motion == motionIndex && (position - at).sqrMagnitude < .0004f && Mathf.Approximately(amount, target ? target.AmountRemaining : 0);

        // sample(위상) = 그 위상의 원본 포즈에서 (날 끝, 머리 중앙) 월드 좌표.
        public void Solve(ResourceNode target, Vector3 position, int motionIndex, Func<float, (Vector3 tip, Vector3 head)> sample)
        {
            node = target; at = position; motion = motionIndex; amount = target ? target.AmountRemaining : 0; Contact = -1; Exact = false;
            if (!target) return;
            using (var surface = new Surface(target))
            {
                Exact = surface.Exact;
                if (surface.Empty || surface.Inside(sample(Apex).tip)) return; // 들어 올린 자세가 이미 바위 속이면 손대지 않는다
                float outside = Apex;
                for (int i = 1; i <= Samples; i++)
                {
                    float phase = Mathf.Lerp(Apex, Latest, i / (float)Samples);
                    if (!surface.Inside(sample(phase).tip)) { outside = phase; continue; }
                    float inside = phase;
                    for (int k = 0; k < Refine; k++)
                    {
                        float mid = (outside + inside) * .5f;
                        if (surface.Inside(sample(mid).tip)) inside = mid; else outside = mid;
                    }
                    Contact = outside; // 표면 바로 바깥(오차는 표본 간격 / 2^Refine)
                    return;
                }
            }
        }

        // 누적 재생 시간(초) → 클립 시간(초). 들어 올림(되감기) → 내려침(원본 속도) → 접촉에서 잠깐 멈춤.
        public float Remap(float seconds, float clipLength)
        {
            float strike = (Contact - Apex) * clipLength, lift = strike * LiftRatio, cycle = lift + strike + HoldSeconds;
            float t = Mathf.Repeat(seconds, cycle), phase;
            if (t < lift) { float e = t / lift; phase = Mathf.Lerp(Contact, Apex, e * e * (3 - 2 * e)); }
            else if (t < lift + strike) phase = Apex + (t - lift) / clipLength;
            else phase = Contact;
            return phase * clipLength;
        }

        // 대상의 보이는 표면. 읽을 수 있는 메시는 임시 충돌체로 정확히, 아니면 경계 상자에 내접한 반타원체로 근사한다.
        // 임시 충돌체는 풀이가 끝나는 즉시(같은 프레임) 없앤다.
        sealed class Surface : IDisposable
        {
            readonly List<GameObject> colliders = new List<GameObject>();
            readonly List<Bounds> domes = new List<Bounds>();
            public bool Exact => colliders.Count > 0 && domes.Count == 0;
            public bool Empty => colliders.Count == 0 && domes.Count == 0;
            public Surface(ResourceNode target)
            {
                foreach (var r in target.GetComponentsInChildren<MeshRenderer>())
                {
                    if (!r.enabled || r.forceRenderingOff) continue;
                    var filter = r.GetComponent<MeshFilter>(); if (!filter || !filter.sharedMesh) continue;
                    if (!filter.sharedMesh.isReadable) { domes.Add(r.bounds); continue; }
                    var go = new GameObject("Strike surface") { layer = SurfaceLayer, hideFlags = HideFlags.HideAndDontSave };
                    go.transform.SetPositionAndRotation(r.transform.position, r.transform.rotation); go.transform.localScale = r.transform.lossyScale;
                    go.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh; colliders.Add(go);
                }
                if (colliders.Count > 0) Physics.SyncTransforms();
            }
            public bool Inside(Vector3 p)
            {
                if (colliders.Count > 0 && Physics.Raycast(new Vector3(p.x, p.y + 8, p.z), Vector3.down, out var hit, 24, 1 << SurfaceLayer, QueryTriggerInteraction.Ignore)
                    && hit.point.y > p.y) return true;
                foreach (var b in domes)
                {
                    float x = (p.x - b.center.x) / Mathf.Max(.01f, b.extents.x), z = (p.z - b.center.z) / Mathf.Max(.01f, b.extents.z), y = (p.y - b.min.y) / Mathf.Max(.01f, b.size.y);
                    if (y >= 0 && x * x + y * y + z * z < 1) return true;
                }
                return false;
            }
            public void Dispose()
            {
                foreach (var go in colliders) if (Application.isPlaying) { go.SetActive(false); UnityEngine.Object.Destroy(go); } else UnityEngine.Object.DestroyImmediate(go);
                colliders.Clear();
            }
        }
    }
}
