using System.Linq;
using UnityEngine;
using AntColony.Units;
public static class Roster1115Inspect {
 public static string Main()=>string.Join("\n",Object.FindObjectsByType<CommanderAnt>(FindObjectsInactive.Include).Where(c=>c.CommanderName=="Commander 4"||c.IsColonyMember).Select(c=>c.CommanderName+" active="+c.isActiveAndEnabled+" dead="+c.IsDead+" member="+c.IsColonyMember+" combatOrder="+c.IsFollowingCombatOrder+" hp="+c.PersonalHealth+" pos="+c.Position+" away="+c.IsAwayFromHome+" pendingDeparture="+c.PersonalState.social.pendingDeparture+" departure="+c.PersonalState.social.departure+" lowMoodSeconds="+c.PersonalState.lowMoodSeconds+" mood="+c.Mood+" satiety="+c.Satiety));
}


