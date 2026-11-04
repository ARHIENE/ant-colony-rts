import bpy
import numpy as np

def create_face(path):
    n = 1024
    yy, xx = np.mgrid[0:n, 0:n].astype(float)
    x = (xx / (n - 1) - .5) * 1.30
    z = (yy / (n - 1) - .5) * 1.38 + 2.14
    rgb = np.empty((n,n,4), dtype=np.float32)
    rgb[:,:,:3] = (.98,.79,.66)
    rgb[:,:,3] = 1
    def paint(mask, color, opacity=1):
        a = np.clip(mask * opacity, 0, 1)[...,None]
        rgb[:,:,:3] = rgb[:,:,:3]*(1-a) + np.asarray(color)*a
    def ellipse(cx,cz,rx,rz,color):
        q=((x-cx)/rx)**2+((z-cz)/rz)**2
        paint(np.clip((1-q)*200,0,1),color)
    for side in [-1,1]:
        cx=side*.245
        paint(np.exp(-(((x-side*.38)/.10)**2+((z-1.99)/.055)**2)*1.5),(.96,.35,.37),.43)
        ellipse(cx,2.115,.128,.159,(.17,.12,.16))
        ellipse(cx,2.105,.114,.142,(1,.97,.91))
        ellipse(cx+side*.008,2.107,.080,.128,(.22,.29,.40))
        ellipse(cx+side*.008,2.075,.067,.092,(.37,.53,.62))
        ellipse(cx+side*.008,2.12,.042,.093,(.10,.15,.23))
        ellipse(cx-.025,2.178,.030,.036,(1,1,1))
        ellipse(cx+.035,2.047,.013,.015,(.90,.98,1))
        # Upper lid and short outer lash; no protruding eyeballs.
        dx=(x-cx)/.14
        lid=2.13+.137*np.sqrt(np.clip(1-dx*dx,0,1))
        paint((np.abs(dx)<1)*np.clip((.018-np.abs(z-lid))*300,0,1),(.19,.13,.18))
        brow=2.355 + .025*(1-((x-cx)/.13)**2)
        paint((np.abs(x-cx)<.125)*np.clip((.011-np.abs(z-brow))*300,0,1),(.41,.32,.33))
    # Tiny smile and soft nose shading, painted flat into the face.
    smile=1.926+.055*(x/.09)**2
    paint((np.abs(x)<.072)*np.clip((.006-np.abs(z-smile))*350,0,1),(.48,.23,.23))
    paint(np.exp(-((x/.022)**2+((z-2.035)/.018)**2)*2),(.82,.47,.39),.38)
    image=bpy.data.images.new('Rabbit03_Face',width=n,height=n,alpha=True)
    image.pixels.foreach_set(rgb.ravel())
    image.filepath_raw=str(path)
    image.file_format='PNG'
    image.save()
    image.pack()
    return image
