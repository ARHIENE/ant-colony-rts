using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.UI;
using UnityEngine;
using UnityEngine.UI;
using B = AntColony.Data.BuildingKind;
public static class FoodIndustry1028UiChecks
{
    public static async Task<string> Main()
    {
        int count = 0; void Check(bool ok, string label) { if (!ok) throw new Exception("Food UI " + label); count++; }
        var menu = GameMenuController.Instance;
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        GameObject Create(B kind)
        {
            var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { kind, AntColony.Data.UnitRole.Worker });
            var go = UnityEngine.Object.Instantiate(template, CommanderRoster.Instance.Commanders[0].Position + Vector3.right * 20, Quaternion.identity); go.SetActive(true); return go;
        }
        var barrel = SceneObjects.All<Processor>().FirstOrDefault(p => p.Brewing) ?? Create(B.BrewingBarrel).GetComponent<Processor>();
        var still = SceneObjects.All<Processor>().FirstOrDefault(p => p.Data.kind == B.Still) ?? Create(B.Still).GetComponent<Processor>();
        var farm = FarmPlot.All.FirstOrDefault(); if (farm == null) { var go = Create(B.Farm); farm = go.GetComponent<FarmPlot>() ?? go.AddComponent<FarmPlot>(); farm.Configure(FarmCrop.Fungus, false); }
        var capture = typeof(Processor).GetMethod("CaptureState", flags); var restore = typeof(Processor).GetMethod("RestoreState", flags);
        var old = (Processor.State)capture.Invoke(barrel, null);
        try
        {
            menu.ShowFarm(farm); await Task.Delay(80); Canvas.ForceUpdateCanvases();
            var crops = menu.GetComponentsInChildren<RectTransform>().First(t => t.name == "Crops");
            var buttons = crops.GetComponentsInChildren<Button>(); Check(buttons.Length == 8, "all crop choices present");
            var bounds = crops.rect; bounds.xMin -= .5f; bounds.yMin -= .5f; bounds.xMax += .5f; bounds.yMax += .5f;
            foreach (var button in buttons)
            {
                var corners = new Vector3[4]; ((RectTransform)button.transform).GetWorldCorners(corners);
                Check(corners.All(c => bounds.Contains(crops.InverseTransformPoint(c))), "crop button inside wrapped rows " + button.name);
            }
            var state = (Processor.State)capture.Invoke(barrel, null); state.current = 0; state.progress = 0; state.fermenting = true; state.fermented = false; state.fermentation = FoodProcessingRecipes.FermentSeconds / 2;
            restore.Invoke(barrel, new object[] { state }); menu.ShowProcessor(barrel);
            Button Cancel() => menu.GetComponentsInChildren<Button>().First(b => b.GetComponentInChildren<Text>()?.text.Contains("취소") == true && b.GetComponentInChildren<Text>().text.Contains("소실"));
            Cancel().onClick.Invoke(); Check(barrel.Current != null, "first cancel only asks confirmation");
            Cancel().onClick.Invoke(); Check(barrel.Current == null, "second cancel confirms raw loss");
            menu.ShowProcessor(SceneObjects.All<Processor>().First(p => p.Chemical));
            var text = menu.GetComponentsInChildren<Text>().Select(t => t.text).ToArray(); Check(text.Any(t => t.Contains("고무 수액 2")) && text.Any(t => t.Contains("배관 원유 2")), "separate rubber/raw oil costs");
            menu.ShowProcessor(still);
            Check(menu.GetComponentsInChildren<Button>().Count(b => b.GetComponentInChildren<Text>()?.text.StartsWith("목록 추가:") == true) == 15, "all distillation/fuel choices present");
            var result = "PASS " + count + " crop row bounds, fermentation confirmation, chemical costs and distillation UI checks"; File.WriteAllText("Temp/food-industry-ui-result.txt", result); return result;
        }
        finally { restore.Invoke(barrel, new object[] { old }); menu.Resume(); Time.timeScale = 0; }
    }
}
