# 시설 외형·첫 방어 검수 (2026-11-12)

## 반영한 동작

- FacilityVisual: 새 Campfire/Carpentry/Kitchen/ResearchDesk/SpikeTrap/Stockpile/Workshop 모델을 실제 시설에 연결했다. 기본 흙 재료에서 적용하고, 개축 재료는 기존 분리 재질 모델을 유지한다. 모델만 바꾸며 건물 면적·충돌·비용·해금 조건은 바꾸지 않는다. 용광로는 다른 담당의 임포트 후 연결 범위다.
- CommanderDuty: 정수 단위 창고에 넣을 수 없는 1 미만 바닥 화물을 자동 운반 작업으로 계속 재선정하지 않는다. 잔량은 삭제하지 않는다. 수동 채집과 주변 더미 합짐은 유지한다. 실제 실패 상태에서 목재 0.2487418/흙 0.6060927/식량 0.7079277을 확인했다.
- CommanderAnt/SoldierAnt/CommanderDuty: 직접 공격 시 기존 작업·자가 치료를 해제하고 잠든 장수를 깨운다. 습격 중 활성 공격/후퇴 지시를 자동 식사·운반·자가 치료가 취소하지 않는다. 쓰러짐/피해·건강 갱신은 유지하며 전투 명령 종료 후 생활 처리로 돌아간다.
- SoldierAnt: 파괴된 적이 IDamageable 인터페이스에 남아 CommanderFormation에서 Position을 조회하던 MissingReference를 방지한다. 이 예외는 장수 Update 전체를 막아 첫 처치 뒤 공격이 멈추는 원인이었다.
- CommandCard: 습격 중 비소집 장수와 다중 선택에도 실제 공격 이동 버튼을 제공한다. 기존 A+지점 클릭을 재사용한다.
- Precipitation/WeatherSystem: 화면을 덮던 긴 불투명 빗줄기를 짧은 입자로 바꾸고 비/폭풍 밀도·불투명도를 낮췄다. 날씨 이동/작업/기분 효과는 유지한다.
- GatherDesignation/ResourceNodeStatus/BetaProgress: 전투 중 배경 작업 문구를 줄이고 방어 지휘 안내를 표시한다. 지정 도구, 커서 근처, Alt 조회는 필요한 정보를 계속 제공한다.

## 실제 Play 진행

공식 Unity CLI로 실제 Play의 이동·건설·채집·공격 명령을 실행했다. 준비 관찰은 5배속, 전투는 1배속, 복구는 3배속이다. 아래 시간은 **게임 시간**이다. 저장 슬롯을 쓰지 않았고 매 실행 finally에서 설정/저장 root/검사용 해상도를 복원했다. 자원 지급, 즉시 건설, 적 강제 처치는 사용하지 않았다.

| 실행 | 준비/전투 결과 |
|---|---|
| baseline | 창고98초, 밭228초, 산성탑 완성. 자연 첫 평화900초 후 매초 직접 표적 지시로28초 방어 성공,60초 복구 관찰. 긴 강수 입자가 화면을 덮는 결함 발견. |
| after | UI/강수 수정 뒤 공격 이동을 각1회만 지시. 바닥 잔량 반복 운반으로 연구대/산성탑이900초까지 미완성.69초 전투 후 패배. 잔량 운반·자가 치료 취소·파괴된 적 참조 조사 계기가 됨. |
| final | 잔량/명령 수정 후 창고72초, 밭216초, 산성탑375초. 이때 실제 HUD WaveAction을 눌러 예정 시각보다 **525초(8분45초) 일찍** 습격 시작. 공격 이동 각1회와 체력30 미만 후퇴로60초 방어 성공, 이후60초 복구 관찰. 부상3명/파손0곳. 이 실행 마지막 콘솔에서 파괴된 적 참조 예외가 발견되어 추가 수정했다. |
| 최종 전투 회귀 | 파괴된 표적 회귀검사와 별도 새 게임의 첫 웨이브. 준비는 생략하고 기존 StartNow를 눌렀다. 자원/시설/체력 지급 없이 공격 이동 각1회로 습격 시작54초에15마리 전멸.45초 시점 남은 적6, 장수 체력70/84/97. 민간 장수 버튼을 실제 Invoke하여 공격 모드 진입도 확인. **콘솔 오류0, 컴파일 오류0, 경고6.** |

