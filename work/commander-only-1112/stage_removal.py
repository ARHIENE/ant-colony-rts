from pathlib import Path
import re, hashlib, json
root=Path('Assets/_Project/Scripts')
out=Path('work/commander-only-1112/removal-pending')
base=Path('work/commander-only-1112/removal-before')
changes={}
def read(p):
    if p not in changes: changes[p]=(root/p).read_text(encoding='utf-8-sig')
    return changes[p]
def put(p,s): changes[p]=s
def sub(p,a,b):
    s=read(p)
    assert a in s,(p,a)
    put(p,s.replace(a,b))
def body(p,signature,new):
    s=read(p); start=s.index(signature); opening=s.index('{',start); depth=1; i=opening+1
    # Used only on known method bodies with balanced literal braces.
    while depth:
        if s[i]=='{': depth+=1
        elif s[i]=='}': depth-=1
        i+=1
    put(p,s[:opening]+'{ '+new+' }'+s[i:])

# Keep serialized formation IDs/state readable, but remove the gameplay system.
p='Units/CommanderFormation.cs'
sub(p,'public bool HasLegion => traits.Has(CommanderTrait.Legion);','public bool HasLegion => false; // Retired trait ID remains readable in old saves.')
sub(p,'public Formation SelectedFormation => FormationState.selected;','public Formation SelectedFormation => Formation.Plain;')
sub(p,'public IReadOnlyList<Formation> FormationAffinities { get { EnsureFormationAffinities(); return FormationState.affinities; } }','public IReadOnlyList<Formation> FormationAffinities => System.Array.Empty<Formation>();')
sub(p,'public bool CanSelectFormation => CanMobilize && !IsDeployed && !IsAwayFromHome && !IsInCombat;','public bool CanSelectFormation => false;')
body(p,'public string FormationBlockReason(', 'return "진형은 사용하지 않습니다.";')
body(p,'public bool TrySelectFormation(', 'return false;')
body(p,'private void EnsureFormationAffinities(', '')
body(p,'private void TickFormation(', '')
body(p,'private FormationModifiers FormationEffect(', 'return new FormationModifiers();')
sub(p,'public float FormationWorldMoveMultiplier => 1 + FormationRules.Modifiers(SelectedFormation, HasLegion).WorldMove;','public float FormationWorldMoveMultiplier => 1f;')
p='Units/CommanderAnt.cs'
s=read(p); s=re.sub(r'public int CommandLimit => FormationRules.CommandLimit\([\s\S]*?;', 'public int CommandLimit => 0;',s); put(p,s)
sub(p,'traits.AttackBonus + (HasNearbyFriend ? 1 : 0) + (HasSupportAura ? SupportAttackBonus : 0)','traits.AttackBonus')
sub(p,'+ (HasSupportAura ? SupportArmorBonus : 0) + (skills.DefensiveStanceActive', '+ (skills.DefensiveStanceActive')
p='UI/GameMenuConscription.cs'; s=read(p); a=s.index('                // D04 진형:'); b=s.index('                var amount',a); s=s[:a]+s[b:]; s=re.sub(r'                    formation\.[^\n]*\n','',s); a=s.index('        // 진형 증감'); s=s[:a]+'    }\n}\n'; put(p,s)
p='Core/FormationResearch.cs'; body(p,'public static IEnumerable<ScienceDefinition> Definitions()', 'yield break;')

