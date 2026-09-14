# 프로젝트 로그

## 프로젝트 구조와 규칙
- 개미 소굴 RTS / Unity 6000.5.8f1 / URP 17.5.0 / Pipeline 0.7.0-exp.1.
- E:\Git\ant, origin github.com/ARHIENE/ant-colony-rts. develop에서 개발, master는 안정 버전.
- Assets/Scripts/: Core/Data/Units/Buildings/World/Boss/UI/Map. 씬 Assets/Scenes/AntColony.unity, 설정·검사 AgentScripts/.
- .prefab/.prefab.meta와 Assets/_TeamImport/ 커밋 금지. Ponytail full, 공식 Unity CLI/Pipeline 우선. 개발 일지·캡처는 SAVE 때만.

## 2026-09-14 SAVE — 적 소굴 랜덤 배치 완료
- 게임 시작 시 단일 적 소굴 전체를 플레이어 본진에서 30~60m 떨어진 임의 위치로 옮긴다.
- 최대 24개 후보 중 NavMesh에 있고 본진까지 완전한 경로가 있는 위치만 사용하며, 후보가 없으면 기존 위치를 유지한다.
- 소굴 루트를 통째로 이동해 건물·Food/Soil/Special 전리품·침공 생성 지점의 상대 배치를 보존했다. 별도 프리팹·씬 변경은 없다.
- AI 경제 성장 구현을 이어가던 중 SAVE 요청이 들어와 랜덤 배치까지만 완결하고 저장했다.

## 검증
- Unity 프로젝트 컴파일 성공. `EnemyColonyPlacementChecks` 5개 통과.
- 실제 씬에서 소굴 랜덤 이동과 침공대의 본진 도달·공격, 기존 `InvasionChecks`, 핵심 `RegressionChecks` 전체 통과.
- 첫 Scene View 정렬 eval은 시간 초과됐지만 임시 `run_script`로 게임 카메라를 맞춰 공식 CLI 캡처를 완료했다. 프로젝트 코드 오류는 아니다.
- 캡처: `Assets/.unity/save-2026-09-14-enemy-colony-random-placement.png`(1,255,209 bytes). 임시 캡처 스크립트는 삭제했다.

## 다음 작업과 미정 사항
- 다음 작업은 적 AI 경제 성장이다. 현재 침공은 파동 횟수에 따라 증가하지만 적 자원·건설·생산 경제는 없다.
- 번식·영입·포로, Special 소비처·보스 전리품은 후속 작업.
- 병력 0 장수는 선택·보충 대상으로 남음. 사망·포로 규칙과 건설 예약만 남았을 때 유지비 정책은 미정/임시.
- Support 수치와 시각 효과는 밸런스·연출 단계에서 확정한다. 현재 버프는 UI 수치에 즉시 반영되지만 별도 범위 표시는 없다.
- 기획 부모 `334c4a0ecd3180c4a796e5220302a0bd`에는 replace_content+allow_deleting_content 조합을 사용하지 않는다.

## SAVE 결과
- 구현·검사를 `425db1c`로 커밋했다. README·log·changelog와 최종 push는 SAVE 절차에서 이어서 처리한다.
- Notion 개발 일지와 캡처 첨부·재조회 완료: https://app.notion.com/p/3dbc4a0ecd31819894c1e6789edb060a (2026-09-14, 🙂).
- 개인 도구 설정(.claude/settings.json, .codex/), .gitattributes, graphify-out/, Assets/_Recovery/와 메타 파일은 로컬에 보존한다.
- 카카오톡 완료 도구는 현재 세션에 없어 알림 전송 불가.
- SAVE 완료 후 작업을 계속한다. 사용자는 앞으로 모든 GitHub push를 승인했다.
