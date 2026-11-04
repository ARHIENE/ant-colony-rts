"""Run this file in Blender to register the Resident generator sidebar."""
import bpy,json,sys,math
from pathlib import Path
from mathutils import Vector
HERE=Path(__file__).resolve().parent
sys.path.insert(0,str(HERE));import recipes
COLLECTION='Generated_Residents'
PALETTES=[(.98,.88,.69),(.90,.65,.44),(.67,.76,.54),(.69,.62,.83),(.59,.75,.88),(.95,.69,.67),(.62,.49,.38),(.81,.79,.71)]
def source(num,prefixes):
    root=bpy.data.objects.get('Resident_%02d'%num)
    if root is None:raise ValueError('Original resident library is missing')
    return [o for o in root.children if o.type=='MESH' and o.name.startswith(prefixes)]
def material(name,color):
    mat=bpy.data.materials.get(name)
    if mat:return mat
    mat=bpy.data.materials.new(name);mat.use_nodes=True;mat.diffuse_color=(*color,1)
    node=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED');node.inputs['Base Color'].default_value=(*color,1);node.inputs['Roughness'].default_value=.85
    return mat
def selection(recipe):
    groups=[source(1,('V4_Body','V4_Paw','V4_Foot','Tail'))]
    ear_id={'round':2,'short':3,'long':1,'point':4,'upright':7}[recipe['ears']]
    groups.append(source(ear_id,('EarRound','EarInner','EarPoint','EarFlop')))
    if recipe['crown']=='tuft':groups.append(source(1,('CrownTuft',)))
    if recipe['crown']=='leaves':groups.append(source(5,('Leaf',)))
    if recipe['pattern']!='none':groups.append(source({'blaze_a':2,'blaze_b':4,'blaze_c':9}[recipe['pattern']],('FaceBlaze',)))
    face_id={'calm':1,'confident':8,'happy':22,'wink':19}[recipe['expression']]
    groups.append(source(face_id,('V4_Eye','V4_Blush','V4_Mouth','ConfidentBrow','HappyEye','OpenSmile','Tongue')))
    if recipe['clothing']!='none':groups.append(source(1,('ShortCape','Collar')))
    assert all(groups),'Missing source parts'
    return [o for group in groups for o in group]
