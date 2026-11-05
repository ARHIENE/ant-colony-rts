using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Core;
using AntColony.Data;
using UnityEngine;
using UnityEngine.UI;
namespace AntColony.UI
{
    // Presentation preferences only: inventory always comes from ResourceManager.
    public sealed class PrototypeResourceHud : MonoBehaviour
    {
        public static readonly HashSet<ResourceType> Pinned = new HashSet<ResourceType>{ResourceType.Food,ResourceType.SimpleMeal,ResourceType.Wood,ResourceType.Stone};
        private RectTransform root; private readonly Dictionary<ResourceType,Text> labels=new Dictionary<ResourceType,Text>();
        private float next;
        private readonly HashSet<ResourceType> displayed = new HashSet<ResourceType>();
        public static string Symbol(ResourceType t)=>t.IsFood()?"●":t==ResourceType.Wood?"▰":t==ResourceType.Stone?"◆":t==ResourceType.Water?"◉":"■";
        public static Color Tint(ResourceType t)=>t.IsFood()?MenuTheme.Hex(0xd3bb7b):t==ResourceType.Wood?MenuTheme.Hex(0xb08a5a):t==ResourceType.Stone?MenuTheme.Hex(0xb7b6aa):MenuTheme.Hex(0xb9ac93);
        void Start(){root=MenuTheme.Rect("PinnedResources",transform);root.anchorMin=root.anchorMax=root.pivot=new Vector2(0,1);root.anchoredPosition=new Vector2(12,-12);Rebuild();}
        void Rebuild()
        {
            foreach(Transform child in root){child.gameObject.SetActive(false);Destroy(child.gameObject);}labels.Clear();
            int i=0; foreach(var type in Pinned.OrderBy(t=>(int)t))
            {
                var t=type;var b=MenuLayout.Button(root,"Pinned "+t,"",(i%3)*69,(i/3)*27,65,25,()=>GameMenuController.Instance?.PrototypeResources(),t.DisplayName());
                PrototypeResourceIcon.Create(b.transform,t,2,1);
                var n=MenuLayout.Label(b.transform,"—",13,22,0,39,25,align:TextAnchor.MiddleRight);labels[t]=n;i++;
            }
            root.sizeDelta=new Vector2(207,Mathf.Ceil(i/3f)*27);displayed.Clear();displayed.UnionWith(Pinned);
        }
        void Update()
        {
            if(root==null)return;root.gameObject.SetActive(GameSession.Exists&&GameSession.Instance.GameStarted&&!GameMenuController.MenuOpen&&!BuildScreen.IsOpen);
            if(Time.unscaledTime<next)return;next=Time.unscaledTime+.3f;
            if(!displayed.SetEquals(Pinned))Rebuild();
            foreach(var item in labels){int n=ResourceManager.Instance?.GetAmount(item.Key)??0;item.Value.text=n>=10000?(n/1000f).ToString("0.#")+"k":n.ToString();item.Value.transform.parent.GetComponent<MenuTooltip>().Message=item.Key.DisplayName()+" · "+n+"\n창고에서 사용 가능한 재고 · 클릭: 자원 관리";}
        }
    }
}