# 16 serialized slots, 15 live skills. Preserve old Command slot/CA until migration policy is decided.
p='Units/CommanderTalents.cs'
sub(p,'public const int Count = 16, MaxLevel = 20, MaxPotential = 200;','public const int Count = 16, ActiveCount = 15, MaxLevel = 20, MaxPotential = 200;\n        public static bool IsActive(CommanderActivity skill) => skill != CommanderActivity.Command && (int)skill >= 0 && (int)skill < Count;')
sub(p,'!Enum.IsDefined(typeof(CommanderActivity), skill)', '!IsActive(skill)')
sub(p,'!Enum.IsDefined(typeof(CommanderActivity), main)', '!IsActive(main)')
sub(p,'var support = topic == main ? null : topic ?? Support(main);','var support = topic == main || topic.HasValue && !IsActive(topic.Value) ? null : topic ?? Support(main);')
sub(p,'if (usage[i] / total < MinUsageShare', 'if (IsActive((CommanderActivity)i) && usage[i] / total < MinUsageShare')
sub(p,'for (int i = 0; i < Count; i++) levels[i] = Mathf.Clamp', 'for (int i = 0; i < Count; i++) if (IsActive((CommanderActivity)i)) levels[i] = Mathf.Clamp')
sub(p,'if (levels[i] >= MaxLevel || UnityEngine.Random.value', 'if (!IsActive((CommanderActivity)i) || levels[i] >= MaxLevel || UnityEngine.Random.value')
p='Units/CommanderTraits.cs'
sub(p,'if (values.Count >= MaxTraits || values.Contains(t))', 'if (t == CommanderTrait.Legion || values.Count >= MaxTraits || values.Contains(t))')
sub(p,'public bool Has(CommanderTrait trait) => values.Contains(trait) ||', 'public bool Has(CommanderTrait trait) => trait != CommanderTrait.Legion && (values.Contains(trait) ||')
sub(p,'personality == CommanderPersonality.Devoted)));','personality == CommanderPersonality.Devoted))));')
sub(p,'if (result.Flame(activity) > 0) continue;', 'if (!CommanderTalents.IsActive(activity) || result.Flame(activity) > 0) continue;')
sub(p,'pool.AddRange(second.passions);','pool.AddRange(second.passions); pool.RemoveAll(p => !CommanderTalents.IsActive(p.activity));')
p='Core/SpecialtyAcquisition.cs'; sub(p,'{ CommanderActivity.Melee, CommanderActivity.Ranged, CommanderActivity.Command }','{ CommanderActivity.Melee, CommanderActivity.Ranged }')
# Preserve the existing 3-slot specialty save layout; ignore its retired last slot.
sub(p,'s.combat.Length != CombatActivities.Length','s.combat.Length < CombatActivities.Length')
p='Units/CommanderAdvancedSkills.cs'; sub(p,'&& talents.Level(CommanderActivity.Command) >= SocialRules.SkillLevel && Social.rallyCooldown <= 0;', '&& Social.rallyCooldown <= 0;')
p='UI/CommandCard.cs'; sub(p,'(talents.Level(CommanderActivity.Command) < 5 ? "집결\\n지휘 5 필요" : "집결" + Cooldown(c.Social.rallyCooldown))','("집결" + Cooldown(c.Social.rallyCooldown))')
p='Units/CommanderPersonal.cs'; sub(p,'                    GainExperience(CommanderActivity.Command, Budget());','                    // Retired command/cooperation skill does not gain experience.')
p='Units/CommanderJoy.cs'; s=read(p); s='\n'.join(l for l in s.split('\n') if 'GainExperience(CommanderActivity.Command' not in l); put(p,s)
p='UI/GameMenuRoster.cs'; sub(p,'CommanderTalents.Count + 2','CommanderTalents.ActiveCount + 2'); sub(p,'                var i = (int)skill; var level', '                if (!CommanderTalents.IsActive(skill)) continue;\n                var i = (int)skill; var visualIndex = i > (int)CommanderActivity.Command ? i - 1 : i; var level'); sub(p,'var x = i % 3 * 166f; var y = 26 + i / 3 * 46f;', 'var x = visualIndex % 3 * 166f; var y = 26 + visualIndex / 3 * 46f;')
p='UI/GameMenuCrashStart.cs'; sub(p,'                var skill = (CommanderActivity)i; var flame', '                var skill = (CommanderActivity)i; if (!CommanderTalents.IsActive(skill)) continue;\n                var visualIndex = i > (int)CommanderActivity.Command ? i - 1 : i; var flame'); sub(p,'12 + i / 8 * 122, 262 + i % 8 * 22','12 + visualIndex / 8 * 122, 262 + visualIndex % 8 * 22')
p='UI/SelectedUnitPanel.cs'; sub(p,'Enumerable.Range(0, CommanderTalents.Count).OrderByDescending','Enumerable.Range(0, CommanderTalents.Count).Where(i => CommanderTalents.IsActive((CommanderActivity)i)).OrderByDescending')
p='Core/CampaignResearch.cs'; s=read(p); s=re.sub(r'            new ScienceDefinition\(ScienceTechnology\.(ConscriptionLaw|ReserveForces|TotalMobilization),[^\n]*\n','',s); s=s.replace('=> CommanderActivity.Command,','=> null,'); put(p,s)

