# MOTION-ACTIONS-1113 진행 (2026-11-13)

## 승인 범위
- 남은 전체 모션·장비 파지, 미연결 모션의 실제 행동 연결.
- 없는 행동은 Notion 현행 기획에 있는 경우에만 구현.
- 농사·건설 접점. SCV처럼 현장 주변의 접근 가능한 지점 사이를 이동하며 작업.
- 장비·가구 사용 모션, Meshy Animate 프리셋 우선. 추가 Meshy 한도 2,000크레딧.
- 필요한 가구 제작·게임 반영·Unity 실검증. 현재 SAVE/종료 요청 없음.

## 현재 구현 후보 (Assets 미반영)
- prepare_candidates.py: 최신 Assets를 바탕으로 좁은 변경을 work에 생성. 타 담당 CaptureWorkerOrder 변경 보존.
- WorkerAnt.ConstructionMotion: 원본 Heavy_Hammer_Swing/망치 유지, 실제 궤적 접점, .025m 도착, 건설 footprint를 가로지르는 경로 제외, 다른 면 접근 후보 검색.
- WorkerAnt.cs: 건물 중심 fallback 제거, 정확한 도착 후 작업, 다음 지점 이동, 임시 도착거리 복원.
- GatherApproach: 밭을 일반 건물 중앙 접근에서 제외, NeedsSowing에는 Tool_Till 접점 적용.
- NativeContext/Bindings/Player: 안정적인 캐릭터별 대기11종·대화4종·달리기3종·승리4종·부상 변형. 새로운 경제·전투 규칙 없음.
- CommanderPlayApproach/Joy/RecreationSpot/AntVisual.Work: 사용 자리 접근 후 오락 타이머·그룹 인원 판정, 접근 중 사용 모션 제외. 실제 가구 앉기/손 접촉은 후속 검증 필요.
- compile_candidates.py: 전체 게임 후보 외부 Roslyn 컴파일 성공. 아직 Unity 적용·플레이 검증 아님.

## Meshy
- 시작 잔액 2,035. 가구 콘셉트3종 27 + 텍스처 3D3종90 =117크레딧. 잔액1,918 화면 확인.
- 침대, 식탁+벤치, 책장 생성 완료. 게임용 약10K 삼각형 무료 remesh 요청 진행 중.
- 원본 Meadow Sprout 30K remesh/이족 리깅(골반·어깨·팔꿈치·무릎 마커 수동 보정), 스쿼트 프리셋 적용. 이 작업 추가 크레딧 차감은 관찰되지 않음.
- Air Squat with-skin Mixamo FBX ZIP 다운로드 클릭 시 Chrome ERR_BLOCKED_BY_CLIENT. 파일 미확보. 사용자 직접 다운로드 요청 중. 보안 설정 변경/차단 우회 안 함.
- 기존 리그/프로덕션 모델은 교체하지 않음. 새 원본 확보 시 기존 리그 호환과 외형/무릎 검사 필요.

## Unity 조율
- 21:26 UserSettings/SaveStorage.RootOverride 복원, editor_stop, Edit/ready 확인 후 밸런스 채팅에 인계.
- 21:56까지 사용 연장 합의. 이 동안 Assets 편집/Unity 조작 없음.
- 밸런스 소유: GameBalance, Kitchen, WildMonster, LocalIncursions, EarlyConstruction(ScienceLab 비용), WorkerAnt.CaptureWorkerOrder 한정.

## 적용 후 필수 확인
- CC_FarmTile와 CC_ConstructionPlan importer 읽기 허용 후 실제 메시 접점 계산.
- 바인딩 재생성 Native1112Bindings.Run. 전체119 모션에 대해 연결/부적합/기획상 미지원 목록 갱신.
- 정상 건설 명령: 최초 접근·면 사이 이동·망치 표면접점·완공·중단/재개·경로차단.
- 정상 파종/수확 및 도구 전환, 농사 접점.
- 여가시설 실제 도착, 타이머, 인원, 명령 중단, 저장 복원.
- 외형/무릎/승인된 곡괭이·도끼·활·방패 방향 회귀. Unity 화면과 수치 함께 확인.
- 새 가구 파일 확보·검수·임포트·사용 위치 연결, Graphify update, README 검증 상태 반영.

## 기획 근거
- Notion 건물·방어시설 3d0c4a0ecd3181c28b59e26337c15aba (현행 11/12): 장수의 실제 사용 자리/접근성, 건설·배송·예약 보존, 식탁/침대/휴게 가구.
- 장수 3d8c4a0ecd318155a477fda677e21fd1: 실제 시설·동료·안전·도달 가능성 기반 자유시간, 작업 우선.
- 자원 3d0c4a0ecd31812d80fbeceabb70193e: 실제 파종·성장·수확.
- 사다리·수영·마법 모션 등은 원본이 있어도 현행 기획상 행동 근거 없이 새 기능을 만들지 않음.

