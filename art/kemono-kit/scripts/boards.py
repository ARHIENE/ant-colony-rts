"""Render selectable parts, not a runtime generator. No Unity writes."""
import bpy
import sys
import math
import json
from pathlib import Path
from mathutils import Vector, Matrix
ROOT=Path('E:/Git/ant/art/kemono-kit')
sys.path.insert(0,str(ROOT/'scripts'))
from common import copy_mesh,mat,PALETTES
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'kemono-preview.blend'))
src=bpy.context.scene
rig=next(o for o in src.objects if o.name=='KemonoRig')
rig.data.pose_position='REST'
src.frame_set(1)
bpy.context.view_layer.update()
parts={}
for o in src.objects:
    if o.name.startswith('Part_'):
        parts.setdefault(o['slot'],{}).setdefault(o['choice'],[]).append(o)
font=bpy.data.fonts.load('C:/Windows/Fonts/malgun.ttf')
ink=mat('BoardType',(.12,.10,.09))
labels={
 'BOB':'기본 단발','SHORT':'짧은 단발','LONG':'긴 단발','SIDE':'반대 가르마','BUNS':'양쪽 묶음','PONY':'뒤로 묶음',
 'RABBIT':'접힌 토끼귀','UPRIGHT':'선 토끼귀','CAT':'뾰족귀','ROUND':'둥근 귀','DROOP':'늘어진 귀',
 'NEUTRAL':'기본','HAPPY':'기쁨','SURPRISE':'놀람','ANGRY':'화남','PAIN':'아픔','SLEEPY':'졸림','SLEEP':'잠듦',
 'SAD':'풀이 죽음','PROUD':'뿌듯함','CURIOUS':'호기심','EMBARRASSED':'당황','TASTY':'맛있음','EMPTY':'공허함',
 'TEARY':'울먹임','SOBBING':'울음','NERVOUS':'긴장','DEAD':'사망',
 'BLUSH':'홍조','TEARS':'눈물','SWEAT':'땀',
 'Ivory':'아이보리','Silver':'은회색','Honey':'꿀색','Rose':'분홍','Lavender':'연보라','Mint':'민트'}
boards=[]
only=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
def text(s,label,x,z,size=.045):
    c=bpy.data.curves.new('Label','FONT');c.body=label;c.font=font;c.size=size;c.align_x='CENTER'
    o=bpy.data.objects.new('Label',c);s.collection.objects.link(o)
    o.location=(x,-.65,z);o.rotation_euler=(math.pi/2,0,0);c.materials.append(ink)

