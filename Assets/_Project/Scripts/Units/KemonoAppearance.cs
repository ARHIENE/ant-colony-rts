using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AntColony.Units
{
    // Appearance only: never changes equipment stats or animation timing.
    public sealed class KemonoAppearance : MonoBehaviour
    {
        static readonly Color[] Palettes = {
            new Color(.47f, .54f, .61f), new Color(.70f, .48f, .19f), new Color(.65f, .31f, .36f),
            new Color(.33f, .52f, .36f), new Color(.48f, .37f, .60f), new Color(.32f, .39f, .47f)
        };
        Renderer[] parts;
        Renderer[] plates;
        readonly List<(Renderer normal, Renderer armor)> armorPairs = new List<(Renderer, Renderer)>();
        string expression;
        bool armored;
        string outfitBeforeArmor;
        readonly List<(SkinnedMeshRenderer renderer, int index, string name)> facialShapes = new List<(SkinnedMeshRenderer, int, string)>();
        float blinkClock;
        float blinkInterval;
        public int PaletteIndex { get; private set; }

        public void Initialize(int seed)
        {
            parts = GetComponentsInChildren<Renderer>(true);
            armorPairs.Clear();
            CacheFacialShapes();
            blinkInterval = 2.8f + (unchecked((uint)seed) % 17) * .13f;
            blinkClock = (unchecked((uint)seed) % 11) * .17f;
            plates = parts.Where(r => r.name.StartsWith("Part_Equipment_Armor_")).ToArray();
            PaletteIndex = (int)(unchecked((uint)seed) % (uint)Palettes.Length);
            SetPaletteColor(Palettes[PaletteIndex]);
            foreach (var r in parts)
            {
                if (!r.enabled || !r.name.EndsWith("_00")) continue;
                var alternative = parts.FirstOrDefault(p => p.name == r.name.Substring(0, r.name.Length - 3) + "_ARMOR");
                if (alternative != null) armorPairs.Add((r, alternative));
            }
            SetExpression("NEUTRAL");
        }

        // Starting appearance colours fur/hair only; eyes, skin, cloth and equipment keep their materials.
        public void SetPaletteColor(Color color)
        {
            if (parts == null) parts = GetComponentsInChildren<Renderer>(true);
            foreach (var r in parts)
            {
                if (!r.name.StartsWith("Part_Hair_") && !r.name.StartsWith("Part_Ears_") && !r.name.StartsWith("Part_Tail_")) continue;
                for (var i = 0; i < r.sharedMaterials.Length; i++)
                {
                    var material = r.sharedMaterials[i];
                    if (material == null || material.name.IndexOf("Inner", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                    var block = new MaterialPropertyBlock(); r.GetPropertyBlock(block, i);
                    block.SetColor("_BaseColor", color.gamma);
                    // Meshy's UV texture carries strand shading and must survive palette changes.
                    if (!material.name.StartsWith("Meadow_", StringComparison.Ordinal))
                        block.SetTexture("_BaseMap", Texture2D.whiteTexture);
                    r.SetPropertyBlock(block, i);
                }
            }
        }

        public void SetArmor(bool value)
        {
            // Do not expose the cut-away body until the matching plates were imported.
            if (plates == null || plates.Length == 0)
            {
                if (armored == value || !GetVariants("Outfit").Contains("GUARD")) return;
                if (value)
                {
                    outfitBeforeArmor = parts.FirstOrDefault(r => r.enabled && r.name.StartsWith("Part_Outfit_"))?.name.Split('_')[2];
                    SetVariant("Outfit", "GUARD");
                }
                else if (outfitBeforeArmor != null) SetVariant("Outfit", outfitBeforeArmor);
                armored = value;
                return;
            }
            if (armored == value) return;
            armored = value;
            foreach (var pair in armorPairs) { pair.normal.enabled = !value; pair.armor.enabled = value; }
            foreach (var plate in plates) plate.enabled = value;
        }

        public void SetExpression(string value)
        {
            if (expression == value) return;
            expression = value;
            if (parts == null) parts = GetComponentsInChildren<Renderer>(true); // Death clones retain renderer visibility, not managed caches.
            if (facialShapes.Count == 0) CacheFacialShapes();
            foreach (var r in parts)
                if (r.name.StartsWith("Part_Face_"))
                    r.enabled = (r is SkinnedMeshRenderer skin && skin.sharedMesh != null && skin.sharedMesh.blendShapeCount > 0)
                        || r.name.StartsWith("Part_Face_" + value + "_");
        }

        public string[] GetVariants(string slot)
        {
            if (parts == null) parts = GetComponentsInChildren<Renderer>(true);
            return parts.Where(r => r.name.StartsWith("Part_" + slot + "_", StringComparison.Ordinal) && !r.name.Contains("_ARMOR"))
                .Select(r => r.name.Split('_')[2]).Distinct().OrderBy(n => n).ToArray();
        }

        // Switching is visual only; an unknown option never hides the current selection.
        public bool SetVariant(string slot, string option)
        {
            if (slot != "Hair" && slot != "Outfit") return false;
            if (!GetVariants(slot).Contains(option)) return false;
            foreach (var r in parts)
                if (r.name.StartsWith("Part_" + slot + "_", StringComparison.Ordinal) && !r.name.Contains("_ARMOR"))
                    r.enabled = r.name.StartsWith("Part_" + slot + "_" + option + "_", StringComparison.Ordinal);
            return true;
        }

        void CacheFacialShapes()
        {
            facialShapes.Clear();
            foreach (var skin in parts.OfType<SkinnedMeshRenderer>())
            {
                if (!skin.name.StartsWith("Part_Face_") || skin.sharedMesh == null) continue;
                for (var i = 0; i < skin.sharedMesh.blendShapeCount; i++)
                {
                    var name = skin.sharedMesh.GetBlendShapeName(i);
                    facialShapes.Add((skin, i, name.Substring(name.LastIndexOf('.') + 1).ToUpperInvariant()));
                }
            }
        }

        void LateUpdate()
        {
            if (facialShapes.Count == 0) return;
            var dt = Time.deltaTime;
            blinkClock += dt;
            if (blinkInterval <= 0) blinkInterval = 3.7f;
            if (blinkClock > blinkInterval) blinkClock %= blinkInterval;
            var blink = blinkClock < .18f ? Mathf.Sin(blinkClock / .18f * Mathf.PI) * 100f : 0f;
            var target = expression switch {
                "DEAD" or "SLEEP" or "SLEEPY" => "SLEEP", "TASTY" => "SMILE", _ => expression
            };
            foreach (var shape in facialShapes)
            {
                var weight = shape.name == target ? 100f : 0f;
                if (shape.name == "BLINK" && target != "SLEEP" && target != "LAUGH" && target != "PAIN") weight = blink;
                var current = shape.renderer.GetBlendShapeWeight(shape.index);
                shape.renderer.SetBlendShapeWeight(shape.index, Mathf.MoveTowards(current, weight, dt * 1100f));
            }
        }

        public void FollowClip(string clip)
        {
            SetExpression(clip switch {
                "Death" => "DEAD", "Hit" or "Hurt" or "Downed" or "SelfTreat" => "PAIN",
                "Sleep" => "SLEEP", "Eat" => "TASTY", "Captive" or "Mourn" => "SAD",
                "Rest" or "Bathe" => "SLEEPY", _ => "NEUTRAL"
            });
        }
    }
}
