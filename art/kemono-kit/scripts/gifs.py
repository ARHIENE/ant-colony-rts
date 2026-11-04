"""previews/frames → previews/gif/<scene>.gif (real time: frames are every 2nd frame at 24 fps → 83 ms)
and previews/notion/{facilities_NN,motions_N}.gif (4x3 labelled grids, 24 frames spread over each clip).
uv run --with pillow python scripts/gifs.py"""
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parent.parent / 'previews'
FR, GIF, NOTION = ROOT / 'frames', ROOT / 'gif', ROOT / 'notion'
GIF.mkdir(exist_ok=True); NOTION.mkdir(exist_ok=True)
scenes = {d.name: sorted(d.glob('f*.png')) for d in sorted(FR.iterdir()) if d.is_dir() and any(d.glob('f*.png'))}
for name, files in scenes.items():
    ims = [Image.open(f).convert('RGB') for f in files]
    ims[0].save(GIF / f'{name}.gif', save_all=True, append_images=ims[1:], duration=83, loop=0, optimize=True)


def grid(names, out, tile=240, cols=4, rows=3, n=24):
    lab = 18; frames = []
    tiles = {nm: [Image.open(f).convert('RGB').resize((tile, tile)) for f in scenes[nm]] for nm in names}
    for i in range(n):
        im = Image.new('RGB', (cols * tile, rows * (tile + lab)), 'white'); d = ImageDraw.Draw(im)
        for k, nm in enumerate(names):
            x, y = k % cols * tile, k // cols * (tile + lab); t = tiles[nm]
            im.paste(t[i * len(t) // n], (x, y + lab)); d.text((x + 4, y + 3), nm.split('__', 1)[1], fill='black')
        frames.append(im)
    frames[0].save(out, save_all=True, append_images=frames[1:], duration=100, loop=0, optimize=True)


fac = [n for n in scenes if n.startswith('Preview__')] + [n for n in scenes if n.startswith('Swap__')]
for i in range(0, len(fac), 12): grid(fac[i:i + 12], NOTION / f'facilities_{i // 12 + 1:02d}.gif')
mot = [n for n in scenes if n.startswith('Motion__')]
for i in range(0, len(mot), 12): grid(mot[i:i + 12], NOTION / f'motions_{i // 12 + 1}.gif')
print(len(scenes), 'gifs;', -(-len(fac) // 12), 'facility grids;', -(-len(mot) // 12), 'motion grids')