같은 seed261106/Small Forest/3명 시작이지만 실시간 프레임/날씨/작업 분배는 완전 결정적이지 않다. 건설 시간 차이를 정밀한 성능 향상률로 해석하지 않는다. 525초는 실제 앞당긴 시간이며 첫 평화 기본900초를 줄인 새 규칙이 아니다. 앞당기기는 기존 선택 기능이다. 이번 실질적인 대기 감소 수정은 반복 잔량 운반을 없앤 것이다.

전투 명령 유지, 연속 처치, 부상자 후퇴와 산성탑 마무리는 확인했다. 자동 플레이 결과만으로 재미나 출시 품질 전체를 보증하지 않는다. 1명 시작·다른 지형·2웨이브 이후 밸런스는 이번 검증 범위가 아니다.

## 시설·회귀검사

`Facility1112Review.Run`: 완성 시설을 별도 시각 검사용으로 배치했다(자연 진행 속도 증거가 아님).7종 모두 실제 모델 연결, 기존 footprint 안의 크기, 바닥 위치, 자식 Collider 제거, 흙→목재→흙 외형 전환 통과. 실제 캡처에서 목재·돌·작업대 계열의 일관된 형태를 확인했다. 폭풍 입자420개가 실제 방출되는 화면에서 건물 식별을 확인했다.

`HaulFraction1112Checks.Run`:0.25 잔량 자동 운반 거절,10개 운반 허용,0.25 수동 채집 허용 및 수량 보존 통과.

`CombatOrder1112Checks.Run`: 파괴된 WildMonster 참조를 진형 갱신에 전달한 뒤 예외 없음, 민간 공격 이동 버튼 동작 및15마리 연속 처치 확인. 마지막 Play 이후 콘솔 오류0/경고6. 앞선 실패/예외 로그는 삭제하지 않고 결과 파일로 보존했다.

## 실제 캡처

- [시설7종 시각 배치](../work/first-defense-1112/screens/facilities-1440.png)
- [수정한 폭풍 입자](../work/first-defense-1112/screens/storm-1440.png)
- [최종 연속 교전](../work/first-defense-1112/screens/combat-order-60.png)
- [기존 전투 화면을 가린 강수](../work/first-defense-1112/screens/baseline-fight-20.png)

## 남은 품질 문제와 인계

실제 반복 피격 중 장수 크기가 누적 확대되는 별도 문제를 발견해 애니메이션 담당에게 캡처와 함께 전달했다. 담당은 Juice.Punch/Pop의 중첩 Scale 코루틴을 원인으로 확인했으며 수정/회귀를 맡았다. 해당 수정은 이 검수 결과에 포함하지 않는다. 밀집 교전에서 장수 몸체/이름 겹침, 작은 화면 HUD 글자 크기, 시설 전종 통일도 출시 완료로 판정하지 않는다.

화면 점수(이번 시설/강수/첫 방어 표면):

palette: 3/5 — 기존 목재·돌·흙 팔레트 유지, 강수 백색 덮임 해소.
style: 3/5 —7종 작업 시설의 재료와 형태 통일, 전종 교체 아님.
lighting: 3/5 — 기존 지면 그림자/날씨 조명 유지.
silhouette: 2/5 — 군집 교전 겹침과 피격 누적 확대 결함은 별도 담당 후속 검증 필요.
UI: 3/5 — 방어 중 직접 명령 버튼과 상황 안내, 작업 문구 간섭 감소.
feedback: 3/5 — 실제 공격 이동/연속 처치/피해/남은 적 수/방어 종료 확인.

최종 Unity는11-12 00:55 이후 마지막 회귀를 거쳐 Edit/ready로 애니메이션 담당에게 반환했다. 실행 중 Unity 작업 없음. 통합 SAVE/Notion/종료는 사용자 지시를 받은 해당 세션이 수행한다. 이 세션에서는 중복 실행하지 않는다.

참고: ant-workflow, unity-cli/unity-pipeline, craft-game-floors의 game-feel/vfx/visual-scorecard. 강수는 Unity ParticleSystemRenderer [velocityScale](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/particlesystemrenderer/velocityscale), [lengthScale](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/particlesystemrenderer/lengthscale) API를 확인했다.

Graphify 마지막 증분 갱신 종료0:46,910노드/123,410관계/3,296커뮤니티. 로그 work/first-defense-1112/graphify-final.log. 실행 중 Unity/검증/Graphify 작업 없음.
