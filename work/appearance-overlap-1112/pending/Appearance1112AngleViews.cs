using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using AntColony.Core;
using AntColony.Units;

public static class Appearance1112AngleViews
{
    public static async Task<string> Run()
    {
        if (!Application.isPlaying) throw new Exception("Isolated Play required");
        var c = CommanderRoster.Instance.Commanders.First(x => x && x.IsColonyMember);
        c.CommandStop();
        var player = c.GetComponent<MeshyNativeMotionPlayer>();
        var original = c.transform.Find("Kemono");
        var appearance = original.GetComponent<KemonoAppearance>();
        var sourceSkins = original.GetComponentsInChildren<SkinnedMeshRenderer>();
        var oldHair = sourceSkins.First(s => s.enabled && s.name.StartsWith("Part_Hair_")).name.Split('_')[2];
        var face = sourceSkins.First(s => s.name.StartsWith("Part_Face_"));
        var weights = Enumerable.Range(0, face.sharedMesh.blendShapeCount).Select(face.GetBlendShapeWeight).ToArray();
        var camera = Camera.main;
        var oldPosition = camera.transform.position; var oldRotation = camera.transform.rotation;
        float oldSize = camera.orthographicSize, oldNear = camera.nearClipPlane;
        var controller = UnityEngine.Object.FindAnyObjectByType<AntColony.Camera.IsometricCameraController>();
        bool controllerEnabled = controller && controller.enabled;
        string previousMotion = player.PreviewMotion; float previousPhase = player.PreviewPhase;
        string folder = "work/appearance-overlap-1112/angle-review";
        Directory.CreateDirectory(folder);
        int captures = 0;
        void View(float angle)
        {
            var head = player.NativeModel.GetComponentsInChildren<Transform>().First(t => t.name == "mixamorig:Head");
            var focus = head.position + Vector3.up * .08f;
            var direction = Quaternion.AngleAxis(angle, Vector3.up) * original.forward;
            camera.orthographicSize = .65f; camera.nearClipPlane = .02f;
            camera.transform.position = focus + direction * 3 + Vector3.up * .12f;
            camera.transform.LookAt(focus);
        }
        async Task Capture(string name)
        {
            await Task.Delay(150);
            ScreenCapture.CaptureScreenshot(folder + "/" + name + ".png");
            await Task.Delay(150); captures++;
        }
        try
        {
            AntColony.UI.SimulationPause.UserSpeed(0); EditorApplication.isPaused = false;
            if (controller) controller.enabled = false;
            EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
            foreach (var motion in new[] { "Idle_3", "Heavy_Hammer_Swing" })
            {
                player.PreviewMotion = motion; player.PreviewPhase = .3f;
                for (int i = 0; i < weights.Length; i++) face.SetBlendShapeWeight(i, 0);
                await Task.Delay(300);
                foreach (var hair in appearance.GetVariants("Hair"))
                {
                    appearance.SetVariant("Hair", hair);
                    await Task.Delay(100);
                    foreach (var angle in new[] { 0, 75, 180 })
                    {
                        View(angle); await Capture(motion + "-" + hair + "-" + angle);
                    }
                }
                appearance.SetVariant("Hair", "BOB"); View(0);
                for (int shape = 0; shape < weights.Length; shape++)
                {
                    for (int i = 0; i < weights.Length; i++) face.SetBlendShapeWeight(i, i == shape ? 100 : 0);
                    player.NativeModel.GetComponent<MeshyNativeAppearance>().Sync();
                    await Capture(motion + "-expression-" + face.sharedMesh.GetBlendShapeName(shape));
                }
            }
            return captures + " actual Game captures; review visually before accepting";
        }
        finally
        {
            appearance.SetVariant("Hair", oldHair);
            for (int i = 0; i < weights.Length; i++) face.SetBlendShapeWeight(i, weights[i]);
            player.PreviewMotion = previousMotion; player.PreviewPhase = previousPhase;
            camera.transform.SetPositionAndRotation(oldPosition, oldRotation);
            camera.orthographicSize = oldSize; camera.nearClipPlane = oldNear;
            if (controller) controller.enabled = controllerEnabled;
        }
    }
}
