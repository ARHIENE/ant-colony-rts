using System.Collections.Generic;
using System.Linq;
using AntColony.Data;
using UnityEngine;

namespace AntColony.Units
{
    // 2026-10-24 개미(QuirkyAnt) 대신 kemono 캐릭터. Resources/Kemono의 캐릭터 FBX + Animator(에디터 KemonoImport가 구성).
    // 부품(Part_범주_선택지_번호)은 한 FBX에 모두 있어 개체 시드로 머리·귀·꼬리 한 조합만 켠다. 갑옷용 _ARMOR 부품은 끈다.
    public static class KemonoModel
    {
        const float Height = 1.6f; // ponytail: 개미 모델과 비슷한 화면 크기로 맞춘 잠정값. 건물·유닛 비율 조정 시 여기만 바꾼다
        static GameObject source; static RuntimeAnimatorController controller; static bool loaded;
        static readonly Dictionary<string, GameObject> tools = new Dictionary<string, GameObject>();

        public static bool Available { get { Load(); return source != null && controller != null; } }

        static void Load()
        {
            if (loaded) return; loaded = true;
            source = Resources.Load<GameObject>("Kemono/Kemono_Character");
            controller = Resources.Load<RuntimeAnimatorController>("Kemono/KemonoAnimator");
        }

        public static GameObject Create(Transform parent, int seed)
        {
            Load();
            var model = Object.Instantiate(source, parent, false);
            var animator = model.GetComponent<Animator>() ?? model.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller; animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            var parts = model.GetComponentsInChildren<Renderer>(true);
            string Pick(string slot)
            {
                var options = parts.Select(r => r.name).Where(n => n.StartsWith("Part_" + slot + "_") && !n.Contains("_ARMOR"))
                    .Select(n => n.Split('_')[2]).Distinct().OrderBy(n => n).ToArray();
                return options.Length == 0 ? null : options[Mathf.Abs(seed * 7919 + slot.Length * 31) % options.Length];
            }
            var keep = new[] { "Hair_" + Pick("Hair") + "_", "Ears_" + Pick("Ears") + "_", "Tail_" + Pick("Tail") + "_", "Face_NEUTRAL_", "Body_", "Outfit_" };
            foreach (var r in parts) r.enabled = !r.name.Contains("_ARMOR") && keep.Any(k => r.name.StartsWith("Part_" + k));
            var bounds = parts.Where(r => r.enabled).Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
            model.transform.localScale *= Height / Mathf.Max(bounds.size.y, .01f); // 월드 키 = Height
            return model;
        }

        // 손 소켓(Socket.Tool.R)에 도구·무기를 붙인다. name=null이면 치운다.
        public static void Hold(GameObject model, string name)
        {
            var socket = model.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Socket.Tool.R");
            if (socket == null) return;
            foreach (Transform child in socket) Object.Destroy(child.gameObject);
            if (name == null) return;
            if (!tools.TryGetValue(name, out var prefab)) tools[name] = prefab = Resources.Load<GameObject>("Kemono/Handheld/" + name);
            if (prefab == null) return;
            var tool = Object.Instantiate(prefab, socket, false); tool.transform.localPosition = Vector3.zero; tool.transform.localRotation = Quaternion.identity;
            foreach (var c in tool.GetComponentsInChildren<Collider>()) Object.Destroy(c);
        }

        // 채집 자원별 동작과 손 도구(석기 등급, 키트 Tool_*_Stone).
        public static (string clip, string tool) Gather(ResourceType type) => type switch
        {
            ResourceType.Wood or ResourceType.Resin => ("Tool_Chop", "Tool_Axe_Stone"),
            ResourceType.Stone or ResourceType.IronOre or ResourceType.CopperOre or ResourceType.Coal or ResourceType.Sulfur => ("Tool_Mine", "Tool_Pickaxe_Stone"),
            ResourceType.Sand or ResourceType.Clay or ResourceType.Soil => ("Tool_Dig", "Tool_Shovel_Stone"),
            ResourceType.Food or ResourceType.Leaf or ResourceType.Fiber => ("Tool_Harvest", "Tool_Sickle_Stone"),
            _ => ("GroundWork", null)
        };
    }
}
