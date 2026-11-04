"""2026-10-24 새 기획(G02·G07·G12·G22·G34·G46·G16·G63) 소품. 모양은 기획에 외형 지정이 없어 추천안으로 정함.

handheld_extra(): 손에 드는 것(Handheld 컬렉션, handheld.py 규격: 원점=손잡이, +Y=머리 쪽) — 단궁·화살·보스 보상 장비 3종.
props(): 바닥·창고 물건(Props 컬렉션, 원점=바닥 중심) — 돌 조각, 시작 보존식, 드럼통, 가스 용기, 보스 핵심물 A~D,
  탐지기 B~D, 장수 유해, 시민 시신 묶음.
"""
import math
import pk

WOOD, WOOD_D, CORD, IRON, STEEL, STONE = '#9a6a3c', '#6a4628', '#d8c8a0', '#4a4e56', '#9aa2ac', '#8a8580'
GLOW = '#7ae0ff'


def _obj(m, col, kind, length, contact, socket, **extra):
    o = m.to_object(col, origin='keep')
    o['kind'], o['tier'], o['length'], o['contact'], o['rigid'], o['socket'] = kind, 'Unique', length, contact, True, socket
    for k, v in extra.items(): o[k] = v
    return o


def short_bow(col):
    """G34 무연구 단궁(목재 15+식물섬유 5). 왼손 쥠, 활대는 +Y/-Y, 시위는 -Z(몸 쪽)."""
    m = pk.Mesh('Weapon_ShortBow')
    for s in (1, -1):  # 위·아래 활대: 3마디로 휘어 있음
        pts = [(0, 0, 0), (0, .09 * s, .012), (0, .17 * s, .03), (0, .23 * s, .055)]
        for a, b in zip(pts, pts[1:]): m.cyl(a, b, .007, WOOD, seg=6, r2=.005)
    m.cyl((0, -.025, 0), (0, .025, 0), .009, WOOD_D, seg=7)                     # 손잡이 감개
    m.cyl((0, .23, .055), (0, -.23, .055), .0012, CORD, seg=4)                  # 시위
    o = _obj(m, col, 'Bow', .46, [0, 0, .055], 'Socket.Tool.L (활), 화살은 Socket.Tool.R', string=[[0, .23, .055], [0, -.23, .055]])
    o['ko'] = '단궁'; return o


def arrow(col):
    m = pk.Mesh('Ammo_Arrow')
    m.cyl((0, -.15, 0), (0, .13, 0), .0025, WOOD, seg=5)
    m.cyl((0, .13, 0), (0, .16, 0), .006, STONE, seg=4, r2=0)                   # 돌촉
    for a in range(3): m.box((0, -.13, 0), (.001, .04, .012), '#e8e0d0', rot=(0, a * math.tau / 3, 0))  # 깃
    o = _obj(m, col, 'Arrow', .31, [0, .16, 0], 'Socket.Tool.R (시위 걸 때 끝 -Y가 시위 위)')
    o['ko'] = '화살'; return o


def boss_reward_a(col):
    """G22 A 반입 보상: 내구도 높은 고유 장비 — 두꺼운 등판·금속 테가 둘린 대검."""
    m = pk.Mesh('Weapon_BossA_Guardblade'); head = pk.mat('#5a6070', metal=.8, rough=.35)
    m.lathe([(.016, 0), (.016, .26), (.0, .33)], head, seg=4, sx=1, sy=.4, rot=(-math.pi / 2, 0, 0), c=(0, .03, 0), smooth=False)
    m.cyl((0, .05, 0), (0, .28, 0), .006, '#c8a040', seg=4)                      # 금테 등줄
    m.box((0, .025, 0), (.1, .016, .022), '#c8a040'); m.cyl((0, -.04, 0), (0, .02, 0), .007, WOOD_D, seg=7)
    m.ball((0, -.046, 0), (.016,) * 3, '#c8a040', seg=8, rings=5)
    o = _obj(m, col, 'Sword', .4, [0, .33, 0], 'Socket.Tool.R; 칼집은 Scabbard_Steel 공용'); o['ko'] = '수호 대검(A 보상·고내구)'; return o


