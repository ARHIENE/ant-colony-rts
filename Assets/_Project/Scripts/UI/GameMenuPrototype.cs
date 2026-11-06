using System;
using System.Linq;
using AntColony.Core;
using AntColony.Data;
using UnityEngine;
using UnityEngine.UI;
using L=AntColony.UI.MenuLayout;
namespace AntColony.UI
{
    public sealed partial class GameMenuController
    {
        private string resourceFilter="";
        private void InstallPrototypeNavigation(RectTransform bar,Transform canvas)
        {
            ToolbarButton(bar,"Prototype Home","⌂",Resume,"본거지 화면");
            ToolbarButton(bar,"Prototype Population","주거",Population,"주거·시민·이주");
            void Bind(string key,Action action){var b=bar.Find(key).GetComponent<Button>();b.onClick.RemoveAllListeners();b.onClick.AddListener(()=>action());}
            Bind("V6 Roster",Roster);Bind("V6 Production",PrototypeResources);Bind("V6 Logistics",PrototypeOperations);
            var gear=L.Button(canvas,"Prototype Settings","⚙",0,0,32,32,Settings,"설정");
            var rt=(RectTransform)gear.transform;rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(1,0);rt.anchoredPosition=new Vector2(-8,2);
        }
        public void PrototypeResources()
        {
            var w=PrototypeWindow("자원","실제 창고 재고 · HUD 고정 항목은 다음 실행에도 유지",PrototypeResources);
            float width=w.sizeDelta.x, height=w.sizeDelta.y;
            var search=MenuTheme.Input(w,resourceFilter);search.name="Resource Search";search.contentType=InputField.ContentType.Standard;search.characterLimit=30;
            L.Place((RectTransform)search.transform,20,82,Mathf.Min(360,width-220),34);search.onEndEdit.AddListener(v=>{resourceFilter=v;PrototypeResources();});
            L.Label(w,"이름으로 찾기",12,width-180,82,160,34,MenuTheme.Muted);
            var list=L.List(w,20,128,width-40,height-194);
            foreach(ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                if(!type.DisplayName().Contains(resourceFilter))continue;var t=type;
                var row=L.Cell(list,"Stock "+t,36);
                PrototypeResourceIcon.Create(row,t,8,7);
                L.Label(row,t.DisplayName(),14,44,0,width-340,36);
                var amount=L.Label(row,(ResourceManager.Instance?.GetAmount(t)??0).ToString("N0"),14,width-310,0,96,36,align:TextAnchor.MiddleRight);
                refreshDutyScreen+=()=>amount.text=(ResourceManager.Instance?.GetAmount(t)??0).ToString("N0");
                L.Button(row,"Pin "+t,PrototypeResourceHud.Pinned.Contains(t)?"고정 해제":"HUD에 고정",width-194,2,130,30,()=>{PrototypeResourceHud.TogglePin(t);PrototypeResources();});
            }
            L.Button(w,"Storage Details","창고 상세",20,height-46,130,30,()=>Management("물류·식품","창고"));
            L.Button(w,"Production Details","생산 상세",160,height-46,130,30,()=>Management("생산·자원","생산"));
        }
        public void PrototypeOperations()
        {
            var w=PrototypeWindow("물류·생산","시설 · 생산 주문 · 장수 관리",PrototypeOperations);
            var choices=new (string title,string note,Action action)[]{
                ("창고","허용 품목 · 운반 우선순위",()=>Management("물류·식품","창고")),
                ("생산","시설별 주문 · 미완성품",()=>Management("생산·자원","생산")),
                ("장비·도구","보관 장비 · 제작",()=>Management("생산·자원","장비·도구")),
                ("수리","담당 장수 · 재료 · 내구도",()=>Management("생산·자원","수리")),
                ("식단·음주","공통 식단 · 장수별 정책",()=>ShowDiet()),
                ("건설·작업","청사진 · 작업 지정",()=>Management("물류·식품","건설·작업")),
                ("치료","부상 · 수술 · 의료",()=>Management("의료·관리","의료")),
                ("목장","동물 · 위생 · 돌봄",()=>Management("의료·관리","동물 위생")),
                ("퇴비·비료","원료 · 생산 · 농사",()=>Management("물류·식품","퇴비·비료")),
                ("묘비·추모","사망 · 추모 기록",()=>ShowMemorial()),
                ("포로","구금 · 귀환",()=>Management("의료·관리","포로")),
                ("기록","정착지 사건",()=>Management("의료·관리","기록"))};
            var list=L.List(w,20,84,w.sizeDelta.x-40,w.sizeDelta.y-104,8);
            int columns=w.sizeDelta.x>=1000?3:2;
            float cellWidth=(w.sizeDelta.x-64)/columns;
            for(int i=0;i<choices.Length;i+=columns)
            {
                var row=L.Cell(list,"Operations Row "+i,88);
                for(int j=0;j<columns&&i+j<choices.Length;j++)
                {
                    var c=choices[i+j];
                    L.Button(row,"Operations "+c.title,c.title+"\n<size=12>"+c.note+"</size>",j*(cellWidth+8),0,cellWidth,84,c.action,align:TextAnchor.MiddleLeft,size:16);
                }
            }
        }
    }
}