def board(name,title,items,cols,stepx,stepz,center=.92,crop=.60,full=False):
    s=bpy.data.scenes.new('Board_'+name);bpy.context.window.scene=s
    s.world=src.world.copy();s.render.engine='CYCLES';s.cycles.samples=24;s.cycles.use_denoising=True
    s.render.image_settings.file_format='PNG';s.view_settings.exposure=.3
    rows=math.ceil(len(items)/cols);width=cols*stepx+.18;height=rows*stepz+.22
    s.render.resolution_x=1800;s.render.resolution_y=round(1800*height/width);s.render.resolution_percentage=100
    for old in src.objects:
        if old.type=='LIGHT':
            o=old.copy();o.data=old.data.copy();s.collection.objects.link(o)
    for i,(label,selection,palette) in enumerate(items):
        x=(i%cols-(cols-1)/2)*stepx;z=(rows-1-i//cols)*stepz
        selected=[]
        for slot,choice in selection.items():
            if isinstance(choice,str):choice=[choice]
            for key in choice:selected+=parts[slot][key]
        for old in selected:
            old.hide_set(False,view_layer=src.view_layers[0]);old.hide_render=False
            o=copy_mesh(old,'Display_'+old.name,s.collection)
            if name=='tails':
                pivot=Vector((0,.20,.45));rotation=Matrix.Rotation(math.radians(65),3,'Z')
                for vertex in o.data.vertices:vertex.co=pivot+rotation@(vertex.co-pivot)
            if not full:
                # Parts boards show busts without shoulders or boots.
                pass
            o.location=(x,0,z-center)
            if palette and old['slot'] in ('Hair','Ears','Tail'):
                for slot in o.material_slots:
                    if not slot.material or 'Inner' in slot.material.name:continue
                    m=slot.material.copy();p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
                    for socket in ['Base Color','Emission Color']:
                        for link in list(p.inputs[socket].links):m.node_tree.links.remove(link)
                        p.inputs[socket].default_value=(*PALETTES[palette],1)
                    slot.material=m
            old.hide_set(True,view_layer=src.view_layers[0]);old.hide_render=True
        text(s,label,x,z-crop/2-.065)
    text(s,title,0,(rows-1)*stepz+crop/2+.08,.057)
    camdata=bpy.data.cameras.new('BoardCamera');cam=bpy.data.objects.new('BoardCamera',camdata);s.collection.objects.link(cam)
    cam.location=(0,-8,(rows-1)*stepz/2);cam.rotation_euler=(math.pi/2,0,0);camdata.type='ORTHO';camdata.ortho_scale=max(width,height)
    s.camera=cam;s.render.filepath=str(ROOT/'previews'/(name+'.png'))
    if not only or name in only:bpy.ops.render.render(write_still=True)
    boards.append(s)
    return s

head={'Face':'NEUTRAL','Hair':'BOB','Overlay':['BLUSH']}
board('hair','머리 모양 · 얼굴은 동일',[(labels[k],{**head,'Hair':k},None) for k in parts['Hair']],3,.60,.74)
board('hair-parts','독립 머리 부품 · 얼굴 제외',[(labels[k],{'Hair':k},None) for k in parts['Hair']],3,.60,.74)
board('ears','귀 부품',[(labels[k],{**head,'Ears':k},None) for k in parts['Ears']],3,.65,.93,center=1.04,crop=.83)
board('ear-parts','독립 귀 부품 · 머리와 얼굴 제외',[(labels[k],{'Ears':k},None) for k in parts['Ears']],3,.65,.80,center=1.16,crop=.65)
board('expressions','표정 · 얼굴 부품 교체',[(labels[k],{'Face':k,'Hair':'BOB','Overlay':(['TEARS'] if k in ('TEARY','SOBBING') else ['SWEAT'] if k=='NERVOUS' else ['BLUSH'])},None) for k in parts['Face']],5,.55,.68,center=.91,crop=.54)
board('colors','머리·귀·꼬리 색상',[(labels[k],{**head,'Ears':'CAT'},k) for k in PALETTES],3,.65,.82,center=.99,crop=.69)
board('overlays','표정 위에 겹치는 부품',[(labels[k],{'Face':'NEUTRAL','Hair':'BOB','Overlay':[k]},None) for k in parts['Overlay']],3,.60,.74)
board('tails','꼬리 부품',[(label,{'Tail':k},'Ivory') for k,label in [('BOB','짧은 꼬리'),('LONG','긴 꼬리'),('FOX','풍성한 꼬리')]],3,.53,.64,center=.45,crop=.44)
sites={'FOREHEAD':'이마','CHEEK':'볼','TORSO':'몸통','ARM_L':'왼팔','ARM_R':'오른팔','LEG':'다리'}
types={'SCAR':'흉터','SCRATCH':'긁힘','BRUISE':'멍','BANDAGE':'반창고'}
items=[]
for kind,title in types.items():
    for site,place in sites.items():
        items.append((title+' · '+place,{'Body':'BASE','Outfit':'LINEN','Face':'NEUTRAL','Hair':'SHORT','Injury':[kind+'_'+site]},None))
board('injuries','흉터·부상 · 표정과 독립',items,6,.52,1.34,center=.58,crop=1.15,full=True)

# Save a real openable document, including scenes and cameras (not a datablock-only library).
bpy.context.window.scene=boards[0]
for im in bpy.data.images:
    if im.has_data and not im.packed_file:
        try:im.pack()
        except RuntimeError:pass
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'kemono-parts-boards.blend'))
print('Rendered',len(boards),'parts boards and saved a scene-based Blender document.')
