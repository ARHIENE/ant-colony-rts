"""previews/frames/<view>/<clip>/f*.png → previews/gif/<view>/<clip>.gif at real speed (24 fps), clip name + frame stamped.
uv run --with pillow python scripts/gifs.py"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

root = Path(__file__).resolve().parent.parent / 'previews'
font = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 15)
n = 0
for view in sorted(p for p in (root / 'frames').iterdir() if p.is_dir()):
    out = root / 'gif' / view.name; out.mkdir(parents=True, exist_ok=True)
    for d in sorted(p for p in view.iterdir() if p.is_dir()):
        files = sorted(d.glob('f*.png'))
        frames = []
        for i, f in enumerate(files):
            im = Image.open(f).convert('RGB'); ImageDraw.Draw(im).text((6, 4), f'{d.name}  {i + 1}/{len(files)}  24fps', font=font, fill=(30, 30, 34))
            frames.append(im)
        if frames:
            frames[0].save(out / f'{d.name}.gif', save_all=True, append_images=frames[1:], duration=42, loop=0, optimize=True); n += 1
print('GIFS', n)
