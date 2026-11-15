"""Joint selections inspected from each downloaded Meshy Smart Rig.

These are authored prototype motions, not Meshy's preset motion library.
"""
PROFILES = {
    'BreachBeast': dict(feet=[9,14], hips=[12,17], chain=4, head=22, attack=[27,32], kind='slam', stride=.13),
    'Bulwark': dict(feet=[23,29,6,11], hips=[27,33,9,14], chain=[5,5,4,4], head=19, attack=[], kind='bash', stride=.07),
    'CarrionReclaimer': dict(feet=[30,35,42,47,8,12,16,20,24], hips=[34,39,46,51,11,15,19,23,27], chain=[5,5,5,5,4,4,4,4,4], head=7, attack=[41,58,60], kind='grasp', stride=.07),
    'VeilWeaver': dict(feet=[3,7,11,15], hips=[5,9,13,17], chain=3, head=1, attack=[], kind='pulse', stride=.08),
    'ClawHunter': dict(feet=[9,14], hips=[12,17], chain=4, head=6, attack=[24,28], kind='slash', stride=.19),
    'BroodAlien': dict(feet=[9,14], hips=[10,15], chain=2, head=18, attack=[23,28], kind='slash', stride=.15),
    'BroodCarrier': dict(feet=[4,9,14,19,24,29,34,39], hips=[6,11,16,21,26,31,36,41], chain=3, head=1, attack=[], kind='pulse', stride=.08),
    'HuntRider': dict(feet=[41,45,6,11], hips=[44,48,9,14], chain=4, head=23, attack=[25], kind='lunge', stride=.17),
    'SiegeBrooder': dict(feet=[7,11,15,19,23], hips=[8,12,16,20,24], chain=2, head=4, attack=[3], kind='spit', stride=.07),
    'SiegeRider': dict(feet=[7,11,17,21,30,34,38], hips=[10,14,20,24,33,37,41], chain=4, head=29, attack=[27], kind='spit', stride=.09),
    'WallMelter': dict(feet=[], hips=[7,12,17,22,27,32,37,42], chain=3, head=3, attack=[7,12,17,22,27,32,37,42], kind='climb', stride=.06),
    'AcidSpitter': dict(feet=[10,14,18,21,25], hips=[13,17,20,24,28], chain=3, head=9, attack=[8], kind='spit', stride=.10),
    'BurrowLurker': dict(feet=[], hips=[22,27], chain=3, head=2, attack=[22,27], tail=[15,14,13,12,11,10,9,8], kind='slither', stride=.1),
    'HookMaw': dict(feet=[8,13], hips=[10,15], chain=3, head=28, attack=[63,62,61,60,59,58], kind='tongue', stride=.14),
}

def bone(number):
    return f'Bone_{number:03d}'
