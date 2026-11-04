"""작업 클립 포즈 정의(24fps). 축·각도는 아마추어 공간 기준, 레스트 대비.
앞다리 leg.01 = 도구·손, 가운데 leg.02 = 받치기·균형, 뒷다리 leg.03 = 직립 지지.
X축 음수: 몸통은 앞 들기, 다리는 앞으로 뻗기. 머리·더듬이 X축 양수: 숙이기.
좌우 대칭은 Y·Z 회전 부호를 뒤집는다(sym)."""
from mathutils import Vector
X, Y, Z = Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1))

def P(**bones):
    return {k.replace('_', '.'): v for k, v in bones.items()}

def sym(side, rots):
    """L 기준 회전 목록을 R로 미러(Y·Z 부호 반전)."""
    return rots if side == 'L' else [(a, d if a is X else -d) for a, d in rots]

def legs(name, rots_l, rots_r=None):
    return {f'{name}.L': dict(rot=sym('L', rots_l)), f'{name}.R': dict(rot=sym('R', rots_r if rots_r is not None else rots_l))}

def merge(*poses):
    out = {}
    for p in poses:
        for k, v in p.items(): out[k] = v
    return out

def body(pitch, z=0.0, y=0.0, yaw=0.0, roll=0.0):
    return {'body': dict(rot=[(X, pitch), (Z, yaw), (Y, roll)], loc=(0, y, z))}

def head(nod, turn=0.0):
    return {'head': dict(rot=[(X, nod), (Z, turn)])}

def ant(droop_l, droop_r=None, spread=0.0):
    return merge({'antenna.L': dict(rot=[(X, droop_l), (Y, -spread)])},
                 {'antenna.R': dict(rot=[(X, droop_r if droop_r is not None else droop_l), (Y, spread)])})

BASE = {}
# 직립: 몸 앞을 들고 뒷다리로 지지, 머리는 다시 수평, 가운데 다리는 바깥으로 벌려 균형
STAND = merge(body(-55, .022), head(35), ant(-10, spread=5), legs('leg.03', [(X, 55), (Y, -8)]),
              legs('leg.02', [(X, 30), (Y, -20)]), legs('leg.01', [(X, -10), (Z, -10)]))
CROUCH = merge(body(12, -.008), head(15), ant(15), legs('leg.01', [(X, -35)]), legs('leg.02', [(Y, -10)]))
GRAB = merge(body(18, -.012), head(25), ant(20), legs('leg.01', [(X, -45), (Z, -25)]), legs('leg.02', [(Y, -12)]))
HOLD = merge(body(-10, .004), head(5), ant(-5), legs('leg.01', [(X, -62), (Z, -12)]))  # 앞다리로 물건을 머리 앞에 받침

def stand(*extra):
    return merge(STAND, *extra)

def crouch(*extra):
    return merge(CROUCH, *extra)

