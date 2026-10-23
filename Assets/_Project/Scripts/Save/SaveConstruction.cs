using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Buildings;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;

namespace AntColony.Save
{
    // 건설·철거·이동·개보수 예정지 저장(v17, 2026-10-12). 재료를 기다리는 예정지가 오래 남을 수 있어 저장 차단 대신 그대로 저장한다.
    // 예약량과 배송 중 작업을 함께 보존한다.
    [Serializable] public class ConstructionDto
    {
        public string name; public Vec3Dto position, scale; public Color color;
        public float buildTime, remaining; public bool started, paused; public long order; public int priority = 5; public bool yellow;
        public List<BuildingConstructionSite.Need> needs = new List<BuildingConstructionSite.Need>();
        // 새 건물(없으면 철거·이동·개보수 예정지)
        public string kind; public int role, material, crop = -1; public bool wide; public string buildingName; public Vec3Dto buildingPosition; public float buildingRotY;
        // 철거·이동·개보수 표식: 대상은 위치·종류로 다시 찾는다.
        public int mark; public string targetKind; public Vec3Dto targetPosition, moveTo; public int newMaterial; public bool wasEnabled = true, workStarted, packed;
        // 재개발 표식
        public bool redevelop; public Vec3Dto redevelopTarget; public float redevelopAge;
    }

    internal static class SaveConstruction
    {
        internal static List<ConstructionDto> Capture() => UnityEngine.Object.FindObjectsByType<BuildingConstructionSite>().Where(s => s != null && s.isActiveAndEnabled).OrderBy(s => s.Order).Select((s, i) => Capture(s, i)).ToList();
        private static ConstructionDto Capture(BuildingConstructionSite s, int index)
        {
            SaveReferences.Register("construction:" + index, s);
            var d = new ConstructionDto { name = s.name, position = new Vec3Dto(s.transform.position), scale = new Vec3Dto(s.transform.localScale),
                color = s.TryGetComponent<Renderer>(out var r) ? r.material.color : Color.white,
                buildTime = s.BuildTimeSeconds, remaining = s.RemainingWork, started = s.Started, paused = s.Paused, order = s.Order,
                priority = WorkPriorities.Level(s), yellow = WorkPriorities.Yellow(s),
                needs = s.Needs.Select(n => new BuildingConstructionSite.Need { type = n.type, need = n.need, reserved = n.reserved, delivered = n.delivered }).ToList() };
            var b = s.Building != null ? s.Building.GetComponent<BuildingBase>() : null;
            if (b != null && b.Data != null)
            {
                d.kind = b.Data.kind.ToString(); d.material = (int)b.MainMaterial; d.buildingName = b.name;
                d.role = b is Barracks barracks ? (int)barracks.Role : b is ResearchLab lab ? (int)lab.Role : 0;
                d.buildingPosition = new Vec3Dto(b.transform.position); d.buildingRotY = b.transform.eulerAngles.y;
                if (b.TryGetComponent<FarmPlot>(out var plot)) { d.crop = (int)plot.Crop; d.wide = plot.Wide; }
            }
            if (s.TryGetComponent<DemolitionSite>(out var m) && m.Target != null)
            {
                d.mark = m.Renovate ? 3 : m.Move ? 2 : 1; d.targetKind = SaveCatalog.Kind(m.Target); d.targetPosition = new Vec3Dto(m.Target.Position);
                d.moveTo = new Vec3Dto(m.MoveTo); d.newMaterial = (int)m.NewMaterial; d.wasEnabled = m.WasEnabled; d.workStarted = m.WorkStarted; d.packed = m.Packed;
            }
            if (s.TryGetComponent<RedevelopmentSite>(out var rd) && rd.Target != null) { d.redevelop = true; d.redevelopTarget = new Vec3Dto(rd.Target.Position); d.redevelopAge = rd.Age; }
            return d;
        }

