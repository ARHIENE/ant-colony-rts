"""2026-10-24 새 기획(G02·G17·G46·G58·G59·G65·G66) 건물: 불시착 잔해, 학당, 시민 진료소, 공공서비스,
화분형 밭, 수원, 취수 펌프, 증기 고체연료 발전기. 기본 버섯밭은 기존 CC_MushroomFarm을 그대로 쓴다.
규격은 facilities.py와 같음(kit m, 1칸=1 m, 원점=바닥 중심, 정면 -Y). 모양은 기획에 외형 지정이 없어 추천안으로 정함."""
import math
from mathutils import Matrix
import pk
from facilities import Fac, spin, rock, gauge, wheel, lamp, chimney, port, WOOD, WOOD_D, IRON, STEEL, BRICK, STONE, SOOT, FIRE, LIQ_PIPE

ROOF, PLASTER, HULL, SCORCH, SOIL, WATER = '#8a4a3a', '#e8dcc4', '#b8c0c8', '#3a3430', '#5a3e26', '#4a90b8'
WATER_M = pk.mat(WATER, alpha=.75, rough=.05, name='PK_FacWater')


def gable(m, w, d, z, h, color, over=.12):
    """박공지붕: X 방향 용마루. 두 경사판 + 양 끝 삼각 벽."""
    ang = math.atan2(h, d / 2); L = math.hypot(h, d / 2) + over
    for s in (-1, 1):
        m.box((0, s * d / 4, z + h / 2), (w + over * 2, L, .06), color, rot=(-s * ang, 0, 0))
    M = Matrix(((0, 0, 1, -w / 2), (1, 0, 0, 0), (0, 1, 0, z), (0, 0, 0, 1)))  # 삼각형(y,z)을 X로 밀어냄
    m.prism([(-d / 2, 0), (d / 2, 0), (0, h)], 0, w, PLASTER, M=M)


def sign(m, xy, face, size):
    """정면 앞 기둥 간판(지붕 처마에 가리지 않게)."""
    x, y = xy; m.cyl((x, y, 0), (x, y, .95), .03, WOOD_D, seg=6)
    m.box((x, y - .01, .95), (size[0] + .06, .04, size[1] + .06), WOOD_D); m.box((x, y - .03, .95), (size[0], .02, size[1]), face)


def hall(f, w, d, wall_h, wall, roof, door=(.5, .9)):
    m = f.body
    m.box((0, 0, .05), (w + .2, d + .2, .1), STONE)
    m.box((0, 0, .1 + wall_h / 2), (w, d, wall_h), wall)
    gable(m, w, d, .1 + wall_h, .7, roof)
    m.box((0, -d / 2 - .01, .1 + door[1] / 2), (door[0], .04, door[1]), WOOD_D)                    # 문
    for x in (-w / 3, w / 3): m.box((x, -d / 2 - .01, .1 + wall_h * .6), (.35, .03, .3), '#a8d0e0')  # 창
    return m


def crash_wreck(col):
    f = Fac('CrashWreck', '불시착 잔해', (3, 2), col, '시작', '불시착한 수송정 잔해. 기울어진 동체·찢긴 판·흩어진 상자, 그을음. 해체 시 재료 회수')
    m = f.body
    m.ball((.1, .1, .35), (1.25, .7, .55), HULL, seg=14, rings=8, rot=(0, .22, .18))               # 기울어진 동체
    m.ball((.1, .1, .35), (1.28, .73, .22), SCORCH, seg=14, rings=6, rot=(0, .22, .18))              # 그을린 띠
    m.box((-1.05, -.2, .25), (.5, .04, .5), HULL, rot=(.3, .5, .2)); m.box((.9, .55, .15), (.6, .04, .35), HULL, rot=(-.2, .1, .9))  # 찢긴 판
    m.cyl((.95, .0, .7), (1.35, .1, 1.0), .14, IRON, seg=8, r2=.1)                                  # 꺾인 엔진 노즐
    for x, y, r in ((-1.2, -.7, .1), (-.7, -.8, .4), (1.1, -.75, .7)): m.box((x, y, .13), (.26, .26, .26), WOOD, rot=(0, 0, r))  # 쏟아진 상자
    m.ball((.3, -.55, .02), (.5, .25, .02), SCORCH, seg=10, rings=3)                                 # 바닥 그을음
    f.point('WorkPoint_0', (0, -1.05, 0)); f.point('OutputPoint', (-.9, -.95, 0))
    return f.finish()