def boss_reward_b(col):
    """G22 B 보상: 방열 장비 — 열을 반사하는 은빛 방열 방패(방패 소켓 공용)."""
    m = pk.Mesh('Weapon_BossB_HeatShield'); face = pk.mat('#e8eef2', metal=.9, rough=.15)
    m.cyl((0, 0, -.006), (0, 0, .006), .11, face, seg=16)
    for r in (.06, .09): m.torus((0, 0, .007), r, .004, '#d06a2a', seg=16, tseg=4)  # 열 무늬 고리
    m.cyl((0, 0, .006), (0, 0, .02), .025, '#c8a040', seg=10)                       # 방패심
    m.box((0, 0, -.02), (.012, .07, .012), WOOD_D)                                  # 가운데 손잡이(앞면 -Z 규격과 동일)
    o = _obj(m, col, 'Shield', .22, [0, 0, .02], 'Socket.Tool.L (Weapon_Shield_* 와 같은 가운데 손잡이)'); o['ko'] = '방열 방패(B 보상)'; return o


def boss_reward_c(col):
    """G22 C 보상: 범위 공격 장비 — 넓게 휘두르는 양날 전투 망치."""
    m = pk.Mesh('Weapon_BossC_Quakehammer'); head = pk.mat('#6a4a7a', metal=.6, rough=.4)
    m.cyl((0, -.06, 0), (0, .34, 0), .007, WOOD_D, seg=7)
    m.box((0, .34, 0), (.16, .07, .07), head)
    for s in (-1, 1): m.cyl((s * .08, .34, 0), (s * .1, .34, 0), .045, '#c8a040', seg=8)
    m.cyl((0, .375, 0), (0, .4, 0), .02, pk.mat(GLOW, emit=2.5), seg=8)           # 충격 결정
    o = _obj(m, col, 'Hammer', .46, [.1, .34, 0], 'Socket.Tool.R, 양손(grip2)', grip2=[0, .12, 0]); o['ko'] = '진동 망치(C 보상·범위)'; return o


def handheld_extra(col):
    return [short_bow(col), arrow(col), boss_reward_a(col), boss_reward_b(col), boss_reward_c(col)]


# ---- 바닥 물건 ----
def _prop(m, col, pid, ko, note):
    o = m.to_object(col, origin='keep'); o['id'], o['ko'], o['note'] = pid, ko, note
    return o


def stone_chunk(col):
    m = pk.Mesh('Prop_StoneChunk'); m.ico((0, 0, .035), (.05, .04, .035), STONE, sub=1, rot=(.3, .2, .5))
    return _prop(m, col, 'Prop_StoneChunk', '돌 조각', 'G02 시작 지점 근처 맨손 운반 돌 조각(20개 보장)')


def preserved_ration(col):
    m = pk.Mesh('Prop_PreservedRation')
    m.box((0, 0, .03), (.09, .06, .06), '#a08050'); m.box((0, 0, .061), (.095, .065, .004), '#c84a3a')  # 밀봉 상자+띠
    m.cyl((.03, 0, .066), (.03, 0, .072), .012, '#e8d8a0', seg=8)
    return _prop(m, col, 'Prop_PreservedRation', '시작 보존식', 'G12 상온 1년 보존 비상식(1인 14끼)')


def oil_drum(col):
    m = pk.Mesh('Prop_OilDrum'); body = pk.mat('#3a5a8a', metal=.5, rough=.5)
    m.cyl((0, 0, 0), (0, 0, .26), .085, body, seg=14)
    for z in (.08, .18): m.torus((0, 0, z), .086, .004, IRON, seg=14, tseg=4)
    m.cyl((.04, 0, .26), (.04, 0, .27), .012, IRON, seg=8)
    return _prop(m, col, 'Prop_OilDrum', '드럼통', 'G46 강철 5, 원유·연료 액체 용기. FillingStation ChargePoint_Drum에 놓임')


def gas_canister(col):
    m = pk.Mesh('Prop_GasCanister'); body = pk.mat('#e0b830', metal=.4, rough=.4)
    m.cyl((0, 0, 0), (0, 0, .2), .05, body, seg=12); m.ball((0, 0, .2), (.05, .05, .03), body, seg=12, rings=4)
    m.cyl((0, 0, .225), (0, 0, .25), .012, '#c8402a', seg=8)                      # 밸브
    return _prop(m, col, 'Prop_GasCanister', '가스 용기', 'G46 강철 10, 압축 가스 용기. ChargePoint_Canister')


