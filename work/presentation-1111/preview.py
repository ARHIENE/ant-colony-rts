from pathlib import Path
s=Path('work/presentation-1111/draft/CharacterPortraits.cs').read_text(encoding='utf-8-sig')
s=s.replace('class CharacterPortraits','class PortraitPreview1111')
s=s.replace('                if (UnityEngine.Camera.main != null) UnityEngine.Camera.main.cullingMask &= ~(1 << Layer);','')
start=s.index('        public const int Layer')
s=s[:start]+'''        public static string Run()
        {
            string dir="Temp/presentation-1111/neutral-preview";
            System.IO.Directory.CreateDirectory(dir);int i=0;
            try
            {
                foreach(var c in AntColony.Core.CommanderRoster.Instance.Commanders.Where(c=>c!=null))
                {
                    var rt=Get(c) as RenderTexture;
                    var prev=RenderTexture.active;
                    var tex=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);
                    try { RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);tex.Apply();System.IO.File.WriteAllBytes(dir+"/portrait-"+(i++)+".png",tex.EncodeToPNG()); }
                    finally { RenderTexture.active=prev;Object.Destroy(tex); }
                }
                return "Rendered "+i+" studio-only neutral portraits; world state unchanged";
            }
            finally
            {
                foreach(var rt in cache.Values) {rt.Release();Object.Destroy(rt);}cache.Clear();
                if(camera!=null)Object.Destroy(camera.gameObject);
            }
        }
''' +s[start:]
Path('AgentScripts/PortraitPreview1111.cs').write_text(s,encoding='utf-8')
