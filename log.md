# 프로젝트 로그

## 현재 상태 — 2026-10-05 SAVE
- 프로젝트: 개미 소굴 RTS, E:\Git\ant. Unity 6000.5.8f1 / URP 17.5.0 / Pipeline 0.7.0-exp.1. 일반 개발 develop, 안정 master. 소스 Assets/_Project/Scripts, 검사 AgentScripts, 그래프 graphify-out.
- 검사 정리 완료: 실패 27개 분류 → 검사 기대값을 현재 규칙에 맞춤(작업 13종, Phase4 병역 상한, 농장 인력, 간호사 없는 치료 x0.5, HUD v4 이름, Workforce 요청 방식, 바이옴 배율, 패배=장수 0명 등). 전체 실행 .unity/checks-2026-10-04b: 구식 3개 제외 63/63 통과. Stage6·AnnexedSettlement·Corpse는 간헐 실패(개별 재실행 통과).
- 게임 버그 수정: WorkerAnt 반납 시 채집 합산 오차(10→9.9999)로 1이 바닥 더미로 떨어짐 → +0.001 여유. CommanderAnt 운반 한도를 정수 내림(소수 한도면 왕복마다 자투리 손실).
- HUD 명령 카드 3×3(사용자 승인안): 평시 우선·휴식·징집소·건설·작업표·상세·무기·연구·정지 / 출전 +상세·무기 / 둥지 +연구·인구·외교. 평시 버튼 3글자 이하 규칙 유지.
- 바닥·계절: Shaders/SeasonFoliage.shader(+Resources/SeasonFoliage.mat) — 잎만 계절색(봄 연두·가을 주황~빨강·겨울 갈색+윗면 눈), 전역 _SeasonSpring/_SeasonAutumn/_SeasonWinter(SeasonVisuals). MapGenerator.Foliage 목록 제거. TerrainBlend: 3샘플 반복 완화, 색조는 풀에만 온전히(돌·흙 40%), 눈 텍스처 흰색화·한겨울 전면 덮임. 정원 층에서 Rocky_Dirt_2 제거(SetupBiomeStyles 재실행).
- 한계: Fantasy Village 침엽수는 팔레트 공용 재질이라 소나무 구분 불가 → 가을에 같이 물듦. 씬 직접 배치 나무는 없음(맵 나무는 전부 생성 장식).
- 미완료/결정 대기: Player 빌드 미실행. 구식 Invasion/SceneInvasion/Raid 검사(09-18 원정 구조 전 홈 소굴 전제) 재작성/삭제 결정 대기. 원정지 수동 채집 1회 왕복 후 멈춤 → 계속 채집으로 바꿀지 결정 대기. 바이옴 이벤트·다거점 수송 경유 미구현(BiomeEventChecks·MultiStopRouteChecks 초안 로컬).
- 다음 세션: 위 두 결정 반영 → Player 빌드 → 바이옴 이벤트·다거점 경유 구현. 검사 실행은 .unity/checks-2026-10-04b/runall.sh(실행 중 Assets 수정·에디터 재시작 금지).
- 캡처 도구: AgentScripts/FloorShot.cs(바이옴·계절), HudFrameShot.cs. 결과 .unity/floor/.