# Civilian economy is save-compatibility data only, never a source of income or bodies.
p='Core/ColonyPopulation.cs'
for sig in ['private void OnRaid(', 'public void PayTax(', 'public void Immigrate(', 'public void Tick(', 'public void Monthly(', 'internal void OnAntsReturned(', 'internal void OnAntsLost(']: body(p,sig,'')
for name,value in [('Adults','0'),('Total','0'),('HousingCapacity','0'),('Unrest','false'),('Taxpayers','0'),('NextFoodTax','0'),('NextMaterialTax','0'),('Demand','0')]:
    s=read(p); s,n=re.subn(r'(public (?:static )?(?:int|bool|float) '+name+r' => )[^;]+;',r'\g<1>'+value+';',s); assert n==1,name; put(p,s)
body(p,'public bool TrySetPolicy(', 'return false;')
p='Core/AntPool.cs'
s=read(p); a=s.index('        // ponytail:'); b=s.index('        public event Action',a)
s=s[:a]+'''        // Inert legacy save adapter. Counts are archived, never spendable workforce.
        internal int LegacyFree, LegacyAssigned, LegacyReserved;
        public int Free => 0;
        public int Assigned => 0;
        public int Reserved => 0;
        public int Total => 0;

'''+s[b:]; s=s.replace('Free = GameSession.Exists && GameSession.Instance.GameStarted ? 0 : startingAnts;',''); put(p,s)
body(p,'internal void RestoreCounts(', 'LegacyFree = Mathf.Max(0, free); LegacyAssigned = Mathf.Max(0, assigned); LegacyReserved = Mathf.Max(0, reserved); OnPoolChanged?.Invoke();')
for sig in ['public void Breed(', 'public void ReturnAssigned(', 'public void LoseAssigned(', 'public void ReleaseReserved(']: body(p,sig,'')
for sig in ['public bool TryAssign(', 'public bool StarveOne(']: body(p,sig,'return false;')
body(p,'public bool TryReserve(', 'return count <= 0;')
body(p,'public int RemoveFree(', 'return 0;')
p='Save/SaveSnapshot.cs'; sub(p,'antsFree = pool.Free, antsAssigned = pool.Assigned, antsReserved = pool.Reserved','antsFree = pool.LegacyFree, antsAssigned = pool.LegacyAssigned, antsReserved = pool.LegacyReserved')
p='Core/ColonyPopulation.Civilians.cs'
body(p,'public void SyncCivilianLocations(', '')
body(p,'public bool SetCivilianProtection(', 'return false;')
body(p,'public int DamageCivilians(', 'return 0;')
sub(p,'public int LocatedCivilians => S.young + Mathf.Max(0, S.old - S.agingDue) + (AntPool.Instance?.Free ?? 0);','public int LocatedCivilians => 0;')
p='Core/ColonyPopulation.Evacuation.cs'
body(p,'public void ReturnRocketCivilians(', '')
body(p,'public int BeginCivilianEvacuation(', 'return 0;')
body(p,'public int TakeArrivedCivilians(', 'return 0;')
p='Buildings/AirshipYard.Civilians.cs'; body(p,'public int TryBoard(int groupId)', 'return 0;'); body(p,'private void UnloadCivilians()', '')
p='Buildings/AirshipYard.Rocket.cs'; sub(p,'public int CivilianPassengers => state.civilianPassengers;', 'public int CivilianPassengers => 0;')
p='Buildings/AirshipYard.Reservations.cs'; s=read(p); s=re.sub(r'public int ReservedSeats => state.reservedGroups.Sum\([^\n]*\n\s*\+', 'public int ReservedSeats =>',s); put(p,s); body(p,'public int EvacuateAndReserve(', 'return 0;')
p='Buildings/Redevelopment.cs'; body(p,'public static Housing FindTarget(', 'return null;'); body(p,'public static int Residents(', 'return 0;')
p='Buildings/ScoutPost.cs'; s=read(p); a=s.index('            if (dispatchAnts > 0)',s.index('public bool TryDispatch')); b=s.index('\n\n',a); s=s[:a]+'            dispatchedAnts = 0; // Commander-only expedition.\n'+s[b:]; s=s.replace('public int DispatchAnts => dispatchAnts;', 'public int DispatchAnts => 0;'); put(p,s)

