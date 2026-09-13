# 프로젝트 로그

## 프로젝트 구조와 규칙
- 개미 소굴 RTS / Unity 6000.5.8f1 / URP 17.5.0 / Pipeline 0.7.0-exp.1.
- E:\Git\ant, origin github.com/ARHIENE/ant-colony-rts. develop에서 개발, master는 안정 버전.
- Assets/Scripts/: Core/Data/Units/Buildings/World/Boss/UI/Map. 씬 Assets/Scenes/AntColony.unity, 설정·검사 AgentScripts/.
- .prefab/.prefab.meta와 Assets/_TeamImport/ 커밋 금지. Ponytail full, 공식 Unity CLI/Pipeline 우선. 개발 일지·캡처는 SAVE 때만. 컴퓨터 종료 요청 없음.

## 2026-09-13 SAVE — 사용자 점검 완료
- 사용자 요청: “점검 완료했어 save해”. 이번 SAVE에서 게임 코드 추가 수정 없음.
- 장수 중심 1차 프로토타입 구현: AntPool 일반개미 수량 풀, CommanderRoster 생성, CommanderAnt 배정·회수·보직·관직·병력 체력, CommanderProgression 전투 성장.
- 여왕방 일반개미 생산·글로벌 낚시, 병영 연구, 현존 부대 연구 강화, 총수 유지비, 건설 인력 예약·완공·취소 복귀를 씬·UI에 연결.
- 장수만 선택·명령. 채집·농사·낚시·약탈·침공 수비 연결. 초기 장수 12명/일반개미 40마리는 임시값.
- 처치 경험치·최대 20레벨·공격/방어 보너스·UI 표시. 보직·관직·병력 변경 시 성장 유지. 성장 수치 임시.
- README의 기존 장수/경계조건/성장/회귀/낚시/약탈/침공 검사 통과 기록을 보존. 이번 SAVE에서는 전체 검사 재실행 없이 사용자 점검 완료를 기록.
- 직접 확인: Unity ready, 컴파일 실패 없음, 현재 에디터 콘솔 오류 0건. Pipeline 버퍼에는 SAVE 이전 21:19 KST의 메인 스레드 타임아웃 2건이 남아 있음.
- 공식 CLI capture_game_view로 HUD 포함 화면 캡처 후 Play 종료. Assets/.unity/save-2026-09-13.png (1,215,454 bytes).
- Notion 일지 및 이미지 첨부·재조회 완료: https://app.notion.com/p/3dac4a0ecd31815b8237ecd07d63e788 (2026-09-13, 🙂).
- 최초 업로드 MIME 불일치는 image/png 지정으로 해결. 현재 카카오톡 도구가 없어 완료 알림 전송 불가.

## 다음 작업과 미정 사항
- 번식·영입·포로, Support 전용 버프, 적 AI 경제 성장·랜덤 배치, Special 소비처·보스 전리품은 후속 작업.
- 병력 0 장수는 선택·보충 대상으로 남음. 사망·포로 규칙과 건설 예약만 남았을 때 유지비 정책은 미정/임시.
- 장수는 한 짐 반납 후 대기. 운반·건설 중 병력 배정·회수 및 보직 변경 제한. 상세 조작법은 README 참고.
- 기획 부모 334c4a0ecd3180c4a796e5220302a0bd, 장수 3d8c4a0ecd318155a477fda677e21fd1. 이전 로그의 구기획 참조 정리 완료 여부는 다음 개발 재개 시 확인.
- 기획 부모에 replace_content+allow_deleting_content 금지. 과거 개발일지를 현재 구현 사실로 바꿔쓰지 말 것.

## Git 저장 결과
- develop 구현 커밋 985300b를 사용자 명시적 승인 후 origin/develop에 push 완료. 최초 자동 승인 검토 차단은 사용자 승인으로 해소.
- 개인 도구 설정(.claude/settings.json, .codex/), .gitattributes, graphify-out/, Assets/_Recovery/와 메타 파일은 로컬에 보존.
- Git 저장 결과 메모도 별도 문서 커밋으로 보존.
