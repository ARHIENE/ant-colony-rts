using UnityEngine;

namespace AntColony.Units
{
    // The wooden sword shares the fitted sword mesh, with a wood-only material override.
    public static class MeshyEquipmentModel
    {
        public static GameObject Load(string name) => string.IsNullOrEmpty(name) ? null :
            Resources.Load<GameObject>("Kemono/Handheld/" + (name == "Weapon_Sword_Wood" ? "Weapon_Sword_Copper" : name));

        public static void ApplyFinish(GameObject instance, string name)
        {
            if (!instance || name != "Weapon_Sword_Wood") return;
            instance.name = name + "(Clone)";
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
            {
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetColor("_BaseColor", new Color(.40f, .22f, .10f));
                block.SetTexture("_BaseMap", Texture2D.whiteTexture);
                block.SetFloat("_Metallic", 0);
                block.SetFloat("_Smoothness", .15f);
                renderer.SetPropertyBlock(block);
            }
        }
    }
}
