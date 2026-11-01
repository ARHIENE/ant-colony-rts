# S3 인계 — 장수 서사 드러내기

- 목적: RimWorld의 머리 위 생각·사회 기록·동료 애도, Dwarf Fortress의 인물 연대기를 기존 관계·기분·CampaignHistory에 연결했다. 숫자·자원·명령 자동화 시스템은 추가하지 않았다.
- 플레이어 변화: 친구가 된 순간 상대 이름을 말하고, 다툼·중상·죽음을 겪으면 반응한다. 장수 초상→기존 상세의 `이야기`에서 최근 주요 사건5개를 읽는다. 기존 개요·기분·관계·장비·건강 탭 유지.
- 수치·판단: [DECISIONS_1101_S3.md](DECISIONS_1101_S3.md). 전부 사용자 미확인 추천안. 말풍선2.8초·최대3개·일반 발화10초 간격. 위기 우선. 중상 반응은 기존 쓰러짐 수치를 재사용하고 배우자-15를 연결. 사망 기분 기존 규칙 유지.

## 수정 파일

- 신규 `UI/SpeechBubbles.cs`: 고정3슬롯·발화 억제/우선순위·ReserveWorldLabel 예약·기분 원인 표시.
- `UI/MoodWatch.cs`: 말풍선 컴포넌트 연결·위험 발화, 스캔의 LINQ 임시 배열 제거.
- `UI/DetailTabs.cs`: 기존 창 이야기 탭·0.5초 내용 갱신, 사망 원인 ID를 이름으로 표시.
- 신규 `Units/CommanderPersonalStory.cs`: 사건 기록·중상 에피소드1회 반응·발화 간격(세션 상태만).
- `Units/CommanderPersonal.cs`: 중상 관찰 호출·복원 기준값·기존 쓰러짐 기분 루프를 공통 반응 경로로 통합·말다툼 훅.
- `Units/CommanderPersonalState.cs`: 신규/변경 AddMood 알림 이벤트(직렬화 필드 추가 없음).
- `Units/CommanderSocial.cs`: 기존 사망 중복 방지 통과 후 인물 이야기, 비사망 쓰러짐 공통 중상 연결.
- `Units/CommanderCareFeedback.cs`: 친구/라이벌 문턱 통과 시 방향성 이야기.
- `Core/CampaignHistory.cs`·신규 `Core/CampaignHistory.Stories.cs`: 기존 Entry에 개인ID별 이야기, 최근5 보존 압축, 구기록 보조 표시.
- `AgentScripts/Review1101S3Checks.cs`, 이 인계·결정 문서, IMPLEMENTATION_BOARD S3 소유 표시.

## 검증·제한

- Unity 실행·캡처·커밋·SAVE·원본 수정 없음. 금지 파일·prefab 변경 없음.
- offc.sh 단독 최초 실행: worktree에 없는 외부 `SpinFree` 때문에 실패. 기존 Q2 검증 방식처럼 로컬 Temp 응답 파일에 원본 외부 소스43개를 읽기 전용 참조한 전체 게임 컴파일 및 검사 스크립트 컴파일은 오류0. 기존 경고 유지.
- 실제 .NET 오프라인 `Review1101S3Checks.Main()` 17검사 통과: 빈 구저장·구기록·동명이인·개명·최신5 역순·압축 보존/집계/재압축·관계 문턱·배우자·겁쟁이·표시 제한·기존 개요 보존.
- 저장 필드 추가 없음. 기존 SaveDtos.history→SaveSnapshot.Capture/Restore→SavePreflight CampaignHistory.Validate 경로 확인. Unity JsonUtility 실제 save/load 왕복은 미실행.
- 카카오톡 도구 검색 결과 사용 가능한 MemoChat 도구 없음. 완료 알림 전송 못함.

## Claude 확인 요청

1. 병합 후 Unity 컴파일·`Review1101S3Checks.Main()` 실행, 새 파일 meta 생성 확인.
2. 실제 낮 장면에서 우호40 직전 관계를 공동 작업/돌봄으로 넘기고, 상대 이름 발화·방향성 기록을 확인. 실제 말다툼도 양쪽 기록 확인.
3. 친구·일반·배우자 관계별 중상 및 사망: 기분 값·대상 이름·발화·이야기 확인. 같은 부상을 여러 tick 진행해도 기분/기록 중복 없이1회. 완치 후 재부상은 새 사건.
4. 사망 시 기존 복수/격노·애도 중복 방지 유지, ID 문자열이 기분 탭에 노출되지 않는지 확인.
5. 저장→로드 시 부상·기분이 다시 발생한 것으로 발화/기록되지 않고, 기존 이야기5개가 남는지 확인.
6. 1280×720·1920×1080, 밀집 장수·HUD·기존 이름표와 겹침, 긴 이름·5개 이야기 줄 넘침·탭7개 가독성 확인. 예약 공간 부족이면 말풍선 생략은 의도된 동작. 화면은 아직 검증 안 됨.

담당 밖 후속 목록: 첫 사냥 성공은 `Units/CommanderWork.cs`의 TickHunt 실제 처치 성공 지점에서 CampaignHistory.Story(개인ID, 이름, 첫 사냥 성공)를 호출하는 훅 필요. 사냥 시작/취소를 성공으로 오기록하지 않으려고 임의 추정하지 않았다. 추가 원문 보관 정책·긴 사용자 이름 표시는 실화면 피드백에 맞춰 조정.

병합 담당: README.md의 구현 현황·초상 상세 조작 설명에도 이야기 탭을 반영해야 한다(이번 S3 담당 파일 밖이라 미수정). Graphify AST 갱신 산출물은 코드 변경과 별도로 취급한다.
