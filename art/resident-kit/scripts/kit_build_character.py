"""Entry point: character kit = parts + combos, saved to resident-character-kit.blend."""
import bpy, sys, importlib
sys.path.insert(0, r"E:\Git\ant\art\resident-kit\scripts")
exec(open(r"E:\Git\ant\art\resident-kit\scripts\kit_character.py", encoding="utf-8").read())
import kit_combos as C
importlib.reload(C)
import kit_restore_expressions as R
importlib.reload(R)
R.restore()
for i, (n, sp) in enumerate(C.COMBOS.items()):
    C.build_combo(n, sp, (i * 2.8, 0, 0))
bpy.ops.wm.save_as_mainfile(filepath=r"E:\Git\ant\art\resident-kit\resident-character-kit.blend")
