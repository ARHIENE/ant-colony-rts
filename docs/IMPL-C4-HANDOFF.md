# IMPL-C4 인계 — 2026-10-24

## 상태
- 소유 범위 연결 및 검사 작성. **컴파일·Unity·검사 실행·화면·실제 파일 save/load 미검증.** 실행 결과로 해석하지 말 것.
- 격리 worktree C4에서만 작업. 원본 프로젝트 접근, Unity/MCP/Notion, prefab, commit/stash/push 없음.

## G별 결과
| G | 상태 | 내용 |
|---|---|---|
| G20/G16 | 소유 범위 연결 | AirshipYard.TryBoard(int groupId)는 Boarding·8m·도착·좌석 확인 후 TakeArrivedCivilians 반환값만 승객으로 추가. 시민만 탑승한 발사도 허용. 하선·파괴 시 시민 위치/연령 그룹 반환, 발사 뒤 정리에서는 반환하지 않음. |
| G43 | 소유 범위 연결 | 일반 시민 승객은 이미 풀에서 빠졌으므로 잔류 인구에서 다시 빼지 않음. 동반 병력만 전역 인구에서 차감. 장수 ID 중복 시 병력도 중복 합산 방지. |
| G45 | 연결·실행 미검증 | ColonyPopulation.Start/LateUpdate 및 ActivationPreview 직전 Observe. 전 지역 창고 실물, 바닥, 편입 재고, 운반 중 물자, 수송 화물, 장비/개인 식량, 건물/건설 전달량/제작 중 재료/급수 버퍼/로켓 부품·연료 포함. 예약은 제외. 전역 풀에 이미 포함된 수송·주둔 병력을 재합산하지 않음. |
| G52 | 잠정 규격으로 UI 연결 | 연료 규격은 결정문에 없음. **잠정: 정제유(Petroleum) 하루 1**, FuelResource/FuelUnitsPerDay 상수로 정의. 15일 전량 소비 기존 API 재사용. B9 검사용 Special/1은 채택하지 않음. |
| G68 | 가동 직전 연결 | RocketSection→RocketActivation→FinalBattleReserveWarning→TryActivate. 실제 15일 연료 ReserveRequirement 전달. 이미 적재한 연료를 비축 보유량에 포함. 현재 방어 코드에는 별도 소모 탄약이 없으므로 별도 탄약 요구량 0. |
| G14 | 소유 범위 연결 | 바이옴별 기초 세금 목록에 공통 Wood/Stone 추가·중복 제거. 기존 세액 총량과 우선 재료 비율 유지(총 세금 추가 생성 없음). |
| G38/G04 | 수정 | 노년 추가 동원 가용량/실제 차출을 Max(0,old-agingDue)로 통일. Restore의 ages null 또는 Length==0 이관 조건 유지. |
| G17 | 검사 갱신 | 옛 행정 지수감소 가정 Spec1008FIChecks를 하루 필요량 선형 소모 검증으로 교체. |

## 수정 파일
`Assets/_Project/Scripts/` 기준:
- Buildings/AirshipYard.cs, AirshipYard.Rocket.cs, 신규 AirshipYard.Civilians.cs
- Core/CampaignOutcome.cs, RocketEconomy.cs, FinalBattleEconomy.cs
- Core/ColonyPopulation.cs, ColonyPopulation.Military.cs, ColonyPopulation.Evacuation.cs, 신규 ColonyPopulation.Rocket.cs
- UI/GameMenuCampaign.cs, GameMenuRocket.cs
- AgentScripts/ImplC4Checks.cs 신규, AgentScripts/Spec1008FIChecks.cs 기존 행정 검사 1곳(사용자 예외 허용)
- docs/IMPLEMENTATION_BOARD.md, 이 인계 문서. graphify 생성물은 게임 변경 전달에서 제외.

## 저장
- 버전 v19 유지. AirshipYard.State에 civilianPassengers/civilianYoung/civilianOld 추가. 기존 SaveBuildings.airship → CaptureState/RestoreState JSON 경로 재사용. 구저장 기본값0.
- Validate에서 시민 수·연령 부분합·단계·좌석 검사. RestoreState에서 장수 병력까지 더한 좌석을 재검사. 복원 때 이전 시민 승객을 다시 시민 풀에 지급하지 않음.
- 승객의 연령 그룹(어린/성인/노년)은 보존하지만 개별 나이는 기존 시민 집계 방식으로 재배분. 장기간 탑승 중 노화는 미구현.

