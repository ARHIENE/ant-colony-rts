using System.Linq;
using AntColony.Core;
using AntColony.Map;
using UnityEngine;

namespace AntColony.World
{
    // The central 70m retains the saved landing/building/path heights; only the outer skirt slopes.
    public sealed class ExpeditionTerrain : MonoBehaviour
    {
        private ExpeditionSite site;
        private BiomeMapStyle style;
        private Mesh mesh;
        private Material material;
        private Texture2DArray textures;
        private static readonly float[] Heights = { 0, .35f, .75f };

        public static GameObject Create(ExpeditionSite site)
        {
            var ground = new GameObject(site.Title + " Ground");
            ground.transform.SetParent(site.transform, false);
            ground.layer = 8;
            var box = ground.AddComponent<BoxCollider>();
            box.center = Vector3.down * .25f;
            box.size = new Vector3(70, .5f, 70);
            var terrain = ground.AddComponent<ExpeditionTerrain>();
            terrain.site = site;
            terrain.style = BiomeMapStyle.For(site.Biome);
            terrain.Build();
            return ground;
        }

        private void Build()
        {
            const int count = 17;
            var points = new Vector3[count * count];
            var triangles = new int[(count - 1) * (count - 1) * 6];
            float Coordinate(int i) => i == 0 ? -39 : i == count - 1 ? 39 : -35 + (i - 1) * 5;
            for (var z = 0; z < count; z++)
                for (var x = 0; x < count; x++)
                {
                    var edge = x == 0 || z == 0 || x == count - 1 || z == count - 1;
                    var depth = 1.2f + Mathf.PerlinNoise(x * .41f + (int)site.Biome, z * .37f) * 1.8f;
                    points[z * count + x] = new Vector3(Coordinate(x), edge ? -depth : 0, Coordinate(z));
                }
            var t = 0;
            for (var z = 0; z < count - 1; z++)
                for (var x = 0; x < count - 1; x++)
                {
                    var a = z * count + x;
                    triangles[t++] = a; triangles[t++] = a + count; triangles[t++] = a + 1;
                    triangles[t++] = a + 1; triangles[t++] = a + count; triangles[t++] = a + count + 1;
                }
            mesh = new Mesh { name = site.Biome + " Expedition Ground", vertices = points, triangles = triangles };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            material = new Material(Shader.Find("AntColony/TerrainBlend")) { name = site.Biome + " Expedition Terrain" };
            material.SetFloat("_textureScale", 8);
            BuildTextures();
            gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
            // Display-only props stay beyond the walkable boundary. They never create stock or obstacles.
            var ids = site.Biome switch
            {
                MapBiome.Forest => new[] { "CC_TreeTall", "CC_TreeBroadleaf", "CC_Bush" },
                MapBiome.Garden => new[] { "CC_Bush", "CC_GrassTuft" },
                MapBiome.Waterside => new[] { "CC_GrassTuft", "CC_RockOutcrop" },
                MapBiome.City => new[] { "CC_DemolitionRubble", "CC_RockOutcrop" },
                _ => new[] { "CC_RockOutcrop" }
            };
            var decor = new GameObject("Expedition Scenery"); decor.transform.SetParent(transform, false);
            for (var i = 0; i < 16; i++)
            {
                var side = i / 4; var offset = -27 + i % 4 * 18;
                var at = side < 2 ? new Vector3(side == 0 ? -36.5f : 36.5f, -.6f, offset)
                    : new Vector3(offset, -.6f, side == 2 ? -36.5f : 36.5f);
                var id = ids[i % ids.Length];
                var width = id.Contains("Tree") ? 4f : id == "CC_GrassTuft" ? 1.6f : 2.8f;
                ProductionProp.Attach(decor.transform, ResourceVisual.Folder + id, width, transform.TransformPoint(at));
            }
            LateUpdate();
        }

        private void BuildTextures()
        {
            var layers = style != null ? style.layers.Where(l => l.texture != null).ToArray() : new MapGenerator.Layer[0];
            textures = new Texture2DArray(128, 128, 3, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat };
            var resized = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            var target = RenderTexture.GetTemporary(128, 128);
            var previous = RenderTexture.active;
            try
            {
                for (var i = 0; i < 3; i++)
                {
                    var texture = layers.Length > 0 ? layers[Mathf.Min(i + 1, layers.Length - 1)].texture : Texture2D.whiteTexture;
                    Graphics.Blit(texture, target); RenderTexture.active = target;
                    resized.ReadPixels(new Rect(0, 0, 128, 128), 0, 0);
                    textures.SetPixels(resized.GetPixels(), i);
                }
                textures.Apply();
            }
            finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); Destroy(resized); }
        }

        private void LateUpdate()
        {
            if (material == null) return;
            // Rebind non-Properties uniforms after a shader reimport, as the home terrain does.
            material.SetTexture("terrainTextures", textures); material.SetInt("numTextures", 3);
            material.SetFloatArray("terrainHeights", Heights);
            material.SetFloat("minTerrainHeight", transform.position.y - 1);
            material.SetFloat("maxTerrainHeight", transform.position.y + 1);
            material.SetFloat("terraceStep", 0);
            var climate = GameSession.Exists ? GameSession.Instance.Options.climate : MapClimate.Temperate;
            material.SetColor("_Tint", BiomeRules.GroundTint(site.Biome) * SeasonVisuals.GroundSeasonTint(site.Biome, climate));
            var p = SeasonVisuals.Position();
            var snow = Mathf.Lerp(p.season == 3 ? 1 : 0, p.next == 3 ? 1 : 0, p.blend);
            var leaves = Mathf.Lerp(p.season == 2 ? 1 : 0, p.next == 2 ? 1 : 0, p.blend);
            material.SetTexture("_SnowTex", style != null ? style.snowGround : null);
            material.SetTexture("_LeafTex", style != null ? style.leafGround : null);
            material.SetFloat("_Snow", style != null && style.snowGround != null && SeasonVisuals.HasSeasonalSnow(site.Biome, climate) ? snow : 0);
            material.SetFloat("_Leaves", style != null && style.leafGround != null && SeasonVisuals.HasSeasonalLeaves(site.Biome, climate) ? leaves : 0);
        }

        private void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);
            if (material != null) Destroy(material);
            if (textures != null) Destroy(textures);
        }
    }
}
