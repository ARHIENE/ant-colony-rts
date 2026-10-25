using System.Collections.Generic;
using System.Linq;
using AntColony.Data;
using AntColony.World;
using UnityEngine;

namespace AntColony.Units
{
    public sealed class GatherOrderFeedback : MonoBehaviour
    {
        private readonly List<WorkerAnt> workers = new List<WorkerAnt>();
        private ResourceNode node;
        private LineRenderer ring;
        public string OrderText { get; private set; }

        public static void Show(ResourceNode node, WorkerAnt worker)
        {
            var feedback = node.GetComponent<GatherOrderFeedback>();
            if (feedback == null) feedback = node.gameObject.AddComponent<GatherOrderFeedback>();
            if (!feedback.workers.Contains(worker)) feedback.workers.Add(worker);
            feedback.Refresh();
        }

        private void Awake()
        {
            node = GetComponent<ResourceNode>();
            var visual = new GameObject("Gather target ring"); visual.transform.SetParent(transform, false);
            ring = visual.AddComponent<LineRenderer>(); ring.useWorldSpace = true; ring.loop = true;
            ring.positionCount = 40; ring.widthMultiplier = .08f;
            ring.sharedMaterial = Resources.Load<Material>("AntSelection");
            ring.startColor = ring.endColor = new Color(1, .75f, .15f);
            if (GetComponent<ResourceNodeStatus>() == null) gameObject.AddComponent<ResourceNodeStatus>();
        }

        private void LateUpdate() => Refresh();
        private void Refresh()
        {
            workers.RemoveAll(w => w == null || !w.isActiveAndEnabled || w.CurrentResourceNode != node || !w.IsWorking);
            ring.enabled = workers.Count > 0;
            if (workers.Count == 0) { OrderText = null; return; }
            var gathering = workers.Count(w => w.IsGatheringAnimation);
            var returning = workers.Count(w => w.IsReturningCargo);
            OrderText = node.ResourceType.DisplayName() + (gathering > 0 ? $" · 채집 중 {gathering}명"
                : returning > 0 ? $" · 자원 운반 중 {returning}명" : $" · 채집하러 이동 중 {workers.Count}명");
            var bounds = GetComponent<Collider>().bounds;
            var radius = Mathf.Clamp(Mathf.Max(bounds.extents.x, bounds.extents.z) + .2f, .6f, 3f);
            var center = new Vector3(bounds.center.x, bounds.min.y + .08f, bounds.center.z);
            for (var i = 0; i < ring.positionCount; i++)
            {
                var angle = i * Mathf.PI * 2 / ring.positionCount;
                ring.SetPosition(i, center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius);
            }
        }
    }
}
