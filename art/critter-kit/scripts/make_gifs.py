"""previews/frames/<scene>/f*.png → previews/gif/<scene>.gif
uv run --with pillow python scripts/make_gifs.py"""
from pathlib import Path
from PIL import Image

root = Path(__file__).resolve().parent.parent / 'previews'
out = root / 'gif'
out.mkdir(exist_ok=True)
for d in sorted(p for p in (root / 'frames').iterdir() if p.is_dir()):
    frames = [Image.open(f).convert('RGB') for f in sorted(d.glob('f*.png'))]
    if frames:
        frames[0].save(out / f'{d.name}.gif', save_all=True, append_images=frames[1:], duration=125, loop=0, optimize=True)
print(len(list(out.glob('*.gif'))), 'gifs')
