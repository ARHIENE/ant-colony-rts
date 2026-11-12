from pathlib import Path
from PIL import Image
root=Path('work/final-motion-1113/media')
for folder in root.iterdir():
 if not folder.is_dir():continue
 paths=sorted(folder.glob('*.png'))
 if not paths:continue
 frames=[Image.open(p).convert('RGB').resize((600,420)).quantize(colors=64) for p in paths]
 duration=80 if folder.name=='blinking' else 100
 frames[0].save(root/(folder.name+'.gif'),save_all=True,append_images=frames[1:],duration=duration,loop=0,optimize=True)
 print(folder.name,len(frames),(root/(folder.name+'.gif')).stat().st_size)