# Research arrays are indexed by saved enum ID: retain definitions, hide/disable retired entries.
p='Core/FormationResearch.cs'; put(p,(root/p).read_text(encoding='utf-8-sig'))
p='Core/CampaignResearch.cs'; put(p,(root/p).read_text(encoding='utf-8-sig').replace('=> CommanderActivity.Command,','=> null,'))
sub(p,'public ScienceDefinition Active => state.active >= 0 ? Technologies[state.active] : null;', 'public static bool Retired(ScienceTechnology t) => t is ScienceTechnology.ConscriptionLaw or ScienceTechnology.ReserveForces or ScienceTechnology.TotalMobilization\n            or ScienceTechnology.FormationLine or ScienceTechnology.FormationColumn or ScienceTechnology.FormationWedge or ScienceTechnology.FormationDispersed or ScienceTechnology.FormationCover or ScienceTechnology.FormationFocus;\n        public ScienceDefinition Active => state.active >= 0 && !Retired((ScienceTechnology)state.active) ? Technologies[state.active] : null;')
sub(p,'public string BlockReason(ScienceTechnology technology)\n        {','public string BlockReason(ScienceTechnology technology)\n        {\n            if (Retired(technology)) return "현재 규칙에서 사용하지 않는 연구입니다.";')
for p in ['UI/GameMenuResearch.cs','UI/GameMenuResearchTree.cs']:
    s=read(p)
    s=s.replace('CampaignResearch.Technologies.Where(d => d.Tier == researchEra)', 'CampaignResearch.Technologies.Where(d => !CampaignResearch.Retired(d.Technology) && d.Tier == researchEra)')
    s=s.replace('var defs=CampaignResearch.Technologies;', 'var defs=CampaignResearch.Technologies.Where(d => !CampaignResearch.Retired(d.Technology)).ToArray();')
    put(p,s)
