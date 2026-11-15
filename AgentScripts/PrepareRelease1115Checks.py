from pathlib import Path
p = Path('AgentScripts/CampaignComplete1114Checks.cs')
s = p.read_text(encoding='utf-8-sig').replace('CampaignComplete1114', 'CampaignComplete1115').replace('campaign-complete-1114', 'campaign-complete-1115')
s = s.replace('while(SaveSystem.Busy && DateTime.UtcNow<end)', 'while((SaveSystem.Busy || !GameSession.Instance.GameStarted || CommanderRoster.Instance == null || CommanderRoster.Instance.Commanders.Count < 3) && DateTime.UtcNow<end)')
s = s.replace('if(SaveSystem.Busy) throw', 'if(SaveSystem.Busy || CommanderRoster.Instance.Commanders.Count < 3) throw')
s = s.replace('var destination=world.Sites.First(WorldMapManager.SiteRevealed);', '''var escort=CommanderRoster.Instance.Commanders[2];
        Check(party.AssignEscorts(new[]{escort}),"Home commander accepts escort assignment");
        Check(!escort.IsEmbarked && !escort.IsAwayFromHome,"Escort stays physically on home map");
        ExpeditionTransport.CancelCrewOrdersFor(escort);
        Check(party.Escorts.Contains(escort),"Direct combat orders preserve escort membership");
        var destination=world.Sites.First(WorldMapManager.SiteRevealed);''')
s = s.replace('party.Tick(party.TravelRemainingSeconds+.1f);', '''Check(party.EscortReturning,"Departure orders remaining escort to return");
        Check(SaveSystem.TrySave(false,2,out var escortError),"Save returning escort: "+escortError);
        party.Tick(party.TravelRemainingSeconds+.1f);''')
insert = '''
    public static string Main()
    {
        Directory.CreateDirectory(Dir); File.WriteAllText(Dir+"run.txt","RUNNING\\n");
        _ = RunAll(); return "Started; inspect work/campaign-complete-1115/run.txt";
    }
    static async Task RunAll()
    {
        try
        {
            await Prepare(); await Launch(); await Gate(); await RoundTrip(); await Combat();
            var boss=RocketCampaign.Instance.MothershipBoss;
            var encounter=boss.GetComponent<AntColony.Boss.MothershipEncounter>();
            Check(encounter!=null,"Dedicated final encounter attached");
            encounter.Tick(10);
            var warning=encounter.Capture();
            Check(warning.casting>=0,"Final boss starts a telegraphed attack");
            Check(AntColony.Boss.MothershipEncounter.Contains(warning,warning.origin),"Warning center matches damage region");
            Check(!AntColony.Boss.MothershipEncounter.Contains(warning,warning.origin+Vector3.right*100),"Outside warning is safe");
            Check(SaveSystem.TrySave(false,3,out var error),"Save active final-boss warning: "+error);
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false,3),out error),"Reload active warning: "+error);
            await Ready();
            var restored=RocketCampaign.Instance.MothershipBoss.GetComponent<AntColony.Boss.MothershipEncounter>().Capture();
            Check(restored.casting==warning.casting && Mathf.Abs(restored.remaining-warning.remaining)<.01f,"Warning timing survives scene reload");
            await Capture("05-final-encounter");
            await Victory(); File.AppendAllText(Dir+"run.txt","PASS all stages\\n");
        }
        catch(Exception e) { File.AppendAllText(Dir+"run.txt","FAIL "+e+"\\n"); }
        finally { Finish(); File.AppendAllText(Dir+"run.txt","RESTORED\\n"); }
    }
'''
s = s.replace('    public static string Finish()',insert+'    public static string Finish()')
Path('AgentScripts/CampaignComplete1115Checks.cs').write_text(s,encoding='utf-8')
