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
        const float Height = 1.6f; // 기존 유닛과 비슷한 월드 키로 정규화한다.
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
            // FBX 최상위에는 Blender 축 변환 회전이 있다. AntVisual.Face가 최상위 회전을 바꾸므로 빈 부모(pivot)에 넣어 그 회전을 보존한다(2026-10-24 캐릭터가 눕던 원인).
            var pivot = new GameObject("Kemono").transform; pivot.SetParent(parent, false);
            // 리그에 연결된 클립이 축 변환까지 담당하므로 부모에서 추가 회전하지 않는다.
            var upright = new GameObject("Upright").transform; upright.SetParent(pivot, false);
            var model = Object.Instantiate(source, upright, false);
            var animator = RigAnimator.Bind(model, "KemonoRig", controller);
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var parts = model.GetComponentsInChildren<Renderer>(true);
            string Pick(string slot)
            {
                var options = parts.Select(r => r.name).Where(n => n.StartsWith("Part_" + slot + "_") && !n.Contains("_ARMOR"))
                    .Select(n => n.Split('_')[2]).Distinct().OrderBy(n => n).ToArray();
                return options.Length == 0 ? null : options[(int)(unchecked((uint)(seed * 7919 + slot.Length * 31)) % (uint)options.Length)];
            }
            var keep = new[] { "Hair_" + Pick("Hair") + "_", "Ears_" + Pick("Ears") + "_", "Tail_" + Pick("Tail") + "_", "Face_NEUTRAL_", "Body_", "Outfit_" };
            foreach (var r in parts) r.enabled = !r.name.Contains("_ARMOR") && keep.Any(k => r.name.StartsWith("Part_" + k));
            // 초기 FBX 자세와 첫 애니메이션 자세의 축이 달라 먼저 포즈를 평가해야 실제 키를 잰다.
            animator.Rebind(); animator.Play("Base Layer.Idle_A", 0, 0); animator.Update(.01f);
            Bounds Bounds() => parts.Where(r => r.enabled).Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
            var bounds = Bounds();
            upright.localScale *= Height / Mathf.Max(bounds.size.y, .01f); // 애니메이션이 쓰지 않는 부모에서 월드 키 고정
            bounds = Bounds(); upright.position += Vector3.up * ((parent != null ? parent.position.y : 0) - bounds.min.y);
            bounds = Bounds();
            if (parent != null) FitCollider(parent);
            pivot.gameObject.AddComponent<KemonoAppearance>().Initialize(seed);
            return pivot.gameObject;
        }

        public static void FitCollider(Transform owner)
        {
            if (!owner.TryGetComponent<CapsuleCollider>(out var capsule)) return;
            var bounds = owner.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r => r.enabled).Select(r => r.bounds)
                .Aggregate((a, b) => { a.Encapsulate(b); return a; });
            var scale = owner.lossyScale;
            capsule.center = owner.InverseTransformPoint(bounds.center);
            capsule.height = bounds.size.y / Mathf.Abs(scale.y);
            capsule.radius = Mathf.Max(bounds.extents.x / Mathf.Abs(scale.x), bounds.extents.z / Mathf.Abs(scale.z));
        }

        // 손 소켓(Socket.Tool.R)에 도구·무기를 붙인다. name=null이면 치운다.
        public static void Hold(GameObject model, string name)
        {
            var sockets = model.GetComponentsInChildren<Transform>(true);
            foreach (var socket in sockets.Where(t => t.name == "Socket.Tool.R" || t.name == "Socket.Tool.L"))
            {
                foreach (Transform child in socket) { child.gameObject.SetActive(false); Object.Destroy(child.gameObject); }
                var item = name == "Weapon_ShortBow" ? (socket.name.EndsWith(".L") ? name : "Ammo_Arrow")
                    : name != null && name.StartsWith("Weapon_Shield_") ? socket.name.EndsWith(".L") ? name : null
                    : socket.name.EndsWith(".R") ? name : null;
                if (item == null) continue;
                if (!tools.TryGetValue(item, out var prefab)) tools[item] = prefab = Resources.Load<GameObject>("Kemono/Handheld/" + item);
                if (prefab == null) continue;
                var tool = Object.Instantiate(prefab, socket, false); tool.transform.localPosition = Vector3.zero; tool.transform.localRotation = Quaternion.identity;
                foreach (var c in tool.GetComponentsInChildren<Collider>()) Object.Destroy(c);
            }
        }

        // 채집 자원별 동작과 손 도구(석기 등급, 키트 Tool_*_Stone).
        public static (string clip, string tool) Gather(ResourceType type) => type switch
        {
            ResourceType.Wood or ResourceType.Resin => ("Tool_Chop", "Tool_Axe_Stone"),
            ResourceType.Stone or ResourceType.IronOre or ResourceType.CopperOre or ResourceType.TinOre or ResourceType.Coal or ResourceType.Sulfur => ("Tool_Mine", "Tool_Pickaxe_Stone"),
            ResourceType.Sand or ResourceType.Clay or ResourceType.Soil => ("Tool_Dig", "Tool_Shovel_Stone"),
            ResourceType.Food or ResourceType.Leaf or ResourceType.Fiber or ResourceType.Herb => ("Tool_Harvest", "Tool_Sickle_Stone"),
            _ => ("GroundWork", null)
        };
    }
}

