"""Equipment data: material tiers, tool/weapon dimensions, wearable parts. Units = kemono metres.

Tier features are cumulative so each step changes the silhouette, not only the colour:
  stone   – lashed stone head on a raw haft (tools only)
  copper  – plain cast head
  bronze  – + collar band where the head meets the haft
  iron    – + rivets
  steel   – + bright bevel strip on the working edge
  titanium alloy – + slimmer swept head, cord-wrapped grip
  starlight alloy (가칭) – + glowing inlay line and crystal pommel cap
Gold/silver are decoration/trade only and are NOT tiers.
"""

TIERS = [  # id, 한글, head colour, metal, handle colour
    ('Stone', '돌', '#8a8580', 0.0, '#7a5a34'),
    ('Copper', '구리', '#c06a3c', .55, '#8a5a30'),
    ('Bronze', '청동', '#a8783a', .55, '#7a4e2a'),
    ('Iron', '철', '#8a9096', .5, '#6a4426'),
    ('Steel', '강철', '#b8c2ca', .6, '#5a3a22'),
    ('Titanium', '티타늄 합금', '#6f93b4', .55, '#2f3640'),
    ('Starlight', '성휘합금(가칭)', '#7d68e0', .5, '#2a2340'),
]
METAL_TIERS = [t for t in TIERS if t[0] != 'Stone']
LEATHER = ('Leather', '가죽', '#8a5a34', 0.0, '#5a3a22')
GLOW = '#c9a8ff'
BRIGHT = '#e8eef2'
LASH = '#c8a870'

# Tools: total length, grip distance from the butt end, head kind. Grip origin = fist centre.
# Lengths keep the butt above the floor when held at the side (palm z .41).
TOOLS = {
    'Pickaxe': ('곡괭이', .40, .07, '광맥·암석 채굴'),
    'Axe': ('도끼', .36, .07, '벌목·그루터기'),
    'Shovel': ('삽', .44, .06, '모래·점토·땅 파기'),
    'Hammer': ('망치', .24, .05, '건설·수리·철거'),
    'Hoe': ('괭이', .42, .07, '밭 조성·파종'),
    'Sickle': ('낫', .20, .04, '수확·풀 베기'),
}
STONE_NOTES = {
    'Pickaxe': '뾰족한 돌 머리를 자루 홈에 끼우고 끈으로 감음',
    'Axe': '갈아 만든 돌날을 자루 구멍에 끼우고 끈으로 감음',
    'Shovel': '돌로는 넓은 날이 깨지므로 단단한 나무 날 + 끝에 얇은 돌 날을 끈으로 덧댐',
    'Hammer': '둥근 돌을 갈라진 자루에 끼우고 끈으로 감음',
    'Hoe': '납작한 돌날을 자루에 직각으로 묶음',
    'Sickle': '휜 나무 몸체 안쪽에 뗀석기 조각을 박아 날로 씀',
}
WEAPONS = {
    'Sword': ('검', .36, .035, '근접 무기(기존 큰턱 계열의 사람형 대체)'),
    'Spear': ('창', .72, .34, '근접 장병기(기존 Prop_Spear 계열)'),
    'Shield': ('방패', .22, 0, '방어 장비, 왼팔'),
}
# Kept as-is (function preserved, no tiers): resident-kit Equip_AcidSprayer / Equip_Signaller, flight harness.
KEPT = {'Equip_AcidSprayer': '산 분사기', 'Equip_Signaller': '지휘 신호기'}

EARS = ['CAT', 'DROOP', 'RABBIT', 'ROUND', 'UPRIGHT']
TAILS = ['BOB', 'FOX', 'LONG']
HAIRS = ['BOB', 'BUNS', 'LONG', 'PONY', 'SHORT', 'SIDE']
