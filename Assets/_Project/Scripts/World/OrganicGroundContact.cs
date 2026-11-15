using UnityEngine;
using UnityEngine.AI;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace AntColony.World
{
    // The lowest vertex changes between poses; measure the animated mesh, not the initial feet.
    public sealed class OrganicGroundContact : System.IDisposable
    {
        SkinnedMeshRenderer skin;
        NativeArray<Vector3> vertices;
        NativeArray<BoneWeight> weights;
        Matrix4x4[] bind;
        NativeArray<Vector4> heightRows;
        NativeArray<float> minimum;
        Transform[] bones;
        public float LastCorrection { get; private set; }
        public OrganicGroundContact(GameObject model)
        {
            skin = model.GetComponentInChildren<SkinnedMeshRenderer>();
            if (skin == null) return;
            var mesh = skin.sharedMesh;
            if (mesh == null) { skin = null; return; }
            var sourceVertices = mesh.vertices; var sourceWeights = mesh.boneWeights;
            bind = mesh.bindposes; bones = skin.bones;
            if (sourceWeights.Length != sourceVertices.Length || bones.Length != bind.Length) { skin = null; return; }
            vertices = new NativeArray<Vector3>(sourceVertices, Allocator.Persistent);
            weights = new NativeArray<BoneWeight>(sourceWeights, Allocator.Persistent);
            heightRows = new NativeArray<Vector4>(bones.Length, Allocator.Persistent);
            minimum = new NativeArray<float>(1, Allocator.Persistent);
        }
        public void Dispose()
        {
            if (vertices.IsCreated) vertices.Dispose();
            if (weights.IsCreated) weights.Dispose();
            if (heightRows.IsCreated) heightRows.Dispose();
            if (minimum.IsCreated) minimum.Dispose();
            skin = null;
        }
        void UpdateMatrices()
        {
            for (int i = 0; i < heightRows.Length; i++)
                heightRows[i] = (bones[i].localToWorldMatrix * bind[i]).GetRow(1);
        }
        [BurstCompile(CompileSynchronously = true)]
        struct LowestPointJob : IJob
        {
          [ReadOnly] public NativeArray<Vector3> vertices;
          [ReadOnly] public NativeArray<BoneWeight> weights;
          [ReadOnly] public NativeArray<Vector4> heightRows;
          [WriteOnly] public NativeArray<float> minimum;
          public void Execute()
          {
            float lowest = float.PositiveInfinity;
            for (int i = 0; i < vertices.Length; i++) lowest = Mathf.Min(lowest, Height(i));
            minimum[0] = lowest;
          }
          float Height(int i)
          {
            var w = weights[i]; var v = vertices[i];
            // Ground contact needs only world Y. Keep all vertices and the same
            // weighted bone pose, without calculating unused X/Z or zero weights.
            var row = heightRows[w.boneIndex0];
            float y = (row.x * v.x + row.y * v.y + row.z * v.z + row.w) * w.weight0;
            if (w.weight1 > 0) { row = heightRows[w.boneIndex1]; y += (row.x * v.x + row.y * v.y + row.z * v.z + row.w) * w.weight1; }
            if (w.weight2 > 0) { row = heightRows[w.boneIndex2]; y += (row.x * v.x + row.y * v.y + row.z * v.z + row.w) * w.weight2; }
            if (w.weight3 > 0) { row = heightRows[w.boneIndex3]; y += (row.x * v.x + row.y * v.y + row.z * v.z + row.w) * w.weight3; }
            return y;
          }
        }
        public void Apply(GameObject model, Vector3 anchor)
        {
            if (skin == null || vertices.Length == 0) return;
            UpdateMatrices();
            new LowestPointJob { vertices = vertices, weights = weights, heightRows = heightRows, minimum = minimum }.Run();
            float ground = NavMesh.SamplePosition(anchor, out var hit, .5f, NavMesh.AllAreas) ? hit.position.y : anchor.y;
            LastCorrection = ground - minimum[0];
            model.transform.position += Vector3.up * LastCorrection;
        }
    }
}
