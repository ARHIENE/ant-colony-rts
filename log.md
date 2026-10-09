# 프로젝트 로그

## 현재 상태 — 2026-10-09 SAVE(Claude) — 10-08 기획 F~I + 남은 일 구현
- 프로젝트: 개미 소굴 RTS, E:\Git\ant. Unity 6000.5.8f1 / URP 17.5.0 / Pipeline 0.7.0-exp.1. develop 개발, master 안정. 소스 Assets/_Project/Scripts, 검사 AgentScripts. 기준 기획: Notion '메인 게임 화면 구체화'(3e7c4a0ecd3181e38211f6be9c44637d) + 장수·유닛(3d8c4a0ecd318155a477fda677e21fd1)/자원 하위 문서. 수치 전부 잠정.
- **공동 작업:** `docs/IMPLEMENTATION_BOARD.md` SPEC-A~I(Claude). Codex 동시 작업 없음.
- 사용자 지시(10-09): F~I·남은 일 전부 구현, **기획상 미정인 부분은 건너뜀**(미정 수치는 기존 결정대로 잠정값).
- **F 정치·행정:** 기술 14종(정치 `CommanderActivity.Politics`), 작업표 14종(`CommanderJobs.Administration`=8192, All=16383, 작업표 '연구' 다음 칸). 마을 탭 '행정 책상'(`AdminDesk`, 재료 30, 이름·비용 잠정). 허용된 장수가 자율로 가서 정치 기술로 업무 → `ColonyPopulation.S.adminWork` 누적(시간 상수 600초로 서서히 감소). 성과 = 업무량 / (인구×6). 효과: 세금 징수 손실 15%→0, 민심 목표 +5, 이주 수요 +10, 재개발 불만·보상비 완화. 외교 정치력은 외교 기획 구현 대기라 미적용.
- **G 포텐:** `CommanderTalents.potential`(PA, 생성 시 max(80,CA+20)~200 균등·번식은 부모 평균±20, 잠정), CA = 14기술 합(소수 포함). `GainExperience` → `Train`: 실제 작업량을 `usage`(최근 활용, 시간 상수 600)에 기록, 주 80%/보조 20%(채집·건설·농사·낚시·근접→근력, 의료→연구, 요리·예술→제작, 운반(근력)→채집), 활용 비중 5% 미만 기술은 작업량×0.05를 나눠 감소(표본 300 이상), 성장 후 CA>PA면 이번 성장분 되돌림. 학습 배율은 성장 속도에만. 장수 관리 상세에 CA/PA·기술별 활용 비중 표시. 연구 주제별·제작 분류별 보조는 분류가 없어 미적용.
- **H 특성:** 최대 4개(`CommanderTraits.MaxTraits`). 빨강·실버·골드·다이아 등급 배정·시작 개수 확률·만석 처리는 미정이라 보류(시작 1~3개 기존 추첨 유지).
- **I 재개발:** 더 큰 주거를 기존 집 위에 일반 좌클릭 배치 → 재개발(Shift 연속 배치는 미지원). 건설비 60% + 보상비(실제 거주 수 × 식량·재료 0.5 × 보상 수준, 빈집 0, 행정 성과로 최대 20% 절감). 보상 수준 50~200%(인구 창 ±10%), 100% 미만이면 거주 수×0.2×부족분 민심 하락, 공사 120초 초과 시 지연 불만. 공사 중 기존 집 유지, 완공 순간 철거. 배치 줄에 견적·이탈 위험 표시. 취소 환급은 기존 건설 취소 자체에 환급 로직이 없어 미구현.
- **남은 일 처리:** 시민 생산력(주거 배율 초가 1.0/흙집 1.1/아파트 1.25 가중평균 × (1+완료 연구 1%·최대 30%) × (1+공공시설 2%·최대 20%))을 세금에 적용. 상단 HUD 인구 칸에 '납세 N초' 상시 표시(칸 폭 86→130). 장수 미지정 배치(건설 화면 장수 목록 마지막 줄 '장수 미지정', 선택 없이도 배치 시작 가능). PlayableLoop 문구 검사 한국어로 수정 + 목표 문구 '대기 일반개미 8마리'·'식량' 잔재 정리.
- **저장 v15:** 기술 13→14 확장, 작업표 행정 켬, adminWork·redevelopCompensation 추가·검증. 씬·프리팹 직렬화 기술표(13칸)는 `Generate`에서 배열 재생성(적 장수 생성 예외 수정).
- **검사(Play 새 세션·순차, 전부 통과):** Spec1008FI 29(신규), PlayableLoop 46, Workforce 74, Phase4 44, SaveRoundtrip 57, WeaponTalent 93, Stage2 185, Stage1 130, HudV4 313, AcidTower 26, Commander 33, AutonomousDuty 55, Meal 347, ResourceRule 31, DutyUI 63, Era 19. **Corpse 77행 실패 2회**: Alt 키 입력 주입이 반영되지 않음(alt=False) — 입력 시스템 쪽으로 보이며 이번 변경과 무관 추정, 원인 미확인(10-08에는 86 통과). 기존 검사 기대값 갱신(14종·16383·80/20·세금 공식×생산력×징수율·HasObstruction 인자·한국어 목표 문구).
- 남은 일: 특성 등급(빨강~다이아) 배정·획득 조건, 연구 주제/제작 분류별 보조 능력, 외교 정치력, 건설 취소 환급, 재개발 Shift 배치·교체 관계 세부, 행정 시설 정식 이름·수치 확정. VERIFY-01, NEXT-01~06(대부분 기획 미정)은 이전 그대로. WorldMap·FurnitureBatch2·Stage3/4·Corpse 외 미실행 검사는 다음에.
- 참고 버그 후보(미수정): `WorkerAnt.CommandStop`이 건설 중 현장을 `Cancel`(파괴)함 — 자율 시공 장수가 다른 일로 CommandStop되면 예정지가 사라질 수 있음. 확인 필요.
- 보존(미커밋): Water.mat(_SrcBlend 5→1, 출처 불명), TimeManager.asset, graphify-out, design/·design_skill/, package.json·playwright·tests/·reports/·research_notes/(출처 불명), BiomeEvent/MultiStopRoute/SaveMapCapture/FurnitureShot 초안, Assets/Screenshots. .prefab 커밋 금지.
- 도구: Unity는 공식 MCP(`mcp__unity-editor-mcp__*`). 검사: eval로 SessionState `AntColony.CheckFile` 설정 → Play 재시작 → editor_focus → eval_file `AgentScripts/RunChecks.cs` → `AntColony.CheckResult` 폴링(eval 안에서 Sleep 금지 — 메인 스레드 멈춤). python 사용 불가 → perl.
