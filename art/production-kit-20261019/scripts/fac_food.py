"""Food facilities: mill, oil press, brewing barrel, still; greenhouse modules. Cooking reuses resident Kitchen/Hearth."""
import math
from mathutils import Vector
import pk
from facilities import Fac, spin, rock, swing, bench, gauge, wheel, chimney, lamp, port, TAU, WOOD, WOOD_D, IRON, STEEL, COPPER, BRICK, STONE, GLASS, FIRE, BENCH_Z


def mill(col):
    f = Fac('Mill', '제분기', (2, 2), col, '식품', '곡물 → 가루. 위 깔때기에 곡물, 돌 사이로 가루가 앞 자루에 떨어짐. 윗돌 회전=가동')
    m = f.body
    m.cyl((0, 0, 0), (0, 0, .28), .5, WOOD_D, seg=16); m.cyl((0, 0, .28), (0, 0, .36), .42, STONE, seg=16)   # base + bed stone
    m.box((0, -.5, .12), (.2, .16, .08), WOOD); m.cyl((0, -.6, 0), (0, -.6, .14), .07, '#d8c8a0', seg=10)    # spout + flour sack
    top = pk.Mesh('t'); top.cyl((0, 0, .36), (0, 0, .46), .4, STONE, seg=16); top.cyl((0, 0, .46), (0, 0, .47), .08, '#5a5550', seg=10)
    top.lathe([(.06, 0), (.18, .2), (.2, .22), (.04, .02)], WOOD, c=(0, 0, .5), seg=10)                          # hopper
    top.cyl((.3, 0, .46), (.3, 0, .66), .02, WOOD_D, seg=6)                                                       # crank handle
    spin(f.part('TopStone', top, (0, 0, .36)), 'Z', 48, 1, 'Mill|Run')
    f.point('WorkPoint_0', (0, -1.05, 0)); f.point('InputPoint', (0, 0, .72)); f.point('HandTarget', (.3, 0, .62))
    f.point('OutputPoint', (0, -.6, 0)); f.point('CarryPoint', (0, -.6, .14))
    return f.finish()


def oil_press(col):
    f = Fac('OilPress', '착유기', (1, 2), col, '식품', '씨앗·열매 → 기름. 지렛대를 눌러 나사 압착, 아래 통에 기름. PressLever 동작의 HandTarget')
    m = f.body
    m.box((0, 0, .06), (.7, .9, .12), WOOD_D)
    for x in (-.25, .25): m.box((x, .1, .45), (.08, .08, .7), WOOD)
    m.box((0, .1, .82), (.62, .1, .08), WOOD)
    m.cyl((0, .1, .14), (0, .1, .3), .16, WOOD, seg=12); m.cyl((0, .1, .3), (0, .1, .31), .15, '#c8a840', seg=12)  # press basket + oil
    m.cyl((0, -.3, .12), (0, -.3, .22), .08, '#c8a040', seg=10)                                                      # collecting pot
    m.box((0, -.12, .15), (.06, .2, .02), WOOD_D)                                                                  # spout
    lev = pk.Mesh('l'); lev.cyl((0, .1, .78), (0, .1, .36), .025, IRON, seg=6); lev.box((0, .1, .36), (.22, .22, .04), WOOD_D)
    lev.cyl((0, .1, .78), (0, -.45, .78), .02, WOOD_D, seg=6)
    rock(f.part('Lever', lev, (0, .1, .78)), 'X', .25, 32, 'OilPress|Run')
    f.point('WorkPoint_0', (0, -.75, 0)); f.point('HandTarget', (0, -.42, .66)); f.point('InputPoint', (0, .1, .32))  # lever end at the press
    f.point('OutputPoint', (.4, -.4, 0)); f.point('CarryPoint', (0, -.3, .22))
    return f.finish()


def brewing_barrel(col):
    f = Fac('BrewingBarrel', '양조통', (1, 1), col, '식품', '묶음당 한 종류 발효. 뚜껑 열고 원료 투입 → 닫고 무인 발효 → 꼭지로 회수. 발효 중 상주 작업 없음')
    m = f.body
    m.box((0, 0, .2), (.6, .5, .04), WOOD_D)
    for x in (-.22, .22): m.box((x, 0, .1), (.06, .45, .2), WOOD_D)
    m.lathe([(0, 0), (.22, 0), (.26, .2), (.22, .4), (0, .4)], WOOD, c=(0, 0, .22), seg=14)
    for z in (.28, .46): m.torus((0, 0, z), .25, .008, IRON, seg=14, tseg=4)
    m.cyl((0, -.22, .29), (0, -.3, .29), .015, '#8a6a3a', seg=6)                        # tap
    lid = pk.Mesh('l'); lid.cyl((0, 0, .62), (0, 0, .64), .21, WOOD_D, seg=14); lid.box((0, 0, .66), (.12, .03, .03), WOOD_D)
    swing(f.part('Lid', lid, (-.2, 0, .63)), 'Y', -1.4, 'BrewingBarrel|LidOpen')
    air = pk.Mesh('a'); air.cyl((.1, .05, .64), (.1, .05, .72), .015, pk.mat('#e8f4f0', alpha=.5, rough=.05), seg=6); air.ball((.1, .05, .7), (.008,) * 3, '#ffffff', seg=5, rings=3)
    a = f.part('Airlock_Fermenting', air, (.1, .05, .64)); a['rest'] = list(a.location)
    rock(a, 'Z', 0, 24, 'BrewingBarrel|Fermenting', loc=(0, 0, .004))
    f.point('WorkPoint_0', (0, -.5, 0)); f.point('InputPoint', (0, 0, .62)); f.point('HandTarget', (0, -.3, .29))
    f.point('ChargePoint', (0, -.36, 0)); f.point('OutputPoint', (.35, -.3, 0))
    f.root['states'] = {'idle': 'lid closed, airlock hidden', 'fermenting': 'Airlock_Fermenting shown + BrewingBarrel|Fermenting', 'ready': 'airlock hidden'}
    return f.finish()