def boss_core(col, letter, color, shape):
    m = pk.Mesh(f'Prop_BossCore_{letter}'); glow = pk.mat(color, emit=2.0, rough=.2)
    m.cyl((0, 0, 0), (0, 0, .02), .045, '#3a3430', seg=8)                          # 받침
    if shape == 'gem': m.ico((0, 0, .07), (.035, .035, .05), glow, sub=0)
    elif shape == 'orb': m.ball((0, 0, .065), (.04,) * 3, glow, seg=12, rings=8)
    elif shape == 'shard':
        for a in range(3): m.ico((math.cos(a * 2.1) * .015, math.sin(a * 2.1) * .015, .06), (.014, .014, .045), glow, sub=0, rot=(.2, .3 * a, a))
    else:  # rocket core
        m.cyl((0, 0, .02), (0, 0, .1), .03, '#c8c8d0', seg=10); m.ball((0, 0, .065), (.036,) * 3, glow, seg=10, rings=6)
        for a in range(4): m.box((math.cos(a * math.pi / 2) * .03, math.sin(a * math.pi / 2) * .03, .06), (.008, .008, .07), '#c8a040')
    names = {'A': '보스 A 핵심물', 'B': '보스 B 핵심물', 'C': '보스 C 핵심물', 'D': '로켓 핵심물(보스 D 보상)'}
    return _prop(m, col, f'Prop_BossCore_{letter}', names[letter], 'G07 보스 사슬 핵심물. 거래·재활용 불가(G23)')


def detector(col, letter, color):
    m = pk.Mesh(f'Prop_Detector_{letter}')
    m.box((0, 0, .03), (.07, .05, .06), '#5a5e66'); m.cyl((0, .0, .06), (0, 0, .14), .003, STEEL, seg=5)
    m.ball((0, 0, .145), (.01,) * 3, pk.mat(color, emit=2.0), seg=8, rings=5)       # 신호 구슬(대상 핵심물 색)
    m.box((0, -.026, .035), (.04, .003, .025), pk.mat('#2a3a3a', emit=.5))          # 화면
    return _prop(m, col, f'Prop_Detector_{letter}', f'보스 {letter} 탐지기', 'G07 A 핵심물 1+목재 5+돌 5(B·C), 사용 시 위치 영구 공개')


def remains(col):
    m = pk.Mesh('Prop_Remains')
    m.box((0, 0, .025), (.32, .14, .05), '#d8d0c0', rot=(0, 0, .05))               # 천에 싼 유해
    for y in (-.1, 0, .1): m.box((y, 0, .051), (.012, .145, .004), '#8a6a4a')        # 묶은 끈
    return _prop(m, col, 'Prop_Remains', '장수 유해', 'G63 개별 장수 시신→유해. 자동 소멸 없음, 운반·묘비 안치')


def corpse_bundle(col):
    m = pk.Mesh('Prop_CorpseBundle')
    for i, (x, y) in enumerate(((-.08, 0), (.08, .02), (0, .1), (0, -.1))):
        m.box((x, y, .025 + (i == 2) * .03), (.2, .07, .05), '#c8bca8', rot=(0, 0, .3 * i))
    m.ico((0, 0, .02), (.2, .17, .02), '#6a5a4a', sub=1)                             # 깔개
    return _prop(m, col, 'Prop_CorpseBundle', '시민 시신 묶음', 'G16 위치별 수량 시신 묶음, 300초 정리 대상(G63)')


def props(col):
    out = [stone_chunk(col), preserved_ration(col), oil_drum(col), gas_canister(col)]
    for l, c, s in (('A', '#e05a3a', 'gem'), ('B', '#3a9ae0', 'orb'), ('C', '#9a5ae0', 'shard'), ('D', '#7ae0ff', 'rocket')):
        out.append(boss_core(col, l, c, s))
    for l, c in (('B', '#3a9ae0'), ('C', '#9a5ae0'), ('D', '#7ae0ff')):
        out.append(detector(col, l, c))
    return out + [remains(col), corpse_bundle(col)]
