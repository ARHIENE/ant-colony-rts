from pathlib import Path
root=Path(__file__).resolve().parent/'code'
def edit(path,old,new):
    p=root/path; s=p.read_text(encoding='utf-8-sig')
    assert s.count(old)==1,(path,old[:100],s.count(old))
    p.write_text(s.replace(old,new),encoding='utf-8')

edit('World/WildMonster.cs','var d = new Save.WildMonsterDto { commander', 'var d = new Save.WildMonsterDto { organic = CaptureOrganic(), commander')
edit('World/WildMonster.cs','if (targets) { currentTarget = Save.SaveReferences.Get<IDamageable>(d.target); return; }','if (targets) { currentTarget = Save.SaveReferences.Get<IDamageable>(d.target); RestoreOrganic(d.organic, true); return; }')
edit('World/WildMonster.cs','Aim = (EnemyAim)Mathf.Clamp(d.aim, 0, (int)EnemyAim.Wall);','Aim = (EnemyAim)Mathf.Clamp(d.aim, 0, (int)EnemyAim.Wall);\n            RestoreOrganic(d.organic);')
edit('World/WildMonster.cs','if (GetComponent<Critter>() == null && GetComponent<AntVisual>() == null)','if (!IsOrganic && GetComponent<Critter>() == null && GetComponent<AntVisual>() == null)')
edit('World/WildMonster.cs','RootRemaining = Mathf.Max(0f, RootRemaining - Time.deltaTime);','RootRemaining = Mathf.Max(0f, RootRemaining - Time.deltaTime);\n            if (TickOrganic(Time.deltaTime)) return;')
edit('World/WildMonster.cs','if (!TryMilitarySpacing(destination, distance)) StopMoving();','if (IsOrganic) { StopMoving(); BeginOrganicStrike(); return; }\n            if (!TryMilitarySpacing(destination, distance)) StopMoving();')
edit('World/WildMonster.cs','GetComponent<CritterVisual>()?.Hurt();','GetComponent<CritterVisual>()?.Hurt();\n            OrganicHurt();')
edit('World/WildMonster.cs','public void MakeRaider() { isRaider = true; AntVisual.Attach(gameObject); }','public void MakeRaider() { isRaider = true; if (IsOrganic) RestoreOrganicVisual(); else AntVisual.Attach(gameObject); }')
edit('Core/FormationDamage.cs','if (target is CommanderAnt commander)', 'if (target is AntColony.World.WildMonster organic && organic.IsOrganic) amount *= organic.OrganicDamageMultiplier(source, ranged, area);\n            if (target is CommanderAnt commander)')
edit('Units/HordeVisual.cs','!SystemInfo.supportsInstancing || GetComponent<AntVisual>() == null','!SystemInfo.supportsInstancing || GetComponent<AntVisual>() == null && GetComponent<AntColony.World.OrganicEnemyVisual>() == null')
edit('Save/SaveOrders.cs','public bool commander, raider, wasp, night, flying, allied, docile, provoked, visitor;','public AntColony.World.OrganicCombatState organic; // null = 기존 적.\n        public bool commander, raider, wasp, night, flying, allied, docile, provoked, visitor;')
edit('Save/SaveDtos.cs','public List<MonsterDto> monsters = new List<MonsterDto>();','public List<MonsterDto> monsters = new List<MonsterDto>();\n        public List<World.OrganicEffectState> organicEffects; // 구저장 null: 진행 중 생체 공격 없음.')
edit('Save/SaveRuntime.cs','file.monsters.Add(Capture(m, SaveReferences.Key(m)));','file.monsters.Add(Capture(m, SaveReferences.Key(m)));\n            file.organicEffects = OrganicCombatEffects.Capture();')
edit('Save/SaveRuntime.cs','if (state.raider) m.MakeRaider();','m.RestoreOrganic(state.organic);\n                if (state.raider) m.MakeRaider();')
edit('Save/SaveRuntime.cs','foreach (var (d, member) in MilitaryMembers(file.monsters))','OrganicCombatEffects.Restore(file.organicEffects);\n            foreach (var (d, member) in MilitaryMembers(file.monsters))')
edit('World/Corpse.cs','public int count = 1;','public int count = 1;\n            public int organicKind;')
edit('World/Corpse.cs','public bool Edible => Available &&','public bool Edible => Available && Data.organicKind == 0 &&')
edit('World/Corpse.cs','var body = source.GetComponent<AntVisual>()?.Death(true);','var organic = source as WildMonster;\n            bool organicBody = organic != null && organic.IsOrganic;\n            if (organicBody) { kind = CorpseKind.Wildlife; food = 0; }\n            var body = organicBody ? source.GetComponent<OrganicEnemyVisual>()?.DeathBody() : source.GetComponent<AntVisual>()?.Death(true);')
edit('World/Corpse.cs','kind = kind, name = name, count = count, food = food, commanderId = commanderId }, body);','kind = kind, name = name, count = count, food = food, commanderId = commanderId, organicKind = organicBody ? (int)organic.OrganicKind : 0 }, body);')
edit('World/Corpse.cs','bool freshDeath = body != null;','bool freshDeath = body != null;\n            if (body == null && state.organicKind > 0) body = OrganicEnemyVisual.Create((OrganicEnemyKind)state.organicKind);')
edit('World/Corpse.cs','if (animator != null) { animator.applyRootMotion = false;','if (animator != null) { if (state.organicKind > 0) animator.SetInteger("Action", 4); animator.applyRootMotion = false;')
edit('World/CorpseProp.cs','if (shown || corpse.Data == null ||','if (shown || corpse.Data == null || corpse.Data.organicKind > 0 ||')
edit('World/LocalIncursions.cs','visitor.MakeRaider();','// 종류를 먼저 지정해 인간형 AntVisual과 중복 생성하지 않는다.\n                    bool organicWave = wave && !(visitor is EnemyCommander);\n                    int waveNumber = WaveDefense.Instance != null ? WaveDefense.Instance.Wave : 1;\n                    if (organicWave) visitor.ConfigureOrganic(OrganicEnemyRules.Select(waveNumber, visitors.Count, aim), waveNumber <= 3);')
edit('World/LocalIncursions.cs','visitor.gameObject.SetActive(true);','visitor.MakeRaider();\n                    if (organicWave) visitor.name = OrganicEnemyRules.Label(visitor.OrganicKind) + (hordeBodies > 0 ? " 무리 " + hordeBodies : "");\n                    visitor.gameObject.SetActive(true);')
edit('World/LocalIncursions.cs','private static bool HasEntryBarrier(Vector3 endpoint)', '''internal WildMonster SpawnOrganicYoung(WildMonster parent, int slot)
        {
            if (raiderTemplate == null || parent == null || parent.IsDead || !parent.IsOrganic) return null;
            if (WaveDefense.Instance != null && WaveDefense.Instance.RemainingEnemies >= WaveDefense.MaxEnemies) return null;
            var position = parent.Position + parent.transform.right * (slot == 0 ? -.7f : .7f);
            if (!NavMesh.SamplePosition(position, out var hit, .5f, NavMesh.AllAreas)
                || NavMesh.Raycast(parent.Position, hit.position, out _, NavMesh.AllAreas)) return null;
            var child = Instantiate(raiderTemplate, hit.position, parent.transform.rotation);
            child.ConfigureWeakIntruder(); child.ConfigureOrganic(OrganicEnemyKind.ClawHunter, true);
            child.Allied = parent.Allied; child.MakeRaider(); child.name = "운반체 새끼";
            child.gameObject.SetActive(true); RestoreVisitor(child);
            return child;
        }

        private static bool HasEntryBarrier(Vector3 endpoint)''')
print('Draft connection changes prepared; Assets unchanged.')
