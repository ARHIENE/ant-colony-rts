# 프로젝트 로그

## 현재 상태 — 2026-10-18 SAVE(Claude) — 개미→주민 전환 코드, 주민 키트, 가구 62종·동작 10개, 도감
- 프로젝트 E:\Git\ant / develop 개발, master 안정. 이전 기록은 changelog.md. 공동 작업 보드 `docs/IMPLEMENTATION_BOARD.md`.
- **캐릭터 방향:** 개미 대신 Codex가 만든 동물형 주민(`art/characters/residents-24` 외형 24종 + `parts-board` 표정·머리·흉터 부품)을 쓴다. 사람형 SD·토토 단독 시안은 미사용.
- **CHARACTER-CODE-01(Unity 코드, 사용자 승인 A~D):** 신체 부위 두 팔·두 다리(감지 삭제, 저장 v18 이관 `Save/BodyMigration.cs`), 장비·연구 이름, 시민·정착지 용어, 꿀단지 방 이름. 모델 교체(E)는 보류. **검사 미완:** 일괄 검사 2건 실패 — EraChecks 저장 버전 단언을 18로 고쳤으나 마지막 실행이 여전히 16 보고(재컴파일 안 된 것으로 추정, 미확인), 나머지 1건 기록 없음 → 다음 세션 재컴파일 후 재실행.
- **Blender `art/resident-kit/`(Unity 미연결, README 상세):** 시설·캐릭터 부품·공통 동작·도구·미리보기 파일 분리.
  - 공통 리그(뼈 12 + 소켓 4), 부품 94(Codex 표정 복구 포함), 수동 조합 4, 동작 44(기본 30 + Fly·Downed·Captive·CarryPerson + 가구용 10: PlayBoard·Bathe·Wrestle·Throw·PlayMusic·Exercise·Swing·RockCradle·Spin·WaterPlant).
  - **FURNITURE-97:** 건설 가능 97종 중 모델 없던 62종 신규(`scripts/extra_spec.py`·`models_extra.py`·`kit_facilities_extra.py`, 키트 단위 배율 1, 시설 동작 8종). 시설 총 141, `facility-points.json`·FBX 포함.
  - 미리보기 156장면·GIF 156. **백그라운드 Blender는 활성 장면만 애니메이션 평가** → 처음 GIF 94개가 정지 화면이었음. `render_all.py`를 장면별 `-S` 프로세스로 바꿔 전부 재렌더.
  - 검증 `validation.json` 오류 0, 관통 표시 3(화분 물뿌리개·냉장고 꺼내기·샤워). Codex 검수(RESIDENT-KIT-REVIEW): 표정 4종·사망 연결 복구, 카메라·물뿌리개 수정.
  - `art/critter-kit/`: 목장 4종 + 적 2종(말벌·밤딱정벌레) 모델·리그·동작·FBX 43.
- **Notion:** 도감에 새 가구 62종(분야별 새 하위 분류 9개, 신규 분야 휴게·장식·군사·마을)과 장비 분야(10종) 등록 완료. 기존 77개 모델의 개미 GIF → 주민 GIF 교체는 아직. 개발 일지: https://app.notion.com/p/3fdc4a0ecd3181afb689d6e19583d700
- **미커밋/주의:** `art/`는 계속 미추적(용량 큼, 커밋하지 않음). `AntColony.unity`·`Water.mat`·`TimeManager.asset` 변경은 출처 불명 → 커밋 제외. `Assets/Screenshots`·`Assets/reports`·`debug.log`·`design/hud/*.before-*` 임시 파일 제외.
- **다음:** 1) CHARACTER-CODE-01 재컴파일 후 EraChecks 등 재검사 2) 남은 회귀 13개(10-17 목록) 3) 기존 도감 77개 GIF 주민판 교체 4) resident-kit Unity 연결(임포트·Animator·소켓, 대규모 → 승인 후) 5) InfirmaryChecks 삭제는 사용자 `git rm` 필요, C 배관·J 수송 범위 확인 대기.
