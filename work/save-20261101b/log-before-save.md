# 프로젝트 로그

## 현재 상태 — 2026-11-01 SAVE
- 프로젝트 E:\Git\ant, develop(일반)/master(안정). 목표는 Notion 기획(스펙 문서) 전체 구현. 구조: Assets/_Project/Scripts(Core/Units/World/Buildings/Map/UI/Save/Boss), AgentScripts 검사, docs/IMPLEMENTATION_BOARD.md·OPERATIONS_100_STATUS.md·DECISIONS_*/GAPS_*/GAMEFEEL_*/HANDOFF-*.
- 이번 세션: TC 진행 안 함(사용자 결정). 1~4(10-31 기능 실확인·Restore 버그·GAPS_1031 기획 공백·오래된 할 일) Codex 분업 → 게임성 부족점 대량 발굴·구현. 기획 결정은 레퍼런스 게임 기반 추천안 자동 선택(DECISIONS_1101_P1/P2/P3).
  - Codex P1(경제·연구): 성휘240·티타늄320·원유/가스600 새 게임 보장(StellarSupply), 일반 부품 제련로 직접 생산(12번), 3·5시대 연구 재배치, 표본 연구 정지/ETA·생산 UI 등 게임성 23건. 다이아8 연결 확인(누락 없음), 건물 모델 매핑 정적 이상 없음(초가집 외형 지적은 미재현).
  - Codex P2(AI·저장): Missing building:0 원인(시작 시설 삭제 예약 프레임 저장) 수정, AIIndustry(AI 소굴 역할별 시설·교체 장비·재건), 보스 미완성품 후임 인계, 구저장 병력 장비 소급(MonsterDto military*), 장갑 0~5, 게임성 17건.
  - Codex P3: 화면 영어 문자열 전수 한국어화(73파일), 텍스트·안내 게임성.
  - Claude(Unity 캡처 기반): 월드맵 지구 축소 버그(회전 AABB→메시 반지름)·거점 한국어 이름(UI/SiteNames.cs), 가장자리 스크롤 창밖/비포커스 차단(시작 카메라 엉뚱한 곳), 새 게임 카메라 불시착지·줌13, 자원 라벨 상태/커서/Alt만, 선택 패널 욕구 줄, 고지대 테두리(메시 uv1+TerrainBlend)·봉우리 밝기, 시작 안내 체크리스트(UI/StartGuide.cs), 인재 상세 초상화, 메뉴 열림 시 알림 숨김, 씬 로드 시 알림 비움, 새 게임 지역 버튼 4열. 저장 버그: JsonUtility null 장비 부활 → MonsterDto.militaryEquipped + Preflight 정규화.
- 검증: Unity P1 21584·P2 181·P3 285·M1 103·M2 298·N2 77·L1 76·L2 97·Claude 28·Residue 11·Military 9·P1장면·P2장면 23 통과, 캡처 확인(Temp/shot-1101). 첫 일괄 실행은 시스템 메모리 부족으로 중단돼 단건 재실행.
- 미해결/남은 일: 실전투·실파괴·장기 밸런스, 초가집 모델 외형/뒤집힘(미재현), 게임성 하 항목 약 20건(GAMEFEEL_1101_P1/P2/P3, claude 남은 관찰: 장수 라벨 겹침·잔해 모델·온대 봉우리 눈).
- 환경 메모: offc.sh 오프라인 컴파일(scratchpad), Bash python 없음→node, Codex worktree 병합은 줄바꿈 정규화(tr -d '\r') 후 git merge-file. Codex는 ant-wt/patches 쓰기 거부 → worktree 내부 패치.
- SAVE: 7113d6c develop push. Notion 일지 https://app.notion.com/p/40bc4a0ecd31813c80e6f87500308849(캡처: 시작 안내·월드맵·인재 상세). 기획 반영(추가): 자원·과학·전투·UI. Trouble Shooting: 이번 세션 사용자 버그 보고 없음.
- 보존: prefab/.prefab.meta 커밋 금지. 커밋 제외: 재질/URP/씬/ProjectSettings/graphify/art. 이전 log 원문 work/save-20261101/log-before-save.md.
