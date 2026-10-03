using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.Units;
using UnityEngine;
using Object = UnityEngine.Object;

// Phase 3: 충성심 삭제 → 기분이 대신한다(탈주·반란·붕괴 도주·포로 회유).
public static class Phase3Checks
{
    static int checks;
    static void Check(bool value, string label) { if (!value) throw new Exception("FAIL: " + label); checks++; }
    static void Social(CommanderAnt c, float seconds) =>
        typeof(CommanderAnt).GetMethod("TickSocial", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(c, new object[] { seconds });
    static void Fresh(CommanderAnt c, params CommanderTrait[] traits)
    {
        var t = new CommanderTraits(); foreach (var v in traits) Check(t.TryAdd(v), "trait " + v);
        c.ApplyTraits(t); c.PersonalState.moodFactors.Clear(); c.PersonalState.relations.Clear(); c.PersonalState.lowMoodSeconds = 0;
    }
    public static async Task<string> Main()
    {
        checks = 0;
        if (!Application.isPlaying) throw new Exception("Play mode required");
        while (SaveSystem.Busy) await Task.Delay(50);
        SaveSystem.NewGame(new NewGameOptions { seed = 261002, mapSize = MapSize.Small });
        while (SaveSystem.Busy) await Task.Delay(50);
        Time.timeScale = 0;
        var all = CommanderRoster.Instance.Commanders.Where(c => c.IsColonyMember && !c.IsEmbarked).ToArray();
        Check(all.Length >= 4, "enough commanders");
        var a = all[0]; var b = all[1]; var c = all[2]; var d = all[3];

        Check(typeof(CommanderTraits).GetProperty("Loyalty") == null && typeof(CommanderTraits).GetField("loyalty", BindingFlags.Instance | BindingFlags.NonPublic) == null, "loyalty removed");
        Fresh(a); Check(a.Traits.DepartureMood == 20, "default departure mood 20");
        Fresh(b, CommanderTrait.Loyal); Check(b.Traits.DepartureMood == 10, "loyal holds to 10");
        Fresh(c, CommanderTrait.Cunning); Check(c.Traits.DepartureMood == 30, "cunning leaves at 30");

        // 기분 20 이하가 한 달(300초) 내내 이어져야 떠난다. 중간에 회복하면 처음부터.
        Fresh(a); a.PersonalState.AddMood("test", -100, 9999);
        Social(a, 299); Check(!a.IsDeparting && a.PersonalState.lowMoodSeconds >= 299, "no departure before a month");
        a.PersonalState.moodFactors.Clear(); Social(a, 1); Check(a.PersonalState.lowMoodSeconds == 0, "recovery resets low mood timer");
        a.PersonalState.AddMood("test", -100, 9999); Social(a, 299); Check(!a.IsDeparting, "timer restarted");
        Social(a, 1.5f); Check(a.IsDeparting && a.Social.departure == DepartureState.Fleeing, "a month of low mood departs alone");

        // 이벤트 기분은 한 달짜리 요인으로 남는다.
        Fresh(d); d.MoodEvent("포상", 8);
        var reward = d.PersonalState.moodFactors.Single(f => f.reason == "포상");
        Check(reward.value == 8 && Mathf.Approximately(reward.remaining, GameCalendar.SecondsPerMonth), "event mood lasts a month");
        d.MoodEvent("zero", 0); Check(!d.PersonalState.moodFactors.Exists(f => f.reason == "zero"), "zero event ignored");

        // 붕괴 '도주'는 실제로 떠난다.
        Fresh(b); b.StartMentalBreak(MentalBreak.Flee);
        Check(b.IsDeparting && b.Social.departure == DepartureState.Fleeing && b.PersonalState.mentalBreak == MentalBreak.None, "flee break leaves colony");

        // 포로 회유: 기분 낮고 피로 높을수록 쉽다. 탈출은 기분 높을수록 잦다(확률 식만 확인).
        var camp = new GameObject("Phase3 Prison").AddComponent<PrisonerCamp>();
        try
        {
            var happy = new Prisoner("happy", CommanderRank.Sergeant, new[] { UnitRole.Worker }, new CommanderTraits());
            var sad = new Prisoner("sad", CommanderRank.Sergeant, new[] { UnitRole.Worker }, new CommanderTraits());
            var tired = new Prisoner("tired", CommanderRank.Sergeant, new[] { UnitRole.Worker }, new CommanderTraits());
            sad.PersonalState.AddMood("포로 생활", -40, 9999); tired.PersonalState.sleep.fatigue = 100;
            Check(Mathf.Approximately(PrisonerCamp.MoodOf(happy), 60) && Mathf.Approximately(PrisonerCamp.MoodOf(sad), 20), "prisoner mood");
            Check(camp.PersuadeChance(sad) > camp.PersuadeChance(happy), "low mood easier to persuade");
            Check(camp.PersuadeChance(tired) > camp.PersuadeChance(happy), "high fatigue easier to persuade");
        }
        finally { Object.Destroy(camp.gameObject); }
        return "PASS " + checks + " Phase 3 checks";
    }
}
