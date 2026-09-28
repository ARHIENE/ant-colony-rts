using System.Linq;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;

namespace AntColony.Buildings
{
    public sealed class Decoration : BuildingBase
    {
        public int Quality { get; internal set; } = 1;
        public static bool IsKind(BuildingKind kind) => kind >= BuildingKind.FlowerPot && kind <= BuildingKind.FireflyLamp;
        public static float MoodAt(CommanderAnt c) => Mathf.Min(10, FindObjectsByType<Decoration>(FindObjectsSortMode.None)
            .Where(d => !d.IsDead && (d.Position - c.Position).sqrMagnitude <= 64)
            .GroupBy(d => d.Data.kind).Sum(g => g.Max(d => d.MoodBonus)));
        public float MoodBonus => (Data.kind switch { BuildingKind.FlowerPot => 3 + (GameCalendar.CurrentSeason == Season.Spring ? 1 : 0),
            BuildingKind.ShellDecoration => 2, BuildingKind.MarbleMosaic => 4, BuildingKind.BottleMobile => 3, _ => 1 }) * (.5f + .5f * Quality);
        protected override void OnEnable()
        {
            base.OnEnable();
            if (Data != null && Data.kind == BuildingKind.FireflyLamp && GetComponent<Light>() == null)
            { var light = gameObject.AddComponent<Light>(); light.type = LightType.Point; light.range = 8; light.color = new Color(1, .75f, .3f); light.intensity = 2; }
        }
    }
}
