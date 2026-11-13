from pathlib import Path
from PIL import Image,ImageChops
import json
p=Path('work/save-20261114/media')
frames=[]
for f in sorted((p/'dining').glob('*.png')):
 im=Image.open(f).convert('RGB');im.thumbnail((500,375));frames.append(im.copy())
assert len(frames)==40
changed=sum(ImageChops.difference(a,b).getbbox() is not None for a,b in zip(frames,frames[1:]))
assert changed>20
frames[0].save(p/'dining.gif',save_all=True,append_images=frames[1:],duration=120,loop=0,optimize=True)
frames[0].save(p/'dining.jpg',quality=90)
frames[20].save(p/'dining-middle.jpg',quality=90)
r={'frames':len(frames),'changed_frame_pairs':changed,'bytes':(p/'dining.gif').stat().st_size,'actual_capture':'Unity Game ScreenCapture, live MealSeated, 4 Chair_Sit_Idle_M sources'}
(p/'verification.json').write_text(json.dumps(r,indent=2),encoding='utf8');print(r)
