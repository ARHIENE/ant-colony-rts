using System;
using System.Linq;
using System.Reflection;
using AntColony.Core;
using AntColony.Units;
using AntColony.UI;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
public static class TraitChipChecks
{
    static int n;
    static void Check(bool ok,string label){if(!ok)throw new Exception("FAIL "+label);n++;}
    public static string Main()
    {
        n=0;var menu=GameMenuController.Instance;var flags=BindingFlags.NonPublic|BindingFlags.Instance;
        var screen=menu.GetType().GetMethod("NewGameScreen",flags);screen.Invoke(menu,null);
        var options=(NewGameOptions)menu.GetType().GetField("options",flags).GetValue(menu);
        var values=options.startingTeam[0].traits.values;var original=values.ToArray();
        try
        {
            values.Clear();
            foreach(TraitGrade grade in Enum.GetValues(typeof(TraitGrade)))
                values.Add(((CommanderTrait[])Enum.GetValues(typeof(CommanderTrait))).First(t=>TraitRules.Grade(t)==grade));
            screen.Invoke(menu,null);Canvas.ForceUpdateCanvases();
            var chips=menu.GetComponentsInChildren<Image>().Where(i=>i.name.StartsWith("Trait ")).ToArray();
            foreach(var t in values)
            {
                var chip=chips.First(i=>i.name=="Trait "+t);var text=chip.GetComponentInChildren<Text>();
                Check(chip.sprite!=null && !chip.raycastTarget,"grade sprite without input interception "+t);
                Check(text.text==CommanderTraits.DisplayName(t) && text.resizeTextForBestFit,"trait text preserved "+t);
                Check(chip.rectTransform.rect.height==22 && chip.rectTransform.rect.width>100,"candidate chip layout "+t);
                var texture=chip.sprite.texture;var edge=texture.GetPixel(0,texture.height/2);var color=TraitChip.GradeColor(TraitRules.Grade(t));
                Check(Vector3.Distance(new Vector3(edge.r,edge.g,edge.b),new Vector3(color.r,color.g,color.b))<.2f,"grade border color "+t);
            }
            return "PASS "+n+" four-grade candidate trait UI checks";
        }
        finally{values.Clear();values.AddRange(original);screen.Invoke(menu,null);}
    }
    public static string Roster()
    {
        n=0;var menu=GameMenuController.Instance;var c=CommanderRoster.Instance.Commanders.First(x=>x.IsColonyMember);
        var old=c.Traits.values.ToArray();
        try
        {
            c.Traits.values.Clear();c.Traits.values.AddRange(((CommanderTrait[])Enum.GetValues(typeof(CommanderTrait))).GroupBy(TraitRules.Grade).Select(g=>g.First()));
            menu.Details(c);Canvas.ForceUpdateCanvases();
            var head=menu.GetComponentsInChildren<RectTransform>().First(t=>t.name=="DetailHead");
            var chips=head.GetComponentsInChildren<Image>().Where(i=>i.name.StartsWith("Trait ")).ToArray();Check(chips.Length==4,"all four traits visible in commander detail");
            foreach(var chip in chips){Check(chip.rectTransform.rect.height==18,"roster chip row height");Check(Mathf.Abs(chip.rectTransform.anchoredPosition.y)-18<70,"trait row stays above stats");}
            return "PASS "+n+" commander trait layout checks";
        }
        finally{c.Traits.values.Clear();c.Traits.values.AddRange(old);menu.Resume();}
    }
}