## 22:27 Unity 인계 및 검증 갱신
- 12개 후보 파일 프로덕션 반영, 컴파일 오류0. Runtime81바인딩/56원본클립.
- 건설 정상 명령에서 서로 다른4작업점/Heavy_Hammer_Swing 진행 확인. 굶주림 식사 전환으로 자연 중단, 완공/재개 추가 검증 필요.
- 일반 침대 CommanderBedApproach 신규: 실제 도착 후 원본 수면 배치, 매트리스 측정, 선택/정체성 위치도 침대로 이동, Agent exit 보존. SingleBed 접근/수면/기상4m 이동 PASS 및 screens/bed.png 확인. Double/Bunk/Mat/저장복원 추가 검증 필요. Hammock/Dormitory는 사용 접근만 적용, 내부 눕기 자세 미연결.
- 농사: 처음 fitted=False는 검사에서 actor 높이를 floor로 전달한 오류. 실제 지면으로 fitted=True, 목표1.649m/도착1.667m. 원본 도구 머리6극점×72프레임/실제MeshCollider 측정 최소거리.03293m. Farm 파종 접근거리-.025m 보정 적용/컴파일0, 보정 후 재측정은 미완료. screens/farm-contact.png.
- 새 게임 우주선 잔해 생성 제거 사용자 추가 요청 반영: CrashLandingStart.Apply 생성 블록 삭제, 실제 새게임 wrecks0/장수3 PASS.
- 시체 paused 개별 fixture Ant는 bundle만 표시, EnemyCommander 원본 끝 자세 머리-발높이.078m로 정상. 전체저장 postfix-final-save 재현은 남음.
- 22:27 Edit ready/설정 및 SaveStorage.RootOverride 복원 후 밸런스 채팅에 양도. 23:07 전후 반환 예정, 이 동안 Assets/Unity 동결.
- 다운로드 성공: 목공모션/스쿼트/침대/식탁벤치/목공작업대 ZIP5개. MESHY 폴더 실제 존재, art/meshy-1113 추출, download-manifest.json SHA256 기록. 재다운로드 금지. 책장만 남음.
- Meshy 사용 합계166 (가구3종117+목공모션10+작업대39), 예산2000. 새 원본 리그는 기존 프로덕션에 아직 미연결. 검수 및 외형 파츠 호환 준비 필요.
- prepare_candidates.py는 초기 적용 전용이며 이미 반영한 코드에 재실행하지 말 것. 최신 CommanderBedApproach/농사 보정은 Assets가 권위본.

## 22:55 후속 준비
- 책장 ZIP 확보/압축 해제/해시 등록 완료. 다운로드 알림 시간 초과 후 늦게 완료되어 동일 SHA256 ZIP 2개가 생김. 적용 목록에는 원본 1개만 사용, 추가 재다운로드 없음.
- Furniture1113 임포터 AgentScripts/Motion1113Models.cs 준비. 침대/식탁벤치/책장/작업대 원본4종을 art/meshy-1113/staged로 보존 복사. Assets 미반영.
- followup/AntVisual.Work.cs: 여가 시설 도착 후 시설 방향 바라보기. CommanderJoy.cs: 가장 가까운 접근불가 시설 대신 다음 도달가능 시설 선택. 외부 Roslyn 컴파일 종료0, Unity 미검증. 후보 적용 전 patch-manifest.json SHA 검증 필수.
- FarmCheck 검사 지면 Y 오류 수정. 기존 프로덕션 파종 접근거리-.025m 재측정은 Unity 반환 후.
- 새 saw/squat FBX read-only 구조 검사: 새86본과 기존84/86본의 보조본 이름/기준변환 차이 있음. 기존 외형 Kit0/1 직접 재사용 안전성 미확인. fbx-structure.json, audit_fbx.py.
- 의자/벤치/컵/그릇/열린책 추가5종 생성195크레딧 승인, 경량화 완료. 의자 ZIP 다운로드 확인. 나머지4종 다운로드 중. 잔액1674 확인(누계361).
- 사용자 추가: 요리/치료/농사 소품은 없는 것과 기존 Blender 품질 부족분 교체, 울타리 추가. 기존 preview에서 Basket 단순덩어리/Spoon 분리/Pestle 각진막대 확인. 파일 존재만으로 재사용 확정 금지. WateringCan은 후속수정이력 있어 최신FBX 확인 필요.
- Meshy 별도 채팅 Cozy Woodland RTS Prop Set에서 울타리/국자/도마/반죽/약병/붕대/씨앗주머니/손잡이바구니 8종312예상/330상한 제작 요청, 콘셉트72승인. 기존 프로젝트 예산2000 이내.
- Unity 밸런스 세션 반환 예상23:20으로 연장. Assets 동결 유지. 사용자 SAVE/컴퓨터종료 새 요청 없음.
- 23:02: 의자/벤치/컵/그릇/책 ZIP5종 모두 수신·추출·SHA256 등록 완료, 전체 고유11 ZIP. staged 9모델 임포트 준비 완료.
- 소품8종 콘셉트72 + 3D240 승인 완료, 제작 진행. 합계 예상/승인673크레딧(166+195+312), 추가 예산2000 이내. 해당 채팅 URL https://www.meshy.ai/ko/agent/OvHmns4bS80QZwBqM6Hm3?chat=QxHLLsa7IgshIXr_ThfM3 . 생성 결과 다운로드는 아직 미확인.
- 브라우저 CUA kernel timeout 후 기존 탭 Debugger unattached, 새탭 Page.getFrameTree timeout. native Chrome도 captured window activation 실패. 재생성/재다운로드 하지 않음. 다음 호출에서 브라우저 연결 복구 필요.
- 연구대 원거리작업 후보: CommanderResearchApproach 신규, CommanderWork.Needs ResearchWorkReady/ReturnToResearch 실제 작업점 도착 조건, CommanderWork.SuspendWork 및 CommanderAnt.OnDisable 임시stop거리복원, AntVisual.Work 연구자리 도착조건. 외부컴파일 종료0, 미적용/미검증.
- 밸런스 담당 WorkerAnt.TryWorkApproach 및 TravelDistance 근거리 NavMesh섬 대체접근 최소수정 동의. 해당 메서드 소유 타세션, 본세션 편집 금지.

