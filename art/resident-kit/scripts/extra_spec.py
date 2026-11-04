"""FURNITURE-97: buildable furniture (FurnitureCatalog entries with a BuildingKind) that had no model yet.

id: (한국어 이름, BuildingKind, 도감 분야, 클립, 도구, 바라보는 방향, 메모)
방향: 'in' = 시설(+Y)을 바라봄(서서 작업·마주 앉기), 'out' = 시설 앞(-Y)을 바라봄(의자·욕조처럼 등지고 앉기).
지점은 models_extra.py가 Empty로 직접 둔다: WorkPoint_0(서는 곳), SeatPoint/LiePoint, HandTarget, InputPoint.
HINGES: id → [(피벗, 축 0/1/2, 각도, 'swing'|'spin'|'rock')]. swing = 열림→유지→닫힘(기존 문과 같은 48f),
spin = 한 바퀴 반복, rock = 좌우 왕복 반복. RIDE: 캐릭터가 함께 움직이는 피벗(그네).
"""

SPEC = {
    # 타일
    'CC_Floor': ('바닥', 'Floor', '건설·방어', 'Hammer', 'Tool_Hammer', 'in', '깔기·수리 때만'),
    'CC_Pillar': ('기둥', 'Pillar', '건설·방어', 'Hammer', 'Tool_Hammer', 'in', '세우기·수리 때만'),
    'CC_CapWall': ('병뚜껑 벽', 'CapWall', '건설·방어', 'Hammer', 'Tool_Hammer', 'in', '건설·수리·철거 때만'),
    'CC_LockedDoor': ('잠금문', 'LockedDoor', '건설·방어', 'Walk', None, 'in', '허가된 주민 통행 시 문 개폐'),
    'CC_BarredDoor': ('창살문', 'BarredDoor', '건설·방어', 'Walk', None, 'in', '통행 시 창살문 개폐'),
    # 생활
    'CC_SingleBed': ('침대', 'SingleBed', '생활', 'Sleep', None, 'out', '누워 수면'),
    'CC_DoubleBed': ('큰침대', 'DoubleBed', '생활', 'Sleep', None, 'out', '두 명이 나란히 수면(한쪽 표시)'),
    'CC_BunkBed': ('2층침대', 'BunkBed', '생활', 'Sleep', None, 'out', '아래 칸 수면(위 칸은 사다리로)'),
    'CC_Hammock': ('해먹', 'Hammock', '생활', 'Sleep', None, 'out', '해먹에 누워 수면'),
    'CC_Nursery': ('요람', 'Nursery', '생활', 'RockCradle', None, 'in', '요람 흔들어 돌보기'),
    'CC_BanquetTable': ('연회용 긴 식탁', 'BanquetTable', '생활', 'Eat', None, 'in', '긴 의자에 앉아 함께 식사'),
    # 저장
    'CC_Jar': ('항아리', 'Jar', '보관·전력', 'PutDown', None, 'in', '항아리에 넣기·꺼내기'),
    'CC_FoodStore': ('저장고', 'FoodStore', '보관·전력', 'Pickup', None, 'in', '뚜껑 열고 식량 출입'),
    # 환경
    'CC_Brazier': ('화롯불', 'Brazier', '보관·전력', 'PutDown', None, 'in', '장작 넣기'),
    'CC_Lantern': ('등불', 'Lantern', '보관·전력', 'PutDown', None, 'in', '기름 채우기'),
    'CC_Torch': ('횃불', 'Torch', '보관·전력', 'PutDown', None, 'in', '불 붙이기·교체'),
    'CC_FireflyLamp': ('반딧불 등', 'FireflyLamp', '보관·전력', 'PutDown', None, 'in', '반딧불 먹이 넣기'),
    # 전력
    'CC_Treadmill': ('쳇바퀴', 'Treadmill', '보관·전력', 'Run', None, 'out', '바퀴 안에서 달려 발전'),
    'CC_PowerWire': ('전선', 'PowerWire', '보관·전력', 'Hammer', 'Tool_Hammer', 'in', '설치·수리 때만'),
    # 식량
    'CC_MushroomFarm': ('버섯밭', 'MushroomFarm', '농업·목장', 'GroundWork', None, 'in', '버섯 돌보기·수확'),
    'CC_AphidPen': ('축사', 'AphidPen', '농업·목장', 'PutDown', None, 'in', '여물통에 먹이 붓기(가축은 critter-kit)'),
    'CC_Hearth': ('화덕', 'Hearth', '생활', 'Stir', 'Tool_Spoon', 'in', '솥 젓기'),
    'CC_CareStation': ('돌봄대', 'CareStation', '농업·목장', 'Treat', None, 'in', '가축 돌보기'),
    # 작업
    'CC_Carpentry': ('목공대', 'Carpentry', '생산·연구', 'Saw', 'Tool_Saw', 'in', '판재 톱질'),
    'CC_Stonecutter': ('석공대', 'Stonecutter', '생산·연구', 'Hammer', 'Tool_Hammer', 'in', '돌 다듬기'),
    'CC_SpinningWheel': ('물레', 'SpinningWheel', '생산·연구', 'Spin', None, 'in', '앉아서 물레 돌리기'),
    'CC_Smelter': ('용광로·제련소', 'Smelter', '생산·연구', 'PutDown', None, 'in', '광석·연료 투입'),
    'CC_Kiln': ('유리 가마', 'Kiln', '생산·연구', 'Pickup', None, 'in', '구운 유리 꺼내기'),
    # 방어
    'CC_MineField': ('폭발 함정', 'MineField', '건설·방어', 'GroundWork', None, 'in', '묻기·해체 때만'),
    # 휴게
    'CC_BoardGame': ('놀이판', 'BoardGame', '휴게·장식', 'PlayBoard', None, 'in', '마주 앉아 말 옮기기'),
    'CC_Janggi': ('장기판', 'Janggi', '휴게·장식', 'PlayBoard', None, 'in', '마주 앉아 장기'),
    'CC_Baduk': ('바둑판', 'Baduk', '휴게·장식', 'PlayBoard', None, 'in', '마주 앉아 바둑'),
    'CC_WrestlingRing': ('씨름판', 'WrestlingRing', '휴게·장식', 'Wrestle', None, 'in', '모래판에서 씨름'),
    'CC_DartBoard': ('가시 다트', 'DartBoard', '휴게·장식', 'Throw', None, 'in', '떨어져서 가시 던지기'),
    'CC_Instrument': ('악기', 'Instrument', '휴게·장식', 'PlayMusic', None, 'in', '북·실로폰 연주'),
    'CC_ExerciseRig': ('운동기구', 'ExerciseRig', '휴게·장식', 'Exercise', None, 'in', '돌 역기 들기'),
    'CC_WebSwing': ('거미줄 그네', 'WebSwing', '휴게·장식', 'Swing', None, 'out', '그네 타기'),
    'CC_GamblingDen': ('주사위 도박판', 'GamblingDen', '휴게·장식', 'PlayBoard', None, 'in', '주사위 굴리기'),
    'CC_Bathtub': ('목욕통', 'Bathtub', '생활', 'Bathe', None, 'out', '통 안에 앉아 목욕'),
    'CC_HotSpring': ('온천', 'HotSpring', '휴게·장식', 'Bathe', None, 'out', '온천에 몸 담그기'),
    # 장식
    'CC_Statue': ('조각상', 'Statue', '휴게·장식', 'Inspect', None, 'in', '감상'),
    'CC_FlowerPot': ('화분', 'FlowerPot', '휴게·장식', 'WaterPlant', 'Prop_WateringCan', 'in', '물 주기'),
    'CC_Flag': ('깃발', 'Flag', '휴게·장식', 'Inspect', None, 'in', '감상'),
    'CC_Painting': ('그림', 'Painting', '휴게·장식', 'Inspect', None, 'in', '감상'),
    'CC_ShellDecoration': ('조개껍데기', 'ShellDecoration', '휴게·장식', 'Inspect', None, 'in', '감상'),
    'CC_MarbleMosaic': ('구슬 모자이크', 'MarbleMosaic', '휴게·장식', 'Inspect', None, 'in', '감상'),
    'CC_BottleMobile': ('병뚜껑 모빌', 'BottleMobile', '휴게·장식', 'Inspect', None, 'in', '돌아가는 모빌 감상'),
    'CC_Carpet': ('카펫', 'Carpet', '휴게·장식', 'Rest', None, 'out', '카펫에 앉아 쉬기'),
    'CC_Tapestry': ('태피스트리', 'Tapestry', '휴게·장식', 'Inspect', None, 'in', '감상'),
    'CC_CuriosDisplay': ('사람 물건 전시대', 'CuriosDisplay', '휴게·장식', 'Inspect', None, 'in', '감상'),
    'CC_Monument': ('기념비', 'Monument', '휴게·장식', 'Mourn', None, 'in', '고개 숙여 기리기'),
    'CC_BronzeStatue': ('동상', 'BronzeStatue', '휴게·장식', 'Inspect', None, 'in', '감상'),
    'CC_TrophyCase': ('전리품 진열대', 'TrophyCase', '휴게·장식', 'Inspect', None, 'in', '감상'),
    # 군사
    'CC_Barracks': ('허수아비(근접 훈련대)', 'Barracks', '군사·마을', 'Attack', None, 'in', '창으로 허수아비 찌르기'),
    'CC_ConscriptionPost': ('징집소', 'ConscriptionPost', '군사·마을', 'Stamp', 'Tool_Stamp', 'in', '명부에 도장'),
    'CC_Armory': ('무기고', 'Armory', '군사·마을', 'Pickup', None, 'in', '무기 꺼내기·걸기'),
    'CC_PrisonerCamp': ('포로 수용소', 'PrisonerCamp', '군사·마을', 'Captive', None, 'out', '포로가 묶여 앉아 있음'),
    'CC_ScoutPost': ('정찰 초소', 'ScoutPost', '군사·마을', 'Inspect', None, 'in', '망원경으로 둘러보기'),
    # 마을
    'CC_Hut': ('초가집', 'Hut', '군사·마을', 'Walk', None, 'in', '출입문으로 들어감'),
    'CC_House': ('흙집', 'House', '군사·마을', 'Walk', None, 'in', '출입문으로 들어감'),
    'CC_Apartment': ('큰 아파트', 'Apartment', '군사·마을', 'Walk', None, 'in', '1층 출입문으로 들어감'),
    # 이동수단
    'CC_AirshipYard': ('로켓 발사대', 'AirshipYard', '군사·마을', 'Hammer', 'Tool_Hammer', 'in', '로켓 조립 망치질'),
    # 기반 시설(2026-10-19, BuildingKind 중 모델 없던 5종)
    'CC_Stockpile': ('비축더미', 'QueenChamber', '보관·전력', 'PutDown', None, 'in', '자원 내려놓기'),
    'CC_ResearchLab': ('무기 연구소', 'ResearchLab', '생산·연구', 'Hammer', 'Tool_Hammer', 'in', '시제품 두드려 보기'),
    'CC_DefenseLab': ('방어시설 연구소', 'DefenseLab', '생산·연구', 'Inspect', None, 'in', '분사기·함정 시험'),
    'CC_RestRoom': ('휴게실', 'RestRoom', '휴게·장식', 'Rest', None, 'out', '긴 의자에 앉아 쉬기'),
    'CC_AreaAcidTower': ('범위형 분사탑', 'AreaAcidTower', '건설·방어', 'Hammer', 'Tool_Hammer', 'in', '수리 때만(포탑 회전)'),
}

HINGES = {
    'CC_LockedDoor': [('Door_Hinge', 2, 95, 'swing')],
    'CC_BarredDoor': [('Door_Hinge', 2, 95, 'swing')],
    'CC_FoodStore': [('Lid_Hinge', 0, -75, 'swing')],
    'CC_Nursery': [('Cradle_Pivot', 1, 12, 'rock')],
    'CC_Treadmill': [('Wheel_Pivot', 0, -360, 'spin')],
    'CC_SpinningWheel': [('Wheel_Pivot', 0, 360, 'spin')],
    'CC_WebSwing': [('Swing_Pivot', 0, 25, 'rock')],
    'CC_BottleMobile': [('Mobile_Pivot', 2, 360, 'spin')],
    'CC_AreaAcidTower': [('Turret_Pivot', 2, 360, 'spin')],
}
RIDE = {'CC_WebSwing': 'Swing_Seat'}
