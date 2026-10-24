using AntColony.Buildings;
using AntColony.Core;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace AntColony.Save
{
    // JsonUtility materializes absent inline classes. Inspect the JSON before losing that distinction.
    internal static class SaveOptionalStates
    {
        private static bool Absent(JToken token) => token == null || token.Type == JTokenType.Null
            || token is JObject obj && !obj.HasValues;

        internal static void FromJson(SaveFileV1 f, string json)
        {
            var root = JObject.Parse(json);
            if (Absent(root["rocket"])) f.rocket = null;
            if (Absent(root["farmSeeds"])) f.farmSeeds = null;
            if (f.farmSeeds != null && (f.farmSeeds.counts == null || f.farmSeeds.counts.Length == 0)
                && (f.farmSeeds.collected == null || f.farmSeeds.collected.Count == 0)) f.farmSeeds = null;
            if (Absent(root["world"]?["pause"]) && f.world != null) f.world.pause = null;
            var pause = f.world?.pause;
            if (pause != null && pause.speed == 0 && pause.sequence == 0 && pause.reasons?.Count == 0
                && pause.incidents?.Count == 0 && pause.active?.Count == 0) f.world.pause = null;
            if (Absent(root["campaign"]?["bosses"]) && f.campaign != null) f.campaign.bosses = new AntColony.Boss.BossProgress.State();
            var buildings = root["buildings"] as JArray;
            if (buildings != null && f.buildings != null)
                for (int i = 0; i < buildings.Count && i < f.buildings.Count; i++)
                {
                    var b = f.buildings[i]; var raw = buildings[i] as JObject;
                    if (b == null || raw == null) continue;
                    if (Absent(raw["airship"])) b.airship = null;
                    if (Absent(raw["processor"])) b.processor = null;
                    if (Absent(raw["supply"])) b.supply = null;
                    if (Absent(raw["starterCrop"])) b.starterCrop = null;
                    if (Absent(raw["farm"])) b.farm = null;
                    if (Absent(raw["fire"])) b.fire = null;
                }
        }

        internal static string ToJson(SaveFileV1 f, bool pretty = false)
        {
            var root = JObject.Parse(JsonUtility.ToJson(f));
            if (f.rocket == null) root["rocket"] = JValue.CreateNull();
            if (f.farmSeeds == null) root["farmSeeds"] = JValue.CreateNull();
            if (f.world?.pause == null && root["world"] is JObject world) world["pause"] = JValue.CreateNull();
            var buildings = root["buildings"] as JArray;
            for (int i = 0; buildings != null && i < buildings.Count; i++)
            {
                var b = f.buildings[i]; var raw = (JObject)buildings[i];
                if (b.airship == null) raw["airship"] = JValue.CreateNull();
                if (b.processor == null) raw["processor"] = JValue.CreateNull();
                if (b.supply == null) raw["supply"] = JValue.CreateNull();
                if (b.starterCrop == null) raw["starterCrop"] = JValue.CreateNull();
                if (b.farm == null) raw["farm"] = JValue.CreateNull();
                if (b.fire == null) raw["fire"] = JValue.CreateNull();
            }
            return root.ToString(pretty ? Newtonsoft.Json.Formatting.Indented : Newtonsoft.Json.Formatting.None);
        }

        internal static void Normalize(SaveFileV1 f)
        {
            // The kind is the presence discriminator for building-specific states.
            foreach (var b in f.buildings)
            {
                if (b == null) continue;
                if (b.kind != "AirshipYard") b.airship = null;
                if (!System.Enum.TryParse(b.kind, out AntColony.Data.BuildingKind kind) || !Processor.IsKind(kind)) b.processor = null;
                if (!SupplyFacility.IsKind(kind)) b.supply = null;
                if (b.kind != "PlanterBed" && b.kind != "StarterMushroomBed") b.starterCrop = null;
                if (b.kind != "Farm") b.farm = null;
            }
        }
    }
}