CLIPS = [
    # (이름, [(frame, pose)], loop)  — 전환 .2~.4s, 집기/놓기 .4~.8s, 작업 루프 .8~1.6s
    ('StandEnter', [(1, BASE), (5, merge(body(-30, .008), head(18), legs('leg.03', [(X, 30)]))), (8, STAND)], False),
    ('StandExit', [(1, STAND), (5, merge(body(-25, .006), head(15), legs('leg.03', [(X, 25)]))), (9, BASE)], False),
    ('StandIdle', [(1, STAND), (13, stand(body(-52, .013), ant(-4, -14, 8))), (25, STAND)], True),

    ('Pickup', [(1, BASE), (7, GRAB), (11, merge(GRAB, legs('leg.01', [(X, -50), (Z, -38)]))), (17, HOLD)], False),
    ('Carry', 'Walk', merge(HOLD, {'body': None}), True),  # 기존 Walk + 앞다리·머리 운반 자세 덮어쓰기
    ('PutDown', [(1, HOLD), (7, merge(GRAB, legs('leg.01', [(X, -50), (Z, -38)]))), (11, GRAB), (13, merge(GRAB, legs('leg.01', [(X, -30), (Z, -5)]))), (17, BASE)], False),

    # 낮은 자세: 앞다리 교대로 긁기, 두 번째 박자는 진폭을 바꿔 단조로움을 줄임
    ('GroundWork', [(1, crouch(legs('leg.01', [(X, -50)], [(X, -15)]))),
                    (7, crouch(body(15, -.010), legs('leg.01', [(X, -12)], [(X, -48)]))),
                    (13, crouch(legs('leg.01', [(X, -42), (Z, -10)], [(X, -18)]))),
                    (19, crouch(body(14, -.011), legs('leg.01', [(X, -15)], [(X, -40), (Z, -10)]))),
                    (25, crouch(legs('leg.01', [(X, -50)], [(X, -15)])))], True),
    # 직립 작업대: 한 앞다리로 누르고 다른 앞다리로 앞뒤 손질
    ('BenchWork', [(1, stand(legs('leg.01', [(X, -25), (Z, -20)], [(X, -15), (Z, -30)]))),
                   (9, stand(body(-50, .013), legs('leg.01', [(X, -25), (Z, -20)], [(X, -40), (Z, -15)]))),
                   (17, stand(legs('leg.01', [(X, -22), (Z, -22)], [(X, -10), (Z, -35)]))),
                   (25, stand(legs('leg.01', [(X, -25), (Z, -20)], [(X, -15), (Z, -30)])))], True),
    # 망치: 오른 앞다리 들기 → 내려치기(몸 앞으로) → 반동, 왼 앞다리는 대상 고정
    ('Hammer', [(1, stand(body(-60, .016), legs('leg.01', [(X, -20), (Z, -15)], [(X, -120), (Z, -10)]))),
                (9, stand(body(-62, .016), legs('leg.01', [(X, -20), (Z, -15)], [(X, -128), (Z, -10)]))),
                (12, stand(body(-46, .012), head(42), legs('leg.01', [(X, -20), (Z, -15)], [(X, -5), (Z, -15)]))),
                (15, stand(body(-50, .013), legs('leg.01', [(X, -20), (Z, -15)], [(X, -25), (Z, -12)]))),
                (21, stand(body(-60, .016), legs('leg.01', [(X, -20), (Z, -15)], [(X, -120), (Z, -10)])))], True),
    # 도끼: 두 앞다리를 모아 옆으로 젖혔다가 수평으로 휘두름
    ('Chop', [(1, stand(body(-55, .015, yaw=25), legs('leg.01', [(X, -45), (Z, 35)], [(X, -45), (Z, -55)]))),
              (11, stand(body(-55, .015, yaw=30), legs('leg.01', [(X, -50), (Z, 40)], [(X, -50), (Z, -60)]))),
              (14, stand(body(-50, .014, yaw=-15), legs('leg.01', [(X, -40), (Z, -40)], [(X, -40), (Z, 20)]))),
              (17, stand(body(-52, .014, yaw=-8), legs('leg.01', [(X, -42), (Z, -30)], [(X, -42), (Z, 10)]))),
              (25, stand(body(-55, .015, yaw=25), legs('leg.01', [(X, -45), (Z, 35)], [(X, -45), (Z, -55)])))], True),
    # 곡괭이: 두 앞다리를 머리 위로 → 아래로 내려찍고 뒤로 반동
    ('Mine', [(1, stand(body(-65, .017), head(25), legs('leg.01', [(X, -110), (Z, -15)]))),
              (10, stand(body(-68, .018), head(22), legs('leg.01', [(X, -118), (Z, -15)]))),
              (13, stand(body(-40, .010), head(45), legs('leg.01', [(X, 0), (Z, -20)]))),
              (16, stand(body(-58, .015), legs('leg.01', [(X, -35), (Z, -15)]))),
              (25, stand(body(-65, .017), head(25), legs('leg.01', [(X, -110), (Z, -15)])))], True),
    # 일반 처치: 숙여서 앞다리로 가볍게 두드리고 바르기
    ('Treat', [(1, crouch(body(20, -.010), head(28), ant(25), legs('leg.01', [(X, -40), (Z, -20)], [(X, -30), (Z, -15)]))),
               (9, crouch(body(22, -.011), head(30), ant(28), legs('leg.01', [(X, -32), (Z, -25)], [(X, -42), (Z, -20)]))),
               (17, crouch(body(20, -.010), head(26), ant(22), legs('leg.01', [(X, -44), (Z, -15)], [(X, -28), (Z, -22)]))),
               (25, crouch(body(21, -.010), head(30), ant(30), legs('leg.01', [(X, -35), (Z, -28)], [(X, -40), (Z, -12)]))),
               (33, crouch(body(20, -.010), head(28), ant(25), legs('leg.01', [(X, -40), (Z, -20)], [(X, -30), (Z, -15)])))], True),
    # 수술: 직립해 몸을 앞으로 기울이고 앞다리 끝을 모아 작은 정밀 동작
    ('Operate', [(1, stand(body(-38, .010), head(50), ant(20), legs('leg.01', [(X, -20), (Z, -40)], [(X, -24), (Z, -38)]))),
                 (9, stand(body(-38, .010), head(52), ant(22), legs('leg.01', [(X, -22), (Z, -42)], [(X, -18), (Z, -44)]))),
                 (17, stand(body(-36, .009), head(50), ant(18), legs('leg.01', [(X, -18), (Z, -38)], [(X, -26), (Z, -36)]))),
                 (25, stand(body(-38, .010), head(54), ant(24), legs('leg.01', [(X, -21), (Z, -44)], [(X, -20), (Z, -40)]))),
                 (33, stand(body(-38, .010), head(50), ant(20), legs('leg.01', [(X, -20), (Z, -40)], [(X, -24), (Z, -38)])))], True),

    # 수면: 다리를 몸 옆으로 접고 바닥에 엎드림, 느린 호흡
    ('Sleep', [(1, merge(body(4, -.03), head(18), ant(70), {'tail': dict(rot=[(X, 0)])},
                         legs('leg.01', [(X, -60), (Y, -70)]), legs('leg.02', [(Y, -80)]), legs('leg.03', [(X, 40), (Y, -70)]))),
               (25, merge(body(3, -.028), head(16), ant(72), {'tail': dict(rot=[(X, -3)])},
                          legs('leg.01', [(X, -60), (Y, -70)]), legs('leg.02', [(Y, -80)]), legs('leg.03', [(X, 40), (Y, -70)]))),
               (49, merge(body(4, -.03), head(18), ant(70), {'tail': dict(rot=[(X, 0)])},
                          legs('leg.01', [(X, -60), (Y, -70)]), legs('leg.02', [(Y, -80)]), legs('leg.03', [(X, 40), (Y, -70)])))], True),
    # 휴식: 낮게 앉아 앞다리로 더듬이 손질
    ('Rest', [(1, merge(body(-15, -.018), head(10), ant(5), legs('leg.03', [(X, 30), (Y, -40)]), legs('leg.02', [(Y, -35)]))),
              (11, merge(body(-18, -.018), head(25, 10), ant(35, 5), legs('leg.03', [(X, 30), (Y, -40)]), legs('leg.02', [(Y, -35)]),
                         legs('leg.01', [(X, -95), (Z, -45)], [(X, -10)]))),
              (21, merge(body(-18, -.018), head(28, 12), ant(45, 5), legs('leg.03', [(X, 30), (Y, -40)]), legs('leg.02', [(Y, -35)]),
                         legs('leg.01', [(X, -105), (Z, -40)], [(X, -10)]))),
              (31, merge(body(-15, -.018), head(10), ant(5), legs('leg.03', [(X, 30), (Y, -40)]), legs('leg.02', [(Y, -35)]))),
              (41, merge(body(-15, -.018), head(10), ant(5), legs('leg.03', [(X, 30), (Y, -40)]), legs('leg.02', [(Y, -35)])))], True),
    # 추모: 잠깐 고개 숙이고 더듬이를 내림(한 번)
    ('Mourn', [(1, BASE), (10, merge(body(8, -.004), head(38), ant(55))), (28, merge(body(9, -.005), head(40), ant(58))), (37, BASE)], False),
    # 살펴보기: 고개를 좌우로 돌리며 한 앞다리로 짚어 봄
    ('Inspect', [(1, merge(head(5, 20), ant(-5, 10, 10))),
                 (11, merge(body(-8, .002), head(10, -5), ant(10, -8, 5), legs('leg.01', [(X, -55), (Z, -10)], [(X, 0)]))),
                 (21, merge(head(5, -22), ant(-8, 12, 12))),
                 (31, merge(body(-6, .002), head(12, 0), ant(15, 0, 6), legs('leg.01', [(X, 0)], [(X, -50), (Z, -10)]))),
                 (41, merge(head(5, 20), ant(-5, 10, 10)))], True),
]
