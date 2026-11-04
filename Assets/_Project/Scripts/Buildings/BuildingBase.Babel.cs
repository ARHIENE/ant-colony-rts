using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;

namespace AntColony.Buildings
{
    [Serializable] public sealed class BabelState
    {
        [Serializable] public sealed class Cost { public ResourceType type; public int amount; }
        public bool eligible, used;
        public float work;
        public List<Cost> costs = new List<Cost>();
        public static int RebuildCost(int amount) => (int)Math.Ceiling(amount * .75m);
        public static bool Valid(BabelState s) => s == null || s.work >= 0 && !float.IsInfinity(s.work) && s.costs != null && s.costs.Count <= 100
            && s.costs.All(c => c != null && Enum.IsDefined(typeof(ResourceType), c.type) && c.amount >= 0 && c.amount <= 100000000);
    }
    public partial class BuildingBase
    {
        internal BabelState Babel;
        private bool LeaveBabelFoundation()
        {
            if (Babel?.eligible != true || Babel.used || Data == null || this is AirshipYard || this is AntColony.World.ExpeditionTransport) return false;
            var role = this is Barracks barracks ? barracks.Role : this is ResearchLab lab ? lab.Role : default;
            var template = BuildingPlacementController.GetTemplate(Data.kind, role);
            if (template == null) return false;
            Babel.used = true;
            var next = Instantiate(template, Position, transform.rotation); next.SetActive(false); next.name = name;
            next.GetComponent<BuildingBase>().MainMaterial = MainMaterial;
            if (TryGetComponent<FarmPlot>(out var farm)) (next.GetComponent<FarmPlot>() ?? next.AddComponent<FarmPlot>()).Configure(farm.Crop, farm.Wide);
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name + " · 바벨 기초(재건 대기)";
            go.transform.position = Position; go.transform.localScale = new Vector3(2.5f, .2f, 2.5f);
            go.GetComponent<Collider>().isTrigger = true;
            var site = go.AddComponent<BuildingConstructionSite>();
            site.Initialize(next, Babel.work * .5f); site.BabelUsed = true;
            foreach (var cost in Babel.costs) site.Require(cost.type, BabelState.RebuildCost(cost.amount));
            site.SetPaused(true); // 재건은 기존 예정지의 재개 버튼으로 직접 지시한다.
            AntColony.UI.ToastManager.Show(name + ": 바벨 기초가 남았습니다. 재료75%·작업50%, 예정지에서 재개하세요.");
            return true;
        }
    }
}
