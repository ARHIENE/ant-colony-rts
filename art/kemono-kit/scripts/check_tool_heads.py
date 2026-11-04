import bpy,json
from pathlib import Path
from mathutils.bvhtree import BVHTree
def tree(obj,dg):
    ev=obj.evaluated_get(dg);me=ev.to_mesh()
    result=BVHTree.FromPolygons([ev.matrix_world@v.co for v in me.vertices],[list(p.vertices) for p in me.polygons]);ev.to_mesh_clear();return result
hits=[];checked=0
for scene in bpy.data.scenes:
    tools=[o for o in scene.objects if o.get('contact_ik') or o.name.startswith('Prop_Spear.')]
    if not tools:continue
    bpy.context.window_manager.windows[0].scene=scene
    for frame in range(1,scene.frame_end+1):
        scene.frame_set(frame);dg=bpy.context.evaluated_depsgraph_get()
        for tool in tools:
            if not tool.parent:continue
            tt=tree(tool,dg)
            for obj in tool.parent.children:
                if obj.type!='MESH' or not any(k in obj.name for k in ['Part_Hair','Part_Face','Part_Ears']):continue
                pairs=tt.overlap(tree(obj,dg))
                if pairs:hits.append({'scene':scene.name,'frame':frame,'part':obj.name,'triangles':len(pairs)})
            checked+=1
result={'checked':checked,'hits':hits}
Path('E:/Git/ant/art/kemono-kit/tool-head-validation.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print('HEAD_CHECK',checked,'hits',len(hits),'scenes',sorted(set(x['scene'] for x in hits)))