## Chrome 재연결 후 다운로드 완료
- 높은 뾰족한 방벽 생성39크레딧/4546면. 낮은 울타리와 구분하여 확보. 소품8종 전부 확보. 20개 고유 ZIP CRC/SHA 검사, 기존 책장 중복은 적용 제외. 다운로드 재시도 전 실제파일 대조. 이번 추가 중복 없음. 누계712크레딧.
- 사용자 우선순위 정정: 작은 소품 추가 제작 중단. 주요 건물/큰 가구 우선. 기존 큰 가구의 실제 반영/사용 모션 우선.
- 가구 모델 교체 후보 FacilityVisual(책장/큰식탁/목공대) 준비. 침대는 사용 표면 검증 후 연결, 낮은 울타리를 목장 전체 모델로 단순대체하지 않음. 높은 방벽의 건설 항목 연결은 기존 벽 재료/충돌 규칙 대조 후. Unity 아직 타 채팅 소유.
- 23:39: staging 18개 모델 완료(큰 가구/방벽 포함), ZIP20개 고유 항목 검증. 후보 파일 원본SHA 전부 일치. 연구할당 해제 시 stoppingDistance 복원까지 보완. 새 SingleBed 중앙 표면 기반 SleepingSurface 후보 및 CommanderBedApproach 연결 준비. 외부 전체 게임 C# 컴파일 exit0. 후보는 아직 Assets에 적용하지 않음.
- Native 오프라인 연결 감사: Library119 / Runtime81행동,56원본. motion-bindings-offline.tsv 생성. 미연결63을 적용 완료로 기록하지 않음. 사다리/수영 등 기획 미지원 모션은 억지 구현 금지. 의자/앉기/식탁 모션은 실제 사용 자리 검증 후 연결 필요.
- Unity 타 채팅은 3웨이브 후 환자 탈주 결함 수정 후 Play 재진입에서 연결 복구 대기. 본 세션 임포트/프로덕션 변경/Play 확인 미착수. 다른 세션 작업·사용자 설정 보존.
- Unity 명시 반환 수신 후 7개 후보 SHA검증/백업/적용 + CommanderResearchApproach 신규 반영. Unity recompile completed/errors0.
- 큰 에셋5종 SingleBed9139/DiningTable9596/Bookshelf8851/Workbench8560/Palisade4546면 임포트 완료, URP 재질+base/normal 확인. 침대/식탁/책장/목공대 LoadModel 연결, 방벽은 모델 임포트만(건설항목 미연결).
- ModelAudit 성공: 중앙 매트리스 표면 normalizedY .64673. 기존 모델별 사용 자세 런타임 검증 준비. 사용자 설정 캡처 완료, editor_play 성공, 격리 새게임 Prepare 실행 응답 대기.

- 23:58 후속: 새 침대 NavMesh carve 이후 캐시 접근점 불일치 수정(침대/연구/여가 재샘플), 도착 전 수면 자세 차단. 컴파일 errors0. CLI 재연결 후 새 침대 접근5.060632m/arrived=True/수면 진행 확인, 기상 navigation 복원 및1.685535m 이동 PASS. screens/new-bed.png 직접 확인: 몸과 매트리스 맞춤은 여전히 어색하므로 시각 검수 미완. 방벽 건설 항목/가구 사용 모션 전체 완료 아님. 큰 건물·가구 우선, 작은 소품 신규 생성 중단 유지. Play 종료 및 사용자 설정/저장경로 복원.
