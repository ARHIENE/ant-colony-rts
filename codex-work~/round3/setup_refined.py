from pathlib import Path
r=Path('codex-work~/round3'); s=(r/'preflight-bone.py').read_text(encoding='utf-8-sig'); s=s.replace('for mm in (2,5,8):','for mm in (8,):')
s=s.replace("baseline={(r.name,o.name):len(its[r.name].overlap(originals[o.name])) for r,ears in items for o in PARTS if o.name in targets(r.name,ears)}", "baseline_pairs={(r.name,o.name):set(its[r.name].overlap(originals[o.name])) for r,ears in items for o in PARTS if o.name in targets(r.name,ears)}\n  baseline={k:len(v) for k,v in baseline_pairs.items()}")
s=s.replace('for _,faceidx in overlaps:', 'for _,faceidx in set(overlaps)-baseline_pairs[(item,part)]:')
s=s.replace('bone-preflight.json','bone-refined.json').replace('bone-trials.blend','bone-refined-trials.blend').replace("print('BONE'", "print('REFINED'")
(r/'preflight-refined.py').write_text(s,encoding='utf-8')
