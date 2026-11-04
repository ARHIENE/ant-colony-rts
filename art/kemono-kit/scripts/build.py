"""Run inside the live Blender; originals are read, never saved over."""
import bpy
import sys
import json
import importlib
from pathlib import Path
from mathutils import Vector, Quaternion

ROOT=Path('E:/Git/ant/art/kemono-kit')
sys.path.insert(0,str(ROOT/'scripts'))
import common,parts,animations
for m in [common,parts,animations]:importlib.reload(m)
for name in ['previews','exports']: (ROOT/name).mkdir(parents=True,exist_ok=True)
with bpy.data.libraries.load('E:/Git/ant/art/characters/kemono-con-face/rabbit-con-face.blend',link=False) as (a,b):
    b.scenes=list(a.scenes)
src=b.scenes[0]
bpy.context.window.scene=src;bpy.context.view_layer.update()
# Reference armature and morphs come from the already imported, posed reference.
ref_scene=bpy.data.scenes.get('Kiki_Reference_Study')
if not ref_scene:
    ref_scene=bpy.data.scenes.new('Kiki_Reference_Study');bpy.context.window.scene=ref_scene
    bpy.ops.import_scene.fbx(filepath='E:/Git/ant/Assets/Kiki/Xmas Girl/Model/Kiki-v2.fbx')
ref_rig=next(o for o in ref_scene.objects if o.type=='ARMATURE')
ref_face=next(o for o in ref_scene.objects if o.type=='MESH' and o.name.startswith('Face'))
bpy.context.window.scene=ref_scene
for pb in ref_rig.pose.bones:pb.matrix_basis.identity()
for side,angle in [('L',.40),('R',-.40)]:
    pb=ref_rig.pose.bones['upper_arm.'+side]
    axis=pb.bone.matrix_local.to_3x3().inverted()@Vector((0,1,0))
    pb.rotation_mode='QUATERNION';pb.rotation_quaternion=Quaternion(axis,angle)
bpy.context.view_layer.update()

s=bpy.data.scenes.new('Kemono_Character_Kit');bpy.context.window.scene=s
s.render.fps=24
collection=bpy.data.collections.new('Kemono_Parts');s.collection.children.link(collection)
rig=common.make_rig(ref_rig,s.collection,src)
slots,manifest,face_error=parts.create(s,src,ref_face,rig,collection)
actions,specs=animations.create(rig)

default={'Body':'BASE','Outfit':'LINEN','Face':'NEUTRAL','Hair':'BOB','Ears':'RABBIT','Tail':'BOB','Overlay':['BLUSH'],'Injury':[]}
def set_parts(selection):
    for slot,choices in slots.items():
        selected=selection.get(slot,[])
        if isinstance(selected,str):selected=[selected]
        for key,objs in choices.items():
            for o in objs:o.hide_render=key not in selected;o.hide_set(key not in selected)
set_parts(default)
s['slot_rules']='One Body, Face, Hair, Ears, Tail, Outfit; additive Overlay and Injury. No random generation.'

# Store animation library separately: a skeleton and actions, without geometry or scenery.
anim_scene=bpy.data.scenes.new('Kemono_Animation_Library')
animrig=rig.copy();animrig.data=rig.data.copy();anim_scene.collection.objects.link(animrig)
animrig.animation_data_create();animrig.animation_data.action=actions['Idle']
for act in actions.values():
    track=animrig.animation_data.nla_tracks.new();track.name=act.name;track.mute=True
    track.strips.new(act.name,1,act)
# Blender 5.2 crashes writing Scene IDs partially (BKE_view_layer_copy_data); write the rig, which pulls its actions.
bpy.data.libraries.write(str(ROOT/'kemono-animations.blend'),{animrig,*actions.values()},fake_user=True)

# Keep a studio-free parts library. Visibility switches are the manual assembly interface.
bpy.data.libraries.write(str(ROOT/'kemono-character-kit.blend'),{collection,rig},fake_user=True)

def copy_combo(name,selection,palette,location):
    r=rig.copy();r.data=rig.data.copy();r.name=name+'_Rig';s.collection.objects.link(r);r.location=location
    copied=[];mats={}
    for slot,choices in slots.items():
        selected=selection.get(slot,[])
        if isinstance(selected,str):selected=[selected]
        for key in selected:
            for original in choices[key]:
                o=original.copy();o.data=original.data.copy();o.name=name+'__'+original.name
                s.collection.objects.link(o);o.parent=r;o.hide_render=False;o.hide_set(False)
                for mod in o.modifiers:
                    if mod.type=='ARMATURE':mod.object=r
                if slot in ('Hair','Ears','Tail'):
                    for mat_slot in o.material_slots:
                        old=mat_slot.material
                        if not old or 'Inner' in old.name:continue
                        if old not in mats:
                            m=old.copy();p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
                            for socket in ['Base Color','Emission Color']:
                                for link in list(p.inputs[socket].links):m.node_tree.links.remove(link)
                                p.inputs[socket].default_value=(*common.PALETTES[palette],1)
                            mats[old]=m
                        mat_slot.material=mats[old]
                copied.append(o)
    r['selection']=json.dumps(selection);r['palette']=palette
    return r,copied