p='Core/ColonyPopulation.Civilians.cs'; sub(p,'CivilianLocations => S.civilianLocations;', 'CivilianLocations => System.Array.Empty<CivilianLocation>();')
p='UI/GameMenuPopulation.cs'; body(p,'public void Population()', 'Roster();')
p='UI/HUDController.cs'; s=read(p); a=s.index('            var pool = AntPool.Instance;',s.index('private void UpdateResourceText')); b=s.index('\n        }',a)
s=s[:a]+'''            var count = CommanderRoster.Instance?.Commanders.Count(c => c != null && c.IsColonyMember && !c.IsDead) ?? 0;
            resourceTexts[2].text = $"<color=#b1b3a7>장수</color> <b>{count}</b>";
            demandFill.gameObject.SetActive(false);
            resourceTexts[2].transform.parent.GetComponent<MenuTooltip>().Message = "장수 명부 열기";
'''+s[b:]; put(p,s)
p='UI/HudOverview.cs'; sub(p,'workforce.text = $"대기 {AntPool.Instance?.Free ?? 0}    동원 가능 {AvailableDraft}";', 'workforce.text = $"본거지 장수 {HomeCommanders.Length}명";'); sub(p,'대기 인력 {AntPool.Instance?.Free ?? 0} · 추가 동원 가능 {AvailableDraft}', '본거지 장수 {HomeCommanders.Length}명')
p='UI/BuildScreen.cs'; s=read(p)
s=s.replace('new[] { new Entry("초가집", BuildingKind.Hut), new Entry("흙집", BuildingKind.House), new Entry("큰 아파트", BuildingKind.Apartment), new Entry("행정 책상", BuildingKind.AdminDesk), new Entry("학당", BuildingKind.School), new Entry("공공서비스", BuildingKind.CivicPublicService) }','new Entry[0] // Legacy village category retained for saved UI indices.')
s=s.replace(', new Entry("시민 진료소", BuildingKind.CitizenClinic)','').replace('"지휘 연구소"','"지원 연구소"').replace('"지휘 훈련장"','"지원 훈련장"').replace('"징집소"','"소집소"'); put(p,s)
p='UI/GameMenuCampaign.cs'; s=read(p); a=s.index('                var population = ColonyPopulation.Instance;'); b=s.index('                if (yard.Phase == RocketPhase.Boarding)',a); s=s[:a]+s[b:]; s=s.replace(' / 인력 4마리','').replace(' / 인력 10마리',''); put(p,s)
p='Buildings/Housing.cs'; s=read(p); s=re.sub(r'(public int Capacity => )[^;]+;',r'\g<1>0;',s); put(p,s)
# Hide retired traits centrally without mutating old save values.
p='UI/TraitChip.cs'; sub(p,'using System.Collections.Generic;', 'using System.Collections.Generic;\nusing System.Linq;'); sub(p,'            if (traits.Count == 0)', '            traits = traits.Where(t => t != CommanderTrait.Legion).ToArray();\n            if (traits.Count == 0)')
for p in ['UI/DetailTabs.cs','UI/GameMenuManagement.cs']:
    sub(p,'c.Traits.values.Select(CommanderTraits.DisplayName)', 'c.Traits.values.Where(t => t != CommanderTrait.Legion).Select(CommanderTraits.DisplayName)')
p='UI/GameMenuResearchTree.cs'; sub(p,'var d=defs[(int)t];', 'var d=CampaignResearch.Technologies[(int)t];')
p='UI/GameMenuCivicServices.cs'; body(p,'public void CivicServices()', 'Roster();')
p='UI/GameMenuPassengerReservations.cs'; body(p,'private void CivilianDestination(', 'Roster();'); s=read(p); a=s.index('            foreach (var id in yard.ReservedGroups'); b=s.index('\n        }',a); put(p,s[:a]+s[b:])
p='UI/GameMenuManagement.cs'; sub(p,'"포로", "인구", "기록"','"포로", "기록"')
p='UI/GameMenuDock.cs'; sub(p,'"운영","주거","지도"','"운영","장수 명부","지도"')
p='UI/GameMenuController.cs'; sub(p,'4. 시민 60명과 2티어 병영을 확보하면 과학연구소를 지을 수 있습니다.', '4. 정착지 연구대에 장수를 배정해 연구합니다.')
p='UI/GameMenuCrashStart.cs'; sub(p,'장수 즉사 없음이어도 로켓 파괴 시\\n탑승 시민·병력은 사망합니다.', '장수의 생존과 시설 방어를\\n함께 준비하세요.')
for p in ['UI/GameMenuEvents.cs','UI/GameMenuEnding.cs']:
    s=read(p); s='\n'.join(l for l in s.split('\n') if not any(k in l for k in ['<b>시민</b>', '<b>시민 최종 합계</b>', 'MenuTheme.Text(content, $"시민 유입'])); put(p,s)
