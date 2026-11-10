# 프로젝트 로그

## 현재 상태 — 2026-11-10 통합 SAVE
- 프로젝트 F:/UnityProjects/ant-colony-rts, develop. Unity6000.5.8f1/Hub 공식 CLI. 작업 전 GAME_DIRECTION.md·docs/IMPLEMENTATION_BOARD.md 읽기.
- Codex 애니메이션: 수집119종 중24클립을 공통50본 리그로 변환·27상태 배치. 실제 채굴/벌목/잎채집/달리기5.43m/야간수면 검증. 양손 자루 접점·손가락/엄지 보정과 바위 외곽 접근 수정. 상세 docs/ANIMATION_GAME_1110.md.
- Codex 환경: 모델10종·지면8종, 기존6지형 연결.69검사 재실행 통과.530/600크레딧 사용(마지막 표시325). 상세 docs/ENVIRONMENT_ASSETS_1110.md.
- Codex UI: 숯빛 HTML 반영, 316+23검사 통과. 실제 자원 이미지 아이콘은 미완료. docs/UI_CHARCOAL_1110.md.
- Claude: 카메라·선택·명령 표시·합성 효과음·피해 숫자·대형 개선,24검사. 음색 청취·일부 입력은 미검증. 음주 등13항목은 지시서 단계. docs/GAME_FEEL_1110.md 및 docs/CLAUDE_FEATURE_IMPLEMENTATION_1110.md.
- 미완료: 전 무기/외형 조합의 교차 검수, Celebrate 실제 이벤트, Dropped Stone 가림, 기존 missing prefab,6지형 새게임 전수 검사, 웨이브/로켓 후속. 미구현을 완료로 취급하지 않는다.
- 공식 컴파일 오류0. Graphify45370노드/117996간선 갱신 후 SAVE 보조 스크립트 추가. 사용자 저장 슬롯 미사용. 임시 Play 종료 상태.
- 개발 일지 https://app.notion.com/p/414c4a0ecd3181e8b625dff8183a2df1 / 문제 해결 https://app.notion.com/p/414c4a0ecd318156b06fe5e29a770738. 모션GIF4개·실제채굴2프레임GIF와 에셋/UI 사진6개 업로드 완료.
- 통합 저장 묶음: work/save-20261110/README.md와 manifest.json 참조. 대용량 원본은 gzip/base64 조각으로 보관하며 복원 시 SHA256을 검증한다. 최종 원격 커밋 결과는 위 개발 일지에 기록한다. master와 prefab은 커밋 제외. 컴퓨터 종료는 저장 성공 후 실행하며 이 기록에서 성공을 선행 선언하지 않는다.
