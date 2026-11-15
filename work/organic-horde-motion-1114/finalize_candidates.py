from pathlib import Path
import hashlib, json, shutil

root=Path('F:/UnityProjects/ant-colony-rts')
work=root/'work/organic-horde-motion-1114'
pending=work/'pending-runtime'
pending.mkdir(exist_ok=True)
changes={
 'Assets/_Project/Scripts/World/OrganicMotionBank.cs': [('    public sealed class OrganicMotionBank', '    [PreferBinarySerialization]\n    public sealed class OrganicMotionBank')],
 'Assets/_Project/Shaders/OrganicHordeMotion.shader': [('#pragma vertex Vert', '#pragma target 3.5\n            #pragma instancing_options forcemaxcount:250\n            #pragma vertex Vert')],
 'Assets/_Project/Scripts/World/OrganicHordeMotion.cs': [('511','250'), ('seed = Mathf.Repeat(GetEntityId().GetHashCode() * .6180339f, 1);','seed = (unchecked((uint)GetEntityId().GetHashCode()) % 65521) / 65521f;'), ('float burrow = owner.OrganicPhase == 6 ? -.5f : 0;','float burrow = visual.BurrowOffset;')],
 'Assets/_Project/Scripts/World/OrganicEnemyVisual.cs': [('float actionLeft;', 'float actionLeft, burrowOffset;\n        public float BurrowOffset => burrowOffset;'), ('var animated = Instantiate(source, root.transform, false);', 'if (kind == OrganicEnemyKind.WallRam) root.transform.rotation = Quaternion.Euler(0, Mathf.Atan2(.56f, .83f) * Mathf.Rad2Deg, 0);\n            var animated = Instantiate(source, root.transform, false);'), ('model.transform.localPosition = modelOrigin + (owner.OrganicPhase == 6 ? Vector3.down * .5f : Vector3.zero);', 'burrowOffset = Mathf.MoveTowards(burrowOffset, owner.OrganicPhase == 6 ? -.5f : 0, Time.deltaTime * 2.5f);\n            model.transform.localPosition = modelOrigin + Vector3.up * burrowOffset;'), ('if (owner.OrganicPhase != 6) contact?.Apply(model, transform.position);', 'if (Mathf.Abs(burrowOffset) < .001f) contact?.Apply(model, transform.position);')]
}
manifest=[]
for rel, replacements in changes.items():
    src=root/rel
    text=src.read_text(encoding='utf-8-sig')
    for old,new in replacements:
        assert old in text,(rel,old)
        text=text.replace(old,new)
    dest=pending/src.name
    dest.write_text(text,encoding='utf-8')
    manifest.append({'path':rel,'sha256':hashlib.sha256(src.read_bytes()).hexdigest(),'candidate':str(dest)})
(pending/'manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print('Prepared',len(manifest),'runtime candidates; Assets unchanged')
