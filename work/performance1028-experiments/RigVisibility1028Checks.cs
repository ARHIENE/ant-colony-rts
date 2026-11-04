using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AntColony.Units;
using UnityEngine;
using Object = UnityEngine.Object;

public static class RigVisibility1028Checks
{
    public static async Task<string> Main()
    {
        if (!Application.isPlaying) throw new Exception("Run in Play");
        var count = 0;
        void Check(bool ok, string label) { if (!ok) throw new Exception("Rig visibility FAIL " + label); count++; }
        var camera = UnityEngine.Camera.main;
        var controller = camera.GetComponent<AntColony.Camera.IsometricCameraController>();
        var wasEnabled = controller.enabled; controller.enabled = false;
        var root = new GameObject("Rig visibility checks"); root.transform.position = controller.FocusPoint;
        var speed = Time.timeScale; Time.timeScale = 1;
        try
        {
            var model = KemonoModel.Create(root.transform, 1387);
            var animator = RigAnimator.Find(model);
            var appearance = model.GetComponent<KemonoAppearance>();
            var home = root.transform.position;
            Check(model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Any(r => !r.gameObject.activeInHierarchy), "unused fixed variants inactive");
            foreach (var expression in new[] { "NEUTRAL", "PAIN", "SLEEP", "TASTY", "SAD", "DEAD" })
            {
                appearance.SetExpression(expression);
                Check(model.GetComponentsInChildren<Renderer>().Any(r => r.enabled && r.name.StartsWith("Part_Face_" + expression + "_")), "facial variant kept " + expression);
            }
            appearance.SetExpression("NEUTRAL"); appearance.SetArmor(true);
            Check(model.GetComponentsInChildren<Renderer>().Any(r => r.enabled && r.name.StartsWith("Part_Equipment_Armor_")), "armor variant kept");
            appearance.SetArmor(false);
            animator.Play("Base Layer.Walk", 0, 0);
            for (var i = 0; i < 12; i++)
            {
                root.transform.position = home;
                await Task.Delay(250);
                Check(animator.enabled, "visible rig enabled " + i);
                var before = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
                var pose = model.GetComponentsInChildren<Transform>().Select(t => t.localRotation).ToArray();
                await Task.Delay(150);
                Check(animator.GetCurrentAnimatorStateInfo(0).normalizedTime > before, "visible animation advances " + i
                    + " before=" + before + " after=" + animator.GetCurrentAnimatorStateInfo(0).normalizedTime
                    + " scale=" + Time.timeScale + " delta=" + Time.deltaTime + " speed=" + animator.speed);
                Check(model.GetComponentsInChildren<Transform>().Where((t, n) => Quaternion.Angle(t.localRotation, pose[n]) > .1f).Any(), "visible bones move " + i);
                root.transform.position = home + Vector3.right * 1000;
                await Task.Delay(250);
                Check(!animator.enabled, "offscreen rig suspended " + i);
            }
            root.transform.position = home; await Task.Delay(250);
            Check(animator.enabled, "final camera return resumes rig");
            var result = "PASS " + count + " fixed parts/expressions/armor and twelve offscreen/return animation cycles";
            File.WriteAllText("Temp/rig-visibility1028-result.txt", result); return result;
        }
        catch (Exception e) { File.WriteAllText("Temp/rig-visibility1028-result.txt", e.ToString()); throw; }
        finally { Object.Destroy(root); controller.enabled = wasEnabled; Time.timeScale = speed; }
    }
}
