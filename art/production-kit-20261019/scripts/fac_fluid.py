"""Fluids & energy: refinery, filling/discharge station, gas generator, liquid & gas tanks, pipe modules.
Liquid pipes = thick blue + white flange bands; gas pipes = thinner yellow + red bands (told apart by colour AND size).
Modules are 1 cell (1 m), ports at the cell edge centre, axis X/Y, height 0.25 m — they snap end to end."""
import math
from mathutils import Vector
import pk
from facilities import (Fac, spin, rock, swing, gauge, wheel, lamp, panel, port, TAU, WOOD, WOOD_D, IRON, STEEL, BRICK, STONE, SOOT,
                        FIRE, GLASS, LIQ_PIPE, LIQ_BAND, GAS_PIPE, GAS_BAND, OIL, FUEL, GAS_C)

PIPE_Z = .25
SPEC = {'L': ('액체', LIQ_PIPE, LIQ_BAND, .055), 'G': ('기체', GAS_PIPE, GAS_BAND, .04)}


def refinery(col):
    f = Fac('Refinery', '정유소', (3, 3), col, '유체', '원유(액체 포트) → 정제 연료. 전력·작업자 필요. 증류탑 + 가열로, 결과 드럼은 OutputPoint 바닥')
    m = f.body
    m.box((0, 0, .06), (2.6, 2.4, .12), '#5a5e66')
    m.cyl((-.5, .4, .12), (-.5, .4, 2.2), .3, '#b8bec6', seg=14); m.lathe([(0, 0), (.3, 0), (0, .2)], '#b8bec6', c=(-.5, .4, 2.2), seg=14)
    for z in (.6, 1.1, 1.6): m.torus((-.5, .4, z), .31, .02, IRON, seg=14, tseg=4)
    m.box((.6, .5, .4), (.8, .7, .7), BRICK); m.box((.6, .14, .3), (.4, .02, .2), FIRE)
    m.cyl((-.5, .4, 1.8), (.6, .4, 1.8), .05, STEEL, seg=8); m.cyl((.6, .4, 1.8), (.6, .4, .75), .05, STEEL, seg=8)
    m.cyl((.9, -.6, .12), (.9, -.6, .55), .16, FUEL, seg=12)                               # product drum on the pad
    gauge(m, (-.5, .09, .9)); lamp(m, (.3, .14, .7))
    panel(m, (-.2, -.75, .45))
    port(f, m, 'FluidIn_L', (-1.3, .4, PIPE_Z), (-1, 0, 0), 'L'); port(f, m, 'PowerIn', (1.3, .9, .15), (1, 0, 0), 'P')
    pu = pk.Mesh('p'); pu.cyl((-1.0, -.2, .3), (-1.0, -.2, .45), .08, '#3a6a8a', seg=10); pu.box((-1.0, -.2, .48), (.14, .04, .04), IRON)
    spin(f.part('PumpWheel', pu, (-1.0, -.2, .3)), 'Z', 16, 1, 'Refinery|Run')
    f.point('WorkPoint_0', (-.2, -1.25, 0)); f.point('ControlPoint', (-.2, -.78, .45)); f.point('HandTarget', (-.2, -.8, .47))
    f.point('OutputPoint', (.9, -.95, 0))
    return f.finish()


def filling_station(col):
    f = Fac('FillingStation', '충전·배출 설비', (2, 1), col, '유체',
            '원유 드럼(왼쪽)·가스 압축 용기(오른쪽) 충전/배출. 바닥 ChargePoint에 용기를 놓고 연결(CanisterConnect)→밸브(ValveTurn)→회수')
    m = f.body
    m.box((0, .25, .5), (1.6, .2, 1.0), '#8a929c')
    m.box((-.45, -.05, .02), (.5, .5, .04), '#3a3a40'); m.box((.45, -.05, .02), (.5, .5, .04), '#3a3a40')   # two bays
    m.cyl((-.45, .1, .6), (-.45, -.15, .34), .025, LIQ_PIPE, seg=8); m.cyl((.45, .1, .6), (.45, -.15, .34), .018, GAS_PIPE, seg=8)  # hoses reach the container top
    gauge(m, (-.45, .14, .8), .05); gauge(m, (.45, .14, .8), .05)
    m.box((-.45, .14, .95), (.3, .02, .08), LIQ_PIPE); m.box((.45, .14, .95), (.3, .02, .08), GAS_PIPE)     # bay colour signs
    port(f, m, 'FluidIn_L', (-.8, .25, PIPE_Z), (-1, 0, 0), 'L'); port(f, m, 'FluidIn_G', (.8, .25, PIPE_Z), (1, 0, 0), 'G')
    for i, (x, c) in enumerate(((-.45, '#c8402a'), (.45, '#c8402a'))):
        wh = pk.Mesh('w'); wheel(wh, (x + .2, .14, .6), .045, (0, -1, 0), c)
        spin(f.part(f'Valve_{"L" if i == 0 else "G"}', wh, (x + .2, .14, .6)), 'Y', 24, 1, f'FillingStation|Valve{"L" if i == 0 else "G"}')
    f.point('ChargePoint_Drum', (-.45, -.1, .04)); f.point('ChargePoint_Canister', (.45, -.1, .04))
    f.point('WorkPoint_0', (-.45, -.6, 0)); f.point('WorkPoint_1', (.45, -.6, 0))
    f.point('HandTarget', (-.45, -.15, .32)); f.point('HandTarget_1', (.45, -.15, .32))  # coupling on the drum/canister top
    return f.finish()


