"""Extraction: underground prospector, deep drill (driven), advanced drill (automatic), oil derrick, gas extractor.
Deposit data stays separate (deposits.py): removing/re-placing a machine never resets the deposit visuals."""
import math
from mathutils import Vector
import pk
from facilities import Fac, spin, rock, swing, gauge, wheel, lamp, panel, port, TAU, WOOD, WOOD_D, IRON, STEEL, BRICK, STONE, SOOT, FIRE

YEL, ORG = '#e0b830', '#d86a2a'


def prospector(col):
    f = Fac('Prospector', '지하 탐사기', (2, 2), col, '채취', '전력 + 장수 조작으로 깊은 매장층 탐사. 탐침이 오르내림=가동, 조작=ControlPanel')
    m = f.body
    for a in (0, TAU / 3, 2 * TAU / 3):
        d = Vector((math.cos(a + .5), math.sin(a + .5), 0)); m.cyl(d * .55, Vector((0, .1, 1.1)), .025, YEL, seg=6)
    m.box((0, .1, 1.12), (.2, .2, .1), IRON); m.cyl((0, .1, 0), (0, .1, .06), .16, '#5a5e66', seg=12)
    panel(m, (.0, -.55, .42)); port(f, m, 'PowerIn', (.8, .4, .12), (1, 0, 0), 'P'); lamp(m, (.08, .0, 1.12))
    pr = pk.Mesh('p'); pr.cyl((0, .1, .1), (0, .1, 1.05), .03, STEEL, seg=8); pr.cyl((0, .1, .1), (0, .1, .03), .03, STEEL, seg=8, r2=.004)
    p = f.part('Probe', pr, (0, .1, 1.05)); p['rest'] = list(p.location); rock(p, 'Z', 0, 48, 'Prospector|Run', loc=(0, 0, .05))
    f.point('WorkPoint_0', (0, -.95, 0)); f.point('ControlPoint', (0, -.58, .42)); f.point('HandTarget', (0, -.6, .44))
    return f.finish()


def deep_drill(col):
    f = Fac('DeepDrill', '심층 채굴기', (3, 3), col, '채취', '작업자가 운전석에서 조작(DriveSeated), 전력 필요. 드릴 회전=가동, 컨베이어 끝=OutputPoint')
    m = f.body
    m.box((0, .2, .12), (2.4, 2.0, .24), '#5a5e66')
    for x in (-.9, .9): m.box((x, .5, 1.0), (.14, .14, 1.6), YEL)
    m.box((0, .5, 1.85), (2.0, .2, .2), YEL); m.box((0, .5, 1.5), (.5, .4, .5), IRON)
    m.box((-.75, -.55, .5), (.6, .55, .55), '#c8c8c0'); m.box((-.75, -.55, .82), (.65, .6, .05), YEL)  # cab
    m.box((-.75, -.52, .38), (.3, .28, .06), '#3a3a40')                                               # seat
    for x in (-.85, -.65): m.cyl((x, -.36, .36), (x, -.33, .5), .012, IRON, seg=5)                    # two levers in front of the seat
    m.box((.85, -.7, .25), (.35, .8, .06), '#3a3a40', rot=(.25, 0, 0))                               # conveyor
    port(f, m, 'PowerIn', (1.25, .9, .2), (1, 0, 0), 'P')
    dr = pk.Mesh('d'); dr.cyl((0, .5, 1.25), (0, .5, .3), .08, STEEL, seg=8); dr.cyl((0, .5, .3), (0, .5, .1), .08, STEEL, seg=8, r2=.01)
    for k in range(6): dr.cyl((0, .5, .35 + k * .14), (0, .5, .42 + k * .14), .1, IRON, seg=8)
    spin(f.part('Drill', dr, (0, .5, 1.25)), 'Z', 12, 1, 'DeepDrill|Run')
    f.point('SeatPoint', (-.75, -.52, .41), (0, 1, 0)); f.point('WorkPoint_0', (-.75, -1.3, 0)); f.point('HandTarget', (-.65, -.33, .5))
    f.point('OutputPoint', (.85, -1.2, 0)); f.point('ControlPoint', (-.65, -.33, .5))
    return f.finish()


