using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using AntColony.Units;

// Isolated Play only. Compare real playback to the same source clip at three phases.
public static class Appearance1112SourceFidelity
{
    public static async Task<string> Run()
    {
        if (!Application.isPlaying || AntColony.Core.UserSettings.Current.autoSaveEnabled ||
            string.IsNullOrEmpty(AntColony.Save.SaveStorage.RootOverride)) throw new Exception("Isolated Play required");
        var player = UnityEngine.Object.FindAnyObjectByType<MeshyNativeMotionPlayer>();
        var library = Resources.Load<MeshyNativeLibrary>("MeshyNative/Library");
        var runtime = Resources.Load<MeshyNativeLibrary>("MeshyNative/Runtime");
        var oldPreview = player.PreviewMotion; var oldPhase = player.PreviewPhase;
        var report = new List<string>(); GameObject reference = null;
        AntColony.UI.SimulationPause.UserSpeed(0); EditorApplication.isPaused = false;
        try
        {
            player.Configure(library);
            foreach (var entry in library.motions)
            {
                reference = UnityEngine.Object.Instantiate(library.characters[entry.rig]);
                reference.SetActive(false);
                foreach (var animator in reference.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
                var sourceBones = reference.GetComponentsInChildren<Transform>(true)
                    .Where(t => t.name.StartsWith("mixamorig:")).ToDictionary(t => t.name);
                var sampleRoot = entry.armatureRoot ? reference.transform.Find("SmartRigArmature").gameObject : reference;
                player.PreviewMotion = entry.id;
                float maxAngle = 0, jointMotion = 0; string worst = "";
                var initialPose = new Dictionary<string, Quaternion>();
                foreach (float phase in new[] { .15f, .5f, .85f })
                {
                    player.PreviewPhase = phase;
                    await Task.Delay(220); // Allow the real player to finish its 120ms blend.
                    if (player.PlayingMotion != entry.id) throw new Exception("Selection failed: " + entry.id);
                    entry.clip.SampleAnimation(sampleRoot, entry.clip.length * phase);
                    var bones = player.NativeModel.GetComponentsInChildren<Transform>().Where(t => sourceBones.ContainsKey(t.name));
                    foreach (var bone in bones)
                    {
                        // The equipment layer intentionally adjusts fingers. Source limb rotations must remain intact.
                        if (bone.name.Contains("Hand")) continue;
                        if (!initialPose.ContainsKey(bone.name)) initialPose[bone.name] = bone.localRotation;
                        else jointMotion = Mathf.Max(jointMotion, Quaternion.Angle(initialPose[bone.name], bone.localRotation));
                        float error = Quaternion.Angle(bone.localRotation, sourceBones[bone.name].localRotation);
                        if (float.IsNaN(error)) throw new Exception("Non-finite joint: " + entry.id + "/" + bone.name);
                        if (error > maxAngle) { maxAngle = error; worst = bone.name; }
                    }
                }
                report.Add(entry.id + " | " + (maxAngle < .25f && jointMotion > .1f ? "PASS" : "REVIEW") + " | maxSourceJointDegrees=" + maxAngle + " | jointMotionDegrees=" + jointMotion + " | " + worst);
                File.WriteAllLines("work/appearance-overlap-1112/source-fidelity-progress.txt", report);
                UnityEngine.Object.DestroyImmediate(reference); reference = null;
            }
            File.WriteAllLines("work/appearance-overlap-1112/source-fidelity.txt", report);
            return report.Count + " source clips compared at three live phases; review=" + report.Count(s => s.Contains("REVIEW"));
        }
        finally
        {
            if (reference) UnityEngine.Object.DestroyImmediate(reference);
            player.Configure(runtime); player.PreviewMotion = oldPreview; player.PreviewPhase = oldPhase;
        }
    }
}
