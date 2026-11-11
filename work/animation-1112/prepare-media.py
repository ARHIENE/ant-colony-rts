from pathlib import Path
from PIL import Image,ImageDraw
root=Path(__file__).parent
for kind in ('hair','face'):
 files=sorted((root/'portraits').glob(kind+'-*.png'))
 sheet=Image.new('RGB',(320*4,350*((len(files)+3)//4)),(30,32,38))
 for i,p in enumerate(files):
  sheet.paste(Image.open(p).resize((320,320)),((i%4)*320,(i//4)*350))
  ImageDraw.Draw(sheet).text(((i%4)*320+8,(i//4)*350+325),p.stem,fill='white')
 sheet.save(root/(kind+'-review.jpg'))
out=root/'published-media';out.mkdir(exist_ok=True)
for folder in (root/'media').iterdir():
 if not folder.is_dir():continue
 files=sorted(folder.glob('*.png'))
 if not files:continue
 import re
 result=(folder/'result.txt').read_text() if (folder/'result.txt').exists() else ''
 match=re.search(r'captured=(\d+)',result)
 if match:files=files[:int(match[1])]
 frames=[Image.open(p).convert('RGB').resize((480,480)) for p in files]
 # Preserve sampled timing; gameplay clips captured at quarter simulation speed.
 frames[0].save(out/(folder.name+'.gif'),save_all=True,append_images=frames[1:],duration=100,loop=0,optimize=True)
print('Created',len(list(out.glob('*.gif'))),'GIFs and portrait sheets')
