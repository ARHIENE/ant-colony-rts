var go = new GameObject("AirborneCheck");
go.SetActive(false);
var data = ScriptableObject.CreateInstance<AntColony.Data.UnitData>();
try
{
    var soldier = go.AddComponent<AntColony.Units.SoldierAnt>();
    typeof(AntColony.Units.AntUnitBase).GetProperty("Data").SetValue(soldier, data);
    foreach (AntColony.Data.UnitRole role in Enum.GetValues(typeof(AntColony.Data.UnitRole)))
    {
        data.role = role;
        if (AntColony.Core.CombatTargeting.IsAirborne(soldier) != (role == AntColony.Data.UnitRole.Flying))
            throw new Exception("Incorrect airborne classification: " + role);
    }
    if (AntColony.Core.CombatTargeting.IsAirborne(null)) throw new Exception("Null target is airborne");
    return "PASS: all 6 roles and null target";
}
finally
{
    UnityEngine.Object.DestroyImmediate(go);
    UnityEngine.Object.DestroyImmediate(data);
}

