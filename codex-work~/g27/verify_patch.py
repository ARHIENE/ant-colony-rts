from pathlib import Path
import difflib, subprocess, hashlib, json

root = Path('E:/Git/ant')
w = root / 'codex-work~/g27'
manifest = json.loads((w / 'baseline.json').read_text(encoding='utf-8'))
patch = bytearray()
for rel, original in manifest.items():
    before = (w / 'baseline' / rel).read_bytes()
    assert hashlib.sha256((root / rel).read_bytes()).hexdigest() == original['sha256'], 'Original changed concurrently: ' + rel
    after = (w / 'src' / rel).read_bytes()
    patch.extend(('diff --git a/' + rel + ' b/' + rel + '\n').encode())
    diff = difflib.unified_diff(before.decode('latin1').splitlines(keepends=True), after.decode('latin1').splitlines(keepends=True), fromfile='a/' + rel, tofile='b/' + rel)
    patch.extend(''.join(diff).encode('latin1'))
    dest = w / 'verify' / rel
    dest.parent.mkdir(parents=True, exist_ok=True)
    dest.write_bytes(before)
(w / 'G27-1025.patch').write_bytes(patch)
commands = [
    ['git', 'apply', '--check', '--whitespace=nowarn', 'codex-work~/g27/G27-1025.patch'],
    ['git', '-c', 'core.autocrlf=false', 'apply', '--whitespace=nowarn', '--directory=codex-work~/g27/verify', 'codex-work~/g27/G27-1025.patch'],
]
logs = []
for command in commands:
    result = subprocess.run(command, cwd=root, capture_output=True, text=True, encoding='utf-8', errors='replace')
    logs.append(dict(command=command, exit=result.returncode, stdout=result.stdout, stderr=result.stderr))
    print(' '.join(command), '=>', result.returncode)
    assert result.returncode == 0, result.stderr
for rel, original in manifest.items():
    after = (w / 'src' / rel).read_bytes()
    assert (w / 'verify' / rel).read_bytes() == after, 'Patch bytes differ: ' + rel
    assert hashlib.sha256((root / rel).read_bytes()).hexdigest() == original['sha256'], 'Original changed: ' + rel
    endings = dict(crlf=after.count(b'\r\n'), lf=after.count(b'\n')-after.count(b'\r\n'))
    if original['lf'] == 0:
        assert endings['lf'] == 0
    elif original['crlf'] == 0:
        assert endings['crlf'] == 0
    else:
        assert endings['lf'] == original['lf']
    logs.append(dict(file=rel, original_unchanged=True, applied_bytes_match=True, endings=endings))
(w / 'patch-results.json').write_text(json.dumps(logs, indent=2), encoding='utf-8')
print('Verified: original hashes unchanged, patch replay byte-identical, line endings preserved.')
