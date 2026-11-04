"""시설·물품별 작업 지점과 개미 동작(요청서 docs/COLONY_INTERACTION_ANIMATION_REQUEST.md 기준 제안값).
모델 앞쪽 = -Y. 개미 클립은 art/ant-anim/ant-work-clips.blend 이름. 게임 규칙이 아니라 연출 매핑."""

# id: (클립, 도구 or None, 추가 지점 목록, 메모)
# 추가 지점: 'Input','Output','Seat','Patient','Carry' — WorkPoint_0은 항상 생성
SPEC = {
    'CC_NestEntrance': ('Walk', None, [], '입구 경사로 진입'),
    'CC_Storage': ('PutDown', None, ['Input'], '보관 지점에 놓기·꺼내기'),
    'CC_FarmTile': ('GroundWork', None, [], '흙 고르기·심기'),
    'CC_CropFungus_Planted': ('GroundWork', None, [], '파종'),
    'CC_CropFungus_Young': (None, None, [], '성장 중 작업자 없음'),
    'CC_CropFungus_Growing': (None, None, [], '성장 중 작업자 없음'),
    'CC_CropFungus_Mature': ('Pickup', None, ['Output'], '숙여 수확'),
    'CC_CropFungus_Harvested': ('PutDown', None, ['Output'], '수확물 내려놓기'),
    'CC_CropFungus_Pest': ('GroundWork', None, [], '방제 작업 시에만'),
    'CC_RanchFence': ('Hammer', 'Tool_Hammer', [], '건설·수리 때만'),
    'CC_RanchGate': ('Walk', None, [], '통행 시 문 개폐'),
    'CC_Feeder': ('PutDown', None, ['Input'], '먹이 붓기'),
    'CC_CommanderBed': ('Treat', None, ['Patient'], '병상 옆 처치'),
    'CC_AnimalTreatment': ('Operate', 'Tool_Scalpel', ['Patient'], '직립 수술'),
    'CC_Apothecary': ('BenchWork', 'Tool_Pestle', ['Input', 'Output'], '절구 찧기'),
    'CC_ButcherTable': ('Chop', 'Tool_Saw', ['Input', 'Output'], '사체 해체'),
    'CC_Kitchen': ('BenchWork', 'Tool_Spoon', ['Input', 'Output'], '냄비 젓기'),
    'CC_CompostBin': ('PutDown', None, ['Input'], '배설물 투입'),
    'CC_Tombstone': ('Mourn', None, [], '짧게 고개 숙이기'),
    'CC_Dormitory': ('Walk', None, [], '출입 위치로 들어감'),
    'CC_Workshop': ('Hammer', 'Tool_Hammer', ['Input', 'Output'], '고정하고 망치질'),
    'CC_ResearchDesk': ('Inspect', None, [], '표본 살펴보기'),
    'CC_AdminDesk': ('BenchWork', 'Tool_Stamp', [], '서류·도장'),
    'CC_Refrigerator': ('Pickup', None, ['Input'], '문 열고 물건 출입'),
    'CC_Freezer': ('Pickup', None, ['Input'], '뚜껑 들고 물건 출입'),
    'CC_WoodGenerator': ('PutDown', None, ['Input'], '연료 투입'),
    'CC_Battery': ('Hammer', 'Tool_Hammer', [], '설치·수리 때만'),
    'CC_Lamp': ('Hammer', 'Tool_Hammer', [], '설치·수리 때만'),
    'CC_SoilWall': ('Hammer', 'Tool_Hammer', [], '건설·수리·철거 때만'),
    'CC_LeafWall': ('Hammer', 'Tool_Hammer', [], '건설·수리·철거 때만'),
    'CC_CastleWall': ('Hammer', 'Tool_Hammer', [], '건설·수리·철거 때만'),
    'CC_DoorFrame': ('Walk', None, [], '통행 시 문 개폐'),
    'CC_CastleGate': ('Walk', None, [], '통행 시 양쪽 문 개폐'),
    'CC_ConstructionPlan': ('PutDown', None, ['Input'], '자재 내려놓기'),
    'CC_ConstructionFrame': ('Hammer', 'Tool_Hammer', [], '골조 망치질'),
    'CC_DemolitionRubble': ('Mine', 'Tool_Pickaxe', [], '철거 도구질'),
    'CC_TreeBroadleaf': ('Chop', 'Tool_Axe', [], '도끼질'),
    'CC_TreeTall': ('Chop', 'Tool_Axe', [], '도끼질'),
    'CC_Stump': ('Chop', 'Tool_Axe', [], '낮게 절단'),
    'CC_FallenBranch': ('Pickup', None, [], '집기'),
    'CC_RockOutcrop': ('Mine', 'Tool_Pickaxe', [], '곡괭이질'),
    'CC_OreOutcrop': ('Mine', 'Tool_Pickaxe', [], '곡괭이질'),
    'CC_Bush': ('GroundWork', None, [], '뜯기'),
    'CC_GrassTuft': ('GroundWork', None, [], '뜯기'),
    'CC_WildFungus': ('Pickup', None, [], '집기'),
    'CC_PebbleScatter': ('Pickup', None, [], '집기'),
    'CC_SleepingMat': ('Sleep', None, ['Seat'], '엎드려 수면'),
    'CC_Chair': ('Sit', None, ['Seat'], '앉기'),
    'CC_DiningTable': ('Eat', None, ['Seat'], '앉아 먹기'),
    'CC_RestBench': ('Rest', None, ['Seat'], '앉아 더듬이 손질'),
    'CC_Bookshelf': ('Inspect', None, [], '책 살펴보기'),
    'CC_Campfire': ('Rest', None, [], '주변에서 휴식'),
    'CC_Toilet': ('Sit', None, ['Seat'], '앉기(절제)'),
    'CC_Washbasin': ('BenchWork', None, [], '앞다리 씻기'),
    'CC_Shower': ('StandIdle', None, [], '물 맞으며 서 있기'),
    'CC_Watchtower': ('Inspect', None, [], '둘러보기'),
    'CC_AcidTower': (None, None, [], '포탑 조준(개미 없음)'),
    'CC_SpikeTrap': ('Hammer', 'Tool_Hammer', [], '설치·해체 때만'),
    'CC_StickyTrap': ('GroundWork', None, [], '수지 펴 바르기'),
}
# 운반 물품: 공통 Carry, 등 위 CarryAnchor에 표시
for _id in ('CC_ResourceWood CC_ResourceStone CC_ResourceLeaf CC_ResourceFiber CC_ResourceGrain CC_ResourceCompost '
            'CC_ResourceOre CC_ResourcePlanks CC_ResourceSilk CC_FeedMeat CC_FeedPlant CC_FoodRotten CC_Manure '
            'CC_MealGood CC_MealSimple CC_MedicineAdvanced CC_MedicineAnimal CC_MedicineHerbal CC_MedicineStandard '
            'CC_SurgeryKit').split():
    SPEC[_id] = ('Carry', None, ['Carry'], '집어서 운반')

# 경첩: 대상 루트 → [(피벗 이름, 축 인덱스, 열림 각도)]
HINGES = {
    'CC_Refrigerator': [('Door_Hinge', 2, -100)],
    'CC_Freezer': [('Lid_Hinge', 0, -75)],
    'CC_DoorFrame': [('Door_Hinge.001', 2, 95)],
    'CC_RanchGate': [('Gate_Hinge', 2, 95)],
    'CC_CastleGate': [('GateLeft_Hinge', 2, 90), ('GateRight_Hinge', 2, -90)],
    'CC_AcidTower': [('Turret_Pivot', 2, 40)],  # 열림 대신 좌우 조준
}
