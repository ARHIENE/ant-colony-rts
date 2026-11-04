"""Manifest + local catalogue index + asset survey from the build/check reports (plain Python, no Blender).
py scripts/build_manifest.py → manifest.json, CATALOG.md (분야 → 품목 → 이미지·설명·파일), SURVEY.md (기존 자산 조사표)"""
import json, sys
from pathlib import Path

KIT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(KIT / 'scripts'))
import catalog, equip_spec as ES

P = KIT / 'previews'
J = lambda n: json.loads((P / n).read_text(encoding='utf-8')) if (P / n).exists() else {}
res, crops, fac, eq, mot, cc = J('resources-build.json'), J('crops-build.json'), J('facilities-build.json'), J('equipment-build.json'), J('motion-report.json'), J('facility-contacts.json')
RULES = {
    'units': '1 Blender unit = 1 m (kemono rig: character ≈1.2 m, palm socket z .39); 1 grid cell = 1 m',
    'axes_blend': '+Z up, character/facility front = -Y', 'axes_fbx': 'axis_forward -Z, axis_up Y, no leaf bones (same as kemono-kit/export.py)',
    'origin_floor_items': 'bottom centre', 'origin_handheld': 'grip centre (fist), +Y toward the working end, striking side -Z',
    'origin_facility': 'footprint centre on the floor; WorkPoint_0 etc. are child Empties (+Y of each Empty = facing)',
    'sockets': 'Socket.Tool.R/L moved into the fist cavity (rig_adapter.py); Socket.Hip.R sword frog; Socket.Back spear/shield; Socket.Helm = Socket.Hat',
    'resident_reuse_scale': 'resident-kit assets are authored in resident units: × 0.625 to kit metres',
    'license': 'original procedural assets made for this project by the BLENDER-1019 scripts (no third-party content)',
}
man = {'rules': RULES, 'resources': {}, 'crops': {}, 'handheld': {}, 'equipment': {}, 'facilities': {}, 'motions': {}, 'reuse': fac.get('reuse_resident', {})}
ko = {}
for gid, gko, iid, k, recipe, p in catalog.all_items():
    ko[iid] = k
    o = res.get('items', {}).get(iid, {})
    man['resources'][iid] = {'ko': k, 'group': gid, 'group_ko': gko, 'recipe': recipe, 'variants': sorted(o.keys()),
                             'blend': 'pk-resources.blend', 'objects': o, 'fbx': f'exports/resources/{iid}.fbx',
                             'png': {v: f'previews/png/resources/{n}.png' for v, n in o.items()},
                             'existing_model': catalog.EXISTING.get(iid), 'status': 'made' if o else 'missing'}
for gid_, k, model in catalog.LEGACY:
    man['resources'][gid_] = {'ko': k, 'group': 'legacy', 'status': 'kept from older plans, not re-modelled', 'existing_model': model}
for cid, v in crops.get('crops', {}).items():
    man['crops'][cid] = {'ko': v['ko'], 'form': v['form'], 'stages': crops['stages'], 'objects': v['objects'], 'blend': 'pk-crops.blend',
                         'fbx': f'exports/crops/Crop_{cid}.fbx', 'png': f'previews/png/crops/Crop_{cid}.png'}
for kind, (k, L, g, use) in {**ES.TOOLS, **ES.WEAPONS}.items():
    pre = 'Tool' if kind in ES.TOOLS else 'Weapon'
    for t in ES.TIERS:
        if t[0] == 'Stone' and pre == 'Weapon': continue
        n = f'{pre}_{kind}_{t[0]}'
        man['handheld'][n] = {'ko': f'{k} ({t[1]})', 'kind': kind, 'tier': t[0], 'tier_ko': t[1], 'length_m': L, 'use': use,
                              'socket': 'Socket.Tool.L' if kind == 'Shield' else 'Socket.Tool.R', 'rigid': True,
                              'stone_note': ES.STONE_NOTES.get(kind) if t[0] == 'Stone' else None,
                              'blend': 'pk-handheld.blend / pk-equipment.blend (Handheld)', 'fbx': f'exports/handheld/{n}.fbx', 'png': f'previews/png/handheld/{n}.png'}
for t in ES.METAL_TIERS:
    n = f'Scabbard_{t[0]}'
    man['handheld'][n] = {'ko': f'칼집 ({t[1]})', 'kind': 'Scabbard', 'tier': t[0], 'socket': 'Socket.Hip.R via Holder_Sword (carry-placement.json)',
                          'rigid': True, 'fbx': f'exports/handheld/{n}.fbx', 'png': f'previews/png/handheld/{n}.png'}
man['handheld_kept'] = ES.KEPT
for ears, holes in eq.get('shells', {}).items():
    man['equipment'][f'Helm_{ears}'] = {'ko': f'개방형 투구 ({ears} 귀 구멍)', 'ears': ears, 'ear_holes': holes, 'bone': 'head', 'rigid': True,
                                        'compat': f'ears={ears} only (one hole layout per ear type; see helm compatibility)', 'tiers': 'material swap + HelmTrim_<tier>'}
for n in eq.get('armor', []):
    man['equipment'][n] = {'rigid': not n.startswith('Armor_LeatherVest'), 'tiers': 'material swap + <plate>_Trim_<tier>'}