def school(col):
    f = Fac('School', '학당', (3, 3), col, '공공', '어린 시민 20명 교육(G65). 목조 강당 + 칠판 간판 + 지붕 종')
    m = hall(f, 2.6, 2.2, 1.0, '#d8c09a', ROOF)
    sign(m, (1.05, -1.4), '#2e4a3a', (.6, .4))                                                     # 칠판 간판
    m.box((0, 0, 2.0), (.3, .3, .3), WOOD); m.cyl((0, 0, 1.85), (0, 0, 1.75), .1, '#c8a040', seg=10, r2=.14)  # 종탑·종
    f.point('WorkPoint_0', (0, -1.5, 0)); f.point('InputPoint', (0, -1.2, 0))
    return f.finish()


def citizen_clinic(col):
    f = Fac('CitizenClinic', '시민 진료소', (3, 3), col, '공공', '시민 50명 의료 서비스(G66). 흰 벽 + 초록 십자 간판 + 차양')
    m = hall(f, 2.6, 2.2, 1.0, '#f0ece4', '#5a8a6a')
    sign(m, (-1.05, -1.4), '#f8f8f8', (.45, .45))
    m.box((-1.05, -1.43, .95), (.3, .02, .1), '#3aa060'); m.box((-1.05, -1.43, .95), (.1, .02, .3), '#3aa060')  # 십자 간판
    m.box((0, -1.35, .95), (1.2, .5, .04), '#e8f0e8', rot=(-.25, 0, 0))                            # 차양
    m.box((.8, -.6, .35), (.5, .9, .1), '#f8f8f8')                                                  # 침상(창 너머)
    f.point('WorkPoint_0', (0, -1.6, 0))
    return f.finish()


def civic_public(col):
    f = Fac('CivicPublicService', '공공서비스', (3, 3), col, '공공', '행정·공공서비스 500명 기준(G17). 관청 + 깃발 + 게시판')
    m = hall(f, 2.6, 2.2, 1.1, '#c8b8a0', '#4a5a7a')
    for x in (-.6, -.2, .2, .6): m.cyl((x, -1.2, .1), (x, -1.2, 1.2), .06, PLASTER, seg=8)            # 기둥
    m.box((0, -1.25, 1.25), (1.6, .2, .1), PLASTER)
    m.cyl((1.1, -.9, .1), (1.1, -.9, 2.3), .025, IRON, seg=6); m.box((1.32, -.9, 2.1), (.42, .02, .26), '#c84a3a')  # 깃대·깃발
    m.box((-1.15, -1.3, .55), (.5, .04, .4), WOOD); m.box((-1.15, -1.33, .58), (.4, .02, .28), '#efe6c8')  # 게시판
    f.point('WorkPoint_0', (0, -1.6, 0))
    return f.finish()


def planter_bed(col):
    f = Fac('PlanterBed', '화분형 밭', (1, 1), col, '농업', '포장 지면 위 화분형 밭(G58). 나무 틀 + 흙, 작물은 엔진이 SoilPoint 위에 배치')
    m = f.body
    for s in (-1, 1):
        m.box((s * .42, 0, .18), (.08, .92, .36), WOOD); m.box((0, s * .42, .18), (.92, .08, .36), WOOD)
    m.box((0, 0, .3), (.78, .78, .06), SOIL)
    for x in (-.25, 0, .25): m.box((x, 0, .335), (.06, .7, .01), '#4a3220')                         # 이랑
    f.point('SoilPoint', (0, 0, .34)); f.point('WorkPoint_0', (0, -.75, 0))
    return f.finish()


