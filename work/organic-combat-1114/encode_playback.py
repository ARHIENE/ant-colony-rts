from PIL import Image
from pathlib import Path
import sys
folder=Path(sys.argv[1])
frames=[Image.open(p) for p in sorted(folder.glob('*.png'))]
frames[0].save(folder.parent/'playback.gif',save_all=True,append_images=frames[1:],duration=67,loop=0)
print(folder.parent/'playback.gif')
