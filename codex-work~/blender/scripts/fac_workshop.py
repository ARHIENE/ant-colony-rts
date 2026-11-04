"""Workshops: sewing table, charcoal kiln, blacksmith, coke oven, electric workshop, titanium refinery, alloy furnace,
chemistry bench. Repair uses the blacksmith (metal) and the sewing table (cloth/leather) — no shared repair bench."""
import math
from mathutils import Vector
import pk
from facilities import (Fac, spin, rock, swing, bench, gauge, wheel, chimney, lamp, panel, port, TAU, WOOD, WOOD_D, IRON, STEEL,
                        COPPER, BRICK, STONE, SOOT, GLASS, FIRE, BENCH_Z)


def sewing_table(col):
    f = Fac('SewingTable', '재봉 작업대', (1, 1), col, '기초 가공', '천·가죽 제작과 수리. 작업물은 상판 위에 놓인 별도 물건')
    m = f.body; bench(m, .62, .4, top='#b98a5a')
    m.box((.2, .08, BENCH_Z + .03), (.14, .08, .03), '#c84a5a')            # pin cushion box
    m.cyl((-.22, .1, BENCH_Z), (-.22, .1, BENCH_Z + .09), .035, '#e8d8b0', seg=10)  # thread spool stand
    m.cyl((-.22, .1, BENCH_Z + .09), (-.22, .1, BENCH_Z + .1), .045, WOOD_D, seg=10)
    m.box((.0, .12, BENCH_Z + .006), (.18, .1, .012), '#6a8ab8')           # folded cloth stock at the back
    m.box((-.05, -.05, BENCH_Z + .002), (.24, .16, .004), '#d8c8a8')        # work mat (item lies here)
    f.point('WorkPoint_0', (0, -.42, 0)); f.point('HandTarget', (-.03, -.05, BENCH_Z + .02))
    f.point('OutputPoint', (.48, -.1, 0)); f.point('InputPoint', (0, .12, BENCH_Z + .02))
    return f.finish()


def charcoal_kiln(col):
    f = Fac('CharcoalKiln', '숯가마', (2, 2), col, '제련·장비', '목재 → 숯. 앞문으로 장작 투입, 숯은 앞 바닥에 배출')
    m = f.body
    m.lathe([(0, 0), (.85, 0), (.8, .3), (.6, .6), (.3, .78), (0, .82)], '#8a6a4a', seg=16)   # earth dome
    m.box((0, -.78, .3), (.36, .12, .44), STONE)                                             # door frame (hand height)
    chimney(m, (.35, .2, .55), .45, .06, STONE)
    m.cyl((-.5, -.55, 0), (-.5, -.55, .06), .14, '#3a3230', seg=10)                         # charcoal heap spot
    door = pk.Mesh('d'); door.box((0, -.86, .3), (.26, .03, .36), WOOD_D); door.box((.09, -.88, .32), (.03, .02, .03), IRON)
    d = f.part('Door', door, (-.13, -.86, .3)); swing(d, 'Z', -1.6, 'CharcoalKiln|DoorOpen')
    glow = pk.Mesh('g'); glow.box((0, -.84, .24), (.2, .01, .22), FIRE); f.part('Glow_Running', glow, (0, -.84, .24))
    f.point('WorkPoint_0', (0, -1.15, 0)); f.point('InputPoint', (0, -.8, .3)); f.point('HandTarget', (0, -.9, .44))
    f.point('OutputPoint', (-.5, -.55, 0))
    return f.finish()


def blacksmith(col):
    f = Fac('Blacksmith', '대장간', (2, 2), col, '제련·장비', '금속 장비 제작·수리. 작업물은 모루 위에 고정, 망치 타격점=HandTarget')
    m = f.body
    m.box((-.4, .25, .22), (.7, .55, .44), BRICK); m.box((-.4, .25, .45), (.6, .45, .03), SOOT)     # forge hearth
    m.box((-.4, .25, .47), (.4, .3, .02), FIRE)                                                       # coals
    chimney(m, (-.4, .42, .47), .6, .1, BRICK)
    m.cyl((.32, -.1, 0), (.32, -.1, .255), .08, WOOD_D, seg=8)                                       # anvil stump (face at the SmithRepair hit height)
    m.box((.32, -.1, .295), (.24, .09, .08), IRON); m.cyl((.44, -.1, .315), (.53, -.1, .315), .03, IRON, seg=6, r2=.004)  # anvil + horn
    m.cyl((.62, .35, 0), (.62, .35, .16), .1, WOOD, seg=10); m.cyl((.62, .35, .14), (.62, .35, .15), .09, '#3a6a8a', seg=10)  # quench tub
    m.box((.1, .5, .3), (.3, .04, .5), WOOD_D)                                                        # tool rack
    for k in range(3): m.cyl((.0 + k * .1, .47, .5), (.0 + k * .1, .47, .25), .008, IRON, seg=4)
    bel = pk.Mesh('b')  # authored in facility space at the forge's left side, hinged at its back edge
    bel.box((-.86, .25, .3), (.1, .26, .12), '#7a4a2a'); bel.box((-.86, .25, .37), (.12, .3, .02), WOOD_D)
    bel.cyl((-.86, .1, .3), (-.75, .1, .3), .015, IRON, seg=5)
    b = f.part('Bellows', bel, (-.86, .38, .3))
    rock(b, 'X', .18, 24, 'Blacksmith|Run')
    f.point('WorkPoint_0', (.32, -.48, 0)); f.point('HandTarget', (.32, -.1, .345))         # workpiece on the anvil face
    f.point('InputPoint', (-.4, .05, .47)); f.point('OutputPoint', (.75, -.35, 0))
    return f.finish()


