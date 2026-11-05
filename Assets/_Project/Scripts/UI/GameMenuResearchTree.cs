using System;
using System.Linq;
using System.Collections.Generic;
using AntColony.Core;
using UnityEngine;
using UnityEngine.UI;
using L=AntColony.UI.MenuLayout;
namespace AntColony.UI
{
    public sealed partial class GameMenuController
    {
        private Vector2 treeScroll=new Vector2(0,1);
        public void Science()
        {
            var research=CampaignResearch.Instance;
            if(research==null){ScienceDetails();return;}
            var window=V5Window("과학 · 연구","선행 연구 → 해금 시설 · 실제 게임 연구 데이터",680);
            L.Button(window,"Research Assignments","연구소 배정 · 표본",20,630,180,30,ScienceDetails);
            L.Button(window,"Rocket Facilities","로켓 · 과학 시설",210,630,160,30,ScienceFacilities);
            var estimate=L.Label(window,research.TimeEstimate,12,390,630,610,30,MenuTheme.Muted);
            refreshDutyScreen=()=>estimate.text=research.TimeEstimate;
            var defs=CampaignResearch.Technologies;
            if(!researchPick.HasValue)researchPick=defs[0].Technology;
            var levels=new Dictionary<ScienceTechnology,int>();
            int Level(ScienceTechnology t,HashSet<ScienceTechnology> visiting)
            {
                if(levels.TryGetValue(t,out var old))return old;if(!visiting.Add(t))return 0;
                var d=defs[(int)t];int level=d.Prerequisites.Length==0?0:1+d.Prerequisites.Max(p=>Level(p,visiting));
                visiting.Remove(t);levels[t]=level;return level;
            }
            foreach(var d in defs)Level(d.Technology,new HashSet<ScienceTechnology>());
            var rows=new Dictionary<int,int>();var positions=new Dictionary<ScienceTechnology,Vector2>();
            foreach(var d in defs.OrderBy(d=>d.Tier).ThenBy(d=>(int)d.Technology))
            {int level=levels[d.Technology];rows.TryGetValue(level,out int row);positions[d.Technology]=new Vector2(22+level*242,34+row*110);rows[level]=row+1;}
            var area=L.Box(window,"ResearchTree",16,72,740,530,MenuTheme.Well);
            var scroll=area.gameObject.AddComponent<ScrollRect>();scroll.horizontal=scroll.vertical=true;scroll.scrollSensitivity=35;scroll.movementType=ScrollRect.MovementType.Clamped;
            L.Button(window,"Research Left","◀",608,32,38,28,()=>{scroll.horizontalNormalizedPosition=Mathf.Clamp01(scroll.horizontalNormalizedPosition-242f/Mathf.Max(1,scroll.content.rect.width-scroll.viewport.rect.width));treeScroll=scroll.normalizedPosition;});
            L.Button(window,"Research Right","▶",652,32,38,28,()=>{scroll.horizontalNormalizedPosition=Mathf.Clamp01(scroll.horizontalNormalizedPosition+242f/Mathf.Max(1,scroll.content.rect.width-scroll.viewport.rect.width));treeScroll=scroll.normalizedPosition;});
            var view=MenuTheme.Rect("Viewport",area);MenuTheme.Stretch(view);view.gameObject.AddComponent<RectMask2D>();
            var graph=MenuTheme.Rect("ResearchGraph",view);graph.anchorMin=graph.anchorMax=graph.pivot=new Vector2(0,1);
            graph.sizeDelta=new Vector2((levels.Values.Max()+1)*242+30,rows.Values.Max()*110+50);scroll.viewport=view;scroll.content=graph;
            foreach(var d in defs)foreach(var pre in d.Prerequisites)
            {
                var from=positions[pre]+new Vector2(212,43);var to=positions[d.Technology]+new Vector2(0,43);
                var middle=(from.x+to.x)*.5f;var color=research.Has(pre)?MenuTheme.Accent:MenuTheme.Line;
                TreeSegment(graph,from,new Vector2(middle,from.y),color);TreeSegment(graph,new Vector2(middle,from.y),new Vector2(middle,to.y),color);TreeSegment(graph,new Vector2(middle,to.y),to,color);
            }
            foreach(var d in defs)
            {
                var def=d;var pos=positions[d.Technology];
                var button=L.Button(graph,"Tree "+d.Technology,"",pos.x,pos.y,212,86,()=>{treeScroll=scroll.normalizedPosition;researchPick=def.Technology;Science();},primary:researchPick==d.Technology);
                var title=L.Label(button.transform,d.Name,14,10,5,192,28,bold:true);
                var state=L.Label(button.transform,"",12,10,34,192,20,MenuTheme.Muted);
                L.Label(button.transform,ResearchEffect(d.Technology),12,10,56,192,24,MenuTheme.Muted).verticalOverflow=VerticalWrapMode.Truncate;
                refreshDutyScreen+=()=>{state.text=ResearchState(research,def)+" · "+CampaignResearch.EraNames[def.Tier-1];title.color=research.Has(def.Technology)?MenuTheme.Accent:MenuTheme.TextColor;};
            }
            Canvas.ForceUpdateCanvases();scroll.normalizedPosition=treeScroll;
            var selected=defs[(int)researchPick.Value];content=V5Section(window,selected.Name,768,72,316,530);
            MenuTheme.Text(content,"해금",15,28);MenuTheme.Text(content,ResearchEffect(selected.Technology),14,86);
            MenuTheme.Text(content,"선행 연구",15,28);
            foreach(var pre in selected.Prerequisites){var t=pre;MenuTheme.Button(content,(research.Has(t)?"✓ ":"· ")+defs[(int)t].Name,()=>{researchPick=t;Science();});}
            var details=MenuTheme.Text(content,"",13,100);
            var start=MenuTheme.Button(content,"연구 지시",()=>{if(research.Active==selected)research.SetPaused(false);else if(!research.TryStart(selected.Technology))ToastManager.Show("선행 조건과 비용을 확인하세요.");Science();});
            var stop=MenuTheme.Button(content,"일시정지",()=>{research.SetPaused(true);Science();});
            var reason=MenuTheme.Text(content,"",12,75);reason.color=MenuTheme.Warning;
            refreshDutyScreen+=()=>{
                details.text=$"식량 {selected.Food} · 흙 {selected.Soil} · 특수 {selected.Special}\n연구량 {selected.Work:0} · 연구소 {selected.Tier}등급\n"+ResearchState(research,selected);
                string block=research.BlockReason(selected.Technology);if(block=="")block=research.ResearchCostShortage(selected.Technology);
                start.interactable=!research.Has(selected.Technology)&&(research.Active==selected?research.Paused:block=="");stop.interactable=research.Active==selected&&!research.Paused;
                reason.text=research.Has(selected.Technology)?"완료한 연구입니다.":block;
            };refreshDutyScreen();
        }
        private static void TreeSegment(RectTransform parent,Vector2 a,Vector2 b,Color color)
        {
            var line=L.Box(parent,"Prerequisite",Mathf.Min(a.x,b.x),Mathf.Min(a.y,b.y),Mathf.Max(2,Mathf.Abs(a.x-b.x)),Mathf.Max(2,Mathf.Abs(a.y-b.y)),color);line.GetComponent<Image>().raycastTarget=false;
        }
    }
}
