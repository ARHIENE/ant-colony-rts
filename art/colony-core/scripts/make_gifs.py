"""previews/interaction/<id>/f*.png → previews/interaction/<id>.gif
uv run --with pillow python scripts/make_gifs.py"""
from pathlib import Path
from PIL import Image

root = Path(__file__).resolve().parent.parent / 'previews' / 'interaction'
for d in sorted(p for p in root.iterdir() if p.is_dir()):
    frames = [Image.open(f).convert('RGB') for f in sorted(d.glob('f*.png'))]
    if frames:
        frames[0].save(root / f'{d.name}.gif', save_all=True, append_images=frames[1:], duration=167, loop=0, optimize=True)
        print(d.name, len(frames))
