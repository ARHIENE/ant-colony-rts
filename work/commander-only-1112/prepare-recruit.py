from pathlib import Path
root=Path('Assets/_Project/Scripts');dest=Path('work/commander-only-1112/pending')
p=root/'Buildings/ConscriptionPost.cs';s=p.read_text(encoding='utf-8-sig');a=s.index('        public int AvailableTroops');b=s.index('        // 2026-11-12',a);s=s[:a]+'        public int AvailableTroops(CommanderAnt commander) => 0; // Compatibility only.\n\n'+s[b:];a=s.index('        // 구버전(시민 병력 편성).');s=s[:a]+'''        // Legacy callers also mobilize commanders without consuming the old population pool.
        public bool TryDeploy(IReadOnlyList<CommanderAnt> commanders, IReadOnlyList<int> troops)
            => TryDeploy(commanders);
    }
}
''';p=dest/'Buildings/ConscriptionPost.cs';p.parent.mkdir(parents=True,exist_ok=True);p.write_text(s,encoding='utf-8')
p=root/'UI/GameMenuRoster.cs';s=p.read_text(encoding='utf-8-sig');s=s.replace('기분 {c.Mood:0}\\n병력 {c.TroopCount}/{c.CommandLimit}','기분 {c.Mood:0}\\n체력 {c.PersonalHealth:0}');s=s.replace('{shown.Length}명 표시 · 총 병력 {all.Sum(c => c.TroopCount)} / {all.Sum(c => c.CommandLimit)}','{shown.Length}명 표시 · 전체 장수 {all.Length}명');s=s.replace('병력 <b>{c.TroopCount}</b> / {c.CommandLimit} 지휘한도    ','개인 전투    ');s=s.replace('c.CommandLimit > 0 ? (float)c.TroopCount / c.CommandLimit : 0','c.PersonalHealth / GameBalance.CommanderHealth');(dest/'UI/GameMenuRoster.cs').write_text(s,encoding='utf-8')
