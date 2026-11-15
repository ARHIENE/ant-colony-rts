using UnityEngine;
using UnityEngine.AI;

namespace AntColony.World
{
    // The lowest vertex changes between poses; measure the animated mesh, not the initial feet.
    public sealed class OrganicGroundContact
    {
        SkinnedMeshRenderer skin;
        Vector3[] vertices;
        BoneWeight[] weights;
        Matrix4x4[] bind, matrices;
        Transform[] bones;
        public float LastCorrection { get; private set; }
        public OrganicGroundContact(GameObject model)
        {
            skin = model.GetComponentInChildren<SkinnedMeshRenderer>();
            if (skin == null) return;
            var mesh = skin.sharedMesh; vertices = mesh.vertices; weights = mesh.boneWeights; bind = mesh.bindposes; bones = skin.bones;
            if (weights.Length != vertices.Length || bones.Length != bind.Length) { skin = null; return; }
            matrices = new Matrix4x4[bones.Length];
        }
        void UpdateMatrices() { for (int i = 0; i < matrices.Length; i++) matrices[i] = bones[i].localToWorldMatrix * bind[i]; }
        Vector3 Point(int i)
        {
            var w = weights[i]; var v = vertices[i];
            return matrices[w.boneIndex0].MultiplyPoint3x4(v) * w.weight0 + matrices[w.boneIndex1].MultiplyPoint3x4(v) * w.weight1
                + matrices[w.boneIndex2].MultiplyPoint3x4(v) * w.weight2 + matrices[w.boneIndex3].MultiplyPoint3x4(v) * w.weight3;
        }
        public void Apply(GameObject model, Vector3 anchor)
        {
            if (skin == null || vertices.Length == 0) return;
            UpdateMatrices(); float lowest = float.PositiveInfinity;
            for (int i = 0; i < vertices.Length; i++) lowest = Mathf.Min(lowest, Point(i).y);
            float ground = NavMesh.SamplePosition(anchor, out var hit, .5f, NavMesh.AllAreas) ? hit.position.y : anchor.y;
            LastCorrection = ground - lowest;
            model.transform.position += Vector3.up * LastCorrection;
        }
    }
}
