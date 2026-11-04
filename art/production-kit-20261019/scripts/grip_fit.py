"""Hand ↔ handle contact measured on the deformed body skin (not bone tips).

skin_gaps → {finger: min distance from that finger's skin to the handle surface}; negative = skin inside the handle.
Handle = tool local Y axis (grip origin, +Y toward the head), radius HANDLE_R (pk tools share it).
"""
import bpy, math

HANDLE_R = .007  # largest handle the kemono fist closes round (wrap 240°, skin ±3 mm)


def skin_gaps(body, tool, side='R', r=HANDLE_R, y_range=(-.07, .07)):
    dg = bpy.context.evaluated_depsgraph_get(); ev = body.evaluated_get(dg); me = ev.to_mesh()
    names = {g.index: g.name for g in body.vertex_groups}
    M = tool.matrix_world.inverted() @ ev.matrix_world
    out = {}
    for v0, v in zip(body.data.vertices, me.vertices):
        if not v0.groups: continue
        n = names[max(v0.groups, key=lambda g: g.weight).group]
        if not (n.endswith('.' + side) and (n.startswith(('f_', 'thumb')) or n == 'hand.' + side)): continue
        q = M @ v.co
        if not y_range[0] <= q.y <= y_range[1]: continue
        k = n.split('.')[0]; out[k] = min(out.get(k, 9), math.hypot(q.x, q.z) - r)
    ev.to_mesh_clear()
    return {k: round(v, 4) for k, v in out.items()}


def wrap(body, tool, side='R', r=HANDLE_R, near=.005, rays=24, ys=(-.015, 0, .015)):
    """(wrap_deg, min_gap): rays from the handle axis outward (three slices); a ray counts when skin is within
    `near` of the handle surface. wrap_deg = best slice. min_gap = deepest hand-skin vertex (negative = inside).
    A handle resting on the back of the fist scores ~90°, a closed grip 270°+."""
    from mathutils import Vector
    from mathutils.bvhtree import BVHTree
    dg = bpy.context.evaluated_depsgraph_get(); ev = body.evaluated_get(dg); me = ev.to_mesh()
    names = {g.index: g.name for g in body.vertex_groups}
    M = tool.matrix_world.inverted() @ ev.matrix_world
    hand = set(); low = 9
    for v0, v in zip(body.data.vertices, me.vertices):
        if not v0.groups: continue
        n = names[max(v0.groups, key=lambda g: g.weight).group]
        if not (n.endswith('.' + side) and (n.startswith(('f_', 'thumb')) or n == 'hand.' + side)): continue
        hand.add(v0.index); q = M @ v.co
        if -.04 <= q.y <= .04: low = min(low, math.hypot(q.x, q.z) - r)
    polys = [p.vertices[:] for p in me.polygons if any(i in hand for i in p.vertices)]
    tree = BVHTree.FromPolygons([M @ v.co for v in me.vertices], polys)
    ev.to_mesh_clear()
    best = 0
    for y in ys:
        n = 0
        for k in range(rays):
            a = math.tau * k / rays; d = Vector((math.cos(a), 0, math.sin(a)))
            hit = tree.ray_cast(Vector((0, y, 0)), d, r + near)
            n += hit[0] is not None
        best = max(best, n)
    return best * 360 / rays, round(low, 4)
