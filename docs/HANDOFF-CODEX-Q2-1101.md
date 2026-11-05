# Q2 인계 — 2026-11-01

## 판정

- 기준: `codex/q2-1101`, `b45d257`, 작업 사본 `E:/Git/ant-wt/Q2`. 원본은 읽기 전용 참조만 사용. Unity 실행/조작·캡처·커밋·SAVE 일지 작성 없음.
- [게임성 표](GAMEFEEL_1101_Q2.md): 1단계30 + 2단계30 = **60건**. 상·중32건 코드 구현, 하1건 상태 정리, 하27건 후속. 각 항목의 근거·대응·미구현 이유는 표에 기록했다.
- [추천 선택·잠정값](DECISIONS_1101_Q2.md): 돌봄 관계+2, 친구40/라이벌-40 기록, 병상 우선순위, 가용 자동 집도의, 전원 행동 불능 회복 유예, 기존 목표 패널 재사용. 모두 추천안 자동 선택(사용자 미확인).
- 미지정 자연물 자동 채집·제조법 선택·자동 수술/전쟁/출정/발사 없음. 시민·세금·직접 전투 수치는 유지. prefab 생성/변경 없음.

## 항목별 수정 파일

| 항목 | 파일(Assets/_Project/Scripts/ 아래) | 연결된 검사·판정 |
| --- | --- | --- |
| 처치/급식 상태 재검사·포로 작업 무효화·구조 접근/경로·자동 집도의·수술 잔여 기여·부모 부위 선행 | Units/CommanderHealthCare.cs | RunQ2Scene(무효 처치·금지 변경·부모 부위), 실제 구조/수술은 Play 확인 필요 |
| 돌봄 공동 경험·현재 담당·가용 구조 침상·휴식 경쟁 우선순위 | 신규 Units/CommanderCareFeedback.cs | RunQ2Scene 담당 표시, 병상·돌봄 관계 실제 완료 확인 잔여 |
| 행동 불능의 사교 제외·친구/라이벌 문턱 기록 | Units/CommanderPersonal.cs | 컴파일 완료. 관계+2·문턱 기록 실제 완료 경로 Play 잔여 |
| 현재 조건의 상처/면역 회복 예상 | 신규 Units/MedicalOutlook.cs | Main 공식/영구 손상/면역 경계. 원래 HealthLines API는 유지하고 건강 창에만 상세 예상 추가 |
| 작업표 다음 성장·활용 감소 안내 | 신규 Units/CommanderDevelopment.cs, UI/GameMenuDuty.cs | Main의 다음 경험치·감소/하한/만렙 검사 |
| 의료 상태·담당·수술 진행/결과 실시간 표시 | UI/GameMenuHealth.cs | Main·RunQ2Scene + 실제 창 레이아웃 확인 잔여 |
| 첫 계절 이후 연구/승급/생활/회복/원정/탈출 안내 | 신규 UI/CampaignNextSteps.cs, UI/BetaProgress.cs | Main 시대별 안내·RunQ2Scene 실제 상태. firstHints 설정 사용, 메뉴 열림 때 숨김 |
| 전원 행동 불능의 새 환경 위기·야행 포식자 보류 | World/ColonyEvents.cs, World/EventRules.cs | Main 인원0~12·행동 불능 수 경계. 기존 적·질병·기후·침공을 지우거나 멈추지 않음 |
| 재현/문서 | AgentScripts/Review1101Q2Checks.cs·Review1101Q2Compile.ps1, README.md, IMPLEMENTATION_BOARD.md, Q2 결정·게임성·이 문서 | 오프라인 결과 아래 |

