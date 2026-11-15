using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Boss;
using AntColony.Buildings;
using AntColony.Units;
using AntColony.World;
using UnityEngine;

namespace AntColony.Core
{
    public sealed partial class CampaignResearch
    {
        public bool SearchingSignals => state.signalSearching && Has(ScienceTechnology.SignalSurvey);
        public bool HasResearchTask => Active != null || ActiveResource != null || SearchingSignals;
        public bool RegionKnown(ExpeditionSite site) => site == null || !state.regionalDiscovery
            || state.knownRegions.Contains(site.Title) || site.Cleared || site.Visitor != null
            || site.GetComponent<BossStronghold>() is BossStronghold boss && boss.Id != BossId.A && boss.Revealed;
        public string DiscoveryStatus => !state.regionalDiscovery ? "기존 탐사 기록 유지"
            : state.knownRegions.Count == 0 ? $"주변 흔적 조사 {state.trailWork:0}/60 · 본거지 채집·건설 작업 중 발견"
            : !Has(ScienceTechnology.SignalSurvey) ? "현지 원정으로 다음 단서 발견 · 무선 통신 연구로 먼 신호 탐색"
            : state.signalSearching ? $"신호 탐색 {state.signalWork:0}/180 · 다른 연구가 없을 때 연구 담당이 진행" : "무선 신호 탐색 대기 · 과학·수송에서 시작";
        public void ToggleSignalSearch()
        {
            if (!Has(ScienceTechnology.SignalSurvey)) { AntColony.UI.ToastManager.Show("무선 통신 연구가 필요합니다."); return; }
            state.signalSearching = !state.signalSearching;
            if (state.signalSearching && Active == null && ActiveResource == null) state.paused = false;
        }
        private bool RevealRegion(ExpeditionSite site, string source)
        {
            if (site == null || state.knownRegions.Contains(site.Title)) return false;
            state.knownRegions.Add(site.Title);
            var message = source + " · " + AntColony.UI.SiteNames.Name(site) + " 발견. 지역 지도에서 원정 가능";
            AntColony.UI.ToastManager.Show(message);
            Encyclopedia.Discover("World", "trail:"+site.Title, AntColony.UI.SiteNames.Name(site), message);
            return true;
        }
        private IEnumerable<ExpeditionSite> UnknownRegions(Vector2 origin) => WorldMapManager.Instance.Sites
            .Where(s=>!RegionKnown(s) && s.Kind != ExpeditionSiteKind.Empty && s.GetComponent<BossStronghold>() == null)
            .OrderBy(s=>(s.MapPosition-origin).sqrMagnitude).ThenBy(s=>s.Title);
        private void TickDiscovery(float seconds)
        {
            var world=WorldMapManager.Instance;
            if (!state.regionalDiscovery || world == null || Save.SaveSystem.Busy || !(seconds>0) || float.IsInfinity(seconds)) return;
            if (state.knownRegions.Count == 0 && ColonyEvents.HomeCommanders().Any(c=>c.IsWorking || c.IsConstructing))
            {
                state.trailWork=Mathf.Min(60,state.trailWork+seconds);
                if(state.trailWork>=60) RevealRegion(UnknownRegions(new Vector2(.5f,.5f)).FirstOrDefault(s=>s.Kind==ExpeditionSiteKind.ResourceSite),"정착지 주변에서 생존 흔적을 찾았습니다");
            }
            foreach(var site in world.Sites.Where(s=>s.Visitor?.State==ExpeditionState.Deployed && !state.exploredRegions.Contains(s.Title)).ToArray())
            {
                state.exploredRegions.Add(site.Title);
                foreach(var next in UnknownRegions(site.MapPosition).Take(2).ToArray()) RevealRegion(next,"원정 현장의 지도 조각");
                RevealRegion(world.Sites.FirstOrDefault(s=>s.GetComponent<BossStronghold>()?.Id==BossId.A),"현장에서 확인한 첫 침공 거점 좌표");
            }
            if (!state.signalSearching || !Has(ScienceTechnology.SignalSurvey) || Active!=null || ActiveResource!=null || Departed) return;
            foreach(var lab in SceneObjects.All<ScienceLab>())
            {
                var rate=LabRate(lab); if(!(rate>0))continue;
                var work=Math.Min(seconds,Math.Max(0,180-state.signalWork)/rate);
                state.signalWork+=work*rate;lab.Target.GainExperience(CommanderActivity.Research,work);
            }
            if(state.signalWork<180)return;
            var found=UnknownRegions(new Vector2(.5f,.5f)).Reverse().Take(2).ToArray();
            foreach(var next in found)RevealRegion(next,"수신한 생존자 무선 신호");
            state.signalWork=0;state.signalSearching=false;
            if(found.Length==0)AntColony.UI.ToastManager.Show("새 일반 지역 신호가 없습니다. 미발견 필수 보스는 탐지 장치가 필요합니다.");
        }
        private static bool ValidDiscovery(State s) => s.knownRegions!=null && s.exploredRegions!=null
            && s.knownRegions.Count<=100 && s.exploredRegions.Count<=100
            && s.knownRegions.All(t=>!string.IsNullOrEmpty(t)) && s.exploredRegions.All(t=>!string.IsNullOrEmpty(t))
            && s.trailWork>=0 && s.trailWork<=60 && s.signalWork>=0 && s.signalWork<=180;
    }
}
