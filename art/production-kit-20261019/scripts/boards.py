"""Catalogue boards from previews/png → previews/boards/*.png (labels in Korean, readable size).
uv run --with pillow python scripts/boards.py
Boards: tier comparison (tools/weapons × tiers), one per resource group (singles), variants sample, crop stages,
facilities, deposits, pipes, equipment."""
import json, sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

KIT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(KIT / 'scripts'))
import catalog, equip_spec as ES

PNG, OUT = KIT / 'previews' / 'png', KIT / 'previews' / 'boards'
OUT.mkdir(parents=True, exist_ok=True)
FONT = 'C:/Windows/Fonts/malgun.ttf'
F_LABEL, F_HEAD, F_TITLE = (ImageFont.truetype(FONT, s) for s in (17, 22, 34))
BG, INK, SUB = (238, 236, 230), (40, 40, 44), (110, 110, 118)


def board(title, rows, cell=200, label_h=46, cols=None, row_heads=None, col_heads=None, name='board', cell_h=None):
    """rows: list of lists of (png_path|None, label). Optional row/col headers."""
    cols = cols or max(len(r) for r in rows)
    lw = 170 if row_heads else 0; th = 70 + (40 if col_heads else 0)
    ch = cell_h or cell
    W, H = lw + cols * cell + 20, th + len(rows) * (ch + label_h) + 20
    im = Image.new('RGB', (W, H), BG); d = ImageDraw.Draw(im)
    d.text((20, 18), title, font=F_TITLE, fill=INK)
    if col_heads:
        for j, h in enumerate(col_heads): d.text((lw + j * cell + cell / 2, 92), h, font=F_HEAD, fill=INK, anchor='mm')
    for i, row in enumerate(rows):
        y = th + i * (ch + label_h)
        if row_heads: d.text((14, y + ch / 2), row_heads[i], font=F_HEAD, fill=INK, anchor='lm')
        for j, (p, label) in enumerate(row):
            x = lw + j * cell
            if p and Path(p).exists():
                t = Image.open(p).convert('RGBA'); t.thumbnail((cell - 8, ch - 8)); im.paste(t, (x + (cell - t.width) // 2, y + (ch - t.height) // 2), t)
            elif p is not None: d.text((x + cell / 2, y + ch / 2), '(없음)', font=F_LABEL, fill=SUB, anchor='mm')
            for k, line in enumerate(_wrap(label, cell)): d.text((x + cell / 2, y + ch + 4 + k * 20), line, font=F_LABEL, fill=INK, anchor='ma')
    im.save(OUT / f'{name}.png', optimize=True); return OUT / f'{name}.png'


def _wrap(s, cell):
    out, cur = [], ''
    for ch in s:
        if F_LABEL.getlength(cur + ch) > cell - 10: out.append(cur); cur = ch
        else: cur += ch
    return (out + [cur])[:2]


def chunks(xs, n): return [xs[i:i + n] for i in range(0, len(xs), n)]


made = []
# 1. tier comparison
tiers = ES.TIERS; kinds = [('Tool', k, v[0]) for k, v in ES.TOOLS.items()] + [('Weapon', k, v[0]) for k, v in ES.WEAPONS.items()]
rows = [[(PNG / 'handheld' / f'{p}_{k}_{t[0]}.png', '') for t in tiers] for p, k, ko in kinds]
made.append(board('재질 단계 비교 — 도구·무기 (금·은은 단계 아님)', rows, cell=170, label_h=6, row_heads=[ko for *_, ko in kinds],
                  col_heads=[t[1] for t in tiers], name='tier_comparison'))
# 2. one board per resource group
for gid, gko, items in catalog.GROUPS:
    cells = [(PNG / 'resources' / f'{i}.png', ko) for i, ko, *_ in items]
    made.append(board(f'{gko} — {len(items)}종 (단품)', chunks(cells, 10), name=f'resources_{gid}'))
# 3. variants sample
sample = ['Fruit_Apple', 'Veg_Carrot', 'Grain_Wheat', 'Fish_Mackerel', 'Meat_Beef', 'Dish_Rice', 'Proc_Cheese', 'Mush_Shiitake']
ko = {i: k for _, _, i, k, *_ in catalog.all_items()}
rows = [[(PNG / 'resources' / f'{i}{s}.png', '') for s in ('', '__Stack', '__Rotten', '__Seed')] for i in sample]
made.append(board('변형 — 단품 / 운반·적재 / 부패 / 씨앗봉투', rows, cell=170, label_h=6, row_heads=[ko[i] for i in sample],
                  col_heads=['단품', '운반·적재', '부패', '씨앗'], name='resource_variants'))
# 4. crop stages (one strip per crop)
crops = json.loads((KIT / 'previews' / 'crops-build.json').read_text(encoding='utf-8'))['crops']
cells = [(PNG / 'crops' / f'Crop_{cid}.png', v['ko']) for cid, v in crops.items()]
made.append(board('밭 작물 성장 — 각 칸 왼→오: 새싹·생장·성숙·수확 가능·수확 직후(재성장)', chunks(cells, 4), cell=480, cell_h=192, name='crop_stages'))
# 5. facilities / deposits / pipes
fac = json.loads((KIT / 'previews' / 'facilities-build.json').read_text(encoding='utf-8'))['facilities']
groups = {'facilities': lambda v: v['collection'] not in ('Fac_Deposit',) and not v['id'].startswith('Pipe'),
          'deposits': lambda v: v['collection'] == 'Fac_Deposit', 'pipes': lambda v: v['id'].startswith('Pipe')}
titles = {'facilities': '신규 시설 — 산업·식품·채취·유체·에너지·온실 모듈', 'deposits': '매장 지점 — 정상 / 채굴 중 / 고갈 + 지하 범위 표시',
          'pipes': '배관 모듈 — 액체(파랑·굵음) / 기체(노랑·가늚)'}
for g, fn in groups.items():
    cells = [(PNG / 'facilities' / f'{fid}.png', v['ko']) for fid, v in fac.items() if fn(v)]
    made.append(board(titles[g], chunks(cells, 7 if g != 'deposits' else 3), cell=230, name=f'facilities_{g}'))
# 6. equipment
eq = sorted(p for p in (PNG / 'equipment').glob('*.png'))
made.append(board('장착 부품 — 투구(귀 유형별 개방형)·갑옷·보호대·벨트·거치대', chunks([(p, p.stem) for p in eq], 8), name='equipment'))
print('BOARDS', len(made))
