using System.Linq;using AntColony.Core;using AntColony.Buildings;using AntColony.Units;
public static class IndustryStatus1115
{
 public static string Main()=>CampaignResearch.Instance.DiscoveryStatus+"\n"+string.Join("\n",SceneObjects.All<ScienceLab>().Select(l=>l.name+": "+CampaignResearch.Instance.LabWaitReason(l)+" rate="+CampaignResearch.Instance.LabRate(l)))+"\n"+string.Join("\n",CommanderRoster.Instance.Commanders.Select(c=>c.name+" position="+c.Position+" assignment="+c.ScienceAssignment));
}