def build(items):
    recipes.validate(items)
    # Prepare and validate before replacing only this generator's own output.
    selected=[selection(r) for r in items]
    scene=bpy.data.scenes.get('Resident_Generator_Studio')
    if not scene:
        scene=bpy.data.scenes.new('Resident_Generator_Studio');scene.world=bpy.context.scene.world.copy()
        light=bpy.data.lights.new('GeneratorSoftbox','AREA');light.energy=2500;light.size=15;light.use_shadow=False
        ob=bpy.data.objects.new('GeneratorSoftbox',light);scene.collection.objects.link(ob);ob.location=(0,-8,8);ob.rotation_euler=(Vector((0,0,4))-ob.location).to_track_quat('-Z','Y').to_euler()
        cam=bpy.data.objects.new('GeneratorCamera',bpy.data.cameras.new('GeneratorCamera'));scene.collection.objects.link(cam);scene.camera=cam;cam.data.type='ORTHO'
    bpy.context.window.scene=scene
    coll=bpy.data.collections.get(COLLECTION)
    if coll:
        for o in list(coll.objects):
            data=o.data;bpy.data.objects.remove(o,do_unlink=True)
            if isinstance(data,bpy.types.Mesh) and data.users==0:bpy.data.meshes.remove(data)
    else:coll=bpy.data.collections.new(COLLECTION);scene.collection.children.link(coll)
    rows=math.ceil(len(items)/6)
    for idx,(recipe,parts) in enumerate(zip(items,selected)):
        root=bpy.data.objects.new('Generated_%02d'%(idx+1),None);coll.objects.link(root);root['recipe']=json.dumps(recipe,sort_keys=True)
        root.location=((idx%6-2.5)*2.6,0,(rows-1-idx//6)*2.9)
        col=PALETTES[recipe['palette']];fur=material('GenFur_%d'%recipe['palette'],col);inner=material('GenInner_%d'%recipe['palette'],tuple(c*.65+.35 for c in col))
        factor={'round':1,'soft_oval':.94,'wide_cheek':1.045}[recipe['shape']]
        for original in parts:
            o=original.copy();o.data=original.data.copy();coll.objects.link(o);o.parent=root;o.name='Part_'+original.name
            for i,mat in enumerate(o.data.materials):
                if mat.name.startswith('Fur_'):o.data.materials[i]=fur
                elif mat.name.startswith('Inner_'):o.data.materials[i]=inner
                elif original.name.startswith(('ShortCape','Collar')) and mat.name in ('SageCloth','BlueCloth','RustCloth'):
                    o.data.materials[i]=bpy.data.materials[{'sage':'SageCloth','blue':'BlueCloth','rust':'RustCloth'}[recipe['clothing']]]
            # Apply the same transform to face, markings, ears and head, preserving alignment.
            for v in o.data.vertices:
                z=(o.matrix_local@v.co).z
                t=max(0,min(1,(z-.75)/.35));v.co.x*=1+(factor-1)*t
            if original.location.z>.85:o.location.x*=factor
        root['part_count']=len(parts)
    scene['resident_recipes']=json.dumps(items,sort_keys=True)
    cam=scene.camera;cam.location=(0,-30,(rows*2.9-.6)/2);cam.rotation_euler=(math.pi/2,0,0);cam.data.ortho_scale=16.2
    scene.render.resolution_x=1800;scene.render.resolution_y=int(1800*rows*2.9/16.2);scene.render.resolution_percentage=100
    for area in bpy.context.screen.areas:
        if area.type=='VIEW_3D':area.spaces.active.region_3d.view_perspective='CAMERA'
    return scene
def save_json(path):
    items=json.loads(bpy.context.scene['resident_recipes']);recipes.validate(items)
    Path(path).write_text(json.dumps({'version':1,'residents':items},ensure_ascii=False,indent=2),encoding='utf-8')
def load_json(path):
    data=json.loads(Path(path).read_text(encoding='utf-8'))
    if data.get('version')!=1:raise ValueError('Unsupported recipe version')
    return build(recipes.validate(data['residents']))
class RESIDENT_OT_generate(bpy.types.Operator):
    bl_idname='resident.generate';bl_label='Generate / Regenerate'
    def execute(self,context):
        wm=context.window_manager
        try:build(recipes.generate(wm.resident_seed,wm.resident_count))
        except (ValueError,AssertionError) as e:self.report({'ERROR'},str(e));return {'CANCELLED'}
        return {'FINISHED'}
class RESIDENT_OT_save(bpy.types.Operator):
    bl_idname='resident.save';bl_label='Save recipes JSON'
    def execute(self,context):
        try:save_json(bpy.path.abspath(context.window_manager.resident_path))
        except (OSError,ValueError,KeyError) as e:self.report({'ERROR'},str(e));return {'CANCELLED'}
        return {'FINISHED'}
class RESIDENT_OT_load(bpy.types.Operator):
    bl_idname='resident.load';bl_label='Load recipes JSON'
    def execute(self,context):
        try:load_json(bpy.path.abspath(context.window_manager.resident_path))
        except (OSError,ValueError,KeyError,AssertionError) as e:self.report({'ERROR'},str(e));return {'CANCELLED'}
        return {'FINISHED'}
class RESIDENT_PT_panel(bpy.types.Panel):
    bl_label='Resident combinations';bl_idname='RESIDENT_PT_panel';bl_space_type='VIEW_3D';bl_region_type='UI';bl_category='Residents'
    def draw(self,context):
        l=self.layout;wm=context.window_manager;l.prop(wm,'resident_seed');l.prop(wm,'resident_count');l.operator('resident.generate');l.prop(wm,'resident_path');l.operator('resident.save');l.operator('resident.load')
def register():
    for cls in (RESIDENT_OT_generate,RESIDENT_OT_save,RESIDENT_OT_load,RESIDENT_PT_panel):
        old=getattr(bpy.types,cls.__name__,None)
        if old:bpy.utils.unregister_class(old)
        bpy.utils.register_class(cls)
    bpy.types.WindowManager.resident_seed=bpy.props.IntProperty(name='Seed',default=1018,min=0)
    bpy.types.WindowManager.resident_count=bpy.props.IntProperty(name='Count',default=24,min=1,max=48)
    bpy.types.WindowManager.resident_path=bpy.props.StringProperty(name='Recipe file',subtype='FILE_PATH',default=str(HERE/'recipes.json'))
if __name__=='__main__':register()
