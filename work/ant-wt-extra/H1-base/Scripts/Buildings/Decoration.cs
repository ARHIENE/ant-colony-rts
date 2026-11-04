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
        public static bool IsKind(BuildingKind kind) => kind >= BuildingKind.FlowerPot && kind <= BuildingKind.FireflyLamp || kind >= BuildingKind.Statue && kind <= BuildingKind.Pillar
            || kind >= BuildingKind.Brazier && kind <= BuildingKind.CuriosDisplay;
        // 활성 장식 목록. 기분(Mood)은 매 프레임 여러 번 계산되므로 씬 전체 검색(FindObjectsByType) 대신 이 목록을 쓴다(2026-10-07 성능).
        private static readonly System.Collections.Generic.List<Decoration> Active = new System.Collections.Generic.List<Decoration>();
        public static float MoodAt(CommanderAnt c) => Mathf.Min(10, Active
            .Where(d => !d.IsDead && (d.Position - c.Position).sqrMagnitude <= 64)
            .GroupBy(d => d.Data.kind).Sum(g => g.Max(d => d.MoodBonus)));
        public static int CountNear(Vector3 p) => Active.Count(d => !d.IsDead && d.MoodBonus > 0 && (d.Position - p).sqrMagnitude <= 64);
        // 가구 5차(잠정): 조각상 +4 / 깃발 +2 / 그림 +3 / 기둥 0(방 등급 점수만).
        // 가구 6차(잠정): 화롯불·등불·횃불 +1(빛) / 카펫 +1 / 태피스트리 +2 / 기념비·동상 +5 / 전리품 진열대 +3 / 사람 물건 전시대 +4.
        public float MoodBonus => (Data.kind switch { BuildingKind.FlowerPot => 3 + (GameCalendar.CurrentSeason == Season.Spring ? 1 : 0),
            BuildingKind.ShellDecoration => 2, BuildingKind.MarbleMosaic => 4, BuildingKind.BottleMobile => 3,
            BuildingKind.Statue => 4, BuildingKind.Flag => 2, BuildingKind.Painting => 3, BuildingKind.Pillar => 0,
            BuildingKind.Tapestry => 2, BuildingKind.Monument or BuildingKind.BronzeStatue => 5, BuildingKind.TrophyCase => 3, BuildingKind.CuriosDisplay => 4,
            _ => 1 }) * (.5f + .5f * Quality);
        protected override void OnDisable() { Active.Remove(this); base.OnDisable(); }
        protected override void OnEnable()
        {
            base.OnEnable(); Active.Add(this);
            // 조명 장식: 반딧불 램프·화롯불·등불·횃불. ponytail: 빛은 연출만(밤 작업 규칙이 생기면 여기 반경을 쓴다).
            var glow = Data == null ? (Color?)null : Data.kind switch
            {
                BuildingKind.FireflyLamp => new Color(1, .75f, .3f), BuildingKind.Brazier => new Color(1, .55f, .2f),
                BuildingKind.Lantern => new Color(1, .85f, .55f), BuildingKind.Torch => new Color(1, .6f, .25f), _ => (Color?)null
            };
            if (glow != null && GetComponent<Light>() == null)
            { var light = gameObject.AddComponent<Light>(); light.type = LightType.Point; light.range = 8; light.color = glow.Value; light.intensity = 2; }
        }
    }
}
