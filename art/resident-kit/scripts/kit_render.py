"""Render preview scenes to PNG frames (and a contact sheet list). Usage inside Blender:
render(['Preview__CC_Workshop'], frames=[7], out=...) or render_all(frames_per_clip=12)."""
import bpy
from pathlib import Path

OUT = Path(r"E:\Git\ant\art\resident-kit\previews")


def engine(scene):
    for e in ('BLENDER_EEVEE_NEXT', 'BLENDER_EEVEE', 'BLENDER_WORKBENCH'):
        try:
            scene.render.engine = e
            return e
        except TypeError:
            continue


def render(names, frames=None, out=OUT / 'stills', count=None, size=None):
    out.mkdir(parents=True, exist_ok=True)
    done = []
    for n in names:
        sc = bpy.data.scenes[n]
        engine(sc)
        if hasattr(sc, 'eevee'):
            sc.eevee.taa_render_samples = 8
        if size: sc.render.resolution_x = sc.render.resolution_y = size
        fs = frames
        if count:
            span = sc.frame_end - sc.frame_start
            fs = [sc.frame_start + round(i * span / count) for i in range(count)]
        for f in fs or [sc.frame_start]:
            sc.frame_set(f)
            sub = out / n if count else out
            sub.mkdir(parents=True, exist_ok=True)
            sc.render.filepath = str(sub / (f'f{f:03d}.png' if count else f'{n}_f{f:03d}.png'))
            bpy.ops.render.render(write_still=True, scene=n)
            done.append(sc.render.filepath)
    return done
