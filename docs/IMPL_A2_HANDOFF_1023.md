# IMPL-A2-1023 실행·저장 연결 인계

2026-10-23 / Codex. 실행·화면 검증 전이며 완료 판정용 기록이 아니다. 커밋·SAVE·캡처 없음.

## Claude가 먼저 연결할 부분

1. `SaveSystem.NewGame(options)`에서 Busy 검사 다음에 `CrashLandingStart.Prepare(options);` 호출. 이 파일은 쓰기가 거부되어 Codex가 수정하지 못했다. UI 시작·재시작은 `CrashLandingStart.Begin`으로 이미 연결했지만, 직접 NewGame 호출도 동일한 G03 검증을 거쳐야 한다. `Prepare`는 1~3명·후보 유효성·필수 작업 7종을 검증하고 누락 시 예외를 낸다. 저장 불러오기에는 호출하지 않는다.
2. `CommanderAnt` 전투 코드에서 `WeaponKind.ShortBow`를 원거리 역할로 매핑하고 전용 UnitData 공격 사거리 12m·간격 2초를 적용한다. `WoodenSword`는 근접. 병력 0 장수 공격은 Claude G01과 함께 연결한다. 생성·표시·제작에는 두 enum이 이미 들어 있다.
3. `SaveDtos.BuildingDto`에 `public Buildings.WallFire.State fire;`를 추가하고 `SaveBuildings.Capture`에서 `d.fire = b.GetComponent<WallFire>()?.CaptureState();`, `Restore`에서 fire != null이면 컴포넌트를 추가/재사용하여 `RestoreState(d.fire)` 호출. DTO 쓰기가 거부되어 여기서 멈췄다. `State`는 remaining/tick/spreadIn/extinguishWork/originalColor를 보존한다. 소화 지시의 ServiceTarget·ServiceJob도 저장해야 한다(기존 일반 서비스 작업도 저장하지 않는 상태). 불러오기 순서는 건물·불 복원 후 장수 소화 지시 재연결.
4. `SaveValidator` v15→16 코드의 `priorities 14 → CommanderWorkState.JobCount`를 `14 → 15`로 바꾸고 마지막에 구 15칸을 `MigrateLegacyJobs()`로 16칸 이관한다. 파일 쓰기 거부. priorities 필드가 없던 더 오래된 저장은 버전 기반으로 Firefighting bit와 9를 명시 설정해야 한다. **신규 저장에서 사용자가 소화를 0으로 껐다면 다시 켜지 말 것.** 새 16칸은 `MigrateLegacyJobs`가 건드리지 않는다. 기술 15→16은 `CommanderTalents.Validate()`가 기존 값을 보존해 늘린다.
5. 시작 ageMonths는 선택한 18~30세 × 12, lifespanMonths는 임시 80×12다. Claude의 G37/달력 나이 이관과 맞출 것. 초기 관계는 빈 목록이다.
6. 개인 상태의 `toolTier`, `appearanceSeed`, `ordinaryClothes`, `carriedRations`, `groundCraft`, Joy 신규 필드는 기존 CapturePersonalState JSON 전체 직렬화를 사용한다. 별도 DTO 복사 금지. 구 저장의 null 소지 식량은 첫 Tick에서 빈 목록으로 보정한다. 사망·이탈 소지품 처리에 개인 보존식을 포함할지 Claude 생활 작업과 함께 확인한다.

## 구현된 진입점

- 새 게임: GameMenuFront.NewGameScreen의 TitleBackdrop 호출 유지 → GameMenuCrashStart. 후보 1~3명, 슬롯 전체 무제한 재추첨, G03 차단·부족 항목, G24 기술·열정·작업 불가·기후 적합·잠재력 미평가.
- 개인 물자: 인재·편성 → 소지품. 배고프면 보존식을 먼저 꺼내 기존 식사 타이머로 먹음. 1끼 내려놓기(원래 신선도 유지). 기본 14끼, 대식·소식은 14회분의 식량 단위로 지급. 소지 무게는 운반 잔여 한도에 반영. 보존 24일(3600초).
- 첫 지도: 시민0·수용0·기본 창고 식량/재료0, 기존 시작 시설 제거, 맨손 운반 돌20, 불시착 잔해1. 시작 생존자·잔해·지표 자원만 남긴다.
- 잔해 잠정 수치: 건설 철거 20작업초, 목재8+돌6+일반 부품1, 인원 무관 동일. `ResourceType.Component`는 일반 부품이며 고장 부품·변환 레시피는 없다. 파괴로 인한 보너스는 없고 철거 완료 때만 나온다.
- 기초 목공대 Wood10, 정착지 연구대 Wood20+Stone10. 기존 ScienceLab 종류·저장 키 재사용, 무연구·무전력·1인. 시민60/T2 조건 제거.
- 생산/장비/목공대 → 맨바닥 제작. 돌 도구 Wood2+Stone3, 단궁 Wood15+Fiber5. 원재료는 착수 시 1회 소비, 진행은 개인 상태에 저장, 완성품은 도구 개인 적용/단궁 장비 보관함. 제작 시간10은 잠정.
- 작업표 소화9/16칸·의료 명칭, 본거지 아군 건물을 관리 영역으로 사용. 물·도구 없이 작은 불 5초 진압. 소집 중에는 우클릭 명시 소화만. 산불도 즉시 피해 대신 실제 불 생성.
- 모닥불 최소1/정원6, 도박판 최소2/정원4(파트너 대기 최대15초), 무시설 산책/하늘 보기 회복50%. 옛 오락 시설 저장 종류는 유지.

## 검증 순서

1. 위 연결 후 공식 CLI 재컴파일 오류0 확인.
2. `AgentScripts/ImplA2Checks.cs`의 Main 실행. 단일 파일 안에 시작/소지품/UI·제작/연구·소화/오락 검사를 모두 포함했다. 필요하면 StartAndSupplies 및 내부 검사 Main을 나눠 실행(여러 맵 생성으로 Pipeline 30초 제한 가능).
3. 새 게임 1명/3명 → 개인식량 섭취·내려놓기 → 돌20 운반·맨바닥 돌 도구 → 목공대/연구대 → 첫 연구·단궁 실제 전투 → 잔해 철거의 비용/실물 확인.
4. 불타는 중·소화 2.5초·맨바닥 제작 중·산책/도박 대기 중 각각 저장/불러오기. 화재·소화 지시는 연결 전까지 통과로 기록하지 않는다. 새 게임 기본 상태·식량·잔해 저장 왕복 검사는 스크립트에 포함.
5. 새 게임·작업표16칸·맨바닥 제작·개인 소지품 화면 1920×1080/1280×720 실제 넘침·겹침·대비 확인. 이번에는 SAVE 요청이 아니므로 캡처하지 않는다. 화면 캡처는 사용자 SAVE 때만 진행.

기존 12장수/40시민·15작업칸을 가정한 과거 검사는 새 시작 사양과 충돌하므로 그 전제를 이관해야 한다.