def gas_generator(col):
    f = Fac('GasGenerator', '가스 발전기', (2, 2), col, '에너지', '천연가스(기체 포트) → 전력(PowerOut, 기존 전선과 연결). 플라이휠 회전=가동')
    m = f.body
    m.box((0, 0, .08), (1.5, 1.0, .16), '#4a4e56')
    m.box((-.3, 0, .45), (.6, .6, .58), '#3a6a5a'); m.box((.4, 0, .4), (.5, .5, .48), '#c8c8c0')
    m.cyl((-.3, .2, .74), (-.3, .2, 1.2), .05, SOOT, seg=8)                                        # exhaust
    gauge(m, (.4, -.26, .5), .05); lamp(m, (.25, -.26, .6))
    port(f, m, 'FluidIn_G', (-.75, .3, PIPE_Z), (-1, 0, 0), 'G'); port(f, m, 'PowerOut', (.75, .3, .2), (1, 0, 0), 'P')
    fw = pk.Mesh('f'); fw.cyl((-.62, 0, .4), (-.66, 0, .4), .2, IRON, seg=14)
    for k in range(4): fw.box((-.67, .12 * math.cos(TAU * k / 4), .4 + .12 * math.sin(TAU * k / 4)), (.02, .05, .05), STEEL)
    spin(f.part('Flywheel', fw, (-.64, 0, .4)), 'X', 12, 1, 'GasGenerator|Run')
    f.point('WorkPoint_0', (.4, -.75, 0)); f.point('HandTarget', (.4, -.27, .5))
    f.root['connects_to'] = 'resident-kit CC_WoodGenerator / CC_PowerWire / CC_Battery (same PowerIn/PowerOut points)'
    return f.finish()


def tank(col, kind):
    ko, pc, bc, r = SPEC[kind]; fid = 'LiquidTank' if kind == 'L' else 'GasTank'
    f = Fac(fid, f'{ko} 저장 탱크', (2, 2), col, '유체', f'내용물 한 종류. 표시 3단계 Level_Empty/Partial/Full(엔진이 하나만 표시). 입출구 포트 {ko}')
    m = f.body
    if kind == 'L':
        m.cyl((0, 0, .1), (0, 0, 1.4), .65, '#b8bec6', seg=18); m.lathe([(0, 0), (.65, 0), (0, .18)], '#b8bec6', c=(0, 0, 1.4), seg=18)
        m.box((0, 0, .05), (1.5, 1.5, .1), '#5a5e66')
        m.box((0, -.645, .75), (.14, .02, 1.1), '#e8e8e0')                                 # gauge window (light, so the dark level reads)
        for k in range(3): m.torus((0, 0, .4 + k * .4), .66, .015, pc, seg=18, tseg=4)
        levels = {'Empty': .02, 'Partial': .5, 'Full': 1.0}
        for nm, h in levels.items():
            lv = pk.Mesh('l'); lv.box((0, -.66, .22 + .5 * h), (.1, .01, max(.02, 1.0 * h)), pk.mat(OIL if h else '#555', emit=.3))
            o = f.part('Level_' + nm, lv, (0, -.66, .2)); o.hide_render = o.hide_viewport = nm != 'Partial'
    else:
        m.ball((0, 0, .85), (.6, .6, .6), '#e8e8e0', seg=18, rings=10)
        for a in range(4): d = Vector((math.cos(TAU * a / 4 + .78), math.sin(TAU * a / 4 + .78), 0)); m.cyl(d * .5, d * .42 + Vector((0, 0, .7)), .04, pc, seg=6)
        m.torus((0, 0, .85), .6, .02, pc, seg=18, tseg=4)
        gauge(m, (0, -.6, .85), .07)
        for nm, a in {'Empty': 1.2, 'Partial': 0, 'Full': -1.2}.items():   # dial needle angle = fill
            nd = pk.Mesh('n'); nd.box((0, -.62, .85 + .025), (.008, .004, .05), '#c8402a')
            o = f.part('Level_' + nm, nd, (0, -.62, .85)); o.rotation_euler = (0, a, 0); o.hide_render = o.hide_viewport = nm != 'Partial'
    port(f, m, f'FluidIn_{kind}', (-.75, .3, PIPE_Z), (-1, 0, 0), kind); port(f, m, f'FluidOut_{kind}', (.75, .3, PIPE_Z), (1, 0, 0), kind)
    f.point('WorkPoint_0', (0, -1.0, 0))
    f.root['levels'] = 'Level_Empty (0%), Level_Partial (1–99%), Level_Full (100%)'
    return f.finish()