combos=[]
recipes=[
 ('A',default,'Ivory'),
 ('B',{**default,'Hair':'SHORT','Ears':'CAT','Tail':'LONG','Injury':['SCAR_CHEEK']},'Honey'),
 ('C',{**default,'Hair':'BUNS','Ears':'ROUND','Face':'HAPPY','Injury':['BANDAGE_ARM_L']},'Rose'),
 ('D',{**default,'Hair':'LONG','Ears':'DROOP','Tail':'FOX','Face':'TEARY','Overlay':['TEARS']},'Silver'),
 ('E',{**default,'Hair':'SIDE','Ears':'UPRIGHT','Face':'PROUD'},'Lavender'),
 ('F',{**default,'Hair':'PONY','Ears':'CAT','Tail':'LONG','Face':'NERVOUS','Overlay':['SWEAT']},'Mint')]
for i,(name,selection,palette) in enumerate(recipes):
    combos.append(copy_combo('Combo_'+name,selection,palette,((i-2.5)*.72,0,0)))
for o in collection.objects:o.hide_render=True;o.hide_set(True)
rig.hide_set(True);rig.hide_render=True

s.world=src.world.copy();s.render.engine='CYCLES';s.cycles.samples=24;s.cycles.use_denoising=True
s.render.resolution_x=1800;s.render.resolution_y=850;s.render.resolution_percentage=100
s.render.image_settings.file_format='PNG';s.view_settings.exposure=.3
for old in src.objects:
    if old.type=='LIGHT' or old.name.startswith('Settler_Studio'):
        o=old.copy();o.data=old.data.copy();s.collection.objects.link(o)
cam=src.camera.copy();cam.data=src.camera.data.copy();s.collection.objects.link(cam);s.camera=cam
cam.location=(0,-6,1.3);cam.rotation_euler=(Vector((0,0,.76))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=4.75
s.render.filepath=str(ROOT/'previews/combinations.png');bpy.ops.render.render(write_still=True)

# Use the same Action on every combination, not separate animation copies.
for r,objs in combos:
    r.animation_data_create();r.animation_data.action=actions['Walk'];r.animation_data.action_slot=actions['Walk'].slots[0]
s.frame_start=1;s.frame_end=25;s.frame_set(7)
s.render.filepath=str(ROOT/'previews/walk-frame.png');bpy.ops.render.render(write_still=True)
for other in [x for x in bpy.data.scenes if x!=s]:bpy.data.scenes.remove(other)
bpy.data.orphans_purge(do_recursive=True)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'kemono-preview.blend'),copy=True)

report={'face_source_max_error':face_error,'parts':len(manifest),'mesh_objects':sum(len(x['objects']) for x in manifest),
        'actions':len(actions),'combinations':len(combos),'unweighted_vertices':0,'invalid_weight_bones':[],
        'unity_validated':False,'facility_contacts_validated':False}
for o in collection.objects:
    for g in o.vertex_groups:
        if g.name not in rig.data.bones:report['invalid_weight_bones'].append(g.name)
    report['unweighted_vertices']+=sum(not any(g.weight>0 for g in v.groups) for v in o.data.vertices)
assert report['unweighted_vertices']==0 and not report['invalid_weight_bones'],report
# Bone fit: every deform bone sits on the vertices it moves, and no bone pokes outside the default character.
from mathutils.geometry import intersect_point_line
shown=[o for o in collection.objects if o['choice'] in (default['Body'],default['Outfit'],default['Face'],default['Hair'],default['Ears'],default['Tail'])]
world=[o.matrix_world@v.co for o in shown for v in o.data.vertices]
lo=Vector([min(v[i] for v in world) for i in range(3)])-Vector((.02,.02,.02));hi=Vector([max(v[i] for v in world) for i in range(3)])+Vector((.02,.02,.02))
heavy={}
for o in shown:
    names=[g.name for g in o.vertex_groups]
    for v in o.data.vertices:
        for g in v.groups:
            if g.weight>.3:heavy.setdefault(names[g.group],[]).append(o.matrix_world@v.co)
report['bones']=len(rig.data.bones);report['bone_offsets']={};report['bones_outside']=[]
for b in rig.data.bones:
    if not all(lo[i]<=p[i]<=hi[i] for p in (b.head_local,b.tail_local) for i in range(3)):report['bones_outside'].append(b.name)
    if b.name in ('head','Ear.L','Ear.R','Tail'):
        # Rigid parts pivot at their root; the root must touch the part, the centroid need not lie on the bone.
        d=min((p-b.head_local).length for p in heavy.get(b.name,[b.head_local]))
        if d>.03:report['bone_offsets'][b.name]=round(d,3)
    elif b.name in heavy:
        c=sum(heavy[b.name],Vector())/len(heavy[b.name]);_,f=intersect_point_line(c,b.head_local,b.tail_local)
        d=(c-b.head_local.lerp(b.tail_local,max(0,min(1,f)))).length
        if d>.03:report['bone_offsets'][b.name]=round(d,3)
assert not report['bones_outside'] and not report['bone_offsets'],report
assert all(r.animation_data.action is actions['Walk'] for r,objs in combos)
(ROOT/'validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
(ROOT/'parts-manifest.json').write_text(json.dumps({'slots':manifest,'default':default,'palettes':common.PALETTES},indent=2),encoding='utf-8')
(ROOT/'clips.json').write_text(json.dumps({n:{'frames':f,'loop':l,'fps':24} for n,(f,l) in specs.items()},indent=2),encoding='utf-8')
(ROOT/'combinations.json').write_text(json.dumps(recipes,indent=2),encoding='utf-8')
print(json.dumps(report))
