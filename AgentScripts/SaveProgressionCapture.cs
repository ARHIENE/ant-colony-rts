using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

public static class SaveProgressionCapture
{
    public static async Task<string> Main()
    {
        if (!Application.isPlaying) throw new System.Exception("Play mode required");
        Time.timeScale = 1f;
        Object.FindAnyObjectByType<AntColony.Core.UpkeepManager>().enabled = false;
        foreach (var threat in Object.FindObjectsByType<MonoBehaviour>())
            if (threat is AntColony.World.WildMonster || threat is AntColony.World.ColonyInvasion)
                threat.enabled = false;
        var commander = Object.FindObjectsByType<AntColony.Units.CommanderAnt>()
            .First(c => c.AllowedRoles.Contains(AntColony.Data.UnitRole.Ranged));
        commander.CommandStop();
        commander.TrySetRole(AntColony.Data.UnitRole.Ranged);
        AntColony.Core.AntPool.Instance.Breed(10);
        commander.TryAssign(Mathf.Min(5, commander.FreeRanks));
        commander.Progression.AddXp(300);
        var template = Resources.FindObjectsOfTypeAll<AntColony.Buildings.ResearchLab>()
            .First(l => l.gameObject.scene.IsValid() && l.Role == commander.Role);
        var lab = Object.Instantiate(template, commander.Position + Vector3.right * 5f, Quaternion.identity);
        lab.gameObject.SetActive(true);
        var resources = AntColony.Core.ResourceManager.Instance;
        resources.Add(AntColony.Data.ResourceType.Food, 200);
        resources.Add(AntColony.Data.ResourceType.Soil, 200);
        if (!lab.TryResearchAttack(commander)) throw new System.Exception("ATK research refused");
        while (lab.IsResearching) await Task.Delay(50);
        if (!lab.TryResearchArmor(commander)) throw new System.Exception("Armor research refused");
        while (lab.IsResearching) await Task.Delay(50);
        var selection = Object.FindAnyObjectByType<AntColony.Units.SelectionManager>();
        selection.ClearSelection();
        typeof(AntColony.Units.SelectionManager).GetMethod("AddToSelection", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(selection, new object[] { commander.GetComponent<AntColony.Units.SelectableObject>() });
        commander.TryPowerStrike();
        commander.TryDefensiveStance();
        Time.timeScale = 0f;
        await Task.Delay(150);
        return $"{commander.CommanderName}: Lv{commander.Progression.Level}, ATK {commander.LabAttackLevel}, Armor {commander.LabArmorLevel}, Strike {commander.Skills.PowerStrikeArmed}, Guard {commander.Skills.DefensiveStanceActive}";
    }
}
