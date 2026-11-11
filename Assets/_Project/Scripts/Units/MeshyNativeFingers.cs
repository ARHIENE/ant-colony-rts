using System.Collections.Generic;
using UnityEngine;

namespace AntColony.Units
{
    // Finger-only contact solve, evaluated after the authored animation and equipment pose.
    // Shoulder, elbow, wrist and every bone translation remain untouched.
    sealed class MeshyNativeFingers
    {
        sealed class Finger
        {
            public Transform[] joints;
            public Quaternion[] authored = new Quaternion[3];
            public Vector3 tip;
            public bool thumb;
        }
        readonly List<Finger> left = new List<Finger>(), right = new List<Finger>();
        public MeshyNativeFingers(Dictionary<string, Transform> bones)
        {
            foreach (var side in new[] { "Left", "Right" })
            foreach (var name in new[] { "Thumb", "Index", "Middle", "Ring", "Pinky" })
            {
                var joints = new Transform[3]; bool found = true;
                for (int i = 0; i < 3; i++)
                    if (!bones.TryGetValue("mixamorig:" + side + "Hand" + name + (i + 1), out joints[i])) found = false;
                if (!found) continue;
                var end = joints[2].childCount > 0 ? joints[2].GetChild(0).position
                    : joints[2].position + (joints[2].position - joints[1].position) * .7f;
                (side == "Left" ? left : right).Add(new Finger { joints = joints, tip = joints[2].InverseTransformPoint(end), thumb = name == "Thumb" });
            }
        }
        public float Wrap(bool leftHand, Vector3 centre, Vector3 axis, float radius)
        {
            float error = 0;
            foreach (var finger in leftHand ? left : right)
            {
                var joints = finger.joints;
                for (int i = 0; i < 3; i++) finger.authored[i] = joints[i].localRotation;
                var onAxis = centre + axis * Vector3.Dot(joints[0].position - centre, axis);
                var radial = Vector3.ProjectOnPlane(joints[0].position - onAxis, axis);
                if (radial.sqrMagnitude < 1e-8f) continue;
                // Thumb opposes the curled fingers on the near side of the handle.
                var target = onAxis + radial.normalized * radius * (finger.thumb ? 1 : -1);
                float before = Vector3.Distance(joints[2].TransformPoint(finger.tip), target);
                for (int iteration = 0; iteration < 8; iteration++)
                for (int i = 2; i >= 0; i--)
                {
                    var tip = joints[2].TransformPoint(finger.tip);
                    var from = tip - joints[i].position; var to = target - joints[i].position;
                    if (from.sqrMagnitude < 1e-9f || to.sqrMagnitude < 1e-9f) continue;
                    var rotation = Quaternion.FromToRotation(from, to) * joints[i].rotation;
                    var local = Quaternion.Inverse(joints[i].parent.rotation) * rotation;
                    joints[i].localRotation = Quaternion.RotateTowards(finger.authored[i], local, finger.thumb ? 55 : i == 1 ? 85 : 65);
                }
                float after = Vector3.Distance(joints[2].TransformPoint(finger.tip), target);
                if (after > before)
                {
                    for (int i = 0; i < 3; i++) joints[i].localRotation = finger.authored[i];
                    after = before;
                }
                error = Mathf.Max(error, after);
            }
            return error;
        }
    }
}
