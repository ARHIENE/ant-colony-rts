from pathlib import Path
import hashlib, json

root = Path('E:/Git/ant')
w = root / 'codex-work~/g27'
files = ['AgentScripts/ImplG27Checks.cs'] + ['Assets/_Project/Scripts/Save/' + n + '.cs' for n in ['SaveDtos', 'SaveSnapshot', 'SaveSystem', 'SaveValidator']]
manifest = {}
for rel in files:
    data = (root / rel).read_bytes()
    dest = w / 'baseline' / rel
    dest.parent.mkdir(parents=True, exist_ok=True)
    dest.write_bytes(data)
    manifest[rel] = dict(sha256=hashlib.sha256(data).hexdigest(), crlf=data.count(b'\r\n'), lf=data.count(b'\n') - data.count(b'\r\n'))
    s = data.decode('utf-8-sig')
    newline = '\r\n' if b'\r\n' in data else '\n'
    def replace(old, new):
        global s
        old = old.replace('\r\n', '\n').replace('\n', newline)
        new = new.replace('\r\n', '\n').replace('\n', newline)
        assert s.count(old) == 1, (rel, old, s.count(old))
        s = s.replace(old, new)
    if rel.endswith('ImplG27Checks.cs'):
        replace('static void Reward(BossProgress progress, BossId boss)', 'static void Reward(CampaignResearch research, BossId boss)')
        replace('        Check(progress.RecordKill(boss, GameCalendar.GameSeconds)', '        var progress = research.BossProgress;\r\n        Check(progress.RecordKill(boss, GameCalendar.GameSeconds)')
        replace('Check(progress.TryDeliver(parcel.id) && !progress.TryDeliver(parcel.id), "deliver once " + boss);', 'Check(research.DeliverBossReward(parcel.id), "deliver " + boss);\r\n        Check(!research.DeliverBossReward(parcel.id), "deliver once " + boss);')
        s = s.replace('Reward(research.BossProgress, BossId.', 'Reward(research, BossId.')
        old = '''                    Check(CommanderRoster.Instance.Count == n && CommanderRoster.Instance.Commanders.All(x => !x.IsDead)
                        && ids.SequenceEqual(CommanderRoster.Instance.Commanders.Select(x => x.PersonalState.id).OrderBy(x => x))
                        && ColonyPopulation.Instance.Total == citizens && GameSession.Instance.Options.startingCount == n, "roundtrip keeps survivor identities, headcount, citizens and starting count");'''.replace('\n', '\r\n')
        new = '''                    Check(CommanderRoster.Instance.Count == n, $"roundtrip headcount: expected {n}, got {CommanderRoster.Instance.Count}");
                    Check(CommanderRoster.Instance.Commanders.All(x => !x.IsDead), "roundtrip survivors alive");
                    Check(ids.SequenceEqual(CommanderRoster.Instance.Commanders.Select(x => x.PersonalState.id).OrderBy(x => x)), "roundtrip survivor identities");
                    Check(ColonyPopulation.Instance.Total == citizens, $"roundtrip citizens: expected {citizens}, got {ColonyPopulation.Instance.Total}");
                    Check(GameSession.Instance.Options.startingCount == n, $"roundtrip starting count: expected {n}, got {GameSession.Instance.Options.startingCount}");'''.replace('\n', '\r\n')
        replace(old, new)
        replace('                    Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "load: " + error); await Ready();', '''                    Check(SaveStorage.TryReadText(SaveSlots.PathFor(false, 0), out var json, out error), "read saved options: " + error);
                    Check(SaveValidator.TryParse(json, out var saved, out error), "parse saved options: " + error);
                    Check(saved.options.startingCount == n, $"saved starting count: expected {n}, got {saved.options.startingCount}");
                    foreach (var count in new[] { -1, 4 })
                    {
                        saved.options.startingCount = count;
                        Check(!SaveValidator.Validate(saved, out error) && error == "Invalid starting count.", "reject invalid starting count " + count);
                    }
                    saved.options.startingCount = JsonUtility.FromJson<OptionsDto>("{}").startingCount;
                    Check(SaveValidator.Validate(saved, out error) && saved.options.startingCount == 3, "legacy missing starting count defaults to three: " + error);
                    Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "load: " + error); await Ready();''')
    elif rel.endswith('SaveDtos.cs'):
        replace('        public int climate; // 0 = 온대(구 저장 기본값)', '        public int climate; // 0 = 온대(구 저장 기본값)\r\n        public int startingCount; // 0 = 필드가 없는 구 저장, 검증 시 기존 기본값 3으로 이관')
    elif rel.endswith('SaveSnapshot.cs'):
        replace('climate = (int)options.climate },', 'climate = (int)options.climate, startingCount = options.startingCount },')
    elif rel.endswith('SaveSystem.cs'):
        replace('climate = (MapClimate)file.options.climate });', 'climate = (MapClimate)file.options.climate, startingCount = file.options.startingCount });')
    elif rel.endswith('SaveValidator.cs'):
        replace('            var pop = file.population;', '            // 시작 인원은 현재 생존자 수로 추론할 수 없다. 필드 없는 구 저장은 기존 로드 기본값을 유지한다.\r\n            if (file.options.startingCount == 0) file.options.startingCount = 3;\r\n            if (file.options.startingCount < 1 || file.options.startingCount > 3) { error = "Invalid starting count."; return false; }\r\n            var pop = file.population;')
    dest = w / 'src' / rel
    dest.parent.mkdir(parents=True, exist_ok=True)
    dest.write_bytes((b'\xef\xbb\xbf' if data.startswith(b'\xef\xbb\xbf') else b'') + s.encode('utf-8'))
(w / 'baseline.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')
(w / 'IMPLEMENTATION_BOARD.md').write_text('''# G27 담당 범위 (2026-10-25, Codex)
- 원본 공동 보드를 읽고 쓰기 허용 경로에만 담당 표시.
- 수정안 소유: AgentScripts/ImplG27Checks.cs, Save/SaveDtos.cs·SaveSnapshot.cs·SaveSystem.cs·SaveValidator.cs.
- 실제 수정은 g27/src 사본만. 원본/공동 보드/UI/다른 Codex 작업 경로 수정 없음.
- 검증: 현재 파일 기준 패치 적용 검사, Roslyn 본체 및 검사 단독 컴파일. Unity 실행 금지.
''', encoding='utf-8')
print(json.dumps(manifest, indent=2))
