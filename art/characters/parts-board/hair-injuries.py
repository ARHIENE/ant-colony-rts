"""Static hair options and site-specific scars/injuries, no behavior code."""
import bpy,sys,math,json
from pathlib import Path
from mathutils import Vector
HERE=Path(__file__).resolve().parent
sys.path.insert(0,'E:/Git/ant/art/characters/residents-24');import parts as p
f=p.f;old=bpy.context.scene;scene=bpy.data.scenes.new('Hair_Scars_Injuries');bpy.context.window.scene=scene;scene.world=old.world.copy()
template=bpy.data.objects['MIX-04'];ink=bpy.data.materials['ResidentInk']
fur=f.material('Hair_DustyBlue',(.37,.56,.68));scar=f.material('HealedScar',(.70,.44,.40));scratch=f.material('ScratchMark',(.55,.24,.20));bruise=f.material('BruiseMuted',(.47,.40,.51));bandage=f.material('BandageCream',(.89,.83,.68));pad=f.material('BandagePad',(.74,.66,.51))
refs=[]
for original in template.children:
    o=original.copy();o.data=original.data.copy();scene.collection.objects.link(o);o.parent=None;o.hide_render=True;refs.append(o)
bpy.context.view_layer.update()
def point(x,z,off=.012):
    hits=[]
    for o in refs:
        inv=o.matrix_world.inverted();origin=inv@Vector((x,-3,z));direction=inv.to_3x3()@Vector((0,1,0))
        ok,v,n,i=o.ray_cast(origin,direction)
        if ok:hits.append(o.matrix_world@v)
    assert hits,(x,z)
    v=min(hits,key=lambda v:v.y);return (x,v.y-off,z)
def line(name,pts,mat,r,radius=.012):return f.tube(name,[point(x,z) for x,z in pts],radius,mat,r,10)
def spot(name,x,z,rx,rz,mat,r):
    N=48;vs=[point(x,z)];faces=[]
    for k in range(1,6):
        for j in range(N):a=math.tau*j/N;vs.append(point(x+rx*k/5*math.cos(a),z+rz*k/5*math.sin(a)))
    for j in range(N):faces.append((0,j+1,(j+1)%N+1))
    for k in range(4):
        for j in range(N):a=1+k*N+j;b=1+k*N+(j+1)%N;faces.append((a,a+N,b+N,b))
    return f.mesh(name,vs,faces,mat,r,True)
def root(name):
    r=bpy.data.objects.new(name,None);scene.collection.objects.link(r);return r
def display(r,idx):
    r.location=((idx%6-2.5)*2.55,0,(4-idx//6)*2.8)
    b=root('PreviewBody_'+r.name);b.location=r.location
    for orig in template.children:
        if idx<6 and ('Leaf' in orig.name or 'CrownTuft' in orig.name):continue
        o=orig.copy();scene.collection.objects.link(o);o.parent=b
    d=bpy.data.curves.new('Caption','FONT');d.body=r.name;d.size=.12;d.align_x='CENTER';d.materials.append(ink)
    o=bpy.data.objects.new(r.name+'_Label',d);scene.collection.objects.link(o);o.location=(r.location.x,-.8,r.location.z-.15);o.rotation_euler=(math.pi/2,0,0)
records=[]
for i,kind in enumerate(['SIDE PART','ROUND FRINGE','CURL','DOUBLE TUFT','LEAF SHOOTS','SOFT CREST']):
    r=root('HAIR-%02d %s'%(i+1,kind))
    if i in (0,1):
        for j in range(3):
            x=(j-1)*.22
            p.lock('Fringe',[(x,.01,2.03,.06,.06),(x-.08,-.30,1.94,.16,.07),(x+(.13 if i==0 else 0),-.44,1.77,.13,.06),(x+.17,-.45,1.72,.003,.003)],fur,r)
    elif i==2:
        p.lock('Curl',[(0,0,2.0,.08,.06),(.13,0,2.17,.10,.07),(.05,0,2.32,.10,.07),(-.12,0,2.30,.07,.06),(-.15,0,2.21,.005,.005)],fur,r)
    elif i in (3,5):
        for j in range(2 if i==3 else 4):
            x=(j-(.5 if i==3 else 1.5))*.14
            p.lock('Tuft',[(x,0,2.0,.08,.06),(x+.04,0,2.16,.10,.07),(x+.15,0,2.25,.005,.005)],fur,r)
    else:
        green=bpy.data.materials['SageCloth'];p.leaf(0,2.0,green,r);o=p.leaf(0,2.0,green,r);o.scale.x=-1
    if i in (0,1):
        for o in r.children:o.location.y-=.14
    display(r,i);records.append({'id':r.name,'type':'hair'})
sites=[('FOREHEAD',0,1.70),('CHEEK',-.58,1.20),('TORSO',0,.40),('ARM-L',-.43,.66),('ARM-R',.43,.66),('LEG',-.27,.13)]
for row,kind in enumerate(['SCAR','SCRATCH','BRUISE','BANDAGE'],1):
    for col,(site,x,z) in enumerate(sites):
        r=root(kind+'-'+site);size=.70 if site=='LEG' else 1
        if kind=='SCAR':
            line('HealedLine',[(x-.07*size,z-.06*size),(x+.07*size,z+.06*size)],scar,r,.009)
            for t in [-.035,.035]:line('ScarTick',[(x+t-.014,z+t+.015),(x+t+.014,z+t-.015)],scar,r,.007)
        elif kind=='SCRATCH':
            for j in range(3):
                dx=(j-1)*.034*size;line('Scratch',[(x+dx-.025*size,z-.05*size),(x+dx+.025*size,z+.05*size)],scratch,r,.008)
        elif kind=='BRUISE':spot('Bruise',x,z,.075*size,.052*size,bruise,r)
        else:
            spot('Adhesive',x,z,.10*size,.045*size,bandage,r)
            o=spot('DressingPad',x,z,.040*size,.035*size,pad,r);o.location.y-=.009
        r['attachment_site']=site;r['condition']=kind;display(r,row*6+col)
        records.append({'id':r.name,'type':kind,'site':site,'local_center':[x,z]})
for o in refs:o.hide_set(True)
for ob in old.objects:
    if ob.type=='LIGHT':
        o=ob.copy();o.data=ob.data.copy();scene.collection.objects.link(o);o.location=(0,-8,8);o.data.energy=3500;o.data.size=20;o.data.use_shadow=False
cam=bpy.data.objects.new('DetailsCamera',bpy.data.cameras.new('DetailsCamera'));scene.collection.objects.link(cam);scene.camera=cam;cam.location=(0,-30,6.6);cam.rotation_euler=(math.pi/2,0,0);cam.data.type='ORTHO';cam.data.ortho_scale=16.5
scene.render.resolution_x=2100;scene.render.resolution_y=1800;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.render.filepath=str(HERE/'hair-injuries.png')
(HERE/'hair-injury-catalog.json').write_text(json.dumps(records,indent=2),encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(HERE/'appearance-parts.blend'))
bpy.ops.render.render(write_still=True)
print('6 hair pieces, 24 independent scar/injury parts saved')
