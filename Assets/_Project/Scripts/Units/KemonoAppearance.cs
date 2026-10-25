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
            new Color(.82f, .79f, .76f), new Color(.47f, .53f, .62f), new Color(.62f, .37f, .15f),
            new Color(.62f, .29f, .36f), new Color(.43f, .32f, .59f), new Color(.28f, .52f, .43f)
        };
        Renderer[] parts;
        Renderer[] plates;
        readonly List<(Renderer normal, Renderer armor)> armorPairs = new List<(Renderer, Renderer)>();
        string expression;
        bool armored;
        public int PaletteIndex { get; private set; }

        public void Initialize(int seed)
        {
            parts = GetComponentsInChildren<Renderer>(true);
            plates = parts.Where(r => r.name.StartsWith("Part_Equipment_Armor_")).ToArray();
            PaletteIndex = (int)(unchecked((uint)seed) % (uint)Palettes.Length);
            foreach (var r in parts)
            {
                if (r.name.StartsWith("Part_Hair_") || r.name.StartsWith("Part_Ears_") || r.name.StartsWith("Part_Tail_"))
                {
                    for (var i = 0; i < r.sharedMaterials.Length; i++)
                    {
                        var material = r.sharedMaterials[i];
                        if (material == null || material.name.IndexOf("Inner", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                        var block = new MaterialPropertyBlock();
                        r.GetPropertyBlock(block, i);
                        block.SetColor("_BaseColor", Palettes[PaletteIndex].gamma);
                        // Blender's combo replaces the entire hair/fur colour, including its texture.
                        block.SetTexture("_BaseMap", Texture2D.whiteTexture);
                        r.SetPropertyBlock(block, i);
                    }
                }
                if (!r.enabled || !r.name.EndsWith("_00")) continue;
                var alternative = parts.FirstOrDefault(p => p.name == r.name.Substring(0, r.name.Length - 3) + "_ARMOR");
                if (alternative != null) armorPairs.Add((r, alternative));
            }
            SetExpression("NEUTRAL");
        }

        public void SetArmor(bool value)
        {
            // Do not expose the cut-away body until the matching plates were imported.
            if (plates == null || plates.Length == 0) return;
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
            foreach (var r in parts)
                if (r.name.StartsWith("Part_Face_")) r.enabled = r.name.StartsWith("Part_Face_" + value + "_");
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
