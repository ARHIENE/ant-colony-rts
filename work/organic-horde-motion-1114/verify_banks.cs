using System;
using System.IO;
using System.Linq;
using UnityEngine;
using AntColony.World;
using UnityEditor;

public static class OrganicBanks1114Checks
{
    public static string Main()
    {
        int passed = 0;
        for (int id = 1; id <= 17; id++)
        {
            var kind = (OrganicEnemyKind)id;
            var bank = Resources.Load<OrganicMotionBank>("OrganicEnemies1114/" + kind + "/HordeMotion");
            if (bank == null || bank.mesh == null || bank.material == null || !bank.material.enableInstancing) throw new Exception("Incomplete bank " + kind);
            if (ShaderUtil.ShaderHasError(bank.material.shader)) throw new Exception("Shader compilation failure");
            if (bank.clips.Length != (id == 3 ? 6 : 5)) throw new Exception("Clip coverage " + kind);
            if (bank.mesh.vertexCount > bank.positions.width || bank.positions.height != bank.normals.height) throw new Exception("Texture layout " + kind);
            if (bank.animatedBounds.size.magnitude > 8 || bank.animatedBounds.min.y < -.02f) throw new Exception("Animated bounds " + kind + " " + bank.animatedBounds);
            foreach (var clip in bank.clips)
                if (clip.frames < 3 || clip.seconds <= 0 || clip.first + clip.frames > bank.positions.height) throw new Exception("Clip frames " + kind);
            if (Mathf.Abs(bank.Frame(4, 2) - bank.Frame(4, 1)) > .0001f) throw new Exception("Death must hold its final pose");
            if (Mathf.Abs(bank.Frame(1, 0) - bank.Frame(1, 1)) > .0001f) throw new Exception("Move must loop");
            if (bank.Frame(1, .1f, 0) == bank.Frame(1, .1f, .4f)) throw new Exception("Independent body phase");
            var rt = RenderTexture.GetTemporary(bank.positions.width, bank.positions.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            var old = RenderTexture.active; Texture2D readable = null;
            try
            {
                Graphics.Blit(bank.positions, rt); RenderTexture.active = rt;
                readable = new Texture2D(rt.width, rt.height, TextureFormat.RGBAFloat, false, true);
                readable.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); readable.Apply();
                var pixels = readable.GetPixels();
                foreach (var clip in bank.clips)
                {
                    float delta = 0; int a = clip.first * rt.width; int b = (clip.first + Math.Max(1, clip.frames / 4)) * rt.width;
                    for (int i = 0; i < bank.mesh.vertexCount; i++)
                    {
                        var x = pixels[a + i]; var y = pixels[b + i];
                        delta = Mathf.Max(delta, new Vector3(x.r-y.r,x.g-y.g,x.b-y.b).magnitude);
                    }
                    if (delta < .0001f) throw new Exception("Frozen baked motion " + kind + "/" + clip.name);
                }
            }
            finally { RenderTexture.active = old; RenderTexture.ReleaseTemporary(rt); if (readable != null) UnityEngine.Object.DestroyImmediate(readable); }
            passed++;
        }
        string result = "PASS " + passed + " species: complete animation banks, shader, clip bounds, loop/death hold and per-body phases";
        File.WriteAllText("work/organic-horde-motion-1114/bank-checks.txt",result);
        return result;
    }
}