        // ponytail: 철거 대상·재개발 집은 위치(0.3m)·종류로 다시 찾는다. 같은 자리에 같은 종류가 겹치면 저장 식별자로 바꾼다.
        private static T Find<T>(Vec3Dto at, string kind = null) where T : BuildingBase => at == null ? null : UnityEngine.Object.FindObjectsByType<T>()
            .Where(b => (b.Position - at.ToVector3()).sqrMagnitude < .09f && (kind == null || SaveCatalog.Kind(b) == kind)).FirstOrDefault();

        internal static void Restore(List<ConstructionDto> list)
        {
            if (list == null) return;
            foreach (var d in list)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = d.name; go.transform.position = d.position.ToVector3(); go.transform.localScale = d.scale.ToVector3();
                go.GetComponent<Collider>().isTrigger = true; go.GetComponent<Renderer>().material.color = d.color;
                GameObject building = null;
                if (!string.IsNullOrEmpty(d.kind))
                {
                    var template = BuildingPlacementController.GetTemplate(Enum.Parse<BuildingKind>(d.kind), (UnitRole)d.role);
                    if (template == null) throw new InvalidOperationException("Missing building template: " + d.kind);
                    building = UnityEngine.Object.Instantiate(template, d.buildingPosition.ToVector3(), Quaternion.Euler(0, d.buildingRotY, 0));
                    building.SetActive(false); building.name = d.buildingName;
                    building.GetComponent<BuildingBase>().MainMaterial = (ResourceType)d.material;
                    if (d.crop >= 0) building.AddComponent<FarmPlot>().Configure((FarmCrop)d.crop, d.wide);
                }
                var site = go.AddComponent<BuildingConstructionSite>();
                site.Initialize(building, d.buildTime); site.RemainingWork = Mathf.Clamp(d.remaining, 0, d.buildTime);
                foreach (var n in d.needs) { site.Require(n.type, n.need); site.Needs.Last().delivered = Mathf.Clamp(n.delivered, 0, n.need); site.Needs.Last().reserved = n.reserved; }
                SaveReferences.Register("construction:" + list.IndexOf(d), site);
                site.Started = d.started; site.Order = d.order; if (d.paused) site.SetPaused(true);
                WorkPriorities.Set(site, d.priority, d.yellow);
                if (d.mark > 0)
                {
                    var target = Find<BuildingBase>(d.targetPosition, d.targetKind);
                    if (target == null) { UnityEngine.Object.Destroy(go); continue; } // 대상이 사라졌으면 예정지도 없다
                    var m = go.AddComponent<DemolitionSite>(); m.Target = target; m.Renovate = d.mark == 3; m.Move = d.mark == 2;
                    m.MoveTo = d.moveTo.ToVector3(); m.NewMaterial = (ResourceType)d.newMaterial; m.WasEnabled = d.wasEnabled;
                    if (d.packed) m.MarkPacked(); else if (d.workStarted) m.OnWorkStarted();
                }
                if (d.redevelop) { var rd = go.AddComponent<RedevelopmentSite>(); rd.Target = Find<Housing>(d.redevelopTarget); rd.Age = d.redevelopAge; }
            }
        }
        internal static bool Valid(List<ConstructionDto> list) => list == null || list.Count <= 10000 && list.All(d => d != null && d.position != null && d.position.IsFinite() && d.scale != null
            && !float.IsNaN(d.buildTime) && d.buildTime >= 0 && !float.IsNaN(d.remaining) && d.remaining >= 0 && d.priority >= WorkPriorities.Min && d.priority <= WorkPriorities.Max
            && d.needs != null && d.needs.All(n => n != null && Enum.IsDefined(typeof(ResourceType), n.type) && n.need >= 0 && n.delivered >= 0 && n.delivered <= n.need)
            && (string.IsNullOrEmpty(d.kind) ? d.mark > 0 : Enum.TryParse<BuildingKind>(d.kind, out _) && d.buildingPosition != null && d.buildingPosition.IsFinite())
            && d.mark >= 0 && d.mark <= 3 && (d.mark == 0 || d.targetPosition != null && d.moveTo != null));
    }
}
