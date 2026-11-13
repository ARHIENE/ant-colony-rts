using System;
using System.IO;
using System.Linq;
using System.Reflection;
using AntColony.Units;
using AntColony.Core;

public static class EarlyFlow1113DepartureChecks
{
    public static string Run()
    {
        var c = CommanderRoster.Instance.Commanders.First(x => x.IsColonyMember);
        var hp = c.WorkState.health;
        var pending = c.Social.pendingDeparture;
        var departure = c.Social.departure;
        var bloodLoss = c.Health.bloodLoss;
        int passed = 0;
        try
        {
            c.WorkState.health = 0;
            if (c.TryDeparture()) throw new Exception("Incapacitated commander departed");
            passed++;
            if (!c.Social.pendingDeparture || !c.IsColonyMember || c.Social.departure != departure)
                throw new Exception("Direct departure changed membership or lost pending request");
            passed++;
            c.Social.pendingDeparture = false;
            typeof(CommanderAnt).GetMethod("BeginDeparture", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(c, new object[] { true });
            if (!c.Social.pendingDeparture || !c.IsColonyMember || c.Social.departure != departure)
                throw new Exception("Faction departure bypassed incapacitation guard");
            passed++;
            if (c.IsDead || c.PersonalHealth != 0) throw new Exception("Guard killed or healed patient");
            passed++;
            c.WorkState.health = 100;
            c.Health.bloodLoss = .95f;
            if (!c.Health.Downed || c.TryDeparture()) throw new Exception("Health incapacity bypassed guard");
            passed++;
            if (c.IsDead || !c.IsColonyMember || c.Social.departure != departure)
                throw new Exception("Health incapacity changed membership");
            passed++;
            var result = "PASS " + passed + " incapacitated departure checks; disposable fixture, not natural recovery proof.";
            File.WriteAllText("F:/UnityProjects/ant-colony-rts/work/early-flow-1113/departure-regression.txt", result);
            return result;
        }
        finally { c.WorkState.health = hp; c.Health.bloodLoss = bloodLoss; c.Social.pendingDeparture = pending; }
    }
}