def coke_oven(col):
    f = Fac('CokeOven', '코크스로', (2, 2), col, '제련·장비', '석탄 → 코크스. 위 투입구로 석탄, 앞 오븐 문에서 코크스 배출')
    m = f.body
    m.box((0, .05, .4), (1.5, .8, .8), BRICK); m.box((0, .05, .82), (1.55, .85, .04), STONE)
    for x in (-.4, .4): m.cyl((x, .1, .84), (x, .1, .92), .1, IRON, seg=10)   # charging holes
    chimney(m, (.6, .35, .84), .7, .1, BRICK)
    m.box((0, -.5, .02), (1.0, .3, .04), '#4a4a50')                            # coke tray
    for i, x in enumerate((-.4, .4)):
        door = pk.Mesh('d'); door.box((x, -.37, .38), (.42, .04, .5), IRON); door.box((x + .15, -.4, .38), (.03, .03, .2), STEEL)
        d = f.part(f'Door_{i}', door, (x - .21, -.37, .38)); swing(d, 'Z', -1.4, f'CokeOven|Door{i}Open')
    glow = pk.Mesh('g'); glow.box((0, -.36, .2), (1.2, .01, .06), FIRE); f.part('Glow_Running', glow, (0, -.36, .2))
    f.point('WorkPoint_0', (0, -.95, 0)); f.point('InputPoint', (-.4, .1, .92)); f.point('HandTarget', (-.25, -.42, .445))  # door opening at the shared OvenLoad insert height
    f.point('OutputPoint', (0, -.5, .04))
    return f.finish()


def electric_workshop(col):
    f = Fac('ElectricWorkshop', '전기 가공 시설', (2, 1), col, '제련·장비', '전력으로 금속 부품 절삭. 척 회전=가동')
    m = f.body; bench(m, 1.5, .55, top='#6a7078', leg=IRON)
    m.box((-.35, .05, BENCH_Z + .1), (.4, .3, .2), '#3a7a5a')                  # lathe head stock
    m.box((.35, .05, BENCH_Z + .06), (.16, .2, .12), '#3a7a5a')                # tail stock
    m.cyl((-.15, .05, BENCH_Z + .12), (.27, .05, BENCH_Z + .12), .012, STEEL, seg=6)  # bed rod
    lamp(m, (-.5, -.11, BENCH_Z + .17))
    ch = pk.Mesh('c'); ch.cyl((-.15, .05, BENCH_Z + .12), (-.1, .05, BENCH_Z + .12), .05, STEEL, seg=8)
    for k in range(3): ch.box((-.12, .05 + .04 * math.cos(TAU * k / 3), BENCH_Z + .12 + .04 * math.sin(TAU * k / 3)), (.04, .015, .015), IRON)
    spin(f.part('Chuck', ch, (-.12, .05, BENCH_Z + .12)), 'X', 12, 1, 'ElectricWorkshop|Run')
    port(f, m, 'PowerIn', (.75, .2, .15), (1, 0, 0), 'P')
    f.point('WorkPoint_0', (0, -.6, 0)); f.point('HandTarget', (.1, -.1, BENCH_Z + .05)); f.point('ControlPoint', (-.5, -.12, BENCH_Z + .15))
    f.point('OutputPoint', (.95, -.35, 0)); f.point('InputPoint', (.25, -.1, BENCH_Z + .02))
    return f.finish()


