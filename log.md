# 프로젝트 로그

## 프로젝트 구조와 규칙
- 개미 소굴 RTS / Unity 6000.5.8f1 / URP 17.5.0 / Pipeline 0.7.0-exp.1.
- E:\Git\ant, origin github.com/ARHIENE/ant-colony-rts. develop에서 개발, master는 안정 버전.
- Assets/Scripts/: Core/Data/Units/Buildings/World/Boss/UI/Map. 씬 Assets/Scenes/AntColony.unity, 설정·검사 AgentScripts/.
- .prefab/.prefab.meta와 Assets/_TeamImport/ 커밋 금지. Ponytail full, 공식 Unity CLI/Pipeline 우선. 개발 일지·캡처는 SAVE 때만.

## 2026-09-14 SAVE — Support 전용 버프 완료
- 이전 작업 스크립트와 diff에서 중단 지점이 Support 전용 오라 구현임을 확인했다.
- 병력이 있는 Support 장수는 반경 6m 안의 다른 장수에게 중첩 없이 일반개미 1마리당 공격력 +1, 부대 방어력 +1을 제공한다. 병력 0 또는 범위 밖에서는 즉시 해제된다.
- 수치는 1차 프로토타입 임시 상수다. 현재 장수 12명 규모에 맞춰 기존 활성 유닛 목록을 선형 검색하며 별도 캐시·컴포넌트는 추가하지 않았다.
- README와 Notion `유닛(개미) 시스템`의 Support 설명을 현재 구현으로 갱신했다.

## 검증
- Unity 프로젝트 컴파일 성공. `SupportChecks`: 범위·공격·방어·병력 0 해제 8개 통과.
- 기존 `CommanderChecks` 29개, `CommanderProgressionChecks` 28개, 핵심 `RegressionChecks` 전체 통과.
- 공식 Pipeline은 샌드박스 내부 로컬 포트 접속이 차단되어 첫 호출이 실패했으나, 승인된 로컬 접속으로 재실행해 정상 통과했다. 프로젝트 코드 오류는 아니다.
- 공식 CLI `capture_game_view`로 초기화된 Play 상태의 HUD 포함 화면을 `Assets/.unity/save-2026-09-14.png`에 캡처했다(1,336,917 bytes).

## 다음 작업과 미정 사항
- 번식·영입·포로, 적 AI 경제 성장·랜덤 배치, Special 소비처·보스 전리품은 후속 작업.
- 병력 0 장수는 선택·보충 대상으로 남음. 사망·포로 규칙과 건설 예약만 남았을 때 유지비 정책은 미정/임시.
- Support 수치와 시각 효과는 밸런스·연출 단계에서 확정한다. 현재 버프는 UI 수치에 즉시 반영되지만 별도 범위 표시는 없다.
- 기획 부모 `334c4a0ecd3180c4a796e5220302a0bd`에는 replace_content+allow_deleting_content 조합을 사용하지 않는다.

## SAVE 결과
- `develop` 커밋·`origin/develop` push, 2026-09-14 Notion 개발 일지와 캡처 첨부를 완료한 뒤 결과를 기록한다.
- 개인 도구 설정(.claude/settings.json, .codex/), .gitattributes, graphify-out/, Assets/_Recovery/와 메타 파일은 로컬에 보존한다.
- 카카오톡 완료 도구는 현재 세션에 없어 알림 전송 불가.
- 사용자 요청에 따라 SAVE 완료 후 컴퓨터를 종료한다.
