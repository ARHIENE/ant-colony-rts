using System;
using System.Linq;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Save;
using AntColony.Units;
using UnityEngine;
using Object = UnityEngine.Object;
using Res = AntColony.Data.ResourceType;

// G06(2026-10-24): 표본 반입 → 자원 연구(연구소·담당 장수) → 본격 채굴. 과학 연구와 동시 진행 불가, 상태 저장 검증.
public static class ImplG06Checks
{
    static int count;
    static void Check(bool ok, string label) { System.IO.File.WriteAllText("Temp/g06-progress.txt", count + " " + label); if (!ok) throw new Exception("G06 FAIL " + count + ": " + label); count++; }

    public static async Task<string> Main()
    {
        count = 0; Check(Application.isPlaying, "Play mode");
        for (var until = DateTime.UtcNow.AddSeconds(90); SaveSystem.Busy && DateTime.UtcNow < until;) await Task.Delay(50);
        Check(!SaveSystem.Busy, "scene ready within 90s");
        var team = new[] { StartingCandidate.Roll(0) }; team[0].traits.values.Clear();
        CrashLandingStart.Begin(new NewGameOptions { startingCount = 1, seed = 261024, mapSize = MapSize.Small, biome = MapBiome.Forest, startingTeam = team });
        for (var until = DateTime.UtcNow.AddSeconds(90); SaveSystem.Busy && DateTime.UtcNow < until;) await Task.Delay(50);
        Check(!SaveSystem.Busy, "new game ready within 90s");
        AntColony.UI.GameMenuController.Instance.Resume();
        var savedScale = Time.timeScale; Time.timeScale = 0;

        var research = CampaignResearch.Instance; var saved = research.CaptureState();
        var labObject = new GameObject("G06 lab"); var lab = labObject.AddComponent<ScienceLab>();
        try
        {
            research.RestoreState(new CampaignResearch.State());
            const Res ore = Res.IronOre;
            Check(!research.TryStartResourceResearch(ore), "no sample, no research");
            Check(!research.CanMine(ore, 2, 1), "unresearched resource cannot be mined");
            var rm = ResourceManager.Instance;
            Check(rm.AddTo(rm.BaseBin, ore, 0) == 0 && !research.HasSample(ore), "empty deposit is not a sample");
            rm.Add(ore, 1, ResourceReason.Production);
            rm.Add(ore, 1, ResourceReason.Refund);
            Check(!research.HasSample(ore), "production and refund are not sample deliveries");
            rm.Add(ore, 1, ResourceReason.Expedition);
            Check(research.HasSample(ore) && research.PendingSamples.Contains(ore), "delivered sample is pending");
            Check(research.TryStartResourceResearch(ore) && research.ActiveResource == ore, "sample research starts");
            Check(!research.TryStartResourceResearch(ore), "no duplicate start");
            Check(research.BlockReason(ScienceTechnology.Fishing) != "", "science research blocked while sample research runs");
            var copy = research.CaptureState();
            Check(CampaignResearch.Validate(JsonUtility.FromJson<CampaignResearch.State>(JsonUtility.ToJson(copy)), out _), "active sample research saves");
            copy.resourceActive = (int)Res.CopperOre;
            Check(!CampaignResearch.Validate(copy, out _), "reject sample research without a sample");

            var c = CommanderRoster.Instance.Commanders.First(x => x.CivilianWorkReady && x.CanReceiveOrders);
            c.SetJobEnabled(CommanderJobs.Research, true); c.CommandStop();
            lab.transform.position = c.Position;
            Check(lab.TryAssign(c), "assign researcher");
            c.TickDuty(.01f);
            Check(lab.Target == c && c.ScienceAssignment == lab, "duty tick retains sample researcher");
            labObject.SetActive(false);
            research.Tick(1); Check(research.ResourceProgress == 0, "inactive lab pauses sample research");
            labObject.SetActive(true);
            Check(lab.TryAssign(c), "reassign sample researcher");
            Time.timeScale = 1; var frame = Time.frameCount;
            for (var until = DateTime.UtcNow.AddSeconds(5); Time.frameCount < frame + 2 && DateTime.UtcNow < until;) await Task.Delay(25);
            Time.timeScale = 0;
            Check(Time.frameCount >= frame + 2 && lab.Target == c && c.ScienceAssignment == lab, "sample assignment survives real frames");
            research.Tick(1); Check(research.ResourceProgress > 0, "researcher adds sample progress");
            for (var i = 0; i < 2000 && research.ActiveResource != null; i++) research.Tick(5);
            Check(research.ActiveResource == null && research.ResourceResearched(ore), "sample research completes");
            Check(research.CanMine(ore, 1, 1) && !research.CanMine(ore, 0, 1), "researched resource mines with the right tool tier");
            Check(!research.PendingSamples.Contains(ore), "completed sample leaves the pending list");
            rm.AddTo(rm.BaseBin, Res.CopperOre, 1, reason: ResourceReason.Trade);
            Check(research.HasSample(Res.CopperOre), "physical bin delivery records a sample");
            return "PASS " + count + " ImplG06Checks";
        }
        finally { Time.timeScale = savedScale; lab.ReleaseResearcher(); Object.Destroy(labObject); research.RestoreState(saved); }
    }
}
