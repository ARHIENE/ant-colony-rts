using System;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
namespace AntColony.UI
{
    public sealed partial class GameMenuController
    {
        public void ShowAlcohol()
        {
            var window=V5Window("음주 정책", "성인 장수가 창고까지 이동해 한 잔씩 마십니다. 일반 음주는 전투·소집 중 중단합니다.");
            var left=V5Section(window,"술 재고와 주의사항",12,104,360,544);
            var right=V5Section(window,"장수별 정책·현재 상태",388,104,700,544);
            foreach(ResourceType type in Enum.GetValues(typeof(ResourceType)))
                if(ProcessingResources.Alcohol(type))MenuTheme.Text(left,type.DisplayName()+"  "+(ResourceManager.Instance?.GetAmount(type)??0),14,30);
            MenuTheme.Text(left,"음주는 여가를 채우고 잠시 기분을 높입니다. 취기·숙취·금단은 이동·작업·전투 능력을 낮춥니다. 만취하면 움직일 수 없습니다.\n\n의존 장수는 금지·간격·소집보다 음주 충동을 우선할 수 있습니다. 어린 장수는 항상 금지합니다.",13,220);
            foreach(var c in SortedCommanders())
            {
                var a=c.Alcohol;
                MenuTheme.Text(right,c.CommanderName+" · "+c.AlcoholStatus,14,32);
                if(c.IsChild){MenuTheme.Text(right,"어린 장수 · 음주 금지",12,26);continue;}
                MenuTheme.Button(right,a.allowed?"음주 허용 → 금지":"음주 금지 → 허용",()=>{a.allowed=!a.allowed;ShowAlcohol();});
                MenuTheme.Button(right,$"간격: {a.intervalDays}일 → 변경",()=>{a.intervalDays=a.intervalDays==7?1:a.intervalDays+1;ShowAlcohol();});
                var block=c.DrinkBlock();
                var button=MenuTheme.Button(right,block==""?"한 잔 마시기":"대기: "+block,()=>{if(!c.RequestDrink())ToastManager.Show("접근 가능한 창고에 술이 없습니다.");ShowAlcohol();});
                button.interactable=block==""&&!c.SeekingDrink;
            }
            V5Fit(window);
        }
    }
}