def advanced_drill(col):
    f = Fac('AdvancedDrill', '고급 채굴기', (2, 2), col, '채취', '자동 채굴(전력). 장수는 설치·수리만. 드릴 회전=가동, 배출구=OutputPoint')
    m = f.body
    m.box((0, 0, .1), (1.5, 1.5, .2), '#4a4e56')
    m.cyl((0, .1, .2), (0, .1, 1.6), .28, '#8a929c', seg=12); m.cyl((0, .1, 1.6), (0, .1, 1.7), .34, ORG, seg=12)
    m.box((.45, -.45, .45), (.3, .3, .5), '#8a929c'); m.box((.45, -.7, .3), (.25, .3, .05), IRON, rot=(.5, 0, 0))   # output chute
    lamp(m, (-.15, -.2, 1.5)); gauge(m, (.15, -.19, 1.2), .04)
    panel(m, (-.5, -.6, .45)); port(f, m, 'PowerIn', (.75, .5, .15), (1, 0, 0), 'P')
    rot = pk.Mesh('r'); rot.cyl((0, .1, 1.7), (0, .1, 1.95), .05, STEEL, seg=8); rot.box((0, .1, 1.95), (.5, .05, .05), ORG)
    spin(f.part('Rotor', rot, (0, .1, 1.7)), 'Z', 16, 1, 'AdvancedDrill|Run')
    f.point('WorkPoint_0', (-.5, -1.05, 0)); f.point('ControlPoint', (-.5, -.63, .45)); f.point('HandTarget', (-.5, -.65, .47))
    f.point('OutputPoint', (.45, -1.0, 0))
    return f.finish()


def oil_derrick(col):
    f = Fac('OilDerrick', '원유 시추기', (2, 2), col, '채취', '매장 지점 위 설치, 전력 자동 추출. 액체 배관 출구(FluidOut_L). 펌프잭 끄덕임=가동')
    m = f.body
    m.box((0, 0, .08), (1.6, 1.0, .16), '#5a5e66')
    m.box((.1, 0, .55), (.12, .3, .8), IRON); m.cyl((-.6, 0, .16), (-.6, 0, .5), .07, STEEL, seg=10)   # samson post + wellhead
    m.box((.55, 0, .3), (.3, .35, .3), '#7a3a2a')                                                        # motor
    port(f, m, 'FluidOut_L', (-.8, .0, .3), (-1, 0, 0), 'L'); port(f, m, 'PowerIn', (.8, .3, .15), (1, 0, 0), 'P')
    beam = pk.Mesh('b'); beam.box((-.15, 0, .97), (1.0, .08, .08), ORG); beam.lathe([(0, 0), (.12, 0), (.12, .2), (0, .25)], ORG, c=(-.65, 0, .8), seg=8, sx=.5)
    beam.cyl((-.62, 0, .8), (-.6, 0, .5), .01, '#2a2a2a', seg=4)
    rock(f.part('Beam', beam, (.1, 0, .97)), 'Y', .3, 48, 'OilDerrick|Run')
    cr = pk.Mesh('c'); cr.box((.55, -.2, .35), (.25, .04, .08), IRON); cr.box((.66, -.2, .35), (.12, .06, .14), '#3a3a40')
    spin(f.part('Crank', cr, (.55, -.2, .35)), 'Y', 48, 1, 'OilDerrick|Crank')
    f.point('WorkPoint_0', (0, -.85, 0)); f.point('HandTarget', (.55, -.2, .45))
    return f.finish()


def gas_extractor(col):
    f = Fac('GasExtractor', '가스 추출기', (2, 2), col, '채취', '가스 틈 위 설치, 전력 자동 추출. 기체 배관 출구(FluidOut_G). 압축기 팬 회전=가동, 밸브=ValveTurn')
    m = f.body
    m.box((0, 0, .08), (1.5, 1.2, .16), '#5a5e66')
    m.cyl((-.35, 0, .16), (-.35, 0, .7), .1, YEL, seg=10); m.box((-.35, 0, .7), (.3, .12, .08), '#e0b830')  # wellhead tree
    m.box((.35, .05, .4), (.5, .5, .48), '#c8c8c0'); gauge(m, (.35, -.21, .55), .05)                                     # compressor
    m.cyl((-.25, 0, .5), (.1, .05, .5), .035, '#e0b830', seg=8)
    port(f, m, 'FluidOut_G', (.75, .2, .35), (1, 0, 0), 'G'); port(f, m, 'PowerIn', (.75, -.3, .15), (1, 0, 0), 'P')
    fan = pk.Mesh('f'); fan.cyl((.35, .31, .5), (.35, .33, .5), .03, IRON, seg=8)
    for k in range(4): fan.box((.35 + .08 * math.cos(TAU * k / 4), .32, .5 + .08 * math.sin(TAU * k / 4)), (.12, .01, .04), IRON, rot=(0, -TAU * k / 4, 0))
    spin(f.part('Fan', fan, (.35, .32, .5)), 'Y', 8, 1, 'GasExtractor|Run')
    wh = pk.Mesh('w'); wheel(wh, (-.35, -.12, .62), .06, (0, -1, 0), '#c8402a')
    spin(f.part('Valve', wh, (-.35, -.12, .62)), 'Y', 24, 1, 'GasExtractor|ValveTurn')
    f.point('WorkPoint_0', (-.35, -.75, 0)); f.point('HandTarget', (-.35, -.18, .62)); f.point('ControlPoint', (-.35, -.18, .62))
    return f.finish()


ALL = [prospector, deep_drill, advanced_drill, oil_derrick, gas_extractor]