def titanium_refinery(col):
    f = Fac('TitaniumRefinery', '티타늄 제련 설비', (2, 2), col, '제련·장비', '티타늄 광석 → 티타늄. 전력+가스 사용, 반응로 교반기 회전=가동')
    m = f.body
    m.box((0, 0, .05), (1.6, 1.6, .1), '#5a5e66')
    m.cyl((-.3, .2, .1), (-.3, .2, 1.1), .32, '#8a929c', seg=16); m.lathe([(0, 0), (.32, 0), (.2, .15), (0, .2)], '#8a929c', c=(-.3, .2, 1.1), seg=16)
    m.box((.45, .15, .35), (.5, .6, .5), BRICK); m.box((.45, -.16, .25), (.3, .02, .2), FIRE)   # furnace
    m.cyl((-.3, .2, .95), (.45, .2, .95), .04, STEEL, seg=8); m.cyl((.45, .2, .95), (.45, .2, .6), .04, STEEL, seg=8)
    gauge(m, (-.3, -.13, .7)); lamp(m, (-.55, -.05, .4))
    m.box((-.3, -.45, .12), (.3, .25, .04), '#6a7a8a')                    # ore input tray
    st = pk.Mesh('s'); st.cyl((-.3, .2, 1.3), (-.3, .2, 1.45), .02, STEEL, seg=6); st.box((-.3, .2, 1.45), (.18, .02, .04), STEEL)
    spin(f.part('Stirrer', st, (-.3, .2, 1.3)), 'Z', 24, 1, 'TitaniumRefinery|Run')
    port(f, m, 'PowerIn', (.8, .5, .15), (1, 0, 0), 'P'); port(f, m, 'FluidIn_G', (-.8, .5, .2), (-1, 0, 0), 'G')
    panel(m, (.55, -.55, .45))
    f.point('WorkPoint_0', (.0, -1.05, 0)); f.point('InputPoint', (-.3, -.45, .14)); f.point('ControlPoint', (.55, -.58, .45))
    f.point('OutputPoint', (.5, -.95, 0)); f.point('HandTarget', (.55, -.58, .47))
    return f.finish()


def alloy_furnace(col):
    f = Fac('AlloyFurnace', '합금로', (2, 2), col, '제련·장비', '두 금속 투입구(A/B) → 도가니 → 주형. 지정 제조법으로만 합금')
    m = f.body
    m.lathe([(0, 0), (.55, 0), (.55, .55), (.4, .75), (0, .78)], BRICK, seg=14)
    for i, x in enumerate((-.25, .25)):
        m.box((x, -.1, .85), (.18, .18, .1), IRON); m.box((x, -.1, .92), (.22, .22, .02), '#c07a42' if i == 0 else '#9aa2ac')  # chutes tagged by colour
    chimney(m, (0, .3, .75), .5, .08, BRICK)
    m.box((.0, -.82, .05), (.4, .2, .1), IRON); m.box((.0, -.82, .1), (.3, .12, .01), '#5a5050')       # ingot mould
    cr = pk.Mesh('c'); cr.lathe([(0, 0), (.09, 0), (.11, .14), (.1, .15), (0, .15)], '#3a3a40', c=(0, -.62, .3), seg=10)
    cr.cyl((0, -.71, .43), (0, -.76, .41), .02, '#3a3a40', seg=5); cr.cyl((0, -.62, .44), (0, -.62, .445), .095, FIRE, seg=10)
    c = f.part('Crucible', cr, (0, -.55, .35)); rock(c, 'X', .35, 48, 'AlloyFurnace|Run')
    f.point('WorkPoint_0', (0, -1.15, 0)); f.point('InputPoint', (-.25, -.1, .95)); f.point('HandTarget', (.12, -.6, .445))  # crucible rim at the shared OvenLoad insert height
    f.point('OutputPoint', (.45, -.85, 0))
    f.root['input_slots'] = {'A': 'left chute (copper colour)', 'B': 'right chute (steel colour)'}
    return f.finish()


def chemistry_bench(col):
    f = Fac('ChemistryBench', '화학 작업대', (2, 1), col, '소재', '원유 → 플라스틱 등 화학 제조법. 원유는 액체 배관 포트로 공급')
    m = f.body; bench(m, 1.4, .55, top='#d8d8d0', leg=IRON)
    m.box((0, .2, BENCH_Z + .25), (1.3, .08, .5), '#c8c8c0')
    for k in range(4): m.box((-.5 + k * .33, .17, BENCH_Z + .35), (.2, .14, .01), WOOD)          # shelf
    for k, c in enumerate(('#6ac8e8', '#e86a8a', '#a8e86a')):
        m.lathe([(0, 0), (.04, 0), (.04, .02), (.012, .07), (.012, .1), (0, .1)], pk.mat(c, alpha=.6, rough=.1), c=(-.45 + k * .12, .02, BENCH_Z), seg=10)
    m.cyl((.3, .0, BENCH_Z), (.3, .0, BENCH_Z + .06), .025, IRON, seg=8); m.cyl((.3, .0, BENCH_Z + .06), (.3, .0, BENCH_Z + .09), .01, FIRE, seg=6)
    m.box((.55, -.05, BENCH_Z + .06), (.14, .14, .12), '#e8f0f5')                                 # plastic pellet tub
    port(f, m, 'FluidIn_L', (-.7, .2, .2), (-1, 0, 0), 'L')
    f.point('WorkPoint_0', (0, -.6, 0)); f.point('HandTarget', (0, -.15, BENCH_Z + .03)); f.point('OutputPoint', (.9, -.35, 0))
    f.point('InputPoint', (0, -.1, BENCH_Z + .02))
    return f.finish()


ALL = [sewing_table, charcoal_kiln, blacksmith, coke_oven, electric_workshop, titanium_refinery, alloy_furnace, chemistry_bench]
