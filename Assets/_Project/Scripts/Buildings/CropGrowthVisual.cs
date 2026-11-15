using System.Linq;
using AntColony.Core;
using AntColony.World;
using UnityEngine;

namespace AntColony.Buildings
{
    // Existing crop-state assets now follow the saved simulation instead of showing a permanent harvest.
    public sealed class CropGrowthVisual : MonoBehaviour
    {
        FacilityVisual facility;
        FarmPlot plot;
        StarterCropBed starter;
        GameObject cropModel;
        Transform display;
        string stage;
        float nextRefresh;
        Vector3 soil;
        float width;
        public string Stage => stage;
        public static void Attach(FacilityVisual visual)
        {
            if (!visual.GetComponent<FarmPlot>() && !visual.GetComponent<StarterCropBed>()) return;
            var view = visual.GetComponent<CropGrowthVisual>() ?? visual.gameObject.AddComponent<CropGrowthVisual>();
            view.facility = visual; view.plot = visual.GetComponent<FarmPlot>(); view.starter = visual.GetComponent<StarterCropBed>();
            view.display = null; view.nextRefresh = 0;
        }
        void LateUpdate()
        {
            if (!facility || !facility.Model || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + .4f;
            if (display != facility.Model)
            {
                display = facility.Model;
                var surfaces = display.GetComponentsInChildren<Renderer>().Where(r => r.name.Contains("Soil")
                    || r.name.Contains("Compost") || r.name.Contains("Body")).ToArray();
                var body = GetComponent<Renderer>().bounds;
                float top = surfaces.Length > 0 ? surfaces.Max(r => r.bounds.max.y) : body.min.y + .2f;
                soil = new Vector3(body.center.x, top + .01f, body.center.z);
                width = Mathf.Min(body.size.x, body.size.z) * .68f;
                stage = null;
            }
            bool fungus = starter || plot && plot.Crop is FarmCrop.Fungus or FarmCrop.AdvancedFungus;
            foreach (var r in display.GetComponentsInChildren<Renderer>())
                if (r.name.StartsWith("Cap") || r.name.StartsWith("Stem")) r.enabled = !fungus;
            string wanted = null;
            if (fungus && starter && starter.Crop.planted)
                wanted = GrowthStage(starter.Crop.growth / StartSupplyRules.CropSeconds, false);
            else if (fungus && plot && !plot.Data.fallow && !plot.Data.seedPending && !plot.Node.NeedsSowing)
                wanted = plot.Data.pest ? "Pest" : !plot.Node.IsRegrowing && !plot.Node.IsDepleted ? "Mature"
                    : GrowthStage(1 - plot.Node.RegrowTimeRemaining / Mathf.Max(1, plot.Node.RegrowSeconds), true);
            if (wanted == stage) return;
            stage = wanted;
            if (cropModel) { cropModel.SetActive(false); Destroy(cropModel); }
            if (stage != null) cropModel = ProductionProp.Attach(transform, "CropStages1115/CC_CropFungus_" + stage, width, soil);
        }
        static string GrowthStage(float progress, bool harvestByWorker) => progress < .08f ? "Planted" : progress < .35f ? "Young"
            : progress >= .8f && !harvestByWorker ? "Mature" : "Growing";
    }
}
