using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using UnityEngine;

namespace AntColony.Save
{
    [Serializable] public sealed class ConstructionAttributionDto
    {
        public long order;
        public WorkContribution contributions;
        public SoleWorker artist;
        public bool babelUsed;
    }
    [Serializable] public sealed class FoodCargoWrapper { public List<AntColony.World.ExpeditionTransport.FoodCargo> values; }
    internal static class SaveAttribution
    {
        internal static List<ConstructionAttributionDto> Capture() => SceneObjects.All<BuildingConstructionSite>()
            .Where(s => s.isActiveAndEnabled).Select(s => new ConstructionAttributionDto { order = s.Order,
                contributions = s.Contributions, artist = s.Artist, babelUsed = s.BabelUsed })
            .Select(d => JsonUtility.FromJson<ConstructionAttributionDto>(JsonUtility.ToJson(d))).ToList();
        internal static void Restore(List<ConstructionAttributionDto> list)
        {
            foreach (var d in list ?? new List<ConstructionAttributionDto>())
            {
                var s = SceneObjects.All<BuildingConstructionSite>().FirstOrDefault(s => s.Order == d.order);
                if (s == null) continue;
                var copy = JsonUtility.FromJson<ConstructionAttributionDto>(JsonUtility.ToJson(d));
                s.Contributions = copy.contributions ?? new WorkContribution(); s.Artist = copy.artist ?? new SoleWorker(); s.BabelUsed = copy.babelUsed;
            }
        }
        internal static bool Valid(List<ConstructionAttributionDto> list, List<ConstructionDto> sites) => list == null
            || list.Count <= 10000 && list.All(d => d != null && sites?.Any(s => s.order == d.order) == true
                && d.contributions?.Valid == true && d.artist?.Valid == true)
            && list.Select(d => d.order).Distinct().Count() == list.Count;
    }
}
