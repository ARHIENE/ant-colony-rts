using UnityEngine;

namespace AntColony.Boss
{
    public sealed class CampaignPlaceholderBody : MonoBehaviour { }
    // A template without artwork still needs a visible, clickable combat body.
    // Authored boss models take precedence over this temporary mechanical core.
    public static class CampaignBossBody
    {
        static Material shell, core;
        public static void Ensure(BossHealth boss)
        {
            if (boss == null) return;
            if (boss.GetComponent<AntColony.Core.MothershipBossMarker>() != null
                && boss.transform.Find("본선 지휘체 · 생물 핵") == null
                && (boss.GetComponentInChildren<Renderer>(true) == null || boss.GetComponent<CampaignPlaceholderBody>() != null)
                && Resources.Load<GameObject>("OrganicEnemies1114/BroodAlien/Idle") != null)
            {
                foreach (var renderer in boss.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
                foreach (var collider in boss.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
                var body = AntColony.World.OrganicEnemyVisual.Create(AntColony.World.OrganicEnemyKind.BroodAlien, boss.transform);
                body.name = "본선 지휘체 · 생물 핵";
                body.transform.localScale *= 2.5f;
                body.transform.localPosition *= 2.5f;
                var hit = boss.gameObject.AddComponent<CapsuleCollider>(); hit.height = 4; hit.radius = 1.6f; hit.center = Vector3.up * 2;
                return;
            }
            if (boss.GetComponent<CampaignPlaceholderBody>() != null) { AttachSiegeCore(boss); return; }
            if (boss.GetComponentInChildren<Renderer>(true) != null) return;
            boss.gameObject.AddComponent<CampaignPlaceholderBody>();
            if (shell == null)
            {
                shell = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(.34f,.39f,.44f), hideFlags = HideFlags.HideAndDontSave };
                core = new Material(shell) { color = new Color(.95f,.28f,.10f), hideFlags = HideFlags.HideAndDontSave };
                core.EnableKeyword("_EMISSION"); core.SetColor("_EmissionColor", new Color(1.2f,.16f,.03f));
            }
            Part(boss, "지휘체 핵", PrimitiveType.Sphere, new Vector3(0,.8f,0), Vector3.one*.9f, core);
            Part(boss, "지휘체 하부", PrimitiveType.Cylinder, new Vector3(0,.25f,0), new Vector3(1.7f,.25f,1.7f), shell);
            for (int i=0;i<4;i++)
            {
                float a=i*Mathf.PI*.5f;
                Part(boss,"방열 기둥",PrimitiveType.Cube,new Vector3(Mathf.Cos(a)*.8f,.7f,Mathf.Sin(a)*.8f),new Vector3(.25f,1.4f,.25f),shell);
            }
            AttachSiegeCore(boss);
        }
        static void AttachSiegeCore(BossHealth boss)
        {
            if (boss.GetComponent<AntColony.Core.MothershipBossMarker>() != null || boss.transform.Find("침공 지휘체 · Meshy") != null) return;
            var placeholders = boss.GetComponentsInChildren<Renderer>(true);
            var scale = boss.transform.lossyScale;
            var model = AntColony.World.ProductionProp.Attach(boss.transform, "CampaignBoss1115/AlienSiegeCore",
                1.7f * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z)), boss.Position);
            if (model == null) return;
            model.name = "침공 지휘체 · Meshy";
            model.transform.rotation = boss.transform.rotation * model.transform.rotation;
            foreach (var renderer in placeholders) renderer.forceRenderingOff = true;
            // 기존 전투 판정과 공격 패턴은 그대로 둔다. 수입 모델에는 충돌체를 만들지 않는다.
        }
        static void Part(BossHealth boss,string name,PrimitiveType type,Vector3 position,Vector3 scale,Material material)
        {
            var part=GameObject.CreatePrimitive(type); part.name=name;
            part.transform.SetParent(boss.transform,false); part.transform.localPosition=position;part.transform.localScale=scale;
            part.GetComponent<Renderer>().sharedMaterial=material;
        }
    }
}