man['equipment']['variants'] = {'hair': eq.get('hair_variants', {}), 'outfit': eq.get('outfit_variants', {}),
                                'rule': "with body armour worn, swap Part_Hair_<style> / Part_Outfit_<x> for the '_ARMOR' variant"}
for fid, v in fac.get('facilities', {}).items():
    man['facilities'][fid] = {**{k: v.get(k) for k in ('ko', 'group', 'cells', 'note', 'points', 'select_box', 'bounds')},
                              'contact': cc.get(fid), 'fbx': f'exports/facilities/{fid}.fbx', 'png': f'previews/png/facilities/{fid}.png'}
for name, c in mot.get('clips', {}).items():
    man['motions'][name] = {**{k: c.get(k) for k in ('frames', 'loop', 'events', 'prop', 'target', 'two_hand')},
                            'solve': c.get('two_hand_solve') or c.get('one_hand_solve') or c.get('hand_solve'),
                            'grip_skin_gap_m': c.get('grip_skin_gap_m'), 'rigid_length_drift_m': c.get('rigid_length_drift_m'),
                            'fbx': f'exports/animations/Kemono@PK_{name}.fbx', 'gif': f'previews/gif/close/{name}.gif'}
man['motions_reused'] = mot.get('reuse', {})
(KIT / 'manifest.json').write_text(json.dumps(man, ensure_ascii=False, indent=1), encoding='utf-8')

# ---- CATALOG.md: 분야 → 품목 → 이미지·설명·파일 (Notion-friendly tables)
L = ['# 도감 인덱스 — BLENDER-1019 production kit', '', '분야 → 품목 → 이미지·설명·파일. 경로는 이 폴더 기준. 보드: `previews/boards/`.', '']
for gid, gko, items in catalog.GROUPS:
    L += [f'## {gko} ({len(items)})', '', '| ID | 이름 | 이미지 | 변형 | 파일 |', '|---|---|---|---|---|']
    for iid, k, recipe, p in items:
        r = man['resources'][iid]
        L.append(f"| {iid} | {k} | ![](previews/png/resources/{iid}.png) | {', '.join(r['variants'])} | {r['fbx']} |")
    L.append('')
L += ['## 밭 작물 성장 단계', '', '| ID | 이름 | 단계 이미지(S1–S5) | 형태 |', '|---|---|---|---|']
L += [f"| {cid} | {v['ko']} | ![]({v['png']}) | {v['form']} |" for cid, v in man['crops'].items()] + ['']
L += ['## 도구·무기 (재질 단계)', '', '| ID | 이름 | 이미지 | 소켓 |', '|---|---|---|---|']
L += [f"| {n} | {v['ko']} | ![]({v['png']}) | {v['socket']} |" for n, v in man['handheld'].items()] + ['']
L += ['## 시설·배관·매장 지점', '', '| ID | 이름 | 이미지 | 분류 | 설명 |', '|---|---|---|---|---|']
L += [f"| {fid} | {v['ko']} | ![]({v['png']}) | {v['group']} | {v['note']} |" for fid, v in man['facilities'].items()] + ['']
L += ['## 동작', '', '| 클립 | 프레임 | 이벤트 | GIF |', '|---|---|---|---|']
L += [f"| {n} | {v['frames']} | {json.dumps(v['events'], ensure_ascii=False)} | {v['gif']} |" for n, v in man['motions'].items()]
(KIT / 'CATALOG.md').write_text('\n'.join(L), encoding='utf-8')

# ---- SURVEY.md: 기존 자산 조사표 (재사용/수정/신규)
S = ['# 기존 자산 조사표 — BLENDER-1019', '', '| 품목 ID | 한글명 | 기존 파일/오브젝트 | 판정 | 의존성 | 상태 |', '|---|---|---|---|---|---|']
for iid, r in man['resources'].items():
    if r.get('group') == 'legacy':
        S.append(f"| {iid} | {r['ko']} | resident-kit {r.get('existing_model') or '-'} | 재사용(유지) | - | 이번 범위 밖, 삭제 안 함 |"); continue
    ex = r.get('existing_model')
    S.append(f"| {iid} | {r['ko']} | {('resident-kit ' + ex) if ex else '-'} | {'신규(기존 모델은 구형 단위·형태)' if ex else '신규'} | resources.py `{r['recipe']}` | {r['status']} |")
for req, roots in man['reuse'].items():
    S.append(f"| {req} | {req} | resident-facility-kit {', '.join(roots)} | 재사용 | facility-points.json ×0.625 | 기존 |")
for fid, v in man['facilities'].items():
    S.append(f"| {fid} | {v['ko']} | - | 신규 | facilities.py / fac_*.py | made |")
S += ['| Tool/Weapon × 재질 | 도구 6종·검·창·방패 | resident-kit tools (구형) | 수정(사람형 손·재질 단계) | handheld.py | made |',
      '| KemonoRig 동작 | 기존 Kemono 클립 | kemono-animations.blend | 재사용 + 신규 PK| 클립 | motions_pk.py | made |']
(KIT / 'SURVEY.md').write_text('\n'.join(S), encoding='utf-8')
print('MANIFEST', {k: len(v) for k, v in man.items() if isinstance(v, dict)})
