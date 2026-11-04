import bpy,math,sys
from mathutils import Vector
sys.path.insert(0,'E:/Git/ant/art/characters/toto-v1')
import forms as f
def soft(o):
    m=o.modifiers.new('Soft silhouette','SUBSURF');m.levels=2;return o
def lock(name,points,mat,r):return soft(f.bent_ear(name,points,mat,r,16))
def ears(kind,mat,inner,r):
    for s in [-1,1]:
        if kind=='round':
            f.oval('EarRound',(s*.67,.04,1.96),(.28,.17,.29),mat,r,24,16)
            f.oval('EarInner',(s*.67,-.115,1.97),(.16,.025,.17),inner,r,24,12)
        elif kind in ('point','upright'):
            height=.53 if kind=='upright' else .34
            lock('EarPoint',[(s*.53,.02,1.85,.17,.11),(s*.64,.02,2.09,.23,.13),(s*.73,.01,2.09+height,.005,.005)],mat,r)
            f.oval('EarInner',(s*.64,-.11,2.16),(.11,.026,.17),inner,r,24,12)
        else:
            bottom=.94 if kind=='long' else 1.35
            lock('EarFlop',[(s*.49,.04,1.96,.09,.07),(s*.77,.03,1.82,.23,.15),(s*.96,-.01,1.57,.24,.14),(s*1.02,-.06,bottom+.12,.20,.12),(s*.87,-.11,bottom,.02,.02)],mat,r)
def tuft(mat,r):
    for i in range(3):
        x=(i-1)*.13
        lock('CrownTuft',[(x,0,1.99,.11,.09),(x+.03,-.01,2.14,.12,.08),(x+.15,-.02,2.24-(i%2)*.06,.005,.005)],mat,r)
def cape(mat,r):
    verts=[];faces=[];N=32
    for z,rx,ry in [(.89,.42,.39),(.81,.57,.46),(.56,.69,.48)]:
        for j in range(N+1):
            a=-math.pi/2+.55+j*(math.tau-1.1)/N;verts.append((rx*math.cos(a),ry*math.sin(a),z))
    for k in range(2):
        for j in range(N):a=k*(N+1)+j;faces.append((a,a+1,a+N+2,a+N+1))
    o=soft(f.mesh('ShortCape',verts,faces,mat,r,True));m=o.modifiers.new('Cloth thickness','SOLIDIFY');m.thickness=.02
    f.tube('Collar',[(-.43,-.30,.88),(-.23,-.43,.83),(0,-.46,.80),(.23,-.43,.83),(.43,-.30,.88)],.065,mat,r,12)
def leaf(x,z,mat,r):return lock('Leaf',[(x,0,z,.01,.01),(x+.17,0,z+.18,.15,.045),(x+.40,0,z+.32,.005,.005)],mat,r)
def flower(x,y,z,mat,center,r):
    for j in range(5):
        a=j*math.tau/5;f.oval('Petal',(x+.10*math.cos(a),y,z+.10*math.sin(a)),(.073,.035,.073),mat,r,16,8)
    f.oval('FlowerCenter',(x,y-.035,z),(.055,.025,.055),center,r,16,8)
def prop(kind,r,wood,green,cream,dark):
    if kind=='spear':
        f.tube('Staff',[(-.85,-.30,.10),(-.85,-.30,1.70)],.035,wood,r)
        lock('SpearTip',[(-.85,-.30,1.65,.04,.04),(-.85,-.30,1.85,.13,.05),(-.85,-.30,2.13,.004,.004)],cream,r)
    elif kind=='shield':
        f.oval('RoundShield',(.67,-.50,.64),(.32,.08,.38),wood,r,32,16)
        pts=[(.67+.30*math.cos(t*math.tau/48),-.59,.64+.36*math.sin(t*math.tau/48)) for t in range(49)]
        f.tube('ShieldRim',pts,.022,cream,r)
        f.tube('ShieldStripe',[(.67,-.59,.33),(.67,-.59,.95)],.012,dark,r)
    elif kind=='book':
        for s in [-1,1]:
            vs=[(0,-.59,.30),(s*.36,-.53,.36),(s*.36,-.53,.79),(0,-.59,.73)]
            o=f.mesh('Book',vs,[(0,1,2,3)],wood,r);m=o.modifiers.new('Pages','SOLIDIFY');m.thickness=.06
    elif kind=='basket':
        f.oval('Basket',(0,-.55,.50),(.38,.24,.21),wood,r,24,12)
        berry=f.material('Berry',(.57,.15,.12))
        for i in range(7):f.oval('Berry',((i%3-1)*.15,-.55+(i//3-1)*.12,.66),(.09,.09,.09),berry,r,16,8)
    elif kind=='cloth':
        for i in range(3):f.oval('FoldedCloth',(0,-.55,.45+i*.12),(.32,.17,.07),cream,r,24,12)
    elif kind=='watering':
        f.oval('WateringCan',(0,-.57,.50),(.25,.16,.22),green,r,24,12)
        f.tube('Spout',[(.15,-.57,.48),(.40,-.57,.73)],.065,green,r)
        f.tube('Handle',[(-.20,-.57,.65),(-.38,-.57,.65),(-.39,-.57,.44),(-.20,-.57,.40)],.035,green,r)
    elif kind=='leaf':
        f.tube('Stem',[(.12,-.58,.28),(.42,-.58,.90)],.018,green,r)
        o=leaf(.23,.60,green,r);o.location.y=-.58