def water_source(col):
    f = Fac('WaterSource', '수원', (1, 1), col, '자연', '얕은 수원(G59). 돌 테두리 웅덩이. 사막 지하수·동굴 용출수도 같은 모델(엔진 색만 변경 가능)')
    m = f.body
    m.cyl((0, 0, 0), (0, 0, .03), .44, SOIL, seg=16)
    for i in range(9):
        a = i / 9 * math.tau; m.ico((math.cos(a) * .42, math.sin(a) * .42, .05), (.09, .07, .06), STONE, rot=(0, 0, a))
    m.cyl((0, 0, .03), (0, 0, .045), .38, WATER_M, seg=16)
    f.point('WaterPoint', (0, 0, .045)); f.point('WorkPoint_0', (0, -.6, 0))
    return f.finish()


def water_pump(col):
    f = Fac('WaterPump', '취수 펌프', (1, 1), col, '유체', '수원 위 수동 취수 펌프(G59). 손잡이 왕복=취수, 액체 포트로 배관 연결')
    m = f.body
    m.box((0, 0, .04), (.7, .7, .08), STONE)
    m.cyl((0, .1, .08), (0, .1, .75), .07, LIQ_PIPE, seg=10); m.cyl((0, .1, .6), (0, -.18, .55), .03, LIQ_PIPE, seg=8)  # 몸통·주둥이
    m.cyl((0, -.22, .08), (0, -.22, .14), .12, WOOD_D, seg=10)                                       # 물받이
    port(f, m, 'FluidOut_L', (.35, .1, .25), (1, 0, 0), 'L')
    h = pk.Mesh('h'); h.box((0, .3, .8), (.04, .4, .04), IRON); h.ball((0, .5, .8), (.035,) * 3, WOOD)
    rock(f.part('Handle', h, (0, .1, .8)), 'X', .35, name='WaterPump|Run')
    f.point('WorkPoint_0', (0, .9, 0), face=(0, -1, 0)); f.point('HandTarget', (0, .5, .8))
    return f.finish()


def steam_generator(col):
    f = Fac('SteamGenerator', '증기 고체연료 발전기', (1, 1), col, '에너지', '석탄을 태워 증기→전력(G46). 보일러 + 굴뚝 + 플라이휠, 화구=InputPoint')
    m = f.body
    m.box((0, 0, .05), (.9, .9, .1), IRON)
    m.cyl((-.35, .1, .25), (.2, .1, .25), .25, '#7a4a3a', seg=14)                                    # 누운 보일러 드럼
    for x in (-.25, 0, .15): m.torus((x, .1, .25), .26, .015, IRON, seg=14, tseg=4, rot=(0, math.pi / 2, 0))
    m.box((-.1, -.18, .2), (.2, .04, .14), FIRE)                                                      # 화구 불빛
    chimney(m, (-.3, .25, .5), .9)
    gauge(m, (.1, -.15, .5), .045); lamp(m, (.3, -.15, .4))
    port(f, m, 'PowerOut', (.45, .3, .15), (1, 0, 0), 'P')
    fw = pk.Mesh('f'); fw.cyl((.36, -.1, .3), (.32, -.1, .3), .18, IRON, seg=14)
    for a in range(4): fw.box((.34, -.1, .3), (.02, .3, .02), STEEL, rot=(a * math.pi / 4, 0, 0))
    spin(f.part('Flywheel', fw, (.34, -.1, .3)), 'X', 24, 1, 'SteamGenerator|Run')
    f.point('WorkPoint_0', (-.1, -.7, 0)); f.point('InputPoint', (-.1, -.2, .2))
    return f.finish()


ALL = [crash_wreck, school, citizen_clinic, civic_public, planter_bed, water_source, water_pump, steam_generator]
