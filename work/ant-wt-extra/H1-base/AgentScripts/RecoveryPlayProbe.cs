using System;
using System.Linq;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Save;
using AntColony.Units;
using UnityEngine;
using UnityEngine.EventSystems;

public static class RecoveryPlayProbe
{
    public static async Task<string> Existing()
    {
        var c = CommanderRoster.Instance.Commanders.First(x => x.IsColonyMember);
        var animator = c.GetComponentInChildren<Animator>();
        var before = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        Time.timeScale = 1;
        await Task.Delay(600);
        Time.timeScale = 0;
        var after = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        Physics.SyncTransforms();
        var cam = UnityEngine.Camera.main;
        var selection = UnityEngine.Object.FindAnyObjectByType<AntColony.Units.SelectionManager>();
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        typeof(AntColony.Units.SelectionManager).GetMethod("ClickSelectOrClear", flags).Invoke(selection,
            new object[] { (Vector2)cam.WorldToScreenPoint(c.GetComponent<Collider>().bounds.center), false });
        var selected = selection.GetSelectedObjects().Contains(c.GetComponent<AntColony.Units.SelectableObject>());
        var node = AntColony.World.ResourceNode.Available.Where(n => n.CanGatherWith(c) && n.ResourceType == AntColony.Data.ResourceType.Wood)
            .OrderBy(n => (n.transform.position - c.Position).sqrMagnitude).FirstOrDefault();
        if (node == null) return "selected=" + selected + "; animationAdvanced=" + (after > before) + "; NO WOOD NODE";
        var point = cam.WorldToScreenPoint(node.GetComponent<Collider>().bounds.center);
        var physics = Physics.RaycastAll(cam.ScreenPointToRay(point), 500).OrderBy(h => h.distance).Take(4).Select(h => h.collider.name);
        typeof(AntColony.Units.UnitSelectionController).GetMethod("IssueCommand", flags).Invoke(UnityEngine.Object.FindAnyObjectByType<AntColony.Units.UnitSelectionController>(), new object[] { (Vector2)point });
        return "selected=" + selected + "; animationAdvanced=" + (after > before) + "; woodGatherAccepted=" + (c.CurrentResourceNode == node)
            + "; physics=" + string.Join(",", physics) + "; wood=" + node.name;
    }

    public static async Task<string> Main()
    {
        var settings = UserSettings.Current.Clone();
        var isolated = settings.Clone(); isolated.autoSaveEnabled = false; UserSettings.Apply(isolated, false);
        var storage = SaveStorage.RootOverride;
        SaveStorage.RootOverride = System.IO.Path.Combine(Application.temporaryCachePath, "RecoveryProbe-" + Guid.NewGuid().ToString("N"));
        try
        {
            StartingCandidate candidate;
            do { candidate = StartingCandidate.Roll(0); } while (StartingCandidate.Missing(new[] { candidate }).Length > 0);
            SaveSystem.NewGame(new NewGameOptions { seed = 261025, mapSize = MapSize.Small, startingCount = 1, startingTeam = new[] { candidate } });
            var limit = Time.realtimeSinceStartup + 45;
            while (SaveSystem.Busy && Time.realtimeSinceStartup < limit) await Task.Delay(50);
            if (SaveSystem.Busy) throw new Exception("new game timed out");
            AntColony.UI.GameMenuController.Instance.Resume(); Time.timeScale = 0;
            await Task.Yield(); await Task.Yield();
            var commanders = CommanderRoster.Instance.Commanders.Where(c => c.IsColonyMember).ToArray();
            if (commanders.Length != 1) throw new Exception("startingCount 1 produced " + commanders.Length);
            var c = commanders[0];
            var cam = UnityEngine.Camera.main;
            var collider = c.GetComponent<Collider>();
            var screen = cam.WorldToScreenPoint(collider.bounds.center);
            var ray = cam.ScreenPointToRay(screen);
            var physics = Physics.RaycastAll(ray, 500).OrderBy(h => h.distance).Select(h => h.collider.name + ":" + h.collider.GetComponentInParent<SelectableObject>()?.name).Take(5);
            var hits = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = screen }, hits);
            var visual = c.GetComponent<AntVisual>();
            var animator = c.GetComponentInChildren<Animator>();
            return "ONE COMMANDER PASS; " + "collider=" + collider.bounds + "; physics=" + string.Join(",", physics)
                + "; UI=" + string.Join(",", hits.Take(5).Select(h => h.gameObject.name))
                + "; visual=" + (visual != null) + "; animator=" + (animator != null)
                + "; idleState=" + (animator != null && animator.HasState(0, Animator.StringToHash("Base Layer.Idle_A")))
                + "; walkState=" + (animator != null && animator.HasState(0, Animator.StringToHash("Base Layer.Walk")));
        }
        finally { SaveStorage.RootOverride = storage; UserSettings.Apply(settings, false); }
    }
}
