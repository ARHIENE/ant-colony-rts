using System;
using System.Linq;
using System.Threading.Tasks;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using Object = UnityEngine.Object;

public static class KemonoQualityChecks
{
    static int checks;
    static void Check(bool value, string label) { if (!value) throw new Exception(label); checks++; }
    public static async Task<string> Main()
    {
        Check(Application.isPlaying && KemonoModel.Available, "Play mode and configured Kemono required");
        checks = 0;
        var root = new GameObject("KemonoQualityChecks");
        root.transform.position = new Vector3(1000, 0, 1000);
        try
        {
            for (var seed = 0; seed < 6; seed++)
            {
                var model = KemonoModel.Create(root.transform, seed);
                var appearance = model.GetComponent<KemonoAppearance>();
                Check(appearance.PaletteIndex == seed, "six deterministic palettes");
                var parts = model.GetComponentsInChildren<Renderer>(true);
                foreach (var face in new[] { "PAIN", "SLEEP", "DEAD", "NEUTRAL" })
                {
                    appearance.SetExpression(face);
                    var faces = parts.Where(r => r.enabled && r.name.StartsWith("Part_Face_")).ToArray();
                    Check(faces.Length > 0 && faces.All(r => r.name.StartsWith("Part_Face_" + face + "_")), "exclusive expression " + face);
                }
                var plates = parts.Where(r => r.name.StartsWith("Part_Equipment_Armor_")).ToArray();
                Check(plates.Length >= 10, "rigid armor plates imported");
                appearance.SetArmor(true);
                Check(plates.All(r => r.enabled), "equipped plates visible");
                Check(parts.Any(r => r.enabled && r.name == "Part_Body_BASE_ARMOR"), "armor body selected");
                Check(!parts.Any(r => r.enabled && r.name == "Part_Body_BASE_00"), "regular body hidden");
                appearance.SetArmor(false);
                Check(plates.All(r => !r.enabled) && parts.Any(r => r.enabled && r.name == "Part_Body_BASE_00"), "unequip restores body");
                KemonoModel.Hold(model, "Weapon_ShortBow");
                var sockets = model.GetComponentsInChildren<Transform>(true);
                Check(sockets.Single(t => t.name == "Socket.Tool.L").GetChild(0).name.StartsWith("Weapon_ShortBow"), "bow in left hand");
                Check(sockets.Single(t => t.name == "Socket.Tool.R").GetChild(0).name.StartsWith("Ammo_Arrow"), "arrow in right hand");
                KemonoModel.Hold(model, null);
                var clone = Object.Instantiate(model, root.transform);
                clone.GetComponent<KemonoAppearance>().SetExpression("DEAD");
                Check(clone.GetComponentsInChildren<Renderer>().Any(r => r.enabled && r.name.StartsWith("Part_Face_DEAD_")), "death clone has initialized expression cache");
                Object.Destroy(clone); Object.Destroy(model);
                await Task.Yield();
            }
            foreach (var type in new[] { AntColony.Data.ResourceType.Stone, AntColony.Data.ResourceType.Drum, AntColony.Data.ResourceType.GasCylinder })
            {
                var id = ProductionProp.ForResource(type);
                Check(Resources.Load<GameObject>("Kemono/Props/" + id) != null, "prop resource " + id);
            }
            Check(Resources.Load<Texture2D>("Kemono/Textures/Linen_Baked") != null, "baked linen texture imported");
            return "PASS " + checks + " kemono quality checks";
        }
        finally { Object.Destroy(root); }
    }
}