신규 런타임 소스4개에 .cs.meta를 포함했다. UI/GameMenuRoster·DetailTabs·GameNotifications·WorldMapPanel·WorldMapMarker·MoodWatch*·GameMenuController·장수 이름표·FacilityVisual*·Map/*Terrain*·CommanderEquipment는 수정하지 않았다.

## 검증

- 제공 `offc.sh` 첫 실행은 기존 외부 `SpinFree` 타입 누락으로 실패했다. 같은 기준선의 P1 검증 방식 재사용: 원본 Bee rsp가 참조하지만 worktree에 없는 타사 소스43개를 **읽기 전용으로 로컬 rsp에만 추가**, 원본 파일은 수정하지 않고 전체 게임 재컴파일.
- `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/Review1101Q2Compile.ps1 -OfflineChecks`로 재현. 게임 오류 0, 검사 오류 0. Q2 **135검사**, 기존 M1 **103**, L1 **76**, K1 **87**, N1 **25742** 통과. 원시 출력 `Temp/Q2-validation.txt`.
- `Review1101Q2Checks.Main()`은 Unity 엔진 없이 가능한 모델·문구 검사를 실제 게임 DLL로 실행했다. 의료 작업/경로/창 동작을 실행 통과로 세지 않았다.
- 최종 `graphify update .` 완료: **12527노드·29181간선·607커뮤니티**, AST만 갱신(LLM 비용0). 577개 이전 라벨에서 커뮤니티가 바뀌어231개 이름을 허브 기준으로 재명명했다는 경고가 있다. 문서 의미 그래프의 최신성까지 보장하지 않는다. 로그 `Temp/Q2-graphify-final.txt`.
- `git diff --check` 및 권장 패치의 현재 사본 대상 `git apply --reverse --check` 통과. 원본의 Claude 동시 변경에 대한 무충돌 적용을 뜻하지 않는다.
- `Review1101Q2Checks.RunQ2Scene()` 진입점을 준비했다. 낮의 실제 Play 장면에서 의료 작업 중이 아닌 활동 가능 장수2명이 필요하다. 테스트가 잠시 바꾼 개인 건강 참조·의료 내부 필드는 finally에서 복원한다. **아직 실행하지 않았다.**
- 수술 마지막 틱·침상 경로·실제 돌봄 완료 관계 보너스·회복 유예의 실제 이벤트 생성·왕복 저장은 수동/Unity 확인 잔여. 코드 컴파일만으로 통과라고 기록하지 않았다.

## Claude 화면·플레이 확인 요청

1. 새 게임 첫 체크리스트 후 첫 계절을 넘기고 다음 준비 패널이 이어지는지. firstHints 끔/메뉴 열림 때 숨는지. 1920×1080 및 1280×720에서 미니맵·상단 자원·월드 라벨과 겹치거나 긴 승급 재고가 잘리지 않는지. 캡처는 사용자 SAVE 때만.
2. 건강 창의 부위별 목록·회복 예상·의사 이름/작업량·수술 진행/교대/최종 결과. 부상이 많으면 기존 영역에서 긴 내용이 읽히는지 확인. 장수 상세는 Q1 소유라 직접 수정하지 않음.
3. 막힌 가까운 침상+열린 먼 침상에서 구조, 운반 중 길 차단·침상 제거, 의료 처치 중 환자 회복·정책 변경·약품 소진을 실행. 진료가 끝난 의사가 다른 업무로 돌아가는지.
4. 의료 능력 다른 유휴 의사2명+연구에 묶인 고능력 의사로 자동 집도의 확인. 수동 지정 의사 부재 이유·업무 복귀 후 재개, 마지막 프레임 교대의 평균 능력·재료 중복 차감 없음 확인.
5. 중상·병상 휴식5·소화9에서 불이 없을 때 입상, 실제 관리 화재와 연구9가 있을 때 기존 우선순위 비교. 작업 진행 중에는 기존 '끝낸 뒤 적용' 정책 확인.
6. 실제 처치/급식/구조 성공 시 관계+2, 자기 처치·취소는 지급0, 40/-40 문턱에만 방향별 기록. 의식 없는 상대와 말다툼/결투 시간이 늘지 않는지.
7. 본거지 전원 쓰러짐에서 환경 사건/첫날 밤 포식자만 새 생성 보류, 기존 적·질병·침공 유지, 한 명 회복 뒤 원래 사건 주기로 복귀. 단순 병상 배치로 유예가 켜지지 않는지.
8. 기존 저장24 로드 → 돌봄 관계/수술 진행 저장·로드. 저장 필드/DTO 변경은 없으며 SaveDtos.personalState, SaveSnapshot 개인 JSON, SavePreflight 개인 Validate를 그대로 사용한다.

## 남은 연결·권한 제한

- Q1 금지 파일에 필요한 후속만 인계: 인재 상세의 아동 역할 안내(Q2-24), 병력 회수 전 생활 역할 비교(Q2-53), 월드맵/명부의 귀환 회복 요약(Q2-59). 이번 패치가 해당 UI를 구현한 것으로 표시하지 말 것.
- Q2-17~30·48~60의 미구현 사유는 게임성 표에 명시. 시민 개체화·새 교대/멘토/수술 이력·의료 실물 물류는 새 상태와 저장 경로 설계가 필요하다. 원래 기획의 전체 구현 완료를 주장하지 않는다.
- Notion은 자동 선택 잠정안을 확정 규칙으로 교체하지 않았다. Claude가 관련 생활/유닛/기술 페이지·참조 일지와 대조해 반영할 것. 하위 페이지 보유 루트에 replace_content+allow_deleting_content 사용 금지.
- 요청 `git add -N .`는 원본 `.git/worktrees/Q2/index.lock` 쓰기 거부, 지정 `E:/Git/ant-wt/patches/Q2-1101.patch`도 쓰기 거부. 원본 인덱스·객체 변경 없이 Q2/Temp의 임시 인덱스·객체 저장소로 패치 생성한다.
- 대체 전체 패치: **`E:/Git/ant-wt/Q2/Temp/Q2-1101.patch`**. 권장 게임·검사·문서 패치: **`E:/Git/ant-wt/Q2/Temp/Q2-1101-code.patch`**(graphify 생성물만 제외). 공유 patches 폴더 복사·원본 적용은 Claude가 수행할 것.
- 카카오톡 완료 도구가 현재 제공되지 않아 완료 알림을 발송할 수 없다.
