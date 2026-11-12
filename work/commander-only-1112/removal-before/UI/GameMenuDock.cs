using UnityEngine;
using UnityEngine.UI;
namespace AntColony.UI
{
    [DefaultExecutionOrder(800)]
    public sealed class GameMenuDock : MonoBehaviour
    {
        private static readonly string[] Keys={"V6 Build","Work Schedule [T]","V6 Roster","Science [K]","V6 Logistics","Prototype Population","WorldMapToggle","V6 Production"};
        private static readonly string[] Labels={"건설","작업표","장수","연구","운영","주거","지도","자원"};
        void LateUpdate()
        {
            var r=(RectTransform)transform;float w=((RectTransform)r.parent).rect.width;bool small=w<=760,full=w>1000;
            float total=small?w-222:full?656:484;
            r.anchorMin=r.anchorMax=r.pivot=new Vector2(1,0);r.anchoredPosition=new Vector2(small?-35:full?-56:-44,0);r.sizeDelta=new Vector2(total,35);
            GetComponent<HorizontalLayoutGroup>().enabled=false;
            float x=0;
            for(int i=0;i<Keys.Length;i++)
            {
                var child=transform.Find(Keys[i]);if(child==null)continue;child.gameObject.SetActive(true);child.SetSiblingIndex(i);
                if(!small&&(i==3||i==6))x+=full?16:4;
                float cell=small?total/8:i==0?(full?92:70):(full?76:58);
                var rect=(RectTransform)child;rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.zero;rect.anchoredPosition=new Vector2(x,0);rect.sizeDelta=new Vector2(cell,35);x+=cell;
                var text=child.GetComponentInChildren<Text>();text.text=Labels[i];text.font=MenuTheme.Font;text.fontStyle=FontStyle.Normal;text.fontSize=small?11:13;text.resizeTextForBestFit=false;text.color=MenuTheme.Hex(i==0?0xf2ece1:0xc9c3b8);MenuTheme.Stretch(text.rectTransform);
                text.horizontalOverflow=HorizontalWrapMode.Overflow;text.verticalOverflow=VerticalWrapMode.Truncate;
                var button=child.GetComponent<Button>();var colors=button.colors;colors.normalColor=Color.clear;MenuTheme.SetColorsInstant(button,colors);
                var icon=child.Find("Navigation Icon");
                if(i==0&&!small){if(icon==null){icon=MenuTheme.Rect("Navigation Icon",child);var graphic=icon.gameObject.AddComponent<RawImage>();graphic.texture=Resources.Load<Texture2D>("UI/HudIcons/Hammer");graphic.raycastTarget=false;graphic.color=MenuTheme.Hex(0xf2ece1);}icon.gameObject.SetActive(true);MenuLayout.Place((RectTransform)icon,22,10,16,16);text.rectTransform.offsetMin=new Vector2(18,0);}
                else icon?.gameObject.SetActive(false);
            }
            foreach(var key in new[]{"Prototype Home","Menu [Esc]","Work Designations","V6 Colony"})transform.Find(key)?.gameObject.SetActive(false);
            var image=GetComponent<Image>()??gameObject.AddComponent<Image>();image.color=Color.clear;
            if(r.parent.Find("Prototype Settings") is RectTransform gear){gear.anchorMin=gear.anchorMax=gear.pivot=new Vector2(1,0);gear.anchoredPosition=new Vector2(small?-6:-12,3);gear.sizeDelta=new Vector2(small?26:32,29);gear.GetComponentInChildren<Text>().text="";HudChrome.CenterIcon(HudChrome.Icon(gear,"Settings Icon","Settings",MenuTheme.Hex(0xc9c3b8)),0,0,16);var colors=gear.GetComponent<Button>().colors;colors.normalColor=Color.clear;MenuTheme.SetColorsInstant(gear.GetComponent<Button>(),colors);}
        }
    }
}