## 연결 요청
1. **Save/SavePreflight.cs::ForFile의 rocket occupied seats 검사(기존 149행 인근)**: `b.airship.passengers.Sum(id => 1L + f.commanders[id].troopCount) + b.airship.civilianPassengers <= ...`로 시민 좌석도 포함할 것. 소유 밖 Save는 수정하지 않음. 현재 AirshipYard.RestoreState에서 차단하므로 초과 저장을 조기에 검증하는 연결이 필요.
2. **전투/최종 공세 담당**: Core/RocketCampaign.AssaultTick의 실제 공세 생성기/강도 미리보기 연결은 B9 요청 그대로 남음. 빈 이벤트 구독으로 가동 제한을 우회하지 말 것. 실제 구독이 없으면 UI 가동 버튼은 계속 비활성이다.
3. **UI/GameMenuCivicServices::CivicServices 및 목적지 입력 담당**: 대피 경로 목적지 선택→BeginCivilianEvacuation 연결은 B8 요청 그대로 남음. C4는 로켓 근처에 이미 도착한 그룹 탑승 버튼까지 연결. 로켓으로 순간이동/도착 전 반출 없음.
4. **기획/연료 담당**: 잠정 정제유1/일과 G45 평가 단위를 확정할 것. 재산은 새 가중치를 발명하지 않고 기존 외교의 일반 자원1/특수10·장비 품질가격, 건물은 현재 건설비를 재사용. 결정문에 별도 재산 가격표가 없으므로 확정 가격표로 오인하지 말 것.
5. **엔딩/명부 담당**: 과거 명부에서 제거한 이탈자 ID 저장은 B9 요청 그대로 남음. 편입 지역 별도 시민 인구 필드는 현재 AnnexedSettlement에 없음. 향후 지역 시민 풀 도입 시 Total/CaptureOwned/CampaignOutcome 집계에 연결 필요(현재 주둔 명부는 전역 Assigned 참조이므로 더하지 않음).
6. **Unity 통합 담당**: 신규 .cs.meta 생성, 컴파일 및 아래 검사/기존 B8·B9·SaveRoundtrip 회귀 실행. SAVE 요청 전 캡처/일지 작성 금지 유지.

## 검사 목록
- ImplC4Checks.Main(): 임시 저장 루트/자동저장 끄기·복구, 1명 불시착 새 게임 시작 후 Run() 호출.
- ImplC4Checks.Run(): 정적 진입점·문자열 반환, 검사 전용 1~2명 Play 세션용. 기존 비축더미를 찾지 않고 로켓/바닥 물자/자연 자원/수송 객체 직접 생성.
- 노년 정산 대기 중복 동원 차단·실제 추가 차출, 행정 반일 선형 소모, 목재/돌 실제 납세.
- 바닥 실물/자연 매장량 구분, 목적지 예약 중복 제외, 수송 화물, 전역 Assigned/Reserved 인구, 관측 공급·시간가중 평균.
- 연료 전량1회 결제/중복 차단, 실제 비축 요구량, 15일 준비.
- 도착 전/멀리 도착 탑승 차단, 좌석 제한·중복 반출 방지, 시민 JSON/복원 중복 지급 방지, 엔딩 잔류 합계.
- 하선·반복 하선·파괴 반환, 시민만 발사·엔딩 기록·발사 후 하선 무지급.
- **작성만 했으며 실행하지 않음.** 컴파일 성공/PASS 수/화면 검증/실제 저장 왕복을 보고하지 않는다.

## 남은 위험
- 전체 재산 순회가 매 프레임 수행됨. 정확한 집계 연결을 우선한 구현이며 대규모 월드 성능 미측정. 큰 월드에서 비용이 드러나면 소유권 변화 이벤트/더티 캐시로 전환할 것.
- 프레임 내 여러 변화는 최종 실물 상태로 같은 시각 관측을 덮어쓴다. Unity 실행 순서·로드 후 첫 프레임의 전체 시스템 통합 검증 필요.
- 하선/파괴 시민은 기존 장수 Unload 정책과 같이 생환한다. 파괴 시 시민 사망 규칙은 결정 원본에 없어 추가하지 않음.
- 소유 밖 저장 선검사·공세·대피 목적지 입력이 남아 전체 최종전 플레이 완료 판정은 아님.
