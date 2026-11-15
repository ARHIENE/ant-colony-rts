using AntColony.Core;
using AntColony.World;
using UnityEngine;
using UnityEngine.UI;

namespace AntColony.UI
{
    public sealed class MothershipHud : MonoBehaviour
    {
        RectTransform panel;
        Text status;
        Button gather, depart;
        void Create()
        {
            var canvas=new GameObject("MothershipHUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            canvas.transform.SetParent(transform,false);
            var surface=canvas.GetComponent<Canvas>(); surface.renderMode=RenderMode.ScreenSpaceOverlay; surface.sortingOrder=58;
            var scaler=canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1440,900); scaler.matchWidthOrHeight=.5f;
            panel=MenuTheme.Rect("본선 원정",canvas.transform);
            panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(.5f,1);
            panel.sizeDelta=new Vector2(800,110); panel.anchoredPosition=new Vector2(0,-18);
            MenuTheme.InsetScreen(panel.gameObject);
            status=MenuTheme.Text(panel,"",18,28); status.alignment=TextAnchor.MiddleCenter;
            Place(status.rectTransform,0,48,780,58);
            Add("본선 보기",-294,()=>RocketCampaign.Instance.ViewMothership());
            Add("지구 보기",-98,()=>RocketCampaign.Instance.ViewEarth());
            gather=Add("착륙선으로 집결",98,()=>{
                if (!RocketCampaign.Instance.RequestMothershipBoarding()) ToastManager.Show("이동 가능한 본선 장수가 없습니다.");
            });
            depart=Add("탑승 인원 귀환",294,()=>{
                if (!RocketCampaign.Instance.ReturnFromMothership()) ToastManager.Show("착륙선에 도착해 탑승한 장수가 필요합니다.");
            });
        }
        Button Add(string name,float x,System.Action action)
        {
            var button=MenuTheme.Button(panel,name,action);
            Place(button.GetComponent<RectTransform>(),x,8,188,36); return button;
        }
        static void Place(RectTransform rect,float x,float y,float w,float h)
        { rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,0); rect.anchoredPosition=new Vector2(x,y); rect.sizeDelta=new Vector2(w,h); }
        void Update()
        {
            var campaign=RocketCampaign.Instance;
            var visible=GameSession.Exists && GameSession.Instance.GameStarted && campaign?.Mothership?.reached==true && !CampaignResearch.Instance.Departed;
            if (panel==null) { if (!visible) return; Create(); }
            panel.gameObject.SetActive(visible); if (!visible) return;
            var s=campaign.Mothership;
            status.text=$"본선 지휘체 {Mathf.Max(0,s.bossHp):0} HP · 현지 {s.away.Count}명 · 귀환 탑승 {s.onboard.Count}명";
            status.text += "\n" + campaign.MothershipBoss?.GetComponent<AntColony.Boss.MothershipEncounter>()?.Status;
            gather.interactable=s.away.Count>s.onboard.Count;
            depart.interactable=s.onboard.Count>0;
        }
    }
}
