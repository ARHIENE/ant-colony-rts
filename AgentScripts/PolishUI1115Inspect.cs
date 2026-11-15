using System.Linq;using UnityEngine;using UnityEngine.UI;using AntColony.Core;using AntColony.Buildings;using AntColony.World;
public static class PolishUI1115Inspect
{
 public static string Main()=>string.Join("\n",Object.FindObjectsByType<Text>(FindObjectsInactive.Exclude,FindObjectsSortMode.None)
  .Where(t=>t.text.Contains("목표")||t.text.Contains("시대")||t.name.Contains("ColonySummary"))
  .Select(t=>$"{t.name} parent={t.transform.parent.name} canvas={t.canvas.name} text={t.text} size={t.rectTransform.rect.size} pos={t.rectTransform.position} font={t.fontSize} spacing={t.lineSpacing}"))
  +"\nSEEDS "+string.Join(";",ResourceNode.Available.Where(n=>n.ResourceType==AntColony.Data.ResourceType.PlantingMaterial).Select(n=>$"{n.name} count={n.AmountRemaining} pos={n.transform.position} forbidden={n.GatheringForbidden}"))
  +"\nCROPS "+string.Join(";",SceneObjects.All<CropGrowthVisual>().Select(v=>$"{v.name}:{v.Stage}"));
}