def still(col):
    f = Fac('Still', '증류기', (2, 1), col, '식품', '완성 발효주 → 증류주. 화실 연료 + 구리 솥 → 냉각 코일 → 받는 단지. 밸브 조작=ValveTurn')
    m = f.body
    m.box((-.35, 0, .15), (.45, .45, .3), BRICK); m.box((-.35, -.23, .1), (.2, .02, .12), FIRE)
    m.lathe([(0, 0), (.2, 0), (.24, .15), (.15, .32), (.05, .4), (.04, .5), (0, .5)], COPPER, c=(-.35, 0, .3), seg=14)
    m.cyl((-.35, 0, .78), (.25, 0, .65), .02, COPPER, seg=6)
    m.cyl((.35, 0, 0), (.35, 0, .55), .17, WOOD, seg=12)                                # condenser barrel
    for k in range(3): m.torus((.35, 0, .2 + k * .12), .19, .012, COPPER, seg=12, tseg=4)
    m.cyl((.5, -.08, .12), (.58, -.08, .12), .01, COPPER, seg=5); m.cyl((.62, -.08, 0), (.62, -.08, .1), .05, '#d8c8a0', seg=10)  # outlet + jug
    gauge(m, (-.35, -.2, .5), .035)
    m.cyl((0, 0, .62), (0, -.05, .62), .01, COPPER, seg=5)
    wh = pk.Mesh('w'); wheel(wh, (.0, -.06, .62), .045, (0, -1, 0), '#c8402a')
    spin(f.part('Valve', wh, (0, -.06, .62)), 'Y', 24, 1, 'Still|ValveTurn')
    f.point('WorkPoint_0', (0, -.6, 0)); f.point('HandTarget', (0, -.1, .62)); f.point('InputPoint', (-.35, 0, .82))
    f.point('ChargePoint', (.62, -.08, 0)); f.point('OutputPoint', (.75, -.35, 0)); f.point('FuelPoint', (-.35, -.3, .1))
    return f.finish()


def greenhouse(col):
    """Modules: wall panel, corner post, roof panel, door, vent. 1 cell wide, 1.4 m high wall."""
    out = []
    frame = '#e8e8e0'
    f = Fac('Greenhouse_Wall', '온실 벽', (1, 0), col, '재배·보관', '조립 모듈: 1칸 벽 패널(유리)')
    m = f.body; m.box((0, 0, .05), (1.0, .06, .1), frame); m.box((0, 0, 1.38), (1.0, .05, .04), frame)
    for x in (-.5, 0, .5): m.box((x, 0, .7), (.04, .05, 1.4), frame)
    m.box((0, 0, .74), (.96, .01, 1.26), GLASS); out.append(f.finish())
    f = Fac('Greenhouse_Corner', '온실 모서리 기둥', (0, 0), col, '재배·보관', '조립 모듈: 모서리 기둥')
    f.body.box((0, 0, .72), (.07, .07, 1.44), frame); out.append(f.finish())
    f = Fac('Greenhouse_Roof', '온실 지붕', (1, 1), col, '재배·보관', '조립 모듈: 1×1칸 경사 지붕 패널(벽 위 1.4 m)')
    m = f.body
    for y in (-.5, 0, .5): m.box((0, y, 1.6), (1.0, .04, .04), frame)
    m.box((0, 0, 1.6), (.98, .98, .01), GLASS, rot=(.12, 0, 0)); out.append(f.finish())
    f = Fac('Greenhouse_Door', '온실 문', (1, 0), col, '재배·보관', '조립 모듈: 여닫이 유리문')
    m = f.body; m.box((0, 0, 1.38), (1.0, .05, .04), frame)
    for x in (-.5, .5): m.box((x, 0, .7), (.05, .06, 1.4), frame)
    dr = pk.Mesh('d')
    for x in (-.43, .43): dr.box((x, 0, .65), (.04, .03, 1.25), frame)   # border strips only, glass in between
    for z in (.04, .65, 1.26): dr.box((0, 0, z), (.9, .03, .04), frame)
    dr.box((0, 0, .65), (.82, .01, 1.2), GLASS); dr.box((.35, -.03, .65), (.04, .02, .1), STEEL)
    swing(f.part('Door', dr, (-.45, 0, .65)), 'Z', -1.5, 'Greenhouse_Door|Open')
    f.point('WorkPoint_0', (0, -.6, 0)); out.append(f.finish())
    f = Fac('Greenhouse_Vent', '온실 환기창', (1, 0), col, '재배·보관', '조립 모듈: 벽 위 환기창(열림 회전)')
    m = f.body; m.box((0, 0, 1.38), (1.0, .05, .04), frame)
    v = pk.Mesh('v'); v.box((0, 0, 1.25), (.9, .03, .24), frame); v.box((0, -.02, 1.25), (.84, .01, .2), GLASS)
    rock(f.part('Vent', v, (0, 0, 1.37)), 'X', .5, 96, 'Greenhouse_Vent|Run'); out.append(f.finish())
    return out


ALL = [mill, oil_press, brewing_barrel, still, greenhouse]
