"""Review helper (run inside Blender, e.g. via MCP): dress one or more characters in pk-equipment.blend."""
import bpy, sys, importlib
sys.path.insert(0, r"E:\Git\ant\codex-work~\blender\scripts")
import pk, rig_adapter as R, dress
for m in (pk, R, dress): importlib.reload(m)
ARMOR = ['Armor_Breastplate', 'Armor_Breastplate_Lower', 'Armor_Backplate', 'Armor_Backplate_Lower', 'Armor_Fauld', 'Armor_Tasset_L', 'Armor_Tasset_R',
         'Armor_Bracer_L', 'Armor_Bracer_R', 'Armor_Greave_L', 'Armor_Greave_R', 'Belt']


def setup(path=r"E:\Git\ant\art\production-kit-20261019\pk-equipment.blend"):
    bpy.ops.wm.open_mainfile(filepath=path)
    for c in ('Equipment', 'Handheld'):
        for o in bpy.data.collections[c].objects: o.hide_set(True); o.hide_render = True
    return bpy.data.objects['KemonoRig']


def dress_one(rig, hair='BOB', ears='RABBIT', tail='FOX', tier='Steel', armor=True, helm=True, tag='A'):
    keep = R.combo(hair=hair, ears=ears, tail=tail)
    for o in R.parts():
        on = o.name in keep; o.hide_set(not on); o.hide_render = not on
    col = pk.collection('Review'); E = bpy.data.objects; worn = []
    if helm:
        worn.append(dress.wear(rig, E[f'Helm_{ears}'], tier, col, tag))
        if E.get(f'HelmTrim_{tier}'): worn.append(dress.wear(rig, E[f'HelmTrim_{tier}'], tier, col, tag))
    if armor:
        for n in keep:  # manifest rule: with body armour worn, hair / outfit / body swap to their '_ARMOR' variant
            v = E.get(n.rsplit('_', 1)[0] + '_ARMOR')
            if v: E[n].hide_set(True); E[n].hide_render = True; v.hide_set(False); v.hide_render = False
        for n in ARMOR:
            worn.append(dress.wear(rig, E[n], tier, col, tag))
            if E.get(f'{n}_Trim_{tier}'): worn.append(dress.wear(rig, E[f'{n}_Trim_{tier}'], tier, col, tag))
    return worn


def clear_review():
    c = bpy.data.collections.get('Review')
    if c:
        for o in list(c.objects): bpy.data.objects.remove(o)


def carry(rig, tier='Steel', tag='A'):
    """Sheathed sword / spear / shield at the searched carry placements (previews/carry-placement.json)."""
    import json
    from mathutils import Matrix
    data = json.loads((pk.KIT / 'previews' / 'carry-placement.json').read_text(encoding='utf-8'))
    col = pk.collection('Review'); E = bpy.data.objects; out = []
    for k, v in data.items():
        if k.startswith('_'): continue
        srcs = [E[f'Scabbard_{tier}'], E[f'Weapon_Sword_{tier}']] if k == 'Sword' else [E[f'Weapon_{k}_{tier}']]
        for src, M in zip(srcs, v['world_rest']):
            o = src.copy(); col.objects.link(o); o.name = f'{src.name}__carry_{tag}'
            o.matrix_world = Matrix(M); dress.attach_rigid(o, rig, v['parent']); out.append(o)
    return out


def hide_variants(show_armor_hair=True):
    """Armour-wearing hair variants replace their originals while body armour is worn."""
    for o in bpy.data.collections['Equipment'].objects:
        if o.name.endswith('_ARMOR'):
            orig = bpy.data.objects.get(o['derived_from'])
            vis = show_armor_hair and orig is not None and not orig.hide_get()
            o.hide_set(not vis); o.hide_render = not vis
            if vis: orig.hide_set(True); orig.hide_render = True
