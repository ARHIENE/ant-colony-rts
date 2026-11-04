from pathlib import Path
w=Path('E:/Git/ant/codex-work~'); r=w/'round3'; base=(w/'blender/scripts/fit_check.py').read_text(encoding='utf-8-sig')
for label,file in [('baseline','baseline.blend'),('candidate','candidate.blend')]:
 s=base.replace('import bpy, sys, json, math, time','import bpy, sys, json, math, time\nfrom pathlib import Path')
 old="bpy.ops.wm.open_mainfile(filepath=str(pk.KIT / ('pk-motions.blend' if MOTIONS else 'pk-equipment.blend')))"
 s=s.replace(old,"bpy.ops.wm.open_mainfile(filepath='"+(r/'blender'/file).as_posix()+"')")
 s=s.replace('rep = {}; t0 = time.time(); nframes = 0','rep = {}; frame_pairs = {}; sample_list = []; t0 = time.time(); nframes = 0')
 s=s.replace('nframes += 1','nframes += 1; sample_list.append([clip, f])')
 s=s.replace('d[clip] = max(d.get(clip, 0), n)',"d[clip] = max(d.get(clip, 0), n)\n                    frame_pairs[r.name + '|' + name + '|' + clip + '|' + str(f)] = n")
 s=s.replace("pk.KIT / 'previews' / ('fit-report-motions.json' if MOTIONS else 'fit-report.json')", "Path('"+(r/'reports'/(label+'-full.json')).as_posix()+"')")
 mark="    print('FIT', nframes, 'frames', {k: len(v) for k, v in summary.items()})"
 s=s.replace(mark,mark+"\n    pk.write_json(Path('"+(r/'reports'/(label+'-frame-pairs.json')).as_posix()+"'), frame_pairs)\n    pk.write_json(Path('"+(r/'reports'/(label+'-samples.json')).as_posix()+"'), sample_list)")
 (r/('fit-'+label+'.py')).write_text(s,encoding='utf-8')
