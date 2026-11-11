from pathlib import Path
p=Path('AgentScripts/ReleaseVisualChecks.cs');s=p.read_text(encoding='utf-8-sig');needle='var report=new StringBuilder("Dressing objects: "+decorations.Length+"\\n");'
rep=needle+'''
        var terrain=AntColony.Map.MapGenerator.Active;
        var center=(Vector3)typeof(AntColony.Map.MapGenerator).GetField("flatCenter",Flags).GetValue(terrain);
        foreach(var decoration in decorations)
        {
            var delta=decoration.GetComponentsInChildren<Renderer>()[0].bounds.center-center;
            if(new Vector2(delta.x,delta.z).magnitude < 12f)throw new Exception("Dressing inside build core");
            if(decoration.GetComponentsInChildren<AntColony.World.ResourceNode>().Length>0)throw new Exception("Dressing changes stock");
        }
        report.AppendLine("Dressing: no enabled colliders, no resource components, central core clear");
'''
assert needle in s;s=s.replace(needle,rep);p.write_text(s,encoding='utf-8')