p='UI/GameMenuRocket.cs'; s=read(p).replace('장수+병력+시민','장수').replace('인구가 늘어도','장수가 늘어도'); put(p,s)
p='Core/ColonyPopulation.Shelter.cs'; body(p,'public void InvalidateHousing(Vector3 position)', ''); body(p,'public static void InvalidateHousing(State state, Vector3 position)', '')
p='Core/ColonyPopulation.Ages.cs'; body(p,'public void AgeYear()', '')
p='Core/ColonyPopulation.Military.cs'; body(p,'public int AvailableMilitary(', 'return 0;'); body(p,'public bool TryMilitaryDraft(', 'return false;')

# Existing migration event keeps its commander offer; it no longer produces numeric citizens.
p='World/ColonyEvents.cs'; s=read(p); s=s.replace('            AntPool.Instance?.Breed(EventRules.Migrants);\n',''); s=s.replace('(migrant != null ? migrant.CommanderName + " + " : "") + $"시민 {EventRules.Migrants}명 합류"', 'migrant != null ? migrant.CommanderName + " 합류" : "합류할 장수 없음"'); put(p,s)
p='UI/HUDController.cs'; sub(p,'using AntColony.Boss;', 'using System.Linq;\nusing AntColony.Boss;')
p='UI/BuildScreen.cs'; sub(p,'new Entry[0] // Legacy village category retained for saved UI indices.', 'new Entry[0] /* Legacy village category retained for saved UI indices. */')
p='Core/ColonyPopulation.Economy.cs'; body(p,'public void PayCivicUpkeep()', ''); body(p,'public void ApplyEducationAtAdulthood()', '')
p='Core/CampaignResearch.cs'; sub(p,'            foreach (var storage in AntColony.Core.SceneObjects.All<Storage>()) storage.RefreshCapacity();', '            if (state.active >= 0 && Retired((ScienceTechnology)state.active))\n            { if (!state.partialIds.Contains(state.active)) { state.partialIds.Add(state.active); state.partialProgress.Add(state.progress); } state.active = -1; state.progress = 0; }\n            foreach (var storage in AntColony.Core.SceneObjects.All<Storage>()) storage.RefreshCapacity();')
p='Core/ScienceEffects.cs'; s=read(p); s=s.replace('public static bool BuildingUnlocked(BuildingKind kind) =>', 'public static bool RetiredBuilding(BuildingKind kind) => kind is BuildingKind.Hut or BuildingKind.House or BuildingKind.Apartment or BuildingKind.AdminDesk or BuildingKind.School or BuildingKind.CitizenClinic or BuildingKind.CivicPublicService;\n        public static bool BuildingUnlocked(BuildingKind kind) => !RetiredBuilding(kind) &&'); put(p,s)
p='UI/GameMenuScout.cs'; sub(p,'장수 상태·식량·대기 시민','장수 상태·식량'); sub(p,'var rm = ResourceManager.Instance; var free = AntPool.Instance != null ? AntPool.Instance.Free : 0;', 'var rm = ResourceManager.Instance;'); sub(p,' && free >= post.DispatchAnts','')
p='UI/CommanderAcquisitionPanel.cs'; s=read(p); a=s.index('            if (AntPool.Instance != null && AntPool.Instance.Free < activeScout.DispatchAnts)'); b=s.index('            GameMenuController.Instance?.ShowScout',a); put(p,s[:a]+s[b:])
p='Units/CommanderEquipment.cs'; sub(p,'effect = (TrinketEffect)UnityEngine.Random.Range(0,4)', 'effect = (TrinketEffect)UnityEngine.Random.Range(0,3)')
p='Core/ColonyPopulation.Military.cs'; s=read(p); s=re.sub(r'public int MobilizationLimit\(bool homeThreat\) =>[^;]+;', 'public int MobilizationLimit(bool homeThreat) => 0;',s); put(p,s)
p='Buildings/Barracks.Training.cs'; sub(p,'public bool FreeForTraining => !IsDead', 'public bool FreeForTraining => role != UnitRole.Support && !IsDead')
p='UI/BuildScreen.cs'; sub(p,', new Entry("지원 훈련장", BuildingKind.Barracks, UnitRole.Support)', '')
p='UI/CommandCard.cs'; sub(p,'집결(지휘 5)', '집결(지원 장비)'); sub(p,'UnitRole.Support => "지휘"', 'UnitRole.Support => "지원"')
p='Units/CommanderEquipment.cs'; sub(p,'WeaponKind.Pheromone => "지휘 신호기"', 'WeaponKind.Pheromone => "지원 신호기"')
p='Core/ColonyPopulation.Admin.cs'; body(p,'public void AddAdministration(', '')
p='Units/CommanderTraits.cs'; sub(p,'public bool TryAdd(CommanderTrait t)', 'public bool TryAdd(CommanderTrait t, bool legacySave = false)'); sub(p,'if (t == CommanderTrait.Legion || values.Count', 'if (t == CommanderTrait.Legion && !legacySave || values.Count')
p='Save/SavePreflight.cs'; sub(p,'traits.TryAdd(value)', 'traits.TryAdd(value, legacySave: true)')
p='Core/SpecialtyAcquisition.cs'; sub(p,'s.combat = CombatActivities.Select(a => c.Talents.Level(a) >= DiamondLevel ? 1 : 0).ToArray();', 's.combat = CombatActivities.Select(a => c.Talents.Level(a) >= DiamondLevel ? 1 : 0).Concat(new[] { 0 }).ToArray();'); sub(p,'c.Traits.specialties?.combat?.Length == CombatActivities.Length', 'c.Traits.specialties?.combat?.Length >= CombatActivities.Length')
p='Buildings/BuildingPlacementController.cs'; sub(p,'public static string LockReason(BuildingKind kind)\n        {', 'public static string LockReason(BuildingKind kind)\n        {\n            if (ScienceEffects.RetiredBuilding(kind)) return "현재 규칙에서 사용하지 않는 시민 시설입니다.";')
p='Units/SpecialtyProgress.cs'; sub(p,'traits.TryAdd(t)', 'traits.TryAdd(t, legacySave: true)')
p='Save/SavePreflight.cs'; s=read(p); s='\n'.join(l for l in s.split('\n') if '"배정 시민"' not in l); s=s.replace(' && p.antsReserved == 0',''); put(p,s)
p='Save/SaveSnapshot.cs'; s=read(p); a=s.index('            // 2026-11-12: 구저장에서 장수에게 배속'); b=s.index('            CommanderAnt.CommonDiet',a); put(p,s[:a]+'            // Retired numeric workforce is archived independently of live commander orders.\n            AntPool.Instance.RestoreCounts(p.antsFree, p.antsAssigned, p.antsReserved);\n'+s[b:])
p='Units/CommanderEquipment.cs'; sub(p,'default: return new[] { 3f, 5f, 7f, 10f }[item.quality] * unique;', 'default: return 0; // Retired command insignia remains loadable.')
p='Units/EquipmentRecipes.cs'; sub(p,'public static bool Unlocked(EquipmentRecipe recipe) => Enum.IsDefined', 'public static bool Unlocked(EquipmentRecipe recipe) => recipe != EquipmentRecipe.Insignia && Enum.IsDefined'); sub(p,'"지휘 신호기"','"지원 신호기"'); sub(p,'"지휘 표식"','"구 지휘 표식(효과 없음)"')
p='UI/GameMenuEquipment.cs'; sub(p,'_ => "지휘 한도"', '_ => "없음(이전 장비)"'); sub(p,'지휘 신호기 지원(지휘 범위 +', '지원 신호기(지원 범위 +')
p='UI/GameMenuManagement.cs'; sub(p,'"특성 도감", "진형"', '"특성 도감", "소집"'); sub(p,'case "진형": OpenConscription(); return;', 'case "소집": OpenConscription(); return;')
p='UI/GameMenuController.cs'; sub(p,'장수 · 평가 · 외부 조사 · 특성 · 진형', '장수 · 평가 · 외부 조사 · 특성 · 소집')
p='UI/GameMenuWalking.cs'; sub(p,'장수 1명·병력 0명부터 출발할 수 있습니다. 병력 추가는 징집소에서 편성하세요. 원정 중에는 진형을 바꿀 수 없습니다.', '장수 1명부터 도보 편성을 구성합니다. 장수 본인의 장비와 건강 상태를 확인하세요.')
p='Buildings/ScoutPost.cs'; sub(p,' · 시민 {dispatchAnts}', '')
p='UI/GameMenuResearchTree.cs'; s=read(p).replace('defs[(int)', 'CampaignResearch.Technologies[(int)').replace('if(!researchPick.HasValue)', 'if(!researchPick.HasValue || CampaignResearch.Retired(researchPick.Value))'); put(p,s)
p='Core/CampaignResearch.cs'; sub(p,'public bool Has(ScienceTechnology technology) => state.completed.Contains((int)technology)', 'public bool Has(ScienceTechnology technology) => !Retired(technology) && (state.completed.Contains((int)technology)'); sub(p,'GameManager.Instance.FishingUnlocked;', 'GameManager.Instance.FishingUnlocked);'); sub(p,'public int CompletedCount => state.completed.Count;', 'public int CompletedCount => state.completed.Count(i => !Retired((ScienceTechnology)i));')
p='UI/GameMenuDock.cs'; sub(p,',"Prototype Population"',''); sub(p,',"장수 명부"',''); sub(p,'small?total/8', 'small?total/Keys.Length'); sub(p,'new[]{"Prototype Home"', 'new[]{"Prototype Population","Prototype Home"')
p='UI/GameMenuAssessment.cs'; sub(p,'i < r.Skills.Length; i++) MenuTheme.Text', 'i < r.Skills.Length; i++) if (CommanderTalents.IsActive((CommanderActivity)i)) MenuTheme.Text')
p='UI/GameMenuDock.cs'; sub(p,'full?656:484', 'full?580:426'); sub(p,'i==3||i==6', 'i==3||i==5')
p='Units/CommanderTraits.cs'; sub(p,'public const int MaxTraits = 4;', 'public int ActiveTraitCount => values.Count - (values.Contains(CommanderTrait.Legion) ? 1 : 0);\n        public const int MaxTraits = 4;'); sub(p,'values.Count >= MaxTraits || values.Contains(t)', 't != CommanderTrait.Legion && ActiveTraitCount >= MaxTraits || values.Contains(t)')
p='Core/SpecialtyAcquisition.cs'; sub(p,'c.Traits.values.Count >= CommanderTraits.MaxTraits', 'c.Traits.ActiveTraitCount >= CommanderTraits.MaxTraits')
p='Save/SavePreflight.cs'; sub(p,'L(t.values, CommanderTraits.MaxTraits,', 'L(t.values, CommanderTraits.MaxTraits + 1,')

manifest={}
for p,s in changes.items():
    original=(root/p).read_bytes()
    if s==(root/p).read_text(encoding='utf-8-sig'): continue
    for folder in [base,out]: (folder/p).parent.mkdir(parents=True,exist_ok=True)
    if not (base/p).exists(): (base/p).write_bytes(original)
    (out/p).write_text(s,encoding='utf-8')
    manifest[p]=hashlib.sha256(original).hexdigest()
Path('work/commander-only-1112/removal-manifest.json').write_text(json.dumps(manifest,indent=2))
print('Prepared',len(manifest),'files; Assets unchanged')
