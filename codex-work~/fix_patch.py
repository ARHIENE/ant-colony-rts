from pathlib import Path
p=Path('E:/Git/ant/codex-work~/make_patch.py'); s=p.read_text(); s=s.replace("a=(r/p).read_text(encoding='utf-8-sig'); b=(w/'src'/p).read_text(encoding='utf-8-sig')", "a=(r/p).read_bytes().decode('utf-8-sig'); b=a.replace('            ResourceType.Stone =>', '            ResourceType.Food => \\\"Prop_PreservedRation\\\",\\r\\n            ResourceType.Stone =>'); (w/'src'/p).write_bytes(b.encode('utf-8'))")
s=s.replace(".write_text(patch,encoding='utf-8')", ".write_bytes(patch.encode('utf-8'))")
s=s.replace('\\n            ResourceType.Stone =>\') == b','\\r\\n            ResourceType.Stone =>\') == b'); p.write_text(s)
