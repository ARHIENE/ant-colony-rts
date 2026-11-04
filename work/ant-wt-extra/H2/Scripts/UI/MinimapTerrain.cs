using AntColony.Map;
using UnityEngine;

namespace AntColony.UI
{
    // 장식·날씨 없이 실제 지형 격자의 높이와 절벽만 읽기 쉽게 표시한다.
    public static class MinimapTerrain
    {
        public static Texture2D Build(MapGenerator terrain, Mesh mesh, Rect bounds)
        {
            const int size = 384;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var vertices = mesh.vertices;
            int w = terrain.BaseXSize + 1, h = terrain.BaseZSize + 1;
            var material = terrain.GetComponent<MeshRenderer>().sharedMaterial;
            float baseY = material.GetFloat("terraceBase"), step = Mathf.Max(.01f, material.GetFloat("terraceStep"));
            float extent = Mathf.Max(bounds.width, bounds.height);
            var pixels = new Color[size * size];
            for (int z = 0; z < size; z++)
                for (int x = 0; x < size; x++)
                {
                    var world = new Vector3(bounds.center.x + ((x + .5f) / size - .5f) * extent, 0, bounds.center.y + ((z + .5f) / size - .5f) * extent);
                    var p = terrain.transform.InverseTransformPoint(world);
                    var color = MenuTheme.Well;
                    if (p.x >= 0 && p.x <= w - 1 && p.z >= 0 && p.z <= h - 1)
                    {
                        int ix = Mathf.Clamp(Mathf.RoundToInt(p.x), 0, w - 1), iz = Mathf.Clamp(Mathf.RoundToInt(p.z), 0, h - 1), i = iz * w + ix;
                        float y = terrain.transform.TransformPoint(vertices[i]).y;
                        float level = (y - baseY) / step;
                        color = level < -.3f ? MenuTheme.Hex(0x315f7c) : Color.Lerp(MenuTheme.Hex(0x52634a), MenuTheme.Hex(0xc5bb8d), Mathf.Clamp01(level / 2));
                        float slope = Mathf.Max(Mathf.Abs(vertices[i].y - vertices[iz * w + Mathf.Min(ix + 1, w - 1)].y), Mathf.Abs(vertices[i].y - vertices[Mathf.Min(iz + 1, h - 1) * w + ix].y));
                        if (slope > step * .6f) color = MenuTheme.Hex(0x30382d);
                    }
                    pixels[z * size + x] = color;
                }
            texture.SetPixels(pixels); texture.Apply();
            return texture;
        }
    }
}
