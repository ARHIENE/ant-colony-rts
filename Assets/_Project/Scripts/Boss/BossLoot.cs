using AntColony.Data;
using AntColony.World;
using UnityEngine;
using UnityEngine.AI;

namespace AntColony.Boss
{
    // 보스 처치 전리품. 자원을 즉시 지급하지 않고 장수가 채집·반납하는 바닥 자원노드로 남긴다.
    // 노드는 보스와 부모 관계가 없어 보스 오브젝트가 파괴돼도 유지된다.
    public static class BossLoot
    {
        private const float NavMeshSampleRadius = 10f;
        private const float NodeSize = 1.2f;
        private const float CameraDistance = 30f; // IsometricCameraController 기본 distance

        public static readonly Color FoodColor = new Color(.95f, .75f, .25f);
        public static readonly Color SpecialColor = new Color(.65f, .35f, .9f);
        // 런타임 머티리얼을 만들지 않고 프리미티브 기본 sharedMaterial에 색만 입힌다(URP Lit=_BaseColor, Standard=_Color).
        public static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        public static readonly int ColorId = Shader.PropertyToID("_Color");

        public static void Drop(Vector3 center, float bossRadius, int food, int special)
        {
            // 보스 몸체 콜라이더가 클릭 레이캐스트를 가리지 않도록 몸체 반경 밖에 놓는다.
            var offset = bossRadius + 1.5f;
            var foodPos = FindSpot(center, offset, 0, null);
            if (food > 0) Spawn(ResourceType.Food, food, foodPos);
            if (special > 0) Spawn(ResourceType.Special, special, FindSpot(center, offset, 6, food > 0 ? foodPos : (Vector3?)null));
        }

        // 몸체 밖 12방향×2반경 후보 중 NavMesh 위, 지형물과 겹치지 않고, RTS 시야 방향에서 가리지 않는 첫 지점.
        // ponytail: 후보 24개가 전부 막히면 보상 유지를 위해 기존 +X/-X 위치로 폴백(가려질 수 있음).
        private static Vector3 FindSpot(Vector3 center, float offset, int startIndex, Vector3? avoid)
        {
            var view = UnityEngine.Camera.main != null ? UnityEngine.Camera.main.transform.forward : Quaternion.Euler(50f, 45f, 0f) * Vector3.forward;
            for (var ring = 0; ring < 2; ring++)
            for (var i = 0; i < 12; i++)
            {
                var dir = Quaternion.Euler(0f, (startIndex + i) * 30f, 0f) * Vector3.right;
                // 공중 보스도 지면에 닿도록 폴백과 같은 탐색 반경을 쓴다.
                if (!NavMesh.SamplePosition(center + dir * (offset + ring * 3f), out var hit, NavMeshSampleRadius, NavMesh.AllAreas)) continue;
                var p = hit.position;
                if (avoid.HasValue && (p - avoid.Value).sqrMagnitude < NodeSize * NodeSize * 4f) continue;
                if ((new Vector3(p.x, 0f, p.z) - new Vector3(center.x, 0f, center.z)).magnitude < offset - 0.5f) continue;
                var nodeCenter = p + Vector3.up * (NodeSize * 0.5f);
                if (Physics.CheckBox(nodeCenter + Vector3.up * 0.1f, Vector3.one * (NodeSize * 0.4f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore)) continue;
                if (Physics.Raycast(nodeCenter - view * CameraDistance, view, CameraDistance - NodeSize * 0.5f)) continue;
                return p;
            }
            return Fallback(center + (startIndex == 0 ? Vector3.right : Vector3.left) * offset);
        }

        private static Vector3 Fallback(Vector3 position)
        {
            return NavMesh.SamplePosition(position, out var hit, NavMeshSampleRadius, NavMesh.AllAreas) ? hit.position : position;
        }

        private static void Spawn(ResourceType type, int amount, Vector3 position)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.SetActive(false);
            go.name = $"BossLoot {type}";
            go.transform.position = position + Vector3.up * (NodeSize * 0.5f);
            go.transform.localScale = Vector3.one * NodeSize;

            var renderer = go.GetComponent<MeshRenderer>();
            var color = type == ResourceType.Special ? SpecialColor : FoodColor;
            var block = new MaterialPropertyBlock();
            block.SetColor(BaseColorId, color);
            block.SetColor(ColorId, color);
            renderer.SetPropertyBlock(block);

            go.AddComponent<ResourceNode>().ConfigureLoot(type, amount);
            go.AddComponent<ResourceNodeStatus>();
            go.SetActive(true);
        }
    }
}
