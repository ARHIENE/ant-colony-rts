using UnityEngine;
using UnityEngine.UI;
namespace AntColony.UI
{
    [DefaultExecutionOrder(800)]
    public sealed class GameMenuDock : MonoBehaviour
    {
        private static readonly string[] Keys = { "Prototype Home", "Work Schedule [T]", "V6 Roster", "V6 Build", "Science [K]", "V6 Logistics", "WorldMapToggle", "Prototype Population", "V6 Production" };
        private static readonly string[] Labels = { "⌂", "작업", "장수", "건설", "연구", "물류·생산", "월드맵", "주거", "자원" };
        private void LateUpdate()
        {
            var rect = (RectTransform)transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f,0);
            rect.anchoredPosition = new Vector2(0,12);
            var layout = GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 4; layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            layout.childControlWidth = layout.childControlHeight = true;
            float width = 16;
            for(int i=0;i<Keys.Length;i++)
            {
                var child=transform.Find(Keys[i]); if(child==null)continue;
                child.gameObject.SetActive(true);child.SetSiblingIndex(i);
                var text=child.GetComponentInChildren<Text>();text.text=Labels[i];text.fontSize=13;
                var element=child.GetComponent<LayoutElement>();element.preferredHeight=34;
                element.preferredWidth=text.preferredWidth+20;element.minWidth=element.preferredWidth;
                width+=element.preferredWidth+4;
            }
            foreach(var key in new[]{"Menu [Esc]","Work Designations","V6 Colony"})transform.Find(key)?.gameObject.SetActive(false);
            layout.padding=new RectOffset(8,8,5,5);rect.sizeDelta=new Vector2(width,44);
            var image=GetComponent<Image>()??gameObject.AddComponent<Image>();image.color=MenuTheme.Background;
        }
    }
}
