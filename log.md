# 프로젝트 로그

## 프로젝트 구조와 규칙
- 개미 소굴 RTS / Unity 6000.5.8f1 / URP 17.5.0 / Pipeline 0.7.0-exp.1.
- E:\Git\ant. develop에서 개발, master는 안정 버전.
- Assets/Scripts/: Core/Data/Units/Buildings/World/Boss/UI/Map. 씬 Assets/Scenes/AntColony.unity, 검사 AgentScripts/.
- .prefab/.prefab.meta와 Assets/_TeamImport/ 커밋 금지. 공식 Unity CLI/Pipeline 우선. 일지·캡처는 SAVE 때만.

## 2026-09-15 SAVE — 장수 획득 UI·건설 연결 완료
- 양육실·스카우트 파견소·포로 수용소의 BuildingData, 비활성 씬 템플릿, 건설 버튼과 비용·인력 예약을 연결했다.
- CommanderAcquisitionPanel: 양육실 현황, 파견·진행 상태, 다중 파견소·수용소 전환, 포로 선택·회유·처형·결과 표시.
- 다중 수용소는 빈자리 있는 건물을 선택한다. 양육실은 한 곳만 번식한다. 파견소 비활성화·파괴 시 차출 인력을 한 번만 반환한다.
- CommanderAcquisitionBootstrapper와 AcquisitionSetupChecks로 에셋·템플릿·적 장수 침공 연결을 설정·검사한다.
- FindActive<T>의 제약을 Behaviour로 고쳐 CS1061 컴파일 오류를 해결했다.
- 검사 코드의 공개 회유 메서드 호출을 수정하고, 성장 UI 검사는 고정 100ms 대신 실제 표시를 최대 5초 기다리게 했다.
- 간단한 후속 작업: 비활성 컴포넌트의 수용소·파견소가 HUD에서 제외되고 처형·차출되지 않는 검사 4개 추가.

## 검증
- Unity 재컴파일 오류 0건.
- AcquisitionSetupChecks 30개, AcquisitionBuildingChecks 42개, CommanderAcquisitionChecks 59개 PASS.
- RegressionChecks, CommanderChecks(29개), CommanderProgressionChecks(27개), InvasionChecks, EnemyColonyEconomyChecks 모두 PASS.
- CLI 기본 30초 응답 제한으로 회귀 결과 수신이 한 번 실패해 --timeout 90으로 재실행했다.
- 성장 UI 검사 최초 실패는 고정 대기 방식 보완 후 통과했다. 일부 기존 Unity 검색 API obsolete 경고는 남아 있다.
- 검사 후 Play 종료. Graphify 갱신, README·changelog 정리 완료. Notion 개발 일지와 캡처 첨부 완료.

## 다음 작업과 미정 사항
- 장수 획득 경로와 건설·HUD 연결은 구현·검증 완료. 다음 기능 후보는 숙련도 성장, 연구소 개별 강화, 액티브 스킬이며 범위를 먼저 정한다.
- 플레이어 장수 사망·포로 규칙, 다수 AI 세력, Special 소비처, 보스 전리품, 유지비 정책은 미정.
- 현재 번식 거리는 양육실과 장수 사이가 아니라 장수 쌍 사이 거리다. 수치와 외형은 프로토타입.
- 기획 부모 334c4a0ecd3180c4a796e5220302a0bd에 replace_content+allow_deleting_content 금지.
- 카카오톡 도구가 없어 완료 알림 발송 불가.

## SAVE 결과
- 개발 일지: https://app.notion.com/p/3dcc4a0ecd318194b460d137e2b6b2fa
- 캡처: Assets/.unity/save-2026-09-15-acquisition-hud.png (카메라 화면, Overlay HUD 미포함).
- 이미지 업로드 MIME 불일치는 image/png 명시 후 해결했다.
- SAVE는 검증한 작업을 develop에 커밋하고 origin/develop에 push하는 것까지 포함한다. AGENTS.md에 명시했다. master 반영은 별도 요청 시 진행한다.