def pipe(col, kind, shape):
    ko, pc, bc, r = SPEC[kind]
    names = {'Straight': '직선', 'Corner': '회전', 'Tee': '분기(T)', 'Cross': '교차', 'End': '끝단', 'Valve': '밸브', 'Riser': '세움(시설 연결)'}
    fid = f'Pipe{kind}_{shape}'
    f = Fac(fid, f'{ko} 배관 {names[shape]}', (1, 1), col, '유체', f'{ko} 배관 모듈. 끝점 = 칸 가장자리 중앙, 높이 {PIPE_Z} m')
    m = f.body; c = Vector((0, 0, PIPE_Z))
    dirs = {'Straight': ['-X', '+X'], 'Corner': ['-X', '+Y'], 'Tee': ['-X', '+X', '+Y'], 'Cross': ['-X', '+X', '-Y', '+Y'],
            'End': ['-X'], 'Valve': ['-X', '+X'], 'Riser': ['-X']}[shape]
    V = {'-X': Vector((-1, 0, 0)), '+X': Vector((1, 0, 0)), '-Y': Vector((0, -1, 0)), '+Y': Vector((0, 1, 0))}
    for d in dirs:
        e = c + V[d] * .5; m.cyl(c, e, r, pc, seg=10); m.cyl(e - V[d] * .03, e, r * 1.3, bc, seg=10)
        f.point(f'Port{d}', tuple(e), tuple(V[d]))
    m.ball(c, (r * 1.05,) * 3, pc, seg=10, rings=6)
    for k in (-1, 1):  # floor stands
        m.cyl((k * .3 if shape != 'Corner' else -.3, 0, 0), (k * .3 if shape != 'Corner' else -.3, 0, PIPE_Z - r), .012, IRON, seg=5)
    if shape == 'End': m.cyl(c + Vector((.02, 0, 0)), c - Vector((.02, 0, 0)), r * 1.4, bc, seg=10)
    if shape == 'Riser': m.cyl(c, c + Vector((0, 0, .5)), r, pc, seg=10); m.cyl(c + Vector((0, 0, .47)), c + Vector((0, 0, .5)), r * 1.3, bc, seg=10)
    if shape == 'Valve':
        m.box(c, (r * 2.4, r * 2.4, r * 2.4), IRON); m.cyl(c, c + Vector((0, 0, .12)), .01, IRON, seg=5)
        wh = pk.Mesh('w'); wheel(wh, c + Vector((0, 0, .13)), .05, (0, 0, 1), bc)
        spin(f.part('Wheel', wh, c + Vector((0, 0, .13))), 'Z', 24, 1, f'{fid}|Turn')
        f.point('HandTarget', tuple(c + Vector((0, 0, .14)))); f.point('WorkPoint_0', (0, -.55, 0))
    f.root['ports'] = dirs; f.root['fluid'] = 'liquid' if kind == 'L' else 'gas'
    return f.finish()


def all_pipes(col):
    return [pipe(col, k, s) for k in 'LG' for s in ('Straight', 'Corner', 'Tee', 'Cross', 'End', 'Valve', 'Riser')]


ALL = [refinery, filling_station, gas_generator, lambda c: tank(c, 'L'), lambda c: tank(c, 'G'), all_pipes]
